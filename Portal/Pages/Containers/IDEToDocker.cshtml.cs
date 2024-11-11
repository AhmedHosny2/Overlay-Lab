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
    private IDeploymentService _deploymentService = new Portal.DeploymentService.Class.DeploymentService();// TODO why ?

    //TODO make this function once and use it in all pages not twice  



    public void OnGet()
    {

    }
    public async Task<IActionResult> OnPostExecuteCommand()
    {
        try
        {

            // Get input from the form and parse it into command arguments
            List<string?> command = Request.Form["CommandInput"].ToList() ?? new List<string?>();
            // get container ID
            string? containerId = HttpContext.Session.GetString("CreatedContainerId");
            // Execute the command and capture the output
            StringBuilder execOutput = await _deploymentService.ExecuteCommand(_deploymentService.ConnectToDocker(), command, containerId);

            // Return the command output as a JSON result for AJAX handling
            return new JsonResult(new { success = true, output = execOutput.ToString() });
        }
        catch (Exception e)
        {
            return new JsonResult(new { success = false, output = e.Message });
        }
    }


}
