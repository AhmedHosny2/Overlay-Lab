using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal.DeploymentService.Interface;
using Portal.Models;

namespace MyApp.Namespace
{
    public class DeployContainerModel : PageModel
    {
        private IDeploymentService _deploymentService;
        [BindProperty]
        public string? DeploymentName { get; set; }
        [BindProperty]
        public required string ImageName { get; set; }

        public required Dictionary<string, string> Images { get; set; }
        // get image name from the config file 
        private readonly IConfiguration _configuration;

        public DeployContainerModel(IDeploymentService deploymentService, IConfiguration configuration)
        {
            _deploymentService = deploymentService;
            _configuration = configuration;
            // load the image names list from the config file
            Images = _configuration.GetSection("DockerImages").Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();

        }
        public void OnGet()
        {

        }


        // Main function to run the docker commands
        public async Task MainAsync()
        {
            try
            {
                var client = _deploymentService.ConnectToDocker();


                //  check if image key  is null, empty or not in the list 
                if (string.IsNullOrEmpty(ImageName) || !Images.ContainsKey(ImageName))
                {
                    throw new Exception("Invalid image name");
                }
                // map the image key to the value 
                string MappedImageName = Images[ImageName];
                Console.WriteLine("Deploying container with image: " + MappedImageName);

                var CreatedContainerId = await _deploymentService.CreateContainer(client, ImageName, DeploymentName ?? "default-container");
                // store the created container id in the session
                HttpContext.Session.SetString("CreatedContainerId", CreatedContainerId);
                IList<ServerInstance> Containers = await _deploymentService.ListContainers(client);
                await _deploymentService.RunContainer(client, CreatedContainerId);
                // redirct ot the home page
                Response.Redirect("/");
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
