using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Server.Startup1))]

namespace Server
{
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {

            app.MapSignalR();
        }
    }
}
