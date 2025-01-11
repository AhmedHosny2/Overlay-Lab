using Docker.DotNet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Portal.DeploymentService.Interface;
using Portal.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;

namespace Portal.Pages
{
    public class IDEToDockerModel : PageModel
    {
        [BindProperty]
        public string? CommandInput { get; set; }

        [BindProperty]
        public string? CommandOutput { get; set; }

        private readonly IDeploymentService _deploymentService;
        private readonly ILogger<IDEToDockerModel> _logger;

        public IDEToDockerModel(IDeploymentService deploymentService, ILogger<IDEToDockerModel> logger)
        {
            _deploymentService = deploymentService;
            _logger = logger;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostExecuteCommandAsync()
        {
            try
            {
                // Validate CommandInput
                if (string.IsNullOrWhiteSpace(CommandInput))
                {
                    return new JsonResult(new { success = false, output = "No command provided." });
                }

                // Parse the command input into arguments
                var command = CommandInput.Split(' ').ToList();

                // Get container ID from session
                string? containerId = HttpContext.Session.GetString("CreatedContainerId");
                if (string.IsNullOrEmpty(containerId))
                {
                    return new JsonResult(new { success = false, output = "Container ID not found in session." });
                }

                _logger.LogInformation("Executing command: {Command} in container: {ContainerId}", CommandInput, containerId);

                // Execute the command and capture the output
                string execOutput = await _deploymentService.RunCommandInContainer(command, containerId);

                // Optionally, log the output
                _logger.LogInformation("Command Output: {ExecOutput}", execOutput);

                // Return the command output as a JSON result for AJAX handling
                return new JsonResult(new { success = true, output = execOutput });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error executing command in container.");
                return new JsonResult(new { success = false, output = e.Message });
            }
        }
    }
}