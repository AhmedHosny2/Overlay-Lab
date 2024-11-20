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
                Console.WriteLine("Pulling image ", ImageName);
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
        // we simply create container for the current user 
        //TODO add method to attach user to  a docker 
        // so do we call the same method and internally either create a container or resume another one ? 
        // if we will create new one then we must add the cur user in the a new user list in the labels 
        // else we will just resume another container then we just need to resume it and add the user to the list
        // lastly if the user is on the list so we will return the existing container already 
        // TODO we need to know how will we handle either to create a new container or let him use existing one 
        // for now if the container image exit and health we will return it 
        // so our key is the image type now 
        public async Task<string> CreateContainerOrAddUser(DockerClient client, string ImageName, string UserName)
        {

            Task<IList<ServerInstance>> ContainerList = ListContainers(client);

            foreach (var container in ContainerList.Result)
            {
                // TODO later this will be more complex 
                // we have to take the decision based either create new container or use the existing one
                // for now i just check the image name 
                if (container.ServerType == ImageName)
                {
                    if (container.ConfigLabels.ContainsKey("users"))
                    {
                        // check if the user in the list 
                        if (container.ConfigLabels["users"].Contains(UserName))
                        {
                            Console.WriteLine("User already in the list");
                            return container.InstanceId;
                        }
                        else
                        {
                            Console.WriteLine("User not in the list");
                            // if not then add him
                            container.ConfigLabels["users"] += "," + UserName;
                            return container.InstanceId;
                        }

                    }


                }
                else
                {
                    Console.WriteLine("Image not found");
                    // create new container 
                    return await CreateContainer(client, ImageName, UserName);
                }
            }
            return string.Empty;

        }


        public async Task<string> CreateContainer(DockerClient client, string ImageName, string UserName)
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
                // add users lists 
                Labels = new Dictionary<string, string> { { "users", UserName } },

                //  IDictionary<string, string> Labels 
                // create container with sudo privilages and sleep infinity 
                Cmd = new List<string> { "/bin/sh", "-c", "sleep infinity" },
                //i wanna add a list of users who can acess this container 

                ExposedPorts = new Dictionary<string, EmptyStruct>
            {
                { "80/tcp", new EmptyStruct() }
            }

            };
            string timestamp = DateTime.UtcNow.ToString("o"); // ISO 8601 format for timestamp
                                                              // check if name in use so add random number to it

            var createContainerParameters = new CreateContainerParameters(config)
            {
                HostConfig = hostConfig,
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

                    // log everything about the ip 
                    foreach (var network in container.NetworkSettings.Networks)
                    {
                        Console.WriteLine($"Network: {network.Key}");
                        Console.WriteLine($"IP Address: {network.Value.IPAddress}");
                        Console.WriteLine($"Gateway: {network.Value.Gateway}");
                        Console.WriteLine($"Mac Address: {network.Value.MacAddress}");
                    }

                    var firstNetwork = container.NetworkSettings.Networks.Values.FirstOrDefault();

                    if (firstNetwork != null)
                    {
                        // Get the private IP address from the first network
                        var privateIP = firstNetwork.IPAddress;

                        // Get the public port if available, else assign "No port"
                        string port = "No port";
                        if (container.Ports != null && container.Ports.Count > 0)
                        {
                            port = container.Ports[0].PublicPort.ToString();
                        }

                        // Add container details to the list of ServerInstance
                        Containers.Add(
                            new ServerInstance
                            {
                                Name = container.Names[0],
                                InstanceId = container.ID.Substring(0, 5),
                                ServerType = container.Image.Split("@sha256")[0],
                                Status = container.State,
                                IpAddress = privateIP,
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
        // get container details
        public async Task<ServerInstance> GetContainerDetails(DockerClient client, string ContainerId)
        {

            Console.WriteLine("Getting container details");
            try
            {
                var containerDetails = await client.Containers.InspectContainerAsync(ContainerId);
                ServerInstance container = new ServerInstance(containerDetails);

                // var firstNetwork = containerDetails.NetworkSettings.Networks.Values.FirstOrDefault();

                // if (firstNetwork != null)
                // {
                //     // Get the private IP address from the first network
                //     var privateIP = firstNetwork.IPAddress;

                //     // Get the public port if available, else assign "No port"
                //     string port = "No port";
                //     // get port from config 
                //     if (containerDetails.Config.ExposedPorts != null && containerDetails.Config.ExposedPorts.Count > 0)
                //     {
                //         port = containerDetails.Config.ExposedPorts.Keys.ToString();
                //     }

                //     // Add container details to the list of ServerInstance
                //     container = new ServerInstance
                //     {
                //         Name = containerDetails.Name,
                //         InstanceId = containerDetails.ID.Substring(0, 5),
                //         ServerType = containerDetails.Image.Split("@sha256")[0],
                //         Status = containerDetails.State.Status,
                //         IpAddress = privateIP,
                //         Port = port,
                //         Created = containerDetails.Created
                //     };
                // }
                Console.WriteLine("Container details retrieved successfully");
                return container;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                return null;
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
