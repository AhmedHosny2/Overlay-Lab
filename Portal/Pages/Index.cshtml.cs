using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Portal.DeploymentService.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Pages
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly IDeploymentService _deploymentService;
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _configuration;

        public string UserIpAddress { get; private set; } = string.Empty;
        public IList<string> UsersContainer { get; set; } = new List<string>();

        [BindProperty]
        [Required(ErrorMessage = "Instance ID is required.")]
        public string InstanceId { get; set; } = string.Empty;

        public List<ExerciseConfig> Exercises { get; set; } = new();
        public string UserName { get; private set; } = string.Empty;
        private string _uid = string.Empty;

        public IndexModel(IDeploymentService deploymentService, ILogger<IndexModel> logger, IConfiguration configuration)
        {
            _deploymentService = deploymentService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task OnGetAsync()
        {
            InitializeUserContext();
            UsersContainer = await _deploymentService.ListUsersContainer(_uid);
            Exercises = LoadExercises();
            _logger.LogInformation("User {UserName} with uid {Uid} accessed the portal.", UserName, _uid);
        }

        public async Task<IActionResult> OnPostDeployInstanceAsync(string exerciseName)
        {
            if (string.IsNullOrWhiteSpace(exerciseName))
            {
                ModelState.AddModelError(string.Empty, "Exercise name is required.");
                return Page();
            }

            _logger.LogInformation("Deploying container for exercise: {ExerciseName}", exerciseName);

            try
            {
                await DeployContainerAsync(exerciseName);
                return RedirectToPage("Containers/GetContainerDetails", new { exerciseName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deploying container for exercise: {ExerciseName}", exerciseName);
                ModelState.AddModelError(string.Empty, "An error occurred while deploying the container. Please try again.");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostStopInstanceAsync(string exerciseName)
        {
            if (string.IsNullOrWhiteSpace(exerciseName))
            {
                ModelState.AddModelError(string.Empty, "Exercise name is required.");
                return Page();
            }

            _logger.LogInformation("Stopping container for exercise: {ExerciseName}", exerciseName);

            try
            {
                var containerDetails = await _deploymentService.FetchContainerDetails(exerciseName, new List<string> { "ID" }, _uid, UserIpAddress);
                var instanceId = containerDetails.ID;

                await _deploymentService.RemoveUserOrPauseContainer(instanceId, _uid);
                _logger.LogInformation("Container stopped: {InstanceId}", instanceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop container for exercise: {ExerciseName}", exerciseName);
                ModelState.AddModelError(string.Empty, "There was an error stopping the container. Please try again later.");
            }

            return RedirectToPage();
        }

        private async Task DeployContainerAsync(string exerciseName)
        {
            InitializeUserContext();

            var exerciseConfig = Exercises.FirstOrDefault(e => e.ExerciseName.Equals(exerciseName, StringComparison.OrdinalIgnoreCase));
            if (exerciseConfig == null)
            {
                throw new ArgumentException("Invalid exercise name provided.");
            }

            LogExerciseConfigurations();

            string containerId = await _deploymentService.GetOrCreateContainerForUser(
                exerciseConfig.DockerImage,
                exerciseConfig.ExerciseName,
                _uid,
                exerciseConfig.port ?? string.Empty,
                UserIpAddress,
                exerciseConfig.ClientSide,
                exerciseConfig.ClientPort,
                exerciseConfig.MaxUsers);

            try
            {
                HttpContext.Session.SetString("CreatedContainerId", containerId);
                _logger.LogInformation("Container deployed successfully with ID: {ContainerId}", containerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing container ID in session.");
                throw new Exception("Unable to store container ID in session.", ex);
            }
        }

        private  void InitializeUserContext()
        {
            _uid = User.FindFirst("uid")?.Value ?? string.Empty;
            UserName = User.FindFirst("name")?.Value?.Split(',').FirstOrDefault()?.Split(' ').FirstOrDefault() ?? string.Empty;
            Exercises = LoadExercises();
            UserIpAddress = GetUserIpAddress();

        }

        private List<ExerciseConfig> LoadExercises()
        {
            var exercises = new List<ExerciseConfig>();
            var configDirectory = _configuration.GetValue<string>("ExerciseConfigDirectory") ?? "ExConfiguration";

            foreach (var filePath in Directory.GetFiles(configDirectory, "*.json"))
            {
                var config = new ExerciseConfig();
                var fileConfig = new ConfigurationBuilder()
                    .AddJsonFile(filePath)
                    .Build();
                fileConfig.Bind(config);
                exercises.Add(config);
            }

            return exercises;
        }

        private string GetUserIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                var ip = Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault();
                _logger.LogInformation("User IP from X-Forwarded-For: {UserIpAddress}", ip);
                return ip ?? "192.168.64.1";
            }

            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(remoteIp) && remoteIp != "::1")
            {
                _logger.LogInformation("User IP from RemoteIpAddress: {UserIpAddress}", remoteIp);
                return remoteIp;
            }

            _logger.LogInformation("Default User IP: {UserIpAddress}", "192.168.64.1");
            return "192.168.64.1";
        }

        private void LogExerciseConfigurations()
        {
            foreach (var ex in Exercises)
            {
                _logger.LogInformation("Exercise: {ExerciseName}, Image: {DockerImage}, Port: {Port}, ClientSide: {ClientSide}, ClientPort: {ClientPort}, MaxUsers: {MaxUsers}",
                    ex.ExerciseName, ex.DockerImage, ex.port, ex.ClientSide, ex.ClientPort, ex.MaxUsers);
            }
        }
    }
}