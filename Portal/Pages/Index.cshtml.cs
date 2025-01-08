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
    public string UserIp { get; private set; }

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
    public string UserIpAddress { get; private set; }

    public string? GetIpAddress()
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

        // Log a general message with the IP
        _logger.LogInformation("User IP Address logged: {UserIpAddress}", UserIpAddress);
        return UserIpAddress;


    }
    // list containers
    public async Task OnGetAsync()
    {



        // Retrieve the original user's IP address

        GetIpAddress();




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
        // get user's ip
        GetIpAddress();



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
            // return 
            _logger.LogInformation("Container deployed successfully with ID: {createdContainerId}\n\n\n\n\n\n\n", createdContainerId);
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
        // get ip 
        GetIpAddress();

        // get container id
        var serverDetails = await _deploymentService.FetchContainerDetails(_dockerClient, exerciseName, new List<string> { "ID" }, _uid, UserIpAddress);
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


}
