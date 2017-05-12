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

public class TCPHandler
{
    static string myAddress;
    static string myWebsocket;
    static List<StreamWriter> others;

    public void Init(string websocket, string address)
    {
        myWebsocket = websocket;
        myAddress = address;
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

            var connectTo = JsonConvert.DeserializeObject<List<string>>(sr.ReadLine());

            foreach(var s in connectTo)
            {
                Console.WriteLine(s);
            }

            sr.Dispose();
            sw.Dispose();
            client.Dispose();
        }
        catch
        {
            Console.WriteLine("Can't see server");
            Environment.Exit(-1);
        }
    }

    public async void UserLeft()
    {
        var package = new ComObject();
        package.Address = myAddress;
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
