using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
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
    static List<StreamWriter> others;

    public void Init(string websocket, string address)
    {
        myWebsocket = websocket;
        myAddress = address;
        others = new List<StreamWriter>();
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
                Console.WriteLine(s);
                var add = new TcpClient();
                await add.ConnectAsync(s.Split(':')[0], int.Parse(s.Split(':')[1])); //Other server

				var temp = new StreamWriter(add.GetStream());

				var InternalPackage = new InternalComObject();
				InternalPackage.Add = true;

				temp.WriteLine(JsonConvert.SerializeObject(InternalPackage, settings));
				temp.Flush();
                others.Add(temp);
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

    public async void ListenForOthers() //Listen for add requests and propogated messages
    {
        Console.WriteLine("Listening for others");
        var listener = new TcpListener(IPAddress.Any, int.Parse(myAddress.Split(':')[1]));
        listener.Start();
        for(;;) {
            var client = await listener.AcceptTcpClientAsync();
            //add a check to make sure it's not a closing doober or maybe not
            var sr = new StreamReader(client.GetStream());
            var message = JsonConvert.DeserializeObject<InternalComObject>(sr.ReadLine());
            if(message.Add)
            {
                others.Add(new StreamWriter(client.GetStream()));
            }
            else
            {
                //propogate here
            }
            sr.Dispose();
            client.Dispose();
        }
    }

    public async void UserLeft()
    {
        var package = new ComObject();
        package.Address = myAddress;
        package.Websocket = myWebsocket;
        package.New = false;

        var data = JsonConvert.SerializeObject(package);

        Console.WriteLine(data);

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
