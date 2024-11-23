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
        public async Task<string> GetOrCreateContainerForUser(DockerClient client, string imageName, string Uid, string port)
        {
            IList<ServerInstance> containersList = await ListContainers(client, Uid);
            Console.WriteLine("$imageName: {imageName}, Uid: {Uid}, port: {port}");
            foreach (var container in containersList)
            {
                Console.WriteLine($"Container: {container.InstanceId}, Image: {container.ServerType}, Users: {container.ConfigLabels["users"]}");
                // TODO: Implement more complex logic for container selection
                if (container.ServerType.Equals(imageName, StringComparison.OrdinalIgnoreCase))
                {
                    if (container.ConfigLabels.ContainsKey("users"))
                    {
                        // Check if the user is already in the list
                        var users = container.ConfigLabels["users"].Split(',');
                        if (users.Contains(Uid))
                        {
                            Console.WriteLine("User already in the list");
                            return container.InstanceId;
                        }
                        else
                        {
                            Console.WriteLine("User not in the list");
                            // Add user to the list
                            container.ConfigLabels["users"] += $",{Uid}";
                            return container.InstanceId;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Image not found");
                    // Create new container
                    return await InitializeContainer(client, imageName, Uid, port);
                }
            }

            return string.Empty;
        }

        // Create a new container
        public async Task<string> InitializeContainer(DockerClient client, string imageName, string Uid, string port)
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
                Labels = new Dictionary<string, string>
                {
                    { "users", Uid }
                },
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    { $"{port}/tcp", new EmptyStruct() }
                },
                // Set the command to run.
                // If you want to use the image's default CMD, you can omit this.
                // Here, it's set to keep the container running indefinitely.
                // Cmd = new List<string> { "/bin/sh", "-c", "sleep infinity" }
            };

            // Add additional labels, such as a creation timestamp
            string timestamp = DateTime.UtcNow.ToString("o"); // ISO 8601 format
            var createContainerParameters = new CreateContainerParameters(config)
            {
                HostConfig = hostConfig,
                Labels = new Dictionary<string, string>
                {
                    { "created_at", timestamp }
                }
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
            try
            {
                IList<ContainerListResponse> containers = await client.Containers.ListContainersAsync(new ContainersListParameters
                {
                    // only show container with label list users that contain the user id
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        {
                            "label", new Dictionary<string, bool>
                            {
                                { "users", true }
                            }
                        }
                    }
                });

                foreach (var container in containers)
                {
                    var firstNetwork = container.NetworkSettings.Networks.Values.FirstOrDefault();
                    Console.WriteLine($"Container: {container.ID}, Image: {container.Image}, Users: {container.Labels["users"]}");
                    if (firstNetwork != null && container.Labels["users"].Contains(Uid))
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
        public async Task<ServerInstance> FetchContainerDetails(DockerClient client, string containerId, string Uid)
        {
            Console.WriteLine("Getting container details...");
            try
            {
                var containerDetails = await client.Containers.InspectContainerAsync(containerId);
                if (containerDetails.Config.Labels["users"].Contains(Uid))
                {
                    Console.WriteLine($"Container: {containerDetails.ID}, Image: {containerDetails.Image}, Users: {containerDetails.Config.Labels["users"]}");
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
        public async Task<StringBuilder> RunCommandInContainer(DockerClient client, List<string> command, string containerId)
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
                Console.WriteLine($"Error: {e.Message}");
                return new StringBuilder(e.Message);
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
    }
}