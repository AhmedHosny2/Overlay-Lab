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
    // private readonly ILogger<IndexModel> _logger;
    private readonly IDeploymentService _deploymentService;
    private DockerClient _dockerClient;
    private readonly ILogger<IndexModel> _logger;
    public string UserName { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Instance ID is required.")]
    public string InstanceId { get; set; } = string.Empty;// To bind the InstanceId from the form

    public IList<ServerInstance> Containers { get; set; } = new List<ServerInstance>();

    public IndexModel(IDeploymentService deploymentService, ILogger<IndexModel> logger)
    {
        _deploymentService = deploymentService;
        _dockerClient = _deploymentService.ConnectToDocker();
        _logger = logger;


    }


    // list containers
    public async Task OnGetAsync()
    {
        Containers = await _deploymentService.ListContainers(_dockerClient);
        UserName = User.FindFirst("name")?.Value.Split(",")[0] ?? string.Empty;

        // foreach (var claim in User.Claims)
        // {
        //     _logger.LogInformation($"Claim Type: {claim.Type} - Claim Value: {claim.Value}");
        // }
    }
   
    public async Task<RedirectToPageResult> OnPostPauseInstance(string instanceId)

    {
        _logger.LogInformation($"Pausing container: {instanceId}");

        Console.WriteLine(instanceId);
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
}
