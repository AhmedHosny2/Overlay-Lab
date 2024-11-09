using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Docker.DotNet;
using Docker.DotNet.Models;
using Portal.Models;

namespace Portal.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    // list od containers data
    public IList<ServerInstance> Containers { get; set; } = new List<ServerInstance>();
    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }
    public async Task OnPostAsync()    {
        await MainAsync();
       


    }
    public async Task MainAsync()
    {
        // create a new DockerClient object 
        DockerClient client = new DockerClientConfiguration(
      new Uri("unix:///var/run/docker.sock")) // todo what is this ? 
       .CreateClient();
        // get user's images and check if he got hello-world image
        IList<ImagesListResponse> images = await client.Images.ListImagesAsync(new ImagesListParameters()
        {
            // get hello-world image
            Filters = new Dictionary<string, IDictionary<string, bool>>()
            {
                {
                    "reference", new Dictionary<string, bool>()
                    {
                        { "hello-world", true }
                    }
                }
            }
        });
        // check if images is not null and print the details
        if (images.Count == 0)
        {
            // pull it first 
            Console.WriteLine("Pulling hello-world image");
            try
            {
                await client.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = "hello-world",
                Tag = "latest",
            },
            null, // TODO: add your auth details 
            new Progress<JSONMessage>());
                Console.WriteLine("Image pulled successfully");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }

        }

        else
        {

            Console.WriteLine("Image found: ");
            // Create docker container hello-world
            Console.WriteLine("Creating container hello-world");
            try
            {
                await client.Containers.CreateContainerAsync(new CreateContainerParameters()
                {
                    Image = "hello-world",
                    HostConfig = new HostConfig()
                    {
                        DNS = new[] { "8.8.8.8", "8.8.4.4" }
                    }
                });
                Console.WriteLine("Container created successfully");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        // get user's images and print their details
        // =================================================================================================


        // foreach (var image in images)
        // {
        //     Console.WriteLine("Image ID: " + image.ID);

        //     // Check if RepoTags is available and print each tag
        //     if (image.RepoTags != null && image.RepoTags.Count > 0)
        //     {
        //         foreach (var tag in image.RepoTags)
        //         {
        //             Console.WriteLine("Image Name and Tag: " + tag);
        //         }
        //     }
        //     else
        //     {
        //         Console.WriteLine("Image Name and Tag: <No Tags>");
        //     }

        //     Console.WriteLine("Created: " + image.Created);
        //     Console.WriteLine("Size: " + image.Size);
        // }
        // =================================================================================================

        // list containers
        Console.WriteLine("Listing containers");
        try
        {
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
                        
                        ServerType = container.Image.Split("@sha256")[0],
                        Status = container.Status,
                        IpAddress = "X.X.X.X", // todo double chick 
                        Port = "X.X.X.X",
                        Created = container.Created
                    }
                );



            }
            Console.WriteLine("Containers listed successfully");
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }

    }

    // call the mainAsync method
    public async Task OnGetAsync()
    {
    }


}
