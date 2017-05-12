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

        public static string myLocation = "http://localhost:5000";

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
            if(location != "")
                myLocation = location;
            host.Run();
        }
    }
}
