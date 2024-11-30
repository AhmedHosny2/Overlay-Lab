using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal.Models;
using Portal.DeploymentService.Interface;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Docker.DotNet;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;


namespace Portal.Pages;
[Authorize]
public class IndexModel : PageModel
{
    private readonly IDeploymentService _deploymentService;
    private DockerClient _dockerClient;
    private readonly ILogger<IndexModel> _logger;
    public string UserName { get; set; } = string.Empty;
    private string _uid = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Instance ID is required.")]
    public string InstanceId { get; set; } = string.Empty;

    public IList<ServerInstance> Containers { get; set; } = new List<ServerInstance>();

    private readonly IConfiguration _configuration;

    public IndexModel(IDeploymentService deploymentService, ILogger<IndexModel> logger, IConfiguration configuration)
    {
        _deploymentService = deploymentService;
        _dockerClient = _deploymentService.CreateDockerClient();
        _logger = logger;
        _configuration = configuration;



    }
    public List<ExerciseConfig> Exercises = new();
    // function return list of ex 
    public List<ExerciseConfig> GetExercises()
    {
        List<ExerciseConfig> MyExercises = new();

        var exerciseConfigs = Directory.GetFiles("ExConfiguration", "*.json");

        // todo uise the 
        //  _configuration var with DI 


        foreach (var filePath in exerciseConfigs)
        {
            var fileConfig = new ConfigurationBuilder()
                .AddJsonFile(filePath)
                .Build();

            var config = new ExerciseConfig();
            fileConfig.Bind(config);
            Exercises.Add(config);

            // Log them
            _logger.LogInformation($"Exercise: {config.ExerciseTile}");
        }
        return Exercises;
    }
    // list containers
    public async Task OnGetAsync()
    {
        _uid = User.FindFirst("uid")?.Value ?? string.Empty;
        Containers = await _deploymentService.ListContainers(_dockerClient, _uid);
        UserName = User.FindFirst("name")?.Value.Split(",")[0] ?? string.Empty;
        // get all exercise configurations
        Exercises = GetExercises();



        // Log the claims for debugging
        // foreach (var claim in User.Claims)
        // {
        //     _logger.LogInformation($"Claim Type: {claim.Type} - Claim Value: {claim.Value}");
        // }
    }

    // start container
    public async Task<IActionResult> OnPostDeployInstance(string ExerciseName)
    {
        _logger.LogInformation("Initiating container deployment for exercise: {ExerciseName}", ExerciseName);

        try
        {
            await DeployContainerAsync(ExerciseName);
            // Redirect to the Container Details page, passing ExerciseName as a query parameter
            // todo optimize this url path
            return RedirectToPage("Containers/GetContainerDetails", new { exerciseName = ExerciseName });

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
    private async Task<PageResult> DeployContainerAsync(string ExerciseName)
    {
        try
        {
            _logger.LogInformation("Initiating container deployment for exercise: {ExerciseName}", ExerciseName);
            // get excercise config
            ExerciseConfig exerciseConfig = GetExercises().FirstOrDefault(e => e.ExerciseName == ExerciseName);
            // if not found throw exception
            if (exerciseConfig == null)
            {
                throw new ArgumentException("Invalid exercise name provided.");
            }
            _uid = User.FindFirst("uid")?.Value ?? string.Empty;

            _logger.LogInformation("Deploying container with image: {ImageName}", exerciseConfig.DockerImage);

            _logger.LogInformation("Deploying container with image: {Image}", exerciseConfig.DockerImage);
            _logger.LogInformation("Deploying container with port: {port}", exerciseConfig.port);
            _logger.LogInformation("Deploying container with uid: {uid}", _uid);


            // Create the container
            string createdContainerId = await _deploymentService.GetOrCreateContainerForUser(_dockerClient, exerciseConfig.DockerImage, exerciseConfig.ExerciseName, _uid, exerciseConfig.port ?? "");

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
            // return 
            return Page();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred during container deployment.");
            // Add error to ModelState to display on the page
            ModelState.AddModelError(string.Empty, "An error occurred while deploying the container. Please try again.");
            return Page();
        }

        // Start the container
        // await _deploymentService.RunContainer(_dockerClient, createdContainerId);
    }


    // todo now convert to stop container
    public async Task<RedirectToPageResult> OnPostPauseInstance(string instanceId)

    {
        _logger.LogInformation($"Pausing container: {instanceId}");

        try
        {
            await _deploymentService.PauseContainer(_dockerClient, instanceId);
            return RedirectToPage();
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to pause container: {instanceId}");
            ModelState.AddModelError("", "There was an error pausing the container. Please try again later.");
            return RedirectToPage();
        }
    }


    // public async Task OnPostContainerIDE(string IpAddress, string instanceId, string Port)
    // {
    //     _logger.LogInformation($"Opening container IDE for container: {instanceId}");
    //     _logger.LogInformation($"IP Address: {IpAddress}");
    //     _logger.LogInformation($"Port: {Port}");
    //     // add to http context
    //     HttpContext.Session.SetString("IpAddress", IpAddress);
    //     HttpContext.Session.SetString("Port", Port);
    //     HttpContext.Session.SetString("InstanceId", instanceId);
    //     // redirect to container ide page
    //     Response.Redirect("/Containers/ContainerIDE");

    // }


}
