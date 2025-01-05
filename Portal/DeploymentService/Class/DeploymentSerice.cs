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
using Newtonsoft.Json;

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
                    Console.WriteLine($"Error1 : {e.Message}");
                }
            }
            else
            {
                Console.WriteLine("Image found");
            }
        }

        // Create container or add user
        // each time user tries to connect/deploy to the container, this method will be called
        public async Task<string> GetOrCreateContainerForUser(DockerClient client, string imageName, string exerciseName, string Uid, string port, string ip, bool isClient)
        {
            IList<ContainerListResponse> containersList = await client.Containers.ListContainersAsync(new ContainersListParameters
            {
            });


            // print image uid and port
            if (containersList.Count == 0)
            {
                Console.WriteLine("No containers found, create a new one");
                return await InitializeContainer(client, imageName, exerciseName, Uid, port, ip, isClient);

            }
            bool imageCreated = false;
            foreach (var container in containersList)
            {
                // TODO: Implement more complex logic for container selection
                if (container.Image.Equals(imageName, StringComparison.OrdinalIgnoreCase))
                {
                    imageCreated = true;
                    // check if container is paused
                    if (container.State == "paused")
                    {
                        Console.WriteLine("Container is paused, start the container");
                        await StartContainer(client, container.ID);
                    }
                    // get users list 
                    string usersList = await RunCommandInContainer(client, new List<string> { "cat", "/users.txt" }, container.ID);
                    Console.WriteLine($"Users list: {usersList}");
                    // Check if the user is already in the list
                    // important must use the usersList without split here 
                    if (usersList.Contains(Uid))
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
                        //  chcek if client add the ip 
                        if (isClient)
                        {
                            // checked if their are other ip addresses in the container append the  ip address with comma sepration  else create a new file
                            string ipList = await RunCommandInContainer(client, new List<string> { "cat", "/users_ip.txt" }, container.ID);
                            if (ipList == null)
                            {
                                await RunCommandInContainer(client, new List<string> { $"echo \"{ip}\" > users_ip.txt" }, container.ID);
                            }
                            else
                            {
                                await RunCommandInContainer(client, new List<string> { $"echo \"{ip}\" >> users_ip.txt" }, container.ID);
                            }
                        }
                        return container.ID;
                    }

                }


            }
            if (!imageCreated)
            {
                Console.WriteLine("No containers with image specs found, create a new one");
                return await InitializeContainer(client, imageName, exerciseName, Uid, port, ip, isClient);
            }

            return string.Empty;
        }

        // Create a new container
        public async Task<string> InitializeContainer(DockerClient client, string imageName, string exerciseName, string Uid, string port, string ip, bool isClient)
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
                    if (isClient)
                    {
                        // checked if their are other ip addresses in the container append the  ip address with comma sepration  else create a new file
                        string ipList = await RunCommandInContainer(client, new List<string> { "cat", "/users_ip.txt" }, createdContainer.ID);
                        if (ipList == null)
                        {
                            await RunCommandInContainer(client, new List<string> { $"echo \"{ip}\" > users_ip.txt" }, createdContainer.ID);
                        }
                        else
                        {
                            await RunCommandInContainer(client, new List<string> { $"echo \"{ip}\" >> users_ip.txt" }, createdContainer.ID);
                        }
                        // add port number in port.txt 
                        await RunCommandInContainer(client, new List<string> { $"echo \"{port}\" > port.txt" }, createdContainer.ID);

                    }
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

        public async Task<IList<string>> ListUsersContainer(DockerClient client, string Uid)
        {

            IList<string> containersList = new List<string>();
            Console.WriteLine("Listing containers...");
            // print uid 
            Console.WriteLine($"UID: {Uid}");
            try
            {
                // List only running containers
                IList<ContainerListResponse> containers = await client.Containers.ListContainersAsync(new ContainersListParameters
                {
                    Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                { "status", new Dictionary<string, bool> { { "running", true } } }
            }
                });


                foreach (var container in containers)
                {
                    // check if the user in the list of the container
                    string usersList = await RunCommandInContainer(client, new List<string> { "cat", "/users.txt" }, container.ID);
                    if (usersList == null || !usersList.Contains(Uid))
                    {
                        continue;
                    }
                    string imageName = container.Image.Split("@sha256")[0];
                    containersList.Add(imageName);


                }

                Console.WriteLine("Containers listed successfully");
                return containersList;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error2 : {e.Message}");
                return new List<string>();
            }
        }

        // Get container details
        // todo only get needed details donext
        public async Task<ServerInstance> FetchContainerDetails(DockerClient client, string exerciseName, List<string> DisplayFields, string Uid, string ip)
        {
            Console.WriteLine("Getting container details...");
            try
            {
                string containerId = await GetContainerId(client, exerciseName);

                var containerDetails = await client.Containers.InspectContainerAsync(containerId);
                // convert container detials to json format
                string containerDetailsJson = JsonConvert.SerializeObject(containerDetails, Formatting.Indented);
                // Console.WriteLine($"Container details: {containerDetailsJson}");
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
                ServerInstance container = new ServerInstance(containerDetailsJson, DisplayFields, ip);

                // Additional processing can be done here if needed

                Console.WriteLine("Container details retrieved successfully");
                return container;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error3 : {e.Message}");
                return null;
            }
        }

        // Run container
        public async Task StartContainer(DockerClient client, string containerId)
        {
            Console.WriteLine("Running container...");
            try
            {
                // check if container is paused
                var container = await client.Containers.InspectContainerAsync(containerId);
                if (container.State.Status == "paused")
                {
                    Console.WriteLine("Container is paused, start the container");
                    await client.Containers.UnpauseContainerAsync(containerId);
                }
                else
                    await client.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
                Console.WriteLine("Container started successfully");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error4 : {e.Message}");
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
                Console.WriteLine($"Error5 : {e.Message}");
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
                Console.WriteLine($"Error6 : {e.Message}");
            }
        }


        public async Task RemoveUserOrPauseContainer(DockerClient client, string ContainerId, string Uid)
        {
            // get users list in this container 
            var usersList = await RunCommandInContainer(client, new List<string> { "cat", "/users.txt" }, ContainerId);
            // remove user from the list
            // trim anyspace 

            var updatedUsersList = usersList
                .Split(',')
                .Where(x => !x.Contains(Uid)) // Filter out items containing Uid
                .Where(x => !string.IsNullOrWhiteSpace(x)) // Remove spaces or empty lines
                .Select(x => x.Trim()) // Optional: Remove extra spaces around entries
                .ToList();
            // remove any spaces or empty lines 


            // update the list of users
            await RunCommandInContainer(client, new List<string> { $"echo \"{string.Join(",", updatedUsersList)}\" > users.txt" }, ContainerId);
            Console.WriteLine("User removed from container's  list of users");
            // print new list 
            Console.WriteLine($"Updated users list: {string.Join(",", updatedUsersList)}");
            // if the list is empty pause the container
            //TODO we can either stop the container to save computational resources or pause it to save the state of the container and better UX
            if (updatedUsersList.Count == 0)
            {
                await PauseContainer(client, ContainerId);
                Console.WriteLine("Container paused successfully");
            }
            else
            {
                Console.WriteLine("User removed from the list of users, container still running");
            }

        }
        // get container id from container name

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