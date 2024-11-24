using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System;

namespace MyApp.Namespace
{
    public class ContainerIDEModel : PageModel
    {
        public string IPAddress { get; set; }
        public string Port { get; set; }

        public void OnGet()
        {
            // Retrieve IP Address from HTTP Context
            IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // Retrieve Port from HTTP Context or set a default value
            // For demonstration, we'll set it to a default value
            Port = HttpContext.Connection.RemotePort.ToString();
        }
    }
}