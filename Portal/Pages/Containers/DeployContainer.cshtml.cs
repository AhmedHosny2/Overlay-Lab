using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal.DeploymentService.Interface;
using Portal.Models;

namespace MyApp.Namespace
{
    public class DeployContainerModel : PageModel
    {
        private IDeploymentService _deploymentService = new Portal.DeploymentService.Class.DeploymentService();// TODO why ?
        [BindProperty]
        public string? DeploymentName { get; set; }
        [BindProperty]
        public string? ImageName { get; set; } = "alpine";
        public void OnGet()
        {
        }

        // Main function to run the docker commands
        public async Task MainAsync()
        {
            try
            {
                var client = _deploymentService.ConnectToDocker();


                if (!string.IsNullOrEmpty(ImageName))
                {
                    await _deploymentService.CheckOrCreateImage(client, ImageName);
                }
                else
                {
                    throw new ArgumentNullException(nameof(ImageName), "ImageName cannot be null or empty.");
                }

                var CreatedContainerId = await _deploymentService.CreateContainer(client, ImageName, DeploymentName ?? "default-container");
                // store the created container id in the session
                HttpContext.Session.SetString("CreatedContainerId", CreatedContainerId);
                IList<ServerInstance> Containers = await _deploymentService.ListContainers(client);
                await _deploymentService.RunContainer(client, CreatedContainerId);
                // redirct ot the home page
                Response.Redirect("/");
                // var output = await ExecuteCommand(ConnectToDocker(),   new List<string> { "sh", "-c", "mkdir /root/test && echo 'Hello, World!' > /root/test/hello.txt && ls /root/test" });
                // Console.WriteLine(output);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);



            }
        }


        // DeployInstance is the function name with the button 
        public async Task OnPostDeployInstance()
        {
            Console.WriteLine("Deploying container");

            await MainAsync();

        }

    }
}
