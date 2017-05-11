using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Collections.Generic;
using Newtonsoft.Json;

public sealed class RelayModel
{
    private static volatile RelayModel instance;
    private static object syncRoot = new Object();
    private static object clientLock = new Object();

    private RelayModel()
    {
        clients = new List<SocketHandler>();
    }

    public static RelayModel Instance
    {
        get
        {
            if(instance == null)
            {
                lock (syncRoot)
                {
                    if(instance == null)
                        instance = new RelayModel();
                }
            }
            return instance;
        }
    }

    private List<SocketHandler> clients;

    public void AddClient(SocketHandler socket)
    {
        Console.WriteLine("Adding client");
        lock(clientLock)
        {
            clients.Add(socket);
        }
    }

    public void RemoveClient(SocketHandler socket)
    {
        Console.WriteLine("Removing client");
        lock(clientLock)
        {
            clients.Remove(socket);
        }
    }

    public async Task DumpHistory(SocketHandler socket)
    {
        await socket.ServerSend("{ \"Room\" : \"\" , \"Username\" : \"\" , \"Message\" : \"\" , \"History\" : true }");
    }

    public async Task PropogateMessage(string json)
    {
        Console.WriteLine("Received message");
        foreach(var s in clients)
            await s.ServerSend(json);
    }
}
