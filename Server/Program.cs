using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;

namespace WebApplication
{
    public class Program
    {

        static string myWebsocket = "ws://localhost:5000/ws";
        static string myAddress;

        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
            	.AddCommandLine(args)
            	.Build();
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseConfiguration(config)
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            var location = config.GetSection("server.urls").Value;
            if(location != null)
                myWebsocket = location.Replace("http", "ws") + "/ws";

            myAddress = config.GetSection("myAddress").Value;
            if(myAddress == null)
            {
                Console.WriteLine("Please specify address for TCP");
                Environment.Exit(-1);
            }

            var initTCP = new TCPHandler();
            initTCP.Init(myWebsocket, myAddress);
            initTCP.StartCom();
            host.Run();
        }
    }
}
