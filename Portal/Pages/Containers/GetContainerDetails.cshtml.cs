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
        public string ExerciseName { get; set; }
        public List<string> DisplayFields { get; set; }
        public IDictionary<string, string> Container { get; set; }
        private string _uid = string.Empty;

        public GetContainerDetailsModel(IDeploymentService deploymentService, IConfiguration configuration, ILogger<GetContainerDetailsModel> logger)
        {
            _deploymentService = deploymentService;
            _configuration = configuration;
            _logger = logger;
            _dockerClient = _deploymentService.CreateDockerClient();

        }

        public void OnGet(string exerciseName)
        {
            _uid = User.FindFirst("uid")?.Value ?? string.Empty;
            // get DisplayFields from config file
            var exerciseConfig = _configuration.GetSection("Exercises").GetSection(exerciseName);
            _logger.LogInformation("ExerciseName: {0}", exerciseConfig);
            var Exercises = GetExercises();
            var exercise = Exercises.Find(e => e.ExerciseName == exerciseName);
            if (exercise != null)
            {
                DisplayFields = exercise.DisplayFields;
            }
            else
            {
                _logger.LogWarning("Exercise with name {0} not found.", exerciseName);
                DisplayFields = new List<string>();
            }
            // get ip from url the url will be ip:port 
            string ip = HttpContext.Request.Host.Host;

            ServerInstance serverInstance = _deploymentService.FetchContainerDetails(_dockerClient, exerciseName, DisplayFields, _uid, ip).Result;

            Container = new Dictionary<string, string>
            {
                { "ID", serverInstance.ID },
                { "Image", serverInstance.Image },
                { "Port", serverInstance.Port },
                { "IpAddress", serverInstance.IpAddress },


            };
            // add the map 
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
            List<ExerciseConfig> MyExercises = new();

            var exerciseConfigs = Directory.GetFiles("ExConfiguration", "*.json");

            foreach (var filePath in exerciseConfigs)
            {
                var fileConfig = new ConfigurationBuilder()
                    .AddJsonFile(filePath)
                    .Build();

                var config = new ExerciseConfig();
                fileConfig.Bind(config);
                MyExercises.Add(config);

            }
            return MyExercises;
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
