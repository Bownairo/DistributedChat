using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class TCPHandler
{
    public async void startServer()
    {
        var listener = new TcpListener(IPAddress.Any, 5001);
        listener.Start();
        for(;;) {
            var client = await listener.AcceptTcpClientAsync();

            var sw = new StreamWriter(client.GetStream());
            sw.Write("Initial TCP");
            sw.Flush();
            sw.Dispose();
        }
    }
}
