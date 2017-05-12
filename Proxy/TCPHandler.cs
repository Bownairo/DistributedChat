using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class ComObject
{
    public bool New;
    public string address;
}

public class TCPHandler
{
    public async void StartComServer()
    {
        var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        var listener = new TcpListener(IPAddress.Any, 5001);
        listener.Start();
        for(;;) {
            var client = await listener.AcceptTcpClientAsync();
            var sr = new StreamReader(client.GetStream());
            var data = JsonConvert.DeserializeObject<ComObject>(sr.ReadToEnd(), settings);

            ProxyModel.Instance.AddServer(data.address);
        }
    }
}
