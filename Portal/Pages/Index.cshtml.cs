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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;


namespace Portal.Pages;
[Authorize]
[IgnoreAntiforgeryToken]

public class IndexModel : PageModel
{
    private readonly IDeploymentService _deploymentService;
    private DockerClient _dockerClient;
    public string UserIp { get; private set; } = string.Empty;

    private readonly ILogger<IndexModel> _logger;
    public string UserName { get; set; } = string.Empty;
    private string _uid = string.Empty;
    public string UserIpAddress { get; private set; } = string.Empty;

    public IList<string> usersContainer = new List<string>();

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
    public List<ExerciseConfig> GetExercises()
    {

        var exerciseConfigs = Directory.GetFiles("ExConfiguration", "*.json");

        foreach (var filePath in exerciseConfigs)
        {
            var fileConfig = new ConfigurationBuilder()
                .AddJsonFile(filePath)
                .Build();

            var config = new ExerciseConfig(
               
            
            );
            fileConfig.Bind(config);
            Exercises.Add(config);
        }
        return Exercises;
    }


    public string? LoadUsersIpAddress()
    {
        if (HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            // Extract the first IP from the X-Forwarded-For header
            UserIpAddress = HttpContext.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0];
            _logger.LogInformation("Original user IP retrieved from X-Forwarded-For: {UserIpAddress}", UserIpAddress);
        }
        else if (HttpContext.Connection.RemoteIpAddress != null && HttpContext.Connection.RemoteIpAddress.ToString() != "::1")
        {

            _logger.LogInformation("Original user IP retrieved from RemoteIpAddress: {UserIpAddress}", UserIpAddress);
        }
        else
        {
            // just handling the case where the ip is not found for locacl host not vm  
            UserIpAddress = "192.168.64.1";
            _logger.LogInformation("Original user IP retrieved from Host: {UserIpAddress}", UserIpAddress);
        }

        return UserIpAddress;
    }
    public void LoadIpAndUid()
    {
        _uid = User.FindFirst("uid")?.Value ?? string.Empty;
        UserName = User.FindFirst("name")?.Value.Split(",")[0].Split(" ")[0] ?? string.Empty;

        LoadUsersIpAddress();
    }
    public async Task OnGetAsync()
    {
        LoadIpAndUid();
        usersContainer = await _deploymentService.ListUsersContainer(_dockerClient, _uid);

        // get all exercise configurations
        Exercises = GetExercises();
        _logger.LogInformation("User {UserName} with uid {uid} accessed the portal.", UserName, _uid);
    }

    // start container
    public async Task<IActionResult> OnPostDeployInstance(string ExerciseName)
    {
        try
        {
            _logger.LogInformation("Deploying container for exercise: {ExerciseName}", ExerciseName);

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
        LoadIpAndUid();

        try
        {
            ExerciseConfig? exerciseConfig = GetExercises().FirstOrDefault(e => e.ExerciseName == ExerciseName);
            if (exerciseConfig == null)
            {
                throw new ArgumentException("Invalid exercise name provided.");
            }

            _logger.LogInformation("Deploying container with uid: {uid}", _uid);
            // Create the container
            string createdContainerId = await _deploymentService.GetOrCreateContainerForUser(_dockerClient, exerciseConfig.DockerImage, exerciseConfig.ExerciseName, _uid, exerciseConfig.port ?? "", UserIpAddress
            , exerciseConfig.ClientSide, exerciseConfig.ClientPort, exerciseConfig.MaxUsers);
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
            _logger.LogInformation("Container deployed successfully with ID: {createdContainerId}\n", createdContainerId);
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

        LoadIpAndUid();
        // get container id
        var serverDetails = await _deploymentService.FetchContainerDetails(_dockerClient, exerciseName, new List<string> { "ID" }, _uid, UserIpAddress);
        var instanceId = serverDetails.ID;
        _logger.LogInformation($"Stopping container: {instanceId}");
        try
        {
            await _deploymentService.RemoveUserOrPauseContainer(_dockerClient, instanceId, _uid);
            _logger.LogInformation($"Container stopped: {instanceId}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to stop container: {instanceId}");
            ModelState.AddModelError("", "There was an error stopping the container. Please try again later.");
        }
        finally
        {
            Response.Redirect(Request.Path);

        }
    }


}
