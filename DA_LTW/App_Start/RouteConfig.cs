using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace DA_LTW
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "HomeCustomer", action = "Index", id = UrlParameter.Optional }
                //defaults: new { controller = "HomeAdmin", action = "Index", id = UrlParameter.Optional }

            );
        }
    }
}
