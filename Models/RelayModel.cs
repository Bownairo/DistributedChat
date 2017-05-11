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
    private static object fileLock = new Object();

    private RelayModel()
    {
        clients = new List<SocketHandler>();
        File.CreateText("history.txt");
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
        lock(fileLock)
        {
            using (StreamReader reader = File.OpenText("history.txt"))
            {
                string line;
                while((line = reader.ReadLine()) != null)
                {
                    //Can't await because in a lock =(
                    socket.ServerSend(line);
                }
            }
        }
        await socket.ServerSend("{ \"Room\" : \"\" , \"Username\" : \"\" , \"Message\" : \"\" , \"History\" : true }");
    }

    private void Log(string json)
    {
        lock(fileLock)
        {
            using (StreamWriter writer = File.AppendText("history.txt"))
            {
                writer.WriteLine(json);
            }
        }
    }

    public async Task PropogateMessage(string json)
    {
        Console.WriteLine("Received message");
        Log(json);
        foreach(var s in clients)
            await s.ServerSend(json);
    }
}
