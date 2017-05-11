using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Collections.Generic;
using Newtonsoft.Json;

public sealed class ProxyModel
{
    private static volatile ProxyModel instance;
    private static object syncRoot = new Object();
    private static object serverLock = new Object();

    private ProxyModel()
    {
        servers = new List<SocketHandler>();
    }

    public static ProxyModel Instance
    {
        get
        {
            if(instance == null)
            {
                lock (syncRoot)
                {
                    if(instance == null)
                        instance = new ProxyModel();
                }
            }
            return instance;
        }
    }

    private List<SocketHandler> servers;

    public void Addserver(SocketHandler socket)
    {
        Console.WriteLine("Adding server");
        lock(serverLock)
        {
            servers.Add(socket);
        }
    }

    public void Removeserver(SocketHandler socket)
    {
        Console.WriteLine("Removing server");
        lock(serverLock)
        {
            clients.Remove(socket);
        }
    }

    public async Task PropogateMessage(string json)
    {
        Console.WriteLine("Received message");
        foreach(var s in servers)
            await s.ServerSend(json);
    }
}
