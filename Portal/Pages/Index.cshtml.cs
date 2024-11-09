using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Docker.DotNet;
using Docker.DotNet.Models;
using Portal.Models;

namespace Portal.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    public string? Name { get; set; }
    // list od containers data
    public IList<ServerInstance> Containers { get; set; } = new List<ServerInstance>();
    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }
    public async void OnPost()
    {
        Console.WriteLine("Name: " + Request.Form["Name"]);
        Name = Request.Form["Name"];
      

    }

public async Task OnGetAsync()    {
          DockerClient client =  new  DockerClientConfiguration(
    new Uri("unix:///var/run/docker.sock")) // todo what is this ? 
     .CreateClient();



    // Optionally pull the image or log an error message
    // await client.Images.CreateImageAsync(
    //     new ImagesCreateParameters { FromImage = "ubuntu", Tag = "latest" },
    //     null,
    //     new Progress<JSONMessage>());




    // await client.Containers.CreateContainerAsync(new CreateContainerParameters()
    // {
    //     Image = "hello-world",
    //     HostConfig = new HostConfig()
    //     {
    //         DNS = new[] { "8.8.8.8", "8.8.4.4" }
    //     }
    // });







    // list containers

        IList<ContainerListResponse> containers = await client.Containers.ListContainersAsync(
            new ContainersListParameters()
            {
                Limit = 10,
            });
        foreach (var container in containers)
        {
          
            Containers.Add(
                new ServerInstance
                {
                    InstanceId = container.ID.Substring(0, 5),
                    ServerType = container.Image.Substring(0, 30),
                    Status = container.Status   ,
                    IpAddress = "X.X.X.X", // todo double chick 
                    Port = "X.X.X.X",
                    Created = container.Created
                }
            );



        }
       


    }


}
