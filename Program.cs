using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WMSWebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging => //<---- add in the window event log here
                {
                    logging.AddEventLog(eventLogSettings =>
                    {
                        eventLogSettings.SourceName = "WMSWebApi";
                        eventLogSettings.MachineName = System.Environment.MachineName;
                        eventLogSettings.LogName = "DotNetCoreRuntime";
                    });
                })            
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
