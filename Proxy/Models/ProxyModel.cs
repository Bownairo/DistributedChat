using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Collections.Generic;
using Newtonsoft.Json;

public class Server
{
    private string location;
    private int numUsers;

    public Server (string location)
    {
        this.location = location;
        numUsers = 0;
    }
}

public sealed class ProxyModel
{
    private static volatile ProxyModel instance;
    private static object syncRoot = new Object();
    private static object serverLock = new Object();

    private ProxyModel()
    {
        servers = new List<Server>();
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

    private List<Server> servers;

    public void AddServer(string location)
    {
        Console.WriteLine("Adding server");
        lock(serverLock)
        {
            servers.Add(new Server(location));
        }
    }

    public void UpdateServer(string server, int numUsers)
    {
        //Update numUsers and lookup server method.
    }

    public string SelectServer()
    {
        return "ahhh";
    }
}
