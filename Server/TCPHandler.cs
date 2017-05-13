using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Authentication;
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
	static Aes security;
	static ICryptoTransform e;
	static ICryptoTransform d;

    public void Init(string websocket, string address)
    {
        myWebsocket = websocket;
        myAddress = address;
        others = new List<string>();
		security = Aes.Create();
		e = security.CreateEncryptor(security.Key, security.IV);
		d = security.CreateDecryptor(security.Key, security.IV);
    }

    public const int BufferSize = 4096;
    public async void StartCom()
    {
        var package = new ComObject();
        package.Address = myAddress;
        package.Websocket = myWebsocket;
        package.New = true;
		var buffer = new byte[BufferSize];

		var data = JsonConvert.SerializeObject(package);

        var client = new TcpClient();
        try
        {
            await client.ConnectAsync("localhost", 6000); //Proxy

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
				Console.WriteLine(s);


				//var temp = new StreamWriter(add.GetStream());
				var temp = new StreamWriter(new CryptoStream(add.GetStream(), e, CryptoStreamMode.Write));


				var InternalPackage = new InternalComObject();
				InternalPackage.Add = true;
				InternalPackage.Body = myAddress;

				// security stuff

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
            //var sr = new StreamReader(client.GetStream());
			var sr = new StreamReader(new CryptoStream(client.GetStream(), d, CryptoStreamMode.Read));

			// security stuff
            var message = JsonConvert.DeserializeObject<InternalComObject>(sr.ReadLine());
			Console.WriteLine(message);
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

		var buffer = new byte[BufferSize];
        var data = JsonConvert.SerializeObject(package);

        foreach(var other in others)
        {
            var client = new TcpClient();
            try
            {
                await client.ConnectAsync(other.Split(':')[0], int.Parse(other.Split(':')[1]));
                //var sw = new StreamWriter(client.GetStream());
				var sw =  new StreamWriter(new CryptoStream(client.GetStream(), e, CryptoStreamMode.Write));

				// security stuff
				//var byteWord = System.Text.Encoding.ASCII.GetBytes(data);
				//e.TransformBlock(byteWord, 0, byteWord.Length, buffer, 0);
				//var e_data = System.Text.Encoding.ASCII.GetString(buffer);

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
			//var sw = new StreamWriter(new CryptoStream(client.GetStream(), e, CryptoStreamMode.Write));
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
