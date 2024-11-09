using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Portal.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    public string? Name { get; set; }
    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }
    public async void OnPost()
    {
        Console.WriteLine("Name: " + Request.Form["Name"]);
        Name = Request.Form["Name"];
      

    }

    public async  void OnGet()
    {
          DockerClient client = new DockerClientConfiguration(
    new Uri("unix:///var/run/docker.sock")) // todo what is this ? 
     .CreateClient();

        IList<ContainerListResponse> containers = await client.Containers.ListContainersAsync(
            new ContainersListParameters()
            {
                Limit = 10,
            });
        foreach (var container in containers)
        {
            Console.WriteLine(container.ID);
            Console.WriteLine(container.Names);
            Console.WriteLine(container.State);

        }

    }

    // do docker things 

}
