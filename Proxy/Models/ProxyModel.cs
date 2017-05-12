using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Collections.Generic;
using Newtonsoft.Json;

public class Server
{
    public string Location {get;}
    public int NumUsers {get; set;}

    public Server (string location)
    {
        Location = location;
        NumUsers = 0;
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
        Console.WriteLine("Adding server: " + location);
        lock(serverLock)
        {
            servers.Add(new Server(location));
        }
    }

    public void UserLeftServer(string loc)
    {
        Console.WriteLine("User left server");
        var server = GetByLoc(loc);
        server.NumUsers--;
        Console.WriteLine("Count: " + server.NumUsers);
    }

    public string SelectServer()
    {
        var destination = GetLeastUsers();
        destination.NumUsers++;
        return destination.Location;
    }

    public void ProvisionServer(string loc)
    {
        //Communicate to all servers that they need to let someone new in
    }

    private Server GetByLoc(string loc)
    {
        lock(serverLock)
        {
            foreach(var s in servers)
            {
                if(s.Location == loc)
                    return s;
            }
        }

        return null; //probably break harder than this.
    }

    private Server GetLeastUsers()
    {
        Server leastServer = null;
        int leastUsers = int.MaxValue;
        lock(serverLock)
        {
            foreach(var s in servers)
            {
                if(s.NumUsers < leastUsers)
                {
                    leastServer = s;
                    leastUsers = s.NumUsers;
                }
            }
        }
        return leastServer;
    }
}
