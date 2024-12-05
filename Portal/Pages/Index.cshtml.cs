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
    public IList<string> usersContainer;

    [BindProperty]
    [Required(ErrorMessage = "Instance ID is required.")]
    public string InstanceId { get; set; } = string.Empty;


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
        }
        return Exercises;
    }
    // list containers
    public async Task OnGetAsync()
    {
        _uid = User.FindFirst("uid")?.Value ?? string.Empty;
        usersContainer = await _deploymentService.ListUsersContainer(_dockerClient, _uid);

        UserName = User.FindFirst("name")?.Value.Split(",")[0].Split(" ")[0] ?? string.Empty;
        // get all exercise configurations
        Exercises = GetExercises();
        // for testing only 
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
            return RedirectToPage("Containers/GetContainerDetails", new { exerciseName = ExerciseName });

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during container deployment.");
            ModelState.AddModelError(string.Empty, "An error occurred while deploying the container. Please try again.");
            return Page();
        }
    }

    // Main function to run the docker commands
    private async Task<PageResult> DeployContainerAsync(string ExerciseName)
    {
        try
        {
            ExerciseConfig? exerciseConfig = GetExercises().FirstOrDefault(e => e.ExerciseName == ExerciseName);
            // if not found throw exception
            if (exerciseConfig == null)
            {
                throw new ArgumentException("Invalid exercise name provided.");
            }
            _uid = User.FindFirst("uid")?.Value ?? string.Empty;


            _logger.LogInformation("Deploying container with uid: {uid}", _uid);


            // Create the container
            string createdContainerId = await _deploymentService.GetOrCreateContainerForUser(_dockerClient, exerciseConfig.DockerImage, exerciseConfig.ExerciseName, _uid, exerciseConfig.port ?? "");
            try
            {
                // store id in session just for testing
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
            ModelState.AddModelError(string.Empty, "An error occurred while deploying the container. Please try again.");
            return Page();
        }


    }

    public async Task OnPostStopInstance(string exerciseName)
    {
        _uid = User.FindFirst("uid")?.Value ?? string.Empty;

        _logger.LogInformation($"Stopping container: {exerciseName}");
        // get container id
        var serverDetails = await _deploymentService.FetchContainerDetails(_dockerClient, exerciseName, new List<string> { "ID" }, _uid);
        var instanceId = serverDetails.ID;
        _logger.LogInformation($"Stopping container: {instanceId}");
        try
        {
            await _deploymentService.RemoveUserOrPauseContainer(_dockerClient, instanceId, _uid);
            _logger.LogInformation($"Container stopped: {instanceId}");
            // Redirect to the same page to refresh
            Response.Redirect(Request.Path);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to stop container: {instanceId}");
            ModelState.AddModelError("", "There was an error stopping the container. Please try again later.");
            // Redirect to the same page to refresh
            Response.Redirect(Request.Path);
        }
    }


    // TODO fix this if needed or remove it 
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
