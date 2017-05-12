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
        clients = new List<WebSocketHandler>();
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

    private List<WebSocketHandler> clients;

    public void AddClient(WebSocketHandler socket)
    {
        Console.WriteLine("Adding client");
        lock(clientLock)
        {
            clients.Add(socket);
        }
    }

    public void RemoveClient(WebSocketHandler socket)
    {
        Console.WriteLine("Removing client");
        var updateTCP = new TCPHandler();
        updateTCP.UserLeft();
        lock(clientLock)
        {
            clients.Remove(socket);
        }
    }

    public async Task PropogateMessage(string json)
    {
        Console.WriteLine("Received message");
        foreach(var s in clients)
            await s.ServerSend(json);
    }
}
