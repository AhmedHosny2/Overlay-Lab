using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Portal.DeploymentService.Interface;
using Portal.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Html;

namespace MyApp.Namespace
{
    public class GetContainerDetailsModel : PageModel
    {
        private readonly IDeploymentService _deploymentService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GetContainerDetailsModel> _logger;

        public string ExerciseName { get; set; }
        public List<string> DisplayFields { get; set; }
        public IDictionary<string, string> Container { get; set; }

        private string _uid = string.Empty;

        public GetContainerDetailsModel(IDeploymentService deploymentService, IConfiguration configuration, ILogger<GetContainerDetailsModel> logger)
        {
            _deploymentService = deploymentService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task OnGetAsync(string exerciseName)
        {
            _uid = User.FindFirst("uid")?.Value ?? string.Empty;

            // Get DisplayFields from config file
            var exerciseConfigSection = _configuration.GetSection("Exercises").GetSection(exerciseName);
            _logger.LogInformation("ExerciseName: {ExerciseName}", exerciseConfigSection.Value);

            var exercises = GetExercises();
            var exercise = exercises.Find(e => e.ExerciseName == exerciseName);
            if (exercise != null)
            {
                DisplayFields = exercise.DisplayFields;
            }
            else
            {
                _logger.LogWarning("Exercise with name {ExerciseName} not found.", exerciseName);
                DisplayFields = new List<string>();
            }

            // Get IP from URL (ip:port)
            string ip = HttpContext.Request.Host.Host;

            // Fetch container details
            var serverInstance = await _deploymentService.FetchContainerDetails(exerciseName, DisplayFields, _uid, ip);
            var portNumber = serverInstance.Port;
            if((bool)exercise.ClientSide)
            {
                portNumber = exercise.ClientPort;
                ip = '0.0.0.0';
            }
            Container = new Dictionary<string, string>
            {
                { "ID", serverInstance.ID },
                { "Image", serverInstance.Image },
                { "Port", portNumber },
                { "IpAddress", serverInstance.IpAddress },
            };

            // Add the map
            foreach (var item in serverInstance.map)
            {
                Container.Add(item.Key, item.Value);
            }

            HttpContext.Session.SetString("InstanceId", serverInstance.ID);
            HttpContext.Session.SetString("Port", serverInstance.Port);
            HttpContext.Session.SetString("IpAddress", serverInstance.IpAddress);
        }

        public List<ExerciseConfig> GetExercises()
        {
            var myExercises = new List<ExerciseConfig>();
            var exerciseConfigs = Directory.GetFiles("ExConfiguration", "*.json");

            foreach (var filePath in exerciseConfigs)
            {
                var fileConfig = new ConfigurationBuilder()
                    .AddJsonFile(filePath)
                    .Build();

                var config = new ExerciseConfig();
                fileConfig.Bind(config);
                myExercises.Add(config);
            }

            return myExercises;
        }

        public static object RenderProperty(object value)
        {
            if (value == null)
            {
                return string.Empty;
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
                    return string.Empty;
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
                    return string.Empty;
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
    }
}