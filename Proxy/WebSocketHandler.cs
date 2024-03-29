using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Net.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

public class Direction //use this to send a direction to a user.
{
    public string Server;
}

public class WebSocketHandler
{
    public const int BufferSize = 4096;

    public WebSocket socket;

    public WebSocketHandler(WebSocket socket)
    {
        this.socket = socket;
    }

    private async Task ProxyReceive()
    {
        var buffer = new byte[BufferSize];
        var seg = new ArraySegment<byte>(buffer);

        while(true)
        {
            if (socket.State != WebSocketState.Open)
                break;	//Socket no longer open, should only get here from error

            try
            {
                var result = await socket.ReceiveAsync(seg, CancellationToken.None);

                if(result.MessageType == WebSocketMessageType.Close)
                {
                    //Socket closed
                    break;
                }

                var raw = System.Text.Encoding.ASCII.GetString(seg.Array.Take(result.Count).ToArray());

                //probably encrypt here too
                var server = ProxyModel.Instance.SelectServer();
                await ProxyDirect(server);
                return;
            }
            catch
            {
                System.Console.WriteLine("Error"); //Socket recieving failed
            }
        }
    }

    public async Task ProxyDirect(string server) //Use this only to direct a user towards a server
    {
        if(socket.State != WebSocketState.Open)
            return;

        var direction = new Direction();
        direction.Server = server;
        var message = JsonConvert.SerializeObject(direction);

        var byteWord = System.Text.Encoding.ASCII.GetBytes(message);
        var sending = new ArraySegment<byte>(byteWord, 0, byteWord.Length);

        try
        {
            await socket.SendAsync(sending, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch
        {
            System.Console.WriteLine("Error");
        }
    }

    private static async Task Acceptor(HttpContext hc, Func<Task> n)
    {
        if (!hc.WebSockets.IsWebSocketRequest)
            return;

        try
        {
            var socket = await hc.WebSockets.AcceptWebSocketAsync();
            var h = new WebSocketHandler(socket);
            await h.ProxyReceive();
        }
        catch
        {
            System.Console.WriteLine("Error");
        }
    }

    public static void Map(IApplicationBuilder app)
    {
        app.UseWebSockets();
        app.Use(WebSocketHandler.Acceptor);
    }
}
