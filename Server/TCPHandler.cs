using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

using WebApplication;

public class ComObject
{
    public bool New;
    public string Address;
}

public class TCPHandler
{
    public async void StartCom(string address)
    {
        var package = new ComObject();
        package.Address = address;

        var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
		var data = JsonConvert.SerializeObject(package, settings);

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
