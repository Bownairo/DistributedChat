using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

using WebApplication;

public class ComObject
{
    public bool New;
    public string Address;
    public string Websocket;
}

public class InternalComObject
{
    public bool Add;
    public string Body;
}

public class TCPHandler
{
    static string myAddress;
    static string myWebsocket;
    static List<string> others;

    public void Init(string websocket, string address)
    {
        myWebsocket = websocket;
        myAddress = address;
        others = new List<string>();
    }

    public async void StartCom()
    {
        var package = new ComObject();
        package.Address = myAddress;
        package.Websocket = myWebsocket;
        package.New = true;

		var data = JsonConvert.SerializeObject(package);

        var client = new TcpClient();
        try
        {
            await client.ConnectAsync("localhost", 5001); //Proxy

            var sw = new StreamWriter(client.GetStream());
            var sr = new StreamReader(client.GetStream());

            sw.WriteLine(data);
            sw.Flush();

            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            var connectTo = JsonConvert.DeserializeObject<List<string>>(sr.ReadLine(), settings);

            foreach(var s in connectTo)
            {
                var add = new TcpClient();
                await add.ConnectAsync(s.Split(':')[0], int.Parse(s.Split(':')[1])); //Other server


				var secure = new SslStream(add.GetStream());
				secure.AuthenticateAsServer(new X509Certificate());
				var temp = new StreamWriter(secure);
				var tempRead = new StreamReader(secure);

				var InternalPackage = new InternalComObject();
				InternalPackage.Add = true;
				InternalPackage.Body = myAddress;

				temp.WriteLine(JsonConvert.SerializeObject(InternalPackage, settings));
				temp.Flush();
                others.Add(s);
            }

            sr.Dispose();
            sw.Dispose();
            client.Dispose();
            ListenForOthers();
        }
        catch
        {
            Console.WriteLine("Can't see server");
            Environment.Exit(-1);
        }
    }

    public async void ListenForOthers()
    {
        Console.WriteLine("Listening for others");
        var listener = new TcpListener(IPAddress.Any, int.Parse(myAddress.Split(':')[1]));
        listener.Start();
        for(;;) {
            var client = await listener.AcceptTcpClientAsync();
            //add a check to make sure it's not a closing doober or maybe not
			var secure = new SslStream(client.GetStream());
			secure.AuthenticateAsClient("localhost");
            var sr = new StreamReader(secure);
            var message = JsonConvert.DeserializeObject<InternalComObject>(sr.ReadLine());
            if(message.Add)
            {
                Console.WriteLine("Added server");
                others.Add(message.Body);
            }
            else
            {
                await RelayModel.Instance.PropogateMessage(message.Body);
            }

            sr.Dispose();
        }
    }

    public async void Relay(string message)
    {
        var package = new InternalComObject();
        package.Add = false;
        package.Body = message;

        var data = JsonConvert.SerializeObject(package);

        foreach(var other in others)
        {
            var client = new TcpClient();
            try
            {
                await client.ConnectAsync(other.Split(':')[0], int.Parse(other.Split(':')[1]));
				var secure = new SslStream(client.GetStream());
				secure.AuthenticateAsServer(new X509Certificate());
                var sw = new StreamWriter(secure);
                sw.Write(data);
                sw.Flush();
                sw.Dispose();
                client.Dispose();
            }
            catch
            {
                Console.WriteLine("Can't connect to other servers");
            }
        }
    }

    public async void UserLeft()
    {
        var package = new ComObject();
        package.Address = myAddress;
        package.Websocket = myWebsocket;
        package.New = false;

        var data = JsonConvert.SerializeObject(package);

        var client = new TcpClient();
        try
        {
            await client.ConnectAsync("localhost", 5001); //Proxy

            var sw = new StreamWriter(client.GetStream());
            sw.Write(data);
            sw.Flush();
            sw.Dispose();
            client.Dispose();
        }
        catch
        {
            Console.WriteLine("Can't see server");
            Environment.Exit(-1);
        }
    }
}
