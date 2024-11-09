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
using Newtonsoft.Json;


namespace Portal.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    [BindProperty]
    public string CommandInput { get; set; }
    [BindProperty]
    public string CommandOutput { get; set; }
    // public CreateContainerResponse CreatedContainer = new CreateContainerResponse();
    // public DockerClient client ;
    // list od containers data
    public IList<ServerInstance> Containers { get; set; } = new List<ServerInstance>();
    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    // connect to our docker 
    public DockerClient ConnectToDocker()
    {
        // create a new DockerClient object 
        DockerClient client = new DockerClientConfiguration(
      new Uri("unix:///var/run/docker.sock")) // todo what is this ? 
       .CreateClient();
        return client;
    }
    // check or create image 
    public async Task CheckOrCreateImage(DockerClient client, string ImageName)
    {
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
        }
    }

    // create container
    public async Task<CreateContainerResponse> CreateContainer(DockerClient client, string ImageName)
    {
        var CreatedContainer = new CreateContainerResponse();
        // set container properties
        var config = new Config
        {
            Image = ImageName,         // Use the desired image
            Tty = true,                      // Allocate a pseudo-TTY
            OpenStdin = true,                // Keep stdin open for interaction
            AttachStdin = true,              // Attach stdin to the container
            AttachStdout = true,             // Attach stdout to capture output
            AttachStderr = true,             // Attach stderr to capture errors
            Cmd = new List<string> { "sleep", "infinity" }, // Keeps the container running
            Hostname = "localhost",
            Domainname = "example.com",


        };

        var createContainerParameters = new CreateContainerParameters(config);

        // Create docker container from the image
        Console.WriteLine("Creating container ${ImageName}");
        try
        {
            CreatedContainer = await client.Containers.CreateContainerAsync(createContainerParameters);
            Console.WriteLine("Container created successfully");
            return CreatedContainer;
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
            return null;
        }
    }

    // list containers
    public async Task ListContainers(DockerClient client)
    {
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

    // run container
    public async Task RunContainer(DockerClient client, CreateContainerResponse CreatedContainer)
    {
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
    }


    // excute command into the container
    public async Task<StringBuilder> ExecuteCommand(DockerClient client, List<string> Command)
    {


        var myCreatedContainerId = HttpContext.Session.GetString("CreatedContainerId");

        Console.WriteLine($"Client: {client}, CreatedContainer: {myCreatedContainerId}, Command: {string.Join(" ", Command)}");
        try
        {
            var execCreateResponse = await client.Exec.ExecCreateContainerAsync(myCreatedContainerId, new ContainerExecCreateParameters
            {
                AttachStdin = true,
                AttachStdout = true,
                AttachStderr = true,
                Tty = true,
                // command create directory in the root add file to it and list the files in it

                Cmd = Command ?? new List<string> { "sh", "-c", "mkdir /root/test && echo 'Hello, World!' > /root/test/hello.txt && ls /root/test" }
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
                Console.WriteLine(outputBuilder);
                
                return outputBuilder;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
            return new StringBuilder(e.Message);
        }
    }

    public async Task MainAsync()
    {
        try
        {
            var client = ConnectToDocker();

            string ImageName = "alpine";
            await CheckOrCreateImage(client, ImageName);
            var CreatedContainer = await CreateContainer(client, ImageName);
            // store the created container id in the session
            HttpContext.Session.SetString("CreatedContainerId", CreatedContainer.ID);
            await ListContainers(client);
            await RunContainer(client, CreatedContainer);

            // var output = await ExecuteCommand(ConnectToDocker(),   new List<string> { "sh", "-c", "mkdir /root/test && echo 'Hello, World!' > /root/test/hello.txt && ls /root/test" });
            // Console.WriteLine(output);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);

        }
    }


    // DeployInstance is the function name with the butto 
    public async Task OnPostDeployInstance()
    {
        await MainAsync();

    }

    public async Task OnGetAsync()
    {
        await ListContainers(ConnectToDocker());
    }

       // Method to execute command asynchronously and store the result
    public async Task<IActionResult> OnPostExecuteCommand()
    {
        // get input from the form
        List<string> command = Request.Form["CommandInput"].ToString().Split(' ').ToList();
        // Execute command and get the output
        StringBuilder execOutput = await ExecuteCommand(ConnectToDocker(), command);
        
        // Store the output in CommandOutput
        CommandOutput = execOutput + " this is the output";

        // Return the page with updated model
        return Page();
    }

    // old code 
    // public async Task MainAsync()
    // {
    //     var CreatedContainer = new CreateContainerResponse();
    //     string ImageName = "alpine";
    //     // set container properties
    //     var config = new Config
    //     {
    //         Image = ImageName,         // Use the desired image
    //         Tty = true,                      // Allocate a pseudo-TTY
    //         OpenStdin = true,                // Keep stdin open for interaction
    //         AttachStdin = true,              // Attach stdin to the container
    //         AttachStdout = true,             // Attach stdout to capture output
    //         AttachStderr = true,             // Attach stderr to capture errors
    //         Cmd = new List<string> { "sleep", "infinity" } // Keeps the container running

    //     };

    //     var createContainerParameters = new CreateContainerParameters(config);




    //     // get user's images and check if he got the image
    //     IList<ImagesListResponse> images = await client.Images.ListImagesAsync(new ImagesListParameters()
    //     {
    //         // get the image
    //         Filters = new Dictionary<string, IDictionary<string, bool>>()
    //         {
    //             {
    //                 "reference", new Dictionary<string, bool>()
    //                 {
    //                     { ImageName, true }
    //                 }
    //             }
    //         }
    //     });
    //     // check if images is not null and print the details
    //     if (images.Count == 0)
    //     {
    //         // pull it first 
    //         Console.WriteLine("Pulling image ${ImageName}");
    //         try
    //         {
    //             await client.Images.CreateImageAsync(
    //         new ImagesCreateParameters
    //         {
    //             FromImage = ImageName,
    //             Tag = "latest",
    //         },
    //         null, // TODO: add your auth details 
    //         new Progress<JSONMessage>());
    //             Console.WriteLine("Image pulled successfully");
    //         }
    //         catch (Exception e)
    //         {
    //             Console.WriteLine("Error: " + e.Message);
    //         }

    //     }

    //     else
    //     {

    //         Console.WriteLine("Image found: ");
    //         // Create docker container from the image
    //         Console.WriteLine("Creating container ${ImageName}");
    //         try
    //         {




    //             CreatedContainer = await client.Containers.CreateContainerAsync(createContainerParameters);
    //             Console.WriteLine("Container created successfully");
    //         }
    //         catch (Exception e)
    //         {
    //             Console.WriteLine("Error: " + e.Message);
    //         }
    //     }

    //     // get user's images and print their details
    //     // =================================================================================================


    //     // foreach (var image in images)
    //     // {
    //     //     Console.WriteLine("Image ID: " + image.ID);

    //     //     // Check if RepoTags is available and print each tag
    //     //     if (image.RepoTags != null && image.RepoTags.Count > 0)
    //     //     {
    //     //         foreach (var tag in image.RepoTags)
    //     //         {
    //     //             Console.WriteLine("Image Name and Tag: " + tag);
    //     //         }
    //     //     }
    //     //     else
    //     //     {
    //     //         Console.WriteLine("Image Name and Tag: <No Tags>");
    //     //     }

    //     //     Console.WriteLine("Created: " + image.Created);
    //     //     Console.WriteLine("Size: " + image.Size);
    //     // }
    //     // =================================================================================================

    //     // list containers
    //     Console.WriteLine("Listing containers");
    //     try
    //     {
    //         IList<ContainerListResponse> containers = await client.Containers.ListContainersAsync(
    //             new ContainersListParameters()
    //             {
    //                 Limit = 10,
    //             });
    //         foreach (var container in containers)
    //         {

    //             Containers.Add(
    //                 new ServerInstance
    //                 {
    //                     InstanceId = container.ID.Substring(0, 5),

    //                     ServerType = container.Image.Split("@sha256")[0],
    //                     Status = container.Status,
    //                     IpAddress = "X.X.X.X", // todo double chick 
    //                     Port = "X.X.X.X",
    //                     Created = container.Created
    //                 }
    //             );



    //         }
    //         Console.WriteLine("Containers listed successfully");
    //     }
    //     catch (Exception e)
    //     {
    //         Console.WriteLine("Error: " + e.Message);
    //     }

    //     // run container
    //     Console.WriteLine("Running container");
    //     try
    //     {


    //         await client.Containers.StartContainerAsync(
    //             CreatedContainer.ID, new ContainerStartParameters());


    //         Console.WriteLine("Container started successfully");
    //     }
    //     catch (Exception e)
    //     {
    //         Console.WriteLine("Error: " + e.Message);
    //     }


    //     // excute command into the container 

    //     try
    //     {
    //         var execCreateResponse = await client.Exec.ExecCreateContainerAsync(CreatedContainer.ID, new ContainerExecCreateParameters
    //         {
    //             AttachStdin = true,
    //             AttachStdout = true,
    //             AttachStderr = true,
    //             Tty = true,
    //             // command create directory in the root add file to it and list the files in it

    //             Cmd = new List<string> { "sh", "-c", "mkdir /root/test && echo 'Hello, World!' > /root/test/hello.txt && ls /root/test" }
    //         });

    //         // Start the exec instance and attach to the output
    //         using (var stream = await client.Exec.StartAndAttachContainerExecAsync(execCreateResponse.ID, false))
    //         {
    //             var outputBuilder = new StringBuilder();
    //             var buffer = new byte[4096];

    //             // Read the output synchronously
    //             while (true)
    //             {
    //                 var count = await stream.ReadOutputAsync(buffer, 0, buffer.Length, CancellationToken.None);
    //                 if (count.EOF)
    //                 {
    //                     break;
    //                 }

    //                 outputBuilder.Append(Encoding.UTF8.GetString(buffer, 0, count.Count));
    //             }

    //             // Print the output to the console
    //             Console.WriteLine("Command output:");
    //             Console.WriteLine(outputBuilder.ToString());
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         Console.WriteLine("Error: " + e.Message);
    //     }

    // }



}
