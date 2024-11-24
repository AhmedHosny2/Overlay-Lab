using System.ComponentModel.DataAnnotations;
using Docker.DotNet;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Portal.DeploymentService.Interface;
using Portal.Models;

namespace MyApp.Namespace
{
    public class GetContainerDetailsModel : PageModel
    {


        private readonly IDeploymentService _deploymentService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GetContainerDetailsModel> _logger;
        private DockerClient _dockerClient;
        public string ContainerId { get; set; }
        public ServerInstance Container { get; set; }
        private string _uid = string.Empty;

        public GetContainerDetailsModel(IDeploymentService deploymentService, IConfiguration configuration, ILogger<GetContainerDetailsModel> logger)
        {
            _deploymentService = deploymentService;
            _configuration = configuration;
            _logger = logger;
            _dockerClient = _deploymentService.CreateDockerClient();
            // ContainerId =

            // Load the containers data
            // TODO could be loaded in the home page once and passed as a parameter
            // TODO handle the case where the container is not found 

        }

        public void OnGet(string id)
        {
            _uid = User.FindFirst("uid")?.Value ?? string.Empty;

            ContainerId = id;
            // log id 
            _logger.LogInformation("ContainerId: {0}", ContainerId);
            Container = _deploymentService.FetchContainerDetails(_dockerClient, ContainerId, _uid).Result;


        }

        public static object RenderProperty(object value)
        {
            if (value == null)
            {
                return "N/A";
            }

            Type type = value.GetType();

            if (type == typeof(string) || type.IsPrimitive)
            {
                return value;
            }
            else if (typeof(IEnumerable<string>).IsAssignableFrom(type))
            {
                var list = value as IEnumerable<string>;
                if (list != null && list.Any())
                {
                    return new HtmlString($"<ul>{string.Join("", list.Select(item => $"<li>{item}</li>"))}</ul>");
                }
                else
                {
                    return "N/A";
                }
            }
            else if (typeof(IEnumerable<object>).IsAssignableFrom(type))
            {
                var list = value as IEnumerable<object>;
                if (list != null && list.Any())
                {
                    var items = new List<string>();
                    foreach (var item in list)
                    {
                        if (item.GetType().IsPrimitive || item is string)
                        {
                            items.Add($"<li>{item}</li>");
                        }
                        else
                        {
                            string json = JsonConvert.SerializeObject(item, Formatting.Indented);
                            items.Add($"<li><pre>{json}</pre></li>");
                        }
                    }
                    return new HtmlString($"<ul>{string.Join("", items)}</ul>");
                }
                else
                {
                    return "N/A";
                }
            }
            else if (type.IsClass)
            {
                string json = JsonConvert.SerializeObject(value, Formatting.Indented);
                return new HtmlString($"<pre>{json}</pre>");
            }
            else
            {
                return value.ToString();
            }
        }


        // DeployInstance is the function name triggered with the deploy button in the UI

        // Main function to run the docker commands
    }
}
