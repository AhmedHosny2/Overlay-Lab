// create the class that implements the interface
using Docker.DotNet;
using Docker.DotNet.Models;
using System.Text;
using System;
using System.Collections.Generic;
using Portal.DeploymentService.Interface;
using Portal.Models;

namespace Portal.DeploymentService.Class
{
    public class DeploymentService : IDeploymentService
    {

        // empty constructor
        public DeploymentService()
        {

        }

        // create a new DockerClient object 
        public DockerClient ConnectToDocker()
        {
            DockerClient client = new DockerClientConfiguration(
          new Uri("unix:///var/run/docker.sock"))
           .CreateClient();
            return client;
        }
        // check or create image 
        public async Task CheckOrCreateImage(DockerClient client, string ImageName)
        {

            IList<ImagesListResponse> images = await client.Images.ListImagesAsync(new ImagesListParameters()
            {
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
            if (images.Count == 0)
            {
                Console.WriteLine("Pulling image ",ImageName);  
                try
                {
                    await client.Images.CreateImageAsync(
                new ImagesCreateParameters
                {
                    FromImage = ImageName,
                    Tag = "latest",
                },
                null, // 1- TODO: add your auth details 
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
        public async Task<string> CreateContainer(DockerClient client, string ImageName, string DeploymentName)
        {

           

            var CreatedContainer = new CreateContainerResponse();
            var hostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
    {
        {
            "80/tcp", // Container port 
            // 2-TODO  do i need to add this ?
            new List<PortBinding>
            {
                new PortBinding
                {
                    HostPort = "" // Leave empty to let Docker assign a dynamic port
                }
            }
        }
    }
            };

            // set container properties
            var config = new Config

            {

                Image = ImageName,
                Tty = false,
                OpenStdin = true,
                AttachStdin = true,
                AttachStdout = true,
                AttachStderr = true,
                 // create container with sudo privilages and sleep infinity 
                Cmd = new List<string> { "/bin/sh", "-c", "sleep infinity" },
               
                ExposedPorts = new Dictionary<string, EmptyStruct>
            {
                { "80/tcp", new EmptyStruct() }
            }

            };
            string timestamp = DateTime.UtcNow.ToString("o"); // ISO 8601 format for timestamp
                                                              // check if name in use so add random number to it

            var random = new Random();
            DeploymentName += random.Next(1, 1000).ToString();
            // replcae spaces with -
            DeploymentName = DeploymentName.Replace(" ", "-");
            var createContainerParameters = new CreateContainerParameters(config)
            {
                HostConfig = hostConfig,
                Name = DeploymentName,
                Labels = new Dictionary<string, string>
                {
                    { "created_at", timestamp } // Add timestamp as a label
                }
            };

            Console.WriteLine("Creating container");
            try
            {
                CreatedContainer = await client.Containers.CreateContainerAsync(createContainerParameters);
                Console.WriteLine("Container created successfully");
                return CreatedContainer.ID;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                return string.Empty;
            }
        }

        // list containers
        public async Task<IList<ServerInstance>> ListContainers(DockerClient client)
        {
            IList<ServerInstance> Containers = new List<ServerInstance>();
            Console.WriteLine("Listing containers");
            try
            {
                IList<ContainerListResponse> containers = await client.Containers.ListContainersAsync(
                    // flag to show ports 

                    new ContainersListParameters()
                    {
                        All = true,
                        // force docke  r to show all ports


                    });
                foreach (var container in containers)
                {
                    // get the first network  
                    foreach (var networks in container.NetworkSettings.Networks)
                    {


                        // var networks = container.NetworkSettings.Networks["bridge"];
                        var ip = networks.Value.IPAddress;
                        // var gateway = networks.Gateway;
                        // var mac = networks.MacAddress;
                        // Access port bindings in NetworkSettings
                        foreach (var portMapping in container.Ports)
                        {
                        }

                        string port = "No port";
                        if (container.Ports.Count > 0)
                            port = container.Ports[0].PublicPort.ToString();

                        Containers.Add(
                            new ServerInstance
                            {
                                Name = container.Names[0],
                                InstanceId = container.ID.Substring(0, 5),

                                ServerType = container.Image.Split("@sha256")[0],
                                Status = container.State,
                                IpAddress = ip, // todo double chick 
                                Port = port,
                                Created = container.Created
                            }
                        );
                    }
                }
                Console.WriteLine("Containers listed successfully");
                return Containers;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                return new List<ServerInstance>();
            }
        }

        // run container
        public async Task RunContainer(DockerClient client, string ContainerId)
        {
            Console.WriteLine("Running container");
            try
            {
                await client.Containers.StartContainerAsync(
                   ContainerId, new ContainerStartParameters());
                Console.WriteLine("Container started successfully");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }


        // execute commands in the container that was created in the session

        // 3- TODO not all commands will work with this method 
        public async Task<StringBuilder> ExecuteCommand(DockerClient client, List<string> Command, string ContainerId)
        {
            Console.WriteLine($"Client: {client}, CreatedContainer: {ContainerId}, Command: {string.Join(" ", Command)}");
            try
            {
                // Build a single shell command string for complex operations
                string shellCommand = Command != null ? string.Join(" ", Command) : ""
                    ;

                // Create the exec instance with the shell command
                var execCreateResponse = await client.Exec.ExecCreateContainerAsync(ContainerId, new ContainerExecCreateParameters
                {
                    AttachStdin = true,
                    AttachStdout = true,
                    AttachStderr = true,
                    Tty = false,
                    Cmd = new List<string> { "sh", "-c", shellCommand } // Use 'sh -c' to execute the shell command
                }); 

                // Start the exec instance and attach to the output
                using (var stream = await client.Exec.StartAndAttachContainerExecAsync(execCreateResponse.ID, false))
                {
                    var outputBuilder = new StringBuilder();
                    var buffer = new byte[4096];

                    // Read the output asynchronously
                    while (true)
                    {
                        var count = await stream.ReadOutputAsync(buffer, 0, buffer.Length, CancellationToken.None);
                        if (count.EOF)
                        {
                            break;
                        }

                        outputBuilder.Append(Encoding.UTF8.GetString(buffer, 0, count.Count));
                    }

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

        // pause the container 
        public async Task PauseContainer(DockerClient client, string ContainerId)
        {
            Console.WriteLine("Pausing container");
            try
            {
                await client.Containers.PauseContainerAsync(ContainerId);
                Console.WriteLine("Container paused successfully");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }
   
    }
}
