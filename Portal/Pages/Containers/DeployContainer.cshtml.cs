using Docker.DotNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Portal.DeploymentService.Interface;
using Portal.Models;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Namespace
{
    public class ImageConfig
    {
        public string Name { get; set; }
        public string? Port { get; set; }
    }

    public class DeployContainerModel : PageModel
    {
        private readonly IDeploymentService _deploymentService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DeployContainerModel> _logger;
        private DockerClient _dockerClient;



        [BindProperty]
        [Required(ErrorMessage = "Image Name is required.")]
        public string ImageName { get; set; } = string.Empty;
        public int ImagePort { get; set; } = -1;


        public Dictionary<string, ImageConfig> Images { get; set; }
        public IEnumerable<string> ImageNames { get; set; }
        public DeployContainerModel(IDeploymentService deploymentService, IConfiguration configuration, ILogger<DeployContainerModel> logger)
        {
            _deploymentService = deploymentService;
            _configuration = configuration;
            _logger = logger;
            _dockerClient = _deploymentService.ConnectToDocker();

            // Load the image names list from the config file
            Images = _configuration.GetSection("DockerImages").Get<Dictionary<string, ImageConfig>>() ?? new Dictionary<string, ImageConfig>();
            // now set image name list 
            ImageNames = Images.Keys;
        }

        public void OnGet()
        {
            // No specific logic for GET at the moment
        }

        // DeployInstance is the function name triggered with the deploy button in the UI
        public async Task<IActionResult> OnPostDeployInstance()
        {
            _logger.LogDebug("hellllllo");
            // Check if the model is valid
            if (!ModelState.IsValid)
            {
                // If validation fails, re-render the page with the validation messages
                return Page();
            }

            _logger.LogInformation("Initiating container deployment...");

            try
            {
                await DeployContainerAsync();
                return RedirectToPage("/Index"); // Use RedirectToPage for consistency
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during container deployment.");
                // Add error to ModelState to display on the page
                ModelState.AddModelError(string.Empty, "An error occurred while deploying the container. Please try again.");
                return Page();
            }
        }

        // Main function to run the docker commands
        private async Task DeployContainerAsync()
        {
            _logger.LogInformation("Deploying container with image: {ImageName}", ImageName);
            // Validate ImageName input
            if (string.IsNullOrEmpty(ImageName) || !Images.ContainsKey(ImageName))
            {
                throw new ArgumentException("Invalid image name provided.");
            }

            // Map the image key to the value of the image key 
            string mappedImage = Images[ImageName].Name;
            string? port = Images[ImageName].Port;

            _logger.LogInformation("Deploying container with image: {Image}", mappedImage);
            _logger.LogInformation("Deploying container with port: {Image}", port);


            // Create the container
            string createdContainerId = await _deploymentService.CreateContainer(_dockerClient, ImageName,  User.FindFirst("uid")?.Value ,port ?? "");

            // Store the created container id in the session
            try
            {
                HttpContext.Session.SetString("CreatedContainerId", createdContainerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing container ID in session.");
                throw new Exception("Unable to store container ID in session.");
            }

            // Start the container
            // await _deploymentService.RunContainer(_dockerClient, createdContainerId);
        }
    }
}