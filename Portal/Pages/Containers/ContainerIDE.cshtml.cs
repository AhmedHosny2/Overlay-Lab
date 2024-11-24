using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace MyApp.Namespace
{
    public class ContainerIDEModel : PageModel
    {
        [BindProperty]
        public string Code { get; set; }

        [BindProperty]
        public string Language { get; set; }

        public string IPAddress { get; set; }
        public string Port { get; set; }
        public string InstanceId { get; set; }

        public void OnGet()
        {
            IPAddress = HttpContext.Session.GetString("IpAddress") ?? "127.0.0.1";
            Port = HttpContext.Session.GetString("Port") ?? "8080";
            InstanceId = HttpContext.Session.GetString("InstanceId") ?? "default-instance";
        }

        public IActionResult OnPostRun()
        {
            // Here you can process the code as needed.
            // **WARNING:** Executing arbitrary code can be extremely dangerous.
            // Ensure you have proper security measures in place.

            // For demonstration, we'll just return the received code and language.
            // In a real application, you might send this to a compiler or interpreter.

            // Example response:
            return new JsonResult(new { success = true, message = "Code received successfully.", code = Code, language = Language });
        }
    }
}