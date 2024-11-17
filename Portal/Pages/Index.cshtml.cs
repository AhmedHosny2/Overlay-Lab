using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal.Models;
using Portal.DeploymentService.Interface;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Docker.DotNet;
using System.ComponentModel.DataAnnotations;


namespace Portal.Pages;

public class IndexModel : PageModel
{
    // private readonly ILogger<IndexModel> _logger;
    private readonly IDeploymentService _deploymentService;
    private DockerClient _dockerClient;
    private readonly ILogger<IndexModel> _logger;


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
