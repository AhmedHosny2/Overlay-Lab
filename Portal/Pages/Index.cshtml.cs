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

    public IndexModel(IDeploymentService deploymentService, ILogger<IndexModel> logger)
    {
        _deploymentService = deploymentService;
        _dockerClient = _deploymentService.CreateDockerClient();
        _logger = logger;


    }


    // list containers
    public async Task OnGetAsync()
    {
        _uid = User.FindFirst("uid")?.Value ?? string.Empty;
        Containers = await _deploymentService.ListContainers(_dockerClient, _uid);
        UserName = User.FindFirst("name")?.Value.Split(",")[0] ?? string.Empty;

        // Log the claims for debugging
        // foreach (var claim in User.Claims)
        // {
        //     _logger.LogInformation($"Claim Type: {claim.Type} - Claim Value: {claim.Value}");
        // }
    }

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
    public async Task OnPostContainerIDE(string IpAddress, string instanceId, string Port)
    {
        _logger.LogInformation($"Opening container IDE for container: {instanceId}");
        _logger.LogInformation($"IP Address: {IpAddress}");
        _logger.LogInformation($"Port: {Port}");
        // add to http context
        HttpContext.Session.SetString("IpAddress", IpAddress);
        HttpContext.Session.SetString("Port", Port);
        HttpContext.Session.SetString("InstanceId", instanceId);
        // redirect to container ide page
        Response.Redirect("/Containers/ContainerIDE");

    }


}
