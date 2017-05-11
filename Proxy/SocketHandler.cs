using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

public enum Types {New, Update, Client};

public class DataObject //Use this as secure communication format
{
    public Types type;
}

public class SocketHandler
{
    public const int BufferSize = 4096;

    public WebSocket socket;

    public SocketHandler(WebSocket socket)
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
                    //Do closure stuff here
                    break;
                }

                //Decoding here
                var raw = System.Text.Encoding.ASCII.GetString(seg.Array.Take(result.Count).ToArray());
                var data = JsonConvert.DeserializeObject<DataObject>(raw);

                //If new server connection add to list of servers
                //If count update count
                //If client point at server

                Console.WriteLine(raw);
            }
            catch
            {
                System.Console.WriteLine("Error"); //Socket recieving failed
            }
        }
    }

    public async Task ProxyDirect(string message) //Use this only to direct a user towards a server
    {
        if(socket.State != WebSocketState.Open)
            return;

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
            var h = new SocketHandler(socket);
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
        app.Use(SocketHandler.Acceptor);
    }
}
