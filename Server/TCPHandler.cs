using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WebApplication;

public class TCPHandler
{
    public async void StartClient()
    {
        var client = new TcpClient();
        await client.ConnectAsync("localhost", 5001);

        var sw = new StreamWriter(client.GetStream());
        sw.Write(Program.myLocation);
        sw.Flush();
        sw.Dispose();
        client.Dispose();
    }
}
