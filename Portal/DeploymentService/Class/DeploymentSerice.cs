// Create the class that implements the interface
using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Portal.DeploymentService.Interface;
using Portal.Models;

namespace Portal.DeploymentService.Class
{
    public class DeploymentService : IDeploymentService
    {
        // Empty constructor
        public DeploymentService()
        {
        }
        // create docker client using unix socket
        public DockerClient CreateDockerClient()
        {
            DockerClient client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock"))
                .CreateClient();
            return client;
        }

        // Check or create image
        public async Task EnsureDockerImageExists(DockerClient client, string imageName)
        {
            IList<ImagesListResponse> images = await client.Images.ListImagesAsync(new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    {
                        "reference", new Dictionary<string, bool>
                        {
                            { imageName, true } // get images with the specified name
                        }
                    }
                }
            });
            // if image doesn't exit so fetch it
            if (images.Count == 0)
            {
                Console.WriteLine($"Pulling image {imageName}");
                try
                {
                    await client.Images.CreateImageAsync(
                        new ImagesCreateParameters
                        {
                            FromImage = imageName,
                            Tag = "latest",
                        },
                        null, // TODO: Add your auth details
                        new Progress<JSONMessage>());
                    Console.WriteLine("Image pulled successfully");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error: {e.Message}");
                }
            }
            else
            {
                Console.WriteLine("Image found");
            }
        }

        // Create container or add user
        // each time user tries to connect/deploy to the container, this method will be called
        public async Task<string> GetOrCreateContainerForUser(DockerClient client, string imageName, string exerciseName, string Uid, string port)
        {
            IList<ContainerListResponse> containersList = await client.Containers.ListContainersAsync(new ContainersListParameters
            {
            });


            // print image uid and port
            if (containersList.Count == 0)
            {
                Console.WriteLine("No containers found, create a new one");
                return await InitializeContainer(client, imageName, exerciseName, Uid, port);

            }
            bool imageCreated = false;
            foreach (var container in containersList)
            {
                // TODO: Implement more complex logic for container selection
                if (container.Image.Equals(imageName, StringComparison.OrdinalIgnoreCase))
                {
                    imageCreated = true;
                    // get users list 
                    string usersList = await RunCommandInContainer(client, new List<string> { "cat", "/users.txt" }, container.ID);
                    Console.WriteLine($"Users list: {usersList}");
                    // Check if the user is already in the list
                    var users = usersList.Split(',');
                    if (users.Contains(Uid))
                    {
                        Console.WriteLine("User already in the list");
                        return container.ID;
                    }
                    else
                    {
                        Console.WriteLine("User not in the list, add user to the list of container id " + container.ID);
                        // Add user to the list
                        string updatedUsersList = $"{usersList},{Uid}";
                        Console.WriteLine($"Updated users list: {updatedUsersList}");
                        await RunCommandInContainer(client, new List<string> { $"echo \"{updatedUsersList}\" > users.txt" }, container.ID);
                        return container.ID;
                    }

                }


            }
            if (!imageCreated)
            {
                Console.WriteLine("No containers with image specs found, create a new one");
                return await InitializeContainer(client, imageName, exerciseName, Uid, port);
            }

            return string.Empty;
        }

        // Create a new container
        public async Task<string> InitializeContainer(DockerClient client, string imageName, string exerciseName, string Uid, string port)
        {
            await EnsureDockerImageExists(client, imageName);

            // Log the input parameters
            Console.WriteLine($"Image Name: {imageName}, UID: {Uid}, Port: {port}");

            // Define port bindings to map host port to container port
            var hostConfig = new HostConfig
            {

                PortBindings = new Dictionary<string, IList<PortBinding>>
        {
            {
                $"{port}/tcp", new List<PortBinding>
                {
                    new PortBinding
                    {
                        HostPort = port // Host port (no "/tcp" needed)
                    }
                }
            }
        }
            };

            // Configure container settings
            var config = new Config
            {
                Image = imageName,
                Tty = true, // Allocate a pseudo-TTY (-t)
                OpenStdin = true, // Keep STDIN open even if not attached (-i)
                AttachStdin = true,
                AttachStdout = true,
                AttachStderr = true,
                // set container name to the exercise name
                // Hostname = exerciseName,


                ExposedPorts = new Dictionary<string, EmptyStruct>

                {
            { $"{port}/tcp", new EmptyStruct() }
            }
            };




            var createContainerParameters = new CreateContainerParameters(config)
            {
                HostConfig = hostConfig,
                Name = exerciseName

            };

            Console.WriteLine("Creating container...");
            try
            {
                // Create the container
                var createdContainer = await client.Containers.CreateContainerAsync(createContainerParameters);
                Console.WriteLine($"Container created successfully with ID: {createdContainer.ID}");

                // Start the container immediately after creation
                bool started = await client.Containers.StartContainerAsync(createdContainer.ID, new ContainerStartParameters());
                if (started)
                {
                    Console.WriteLine("Container started successfully.");
                    // add uid to the list of users
                    await RunCommandInContainer(client, new List<string> { $"echo \"{Uid}\" >> users.txt" }, createdContainer.ID);
                    Console.WriteLine("User added to the list of users");
                }
                else
                {
                    Console.WriteLine("Failed to start the container.");
                    return string.Empty;
                }

                return createdContainer.ID;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating container: {ex.Message}");
                return string.Empty;
            }
        }
        // List containers

        public async Task<IList<ServerInstance>> ListContainers(DockerClient client, string Uid)
        {
            IList<ServerInstance> containersList = new List<ServerInstance>();
            Console.WriteLine("Listing containers...");
            // print uid 
            Console.WriteLine($"UID: {Uid}");
            try
            {
                IList<ContainerListResponse> containers = await client.Containers.ListContainersAsync(new ContainersListParameters
                {
                });

                foreach (var container in containers)
                {
                    // check if the user in the list of the container
                    string usersList = await RunCommandInContainer(client, new List<string> { "cat", "/users.txt" }, container.ID);
                    if (usersList == null || !usersList.Contains(Uid))
                    {
                        continue;
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
                        containersList.Add(new ServerInstance
                        {
                            Name = container.Names.FirstOrDefault() ?? "Unknown",
                            InstanceId = container.ID.Substring(0, 5),
                            ServerType = container.Image.Split("@sha256")[0],
                            Status = container.State,
                            IpAddress = privateIP,
                            Port = port,
                            Created = container.Created
                        });
                    }
                }

                Console.WriteLine("Containers listed successfully");
                return containersList;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return new List<ServerInstance>();
            }
        }

        // Get container details
        // todo only get needed details donext
        public async Task<ServerInstance> FetchContainerDetails(DockerClient client, string exerciseName, string Uid)
        {
            Console.WriteLine("Getting container details...");
            try
            {
                string containerId = await GetContainerId(client, exerciseName);
                var containerDetails = await client.Containers.InspectContainerAsync(containerId);
                string usersList = await RunCommandInContainer(client, new List<string> { "cat", "/users.txt" }, containerDetails.ID);
                if (usersList != null && usersList.Contains(Uid))
                {
                    Console.WriteLine($"Container: {containerDetails.ID}, Image: {containerDetails.Image}, Users: {usersList}");
                }
                else
                {
                    Console.WriteLine("User not in the list");
                    throw new Exception("User not in the list");
                }
                ServerInstance container = new ServerInstance(containerDetails);

                // Additional processing can be done here if needed

                Console.WriteLine("Container details retrieved successfully");
                return container;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return null;
            }
        }

        // Run container
        public async Task StartContainer(DockerClient client, string containerId)
        {
            Console.WriteLine("Running container...");
            try
            {
                await client.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
                Console.WriteLine("Container started successfully");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }

        // Execute commands in the container
        // TODO: Not all commands will work with this method
        public async Task<string> RunCommandInContainer(DockerClient client, List<string> command, string containerId)
        {
            Console.WriteLine($"Client: {client}, Container ID: {containerId}, Command: {string.Join(" ", command)}");
            try
            {
                // Build a single shell command string for complex operations
                string shellCommand = command != null ? string.Join(" ", command) : string.Empty;

                // Create the exec instance with the shell command
                var execCreateResponse = await client.Exec.ExecCreateContainerAsync(containerId, new ContainerExecCreateParameters
                {
                    AttachStdin = true,
                    AttachStdout = true,
                    AttachStderr = true,
                    Tty = false,
                    WorkingDir = "/",
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

                    return outputBuilder.ToString();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return e.Message;
            }
        }

        // Pause the container
        // TODO check user id in run pause and stop container
        public async Task PauseContainer(DockerClient client, string containerId)
        {
            Console.WriteLine("Pausing container...");
            try
            {
                await client.Containers.PauseContainerAsync(containerId);
                Console.WriteLine("Container paused successfully");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }

        // get conteriner id from container name
        public async Task<string> GetContainerId(DockerClient client, string containerName)
        {
            IList<ContainerListResponse> containersList = await client.Containers.ListContainersAsync(new ContainersListParameters
            {
            });
            foreach (var container in containersList)
            {
                if (container.Names.Contains($"/{containerName}"))
                {
                    return container.ID;
                }
            }
            return string.Empty;
        }

    }
}