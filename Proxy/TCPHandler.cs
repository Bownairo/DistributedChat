using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class TCPHandler
{
    public async void StartAddingServer()
    {
        var listener = new TcpListener(IPAddress.Any, 5001);
        listener.Start();
        for(;;) {
            var client = await listener.AcceptTcpClientAsync();

            var sr = new StreamReader(client.GetStream());
            Console.WriteLine(sr.ReadToEnd());
        }
    }
}
