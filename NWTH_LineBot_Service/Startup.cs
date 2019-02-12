using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using System.Web.Http;

[assembly: OwinStartup(typeof(NWTH_LineBot_Service.Startup))]

namespace NWTH_LineBot_Service
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Wire Web API 
            var httpConfiguration = new HttpConfiguration();
            httpConfiguration.MapHttpAttributeRoutes();

            app.UseWebApi(httpConfiguration);
        }
    }
}
