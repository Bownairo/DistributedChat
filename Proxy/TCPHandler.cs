using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ComObject
{
    public bool New;
    public string Address;
    public string Websocket;
}

public class TCPHandler
{
    static List<string> ServerList;

    public async void StartComServer()
    {
        var listener = new TcpListener(IPAddress.Any, 5001);
        listener.Start();
        for(;;) {
            var client = await listener.AcceptTcpClientAsync();
            var sr = new StreamReader(client.GetStream());
            var sw = new StreamWriter(client.GetStream());
            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            var data = JsonConvert.DeserializeObject<ComObject>(sr.ReadLine(), settings);

            if (data.New)
            {
                if(ServerList == null)
                    ServerList = new List<string>();
                ProxyModel.Instance.AddServer(data.Websocket);

                var list = JsonConvert.SerializeObject(ServerList);
                sw.WriteLine(list);

                ServerList.Add(data.Address);
                sw.Flush();
            }
            else
            	ProxyModel.Instance.UserLeftServer(data.Websocket);

            sw.Dispose();
            sr.Dispose();
            client.Dispose();
        }
    }
}
