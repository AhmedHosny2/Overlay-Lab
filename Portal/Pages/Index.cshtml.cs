using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Docker.DotNet;
using Docker.DotNet.Models;
using Portal.Models;
using System.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


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
    public async Task OnPostAsync()
    {
        await MainAsync();



    }

    // run continaer and exec command
 
    public async Task MainAsync()
    {
        var CreatedContainer = new CreateContainerResponse();
        string ImageName = "alpine";
        // set container properties
        var config = new Config
        {
            Image = ImageName,         // Use the desired image
            Tty = true,                      // Allocate a pseudo-TTY
            OpenStdin = true,                // Keep stdin open for interaction
            AttachStdin = true,              // Attach stdin to the container
            AttachStdout = true,             // Attach stdout to capture output
            AttachStderr = true,             // Attach stderr to capture errors
                           Cmd =  new List<string> { "sleep", "infinity" } // Keeps the container running

        };

        var createContainerParameters = new CreateContainerParameters(config);



        // create a new DockerClient object 
        DockerClient client = new DockerClientConfiguration(
      new Uri("unix:///var/run/docker.sock")) // todo what is this ? 
       .CreateClient();
        // get user's images and check if he got the image
        IList<ImagesListResponse> images = await client.Images.ListImagesAsync(new ImagesListParameters()
        {
            // get the image
            Filters = new Dictionary<string, IDictionary<string, bool>>()
            {
                {
                    "reference", new Dictionary<string, bool>()
                    {
                        { ImageName, true }
                    }
                }
            }
        });
        // check if images is not null and print the details
        if (images.Count == 0)
        {
            // pull it first 
            Console.WriteLine("Pulling image ${ImageName}");
            try
            {
                await client.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = ImageName,
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
            // Create docker container from the image
            Console.WriteLine("Creating container ${ImageName}");
            try
            {




                CreatedContainer = await client.Containers.CreateContainerAsync(createContainerParameters);
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

        // run container
        Console.WriteLine("Running container");
        try
        {


            await client.Containers.StartContainerAsync(
                CreatedContainer.ID, new ContainerStartParameters());

            
            Console.WriteLine("Container started successfully");
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }
        try
        {
            // Create the exec instance with the specified command
            var execCreateResponse = await client.Exec.ExecCreateContainerAsync(CreatedContainer.ID, new ContainerExecCreateParameters
            {
                AttachStdin = true,
                AttachStdout = true,
                AttachStderr = true,
                Tty = true,
                Cmd = [ 
                        "echo 'Hello World' > /tmp/hello.txt"
                     ] // Executes the command in a Bash shell
            });

            // Start the exec instance and attach to the output
            using (var stream = await client.Exec.StartAndAttachContainerExecAsync(execCreateResponse.ID, false))
            {
                var outputBuilder = new StringBuilder();
                var buffer = new byte[4096];

                // Read the output synchronously
                while (true)
                {
                    var count = await stream.ReadOutputAsync(buffer, 0, buffer.Length, CancellationToken.None);
                    if (count.EOF)
                    {
                        break;
                    }

                    outputBuilder.Append(Encoding.UTF8.GetString(buffer, 0, count.Count));
                }

                // Print the output to the console
                Console.WriteLine("Command output:");
                Console.WriteLine(outputBuilder.ToString());
            }
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
