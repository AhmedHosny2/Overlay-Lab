using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Antiforgery;
using Portal.DeploymentService.Interface;
namespace Portal.Pages;

public class IDEToDockerModel : PageModel
{
    [BindProperty]
    public string? CommandInput { get; set; }
    [BindProperty]
    public string? CommandOutput { get; set; }
    // create DeploymentService object
    private IDeploymentService _deploymentService;

    public IDEToDockerModel(IDeploymentService deploymentService)
    {
        _deploymentService = deploymentService;
    }


    public void OnGet()
    {

    }
    public async Task<IActionResult> OnPostExecuteCommand()
    {
        try
        {

            // Get input from the form and parse it into command arguments
            var command = Request.Form["CommandInput"].Where(x => x != null).Select(x => x!).ToList();
            // get container ID
            string? containerId = HttpContext.Session.GetString("CreatedContainerId");
            if (containerId == null)
            {
                return new JsonResult(new { success = false, output = "Container ID not found in session" });
            }
            // Execute the command and capture the output
            string execOutput = await _deploymentService.RunCommandInContainer(_deploymentService.CreateDockerClient(), command, containerId);

            // Return the command output as a JSON result for AJAX handling
            return new JsonResult(new { success = true, output = execOutput.ToString() });
        }
        catch (Exception e)
        {
            return new JsonResult(new { success = false, output = e.Message });
        }
    }


}
