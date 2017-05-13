using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Net.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

public class DataObject
{
    public string Room;
    public string Username;
    public string Message;
}

public class WebSocketHandler
{
    public const int BufferSize = 4096;

    public WebSocket socket;

    public WebSocketHandler(WebSocket socket)
    {
        this.socket = socket;
    }

    private async Task ServerReceive()
    {
        var buffer = new byte[BufferSize];
        var seg = new ArraySegment<byte>(buffer);

        while(true)
        {
            if (socket.State != WebSocketState.Open)
                break;

            try
            {
                var result = await socket.ReceiveAsync(seg, CancellationToken.None);

                if(result.MessageType == WebSocketMessageType.Close)
                {
                    RelayModel.Instance.RemoveClient(this);
                    break;
                }

                var raw = System.Text.Encoding.ASCII.GetString(seg.Array.Take(result.Count).ToArray());
                var data = JsonConvert.DeserializeObject<DataObject>(raw);
                var relayTCP = new TCPHandler();
                relayTCP.Relay(raw);
                await RelayModel.Instance.PropogateMessage(raw);

                Console.WriteLine(raw);
            }
            catch
            {
                System.Console.WriteLine("Error");
            }
        }
    }

    public async Task ServerSend(string message)
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
            var h = new WebSocketHandler(socket);
            RelayModel.Instance.AddClient(h);
            await h.ServerReceive();
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
