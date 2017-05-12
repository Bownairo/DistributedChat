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
        Console.WriteLine("Adding server");
        lock(serverLock)
        {
            servers.Add(new Server(location));
        }
    }

    public void UserLeftServer(string loc)
    {
        GetByLoc(loc).NumUsers--;
    }

    public string SelectServer()
    {
        return "ws://129.21.50.69:5000/ws";
        //var destination = GetLeastUsers();
        //destination.NumUsers--;
        //return destination.Location;
        //return GetLeastUsers().Location;
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
        var leastServer = null;
        var leastUsers = int.MaxValue;
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
    }
}
