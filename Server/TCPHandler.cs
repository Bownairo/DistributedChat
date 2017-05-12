using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class TCPHandler
{
    public async void startClient()
    {
        Console.WriteLine("Started TCP client");
        var client = new TcpClient();
        await client.ConnectAsync("localhost", 5001);

        var sr = new StreamReader(client.GetStream());
        Console.WriteLine(sr.ReadToEnd());
    }
}
