using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System;

namespace MyApp.Namespace
{
    public class ContainerIDEModel : PageModel
    {
        public string IPAddress { get; set; }
        public string Port { get; set; }
        public string InstanceId { get; set; }

        public void OnGet()
        {

            IPAddress = HttpContext.Session.GetString("IpAddress");
            Port = HttpContext.Session.GetString("Port");
            InstanceId = HttpContext.Session.GetString("InstanceId");


        }
    }
}