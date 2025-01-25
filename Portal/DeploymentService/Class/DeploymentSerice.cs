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
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;

namespace Portal.DeploymentService.Class
{
    public class DeploymentService : IDeploymentService
    {
        private readonly DockerClient _dockerClient;

        // Constructor injection of DockerClient
        public DeploymentService(DockerClient dockerClient)
        {
            _dockerClient = dockerClient ?? throw new ArgumentNullException(nameof(dockerClient));
        }

        // create docker client using unix socket
        // public DockerClient CreateDockerClient()
        // {
        //     DockerClient client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock"))
        //         .CreateClient();
        //     return client;
        // }

        // Check or create image
        public async Task EnsureDockerImageExists(string imageName)
        {
            IList<ImagesListResponse> images = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters
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
                    await _dockerClient.Images.CreateImageAsync(
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
        public async Task<string> GetOrCreateContainerForUser(string imageName, string exerciseName, string Uid, string port, string ip, bool? isClient = false
        , string? clientPort = "0.0.0.0", int? MaxUsers = 10000, Dictionary<string, string> Variables = null)
        {
            // logs variablesobject
            foreach (var variable in Variables)
            {
                Console.WriteLine($"Variable: {variable.Key}, Value: {variable.Value}");
            }

            IList<ContainerListResponse> containersList = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters
            {
            });
            if (isClient == null)
            {
                isClient = false;
            }


            // print image uid and port
            if (containersList.Count == 0)
            {
                Console.WriteLine("No containers found, create a new one");
                return await InitializeContainer(imageName, exerciseName, Uid, port, ip, isClient, clientPort, Variables);

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
                        await StartContainer(container.ID);
                    }
                    // get users list 
                    string usersList = await RunCommandInContainer(new List<string> { "cat", "/users.txt" }, container.ID);
                    Console.WriteLine($"Users list: {usersList}");
                    // Check if the user is already in the list
                    // important must use the usersList without split here 
                    // check number of users in the container

                    if (usersList.Contains(Uid))
                    {
                        Console.WriteLine("User already in the list");
                        return container.ID;
                    }
                    else if (usersList.Split(',').Length >= MaxUsers)
                    {
                        Console.WriteLine("Max number of users reached");
                        await InitializeContainer(imageName, exerciseName, Uid, port, ip, isClient, clientPort, Variables);
                        return container.ID;
                    }

                    else
                    {
                        Console.WriteLine("User not in the list, add user to the list of container id " + container.ID);
                        // Add user to the list
                        string updatedUsersList = $"{usersList},{Uid}";
                        Console.WriteLine($"Updated users list: {updatedUsersList}");
                        await RunCommandInContainer(new List<string> { $"echo \"{updatedUsersList}\" > users.txt" }, container.ID);
                        //  chcek if client add the ip 
                        if (isClient ?? false)
                        {
                            // checked if their are other ip addresses in the container append the  ip address with comma sepration  else create a new file
                            string ipList = await RunCommandInContainer(new List<string> { "cat", "/users_ip.txt" }, container.ID);
                            if (ipList == null)
                            {
                                await RunCommandInContainer(new List<string> { $"echo \"{ip},{Uid}\" > users_ip.txt" }, container.ID);
                            }
                            else
                            {
                                await RunCommandInContainer(new List<string> { $"echo \"{ip},{Uid}\" >> users_ip.txt" }, container.ID);
                            }
                        }
                        return container.ID;
                    }

                }


            }
            if (!imageCreated)
            {
                Console.WriteLine("No containers with image specs found, create a new one");
                return await InitializeContainer(imageName, exerciseName, Uid, port, ip, isClient, clientPort, Variables);
            }

            return string.Empty;
        }

        // Create a new container
        public async Task<string> InitializeContainer(string imageName, string exerciseName, string Uid, string port, string ip, bool? isClient = false, string?
        clientPort = "0.0.0.0", Dictionary<string, string> Variables = null)
        {
            await EnsureDockerImageExists(imageName);
            int hostPort = FindAvailablePort();

            string hostPortStr = hostPort.ToString();
            Console.WriteLine($"Assigned host port: {port} ");
            // Log the input parameters
            Console.WriteLine($"Image Name: {imageName}, UID: {Uid}, Port: {port}");
            // Variables is json object that contains the variables that will be passed to the container
            // print it 
            if (Variables != null)
            {
                foreach (var variable in Variables)
                {
                    Console.WriteLine($"Variable: {variable.Key}, Value: {variable.Value}");
                }
            }
            // Define port bindings to map host port to container port
            var hostConfig = new HostConfig
            {
                NetworkMode = "host",
                PortBindings = new Dictionary<string, IList<PortBinding>>
        {
            {
                $"{port}/tcp", new List<PortBinding>
                {
                    new PortBinding
                    {
                        HostPort = hostPortStr // Host port (no "/tcp" needed)
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
            { $"{hostPortStr}/tcp", new EmptyStruct() }
            }
            };


            // random string of 5 chars 
            string randomString = Guid.NewGuid().ToString().Substring(0, 5);


            var createContainerParameters = new CreateContainerParameters(config)
            {
                HostConfig = hostConfig,
                Name = exerciseName + randomString

            };

            Console.WriteLine("Creating container...");
            try
            {
                // Create the container
                var createdContainer = await _dockerClient.Containers.CreateContainerAsync(createContainerParameters);
                Console.WriteLine($"Container created successfully with ID: {createdContainer.ID}");

                // Start the container immediately after creation
                bool started = await _dockerClient.Containers.StartContainerAsync(createdContainer.ID, new ContainerStartParameters());
                if (started)
                {
                    Console.WriteLine("Container started successfully.");
                    // add uid to the list of users
                    await RunCommandInContainer(new List<string> { $"echo \"{Uid}\" >> users.txt" }, createdContainer.ID);
                    string containerId = createdContainer.ID;
                    await RunCommandInContainer(new List<string> { $"echo \"{containerId}\" > containerid.txt" }, containerId);
                    Console.WriteLine("User added to the list of users");
                    if (Variables != null)
                    {
                        // check if any value is keyword random to create a random value
                        // get values from dictionary and replace the keyword random with a random value
                        foreach (var variable in Variables)
                        {
                            if (variable.Value == "random")
                            {
                                Variables[variable.Key] = Guid.NewGuid().ToString();
                            }
                        }
                    
                    // add the dectioanry to the container txt file 
                    await RunCommandInContainer(new List<string> { $"echo \"{JsonConvert.SerializeObject(Variables)}\" > variables.txt" }, createdContainer.ID);
                    }
                    if (isClient ?? false)
                    {
                        // checked if their are other ip addresses in the container append the  ip address with comma sepration  else create a new file
                        string ipList = await RunCommandInContainer(new List<string> { "cat", "/users_ip.txt" }, createdContainer.ID);
                        if (ipList == null)
                        {
                            await RunCommandInContainer(new List<string> { $"echo \"{ip},{Uid}\" > users_ip.txt" }, createdContainer.ID);
                        }
                        else
                        {
                            await RunCommandInContainer(new List<string> { $"echo \"{ip},{Uid}\" >> users_ip.txt" }, createdContainer.ID);
                        }
                        // add port number in port.txt 
                        await RunCommandInContainer(new List<string> { $"echo \"{clientPort}\" > port.txt" }, createdContainer.ID);

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

        public async Task<IList<string>> ListUsersContainer(string Uid)
        {

            IList<string> containersList = new List<string>();
            Console.WriteLine("Listing containers...");
            // print uid 
            Console.WriteLine($"UID: {Uid}");
            try
            {
                // List only running containers
                IList<ContainerListResponse> containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters
                {
                    Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                { "status", new Dictionary<string, bool> { { "running", true } } }
            }
                });


                foreach (var container in containers)
                {
                    // check if the user in the list of the container
                    string usersList = await RunCommandInContainer(new List<string> { "cat", "/users.txt" }, container.ID);
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
        public async Task<ServerInstance> FetchContainerDetails(string exerciseName, List<string> DisplayFields, string Uid)
        {
            Console.WriteLine("Getting container details...");
            try
            {
                string containerId = await GetContainerId(exerciseName);

                var containerDetails = await _dockerClient.Containers.InspectContainerAsync(containerId);
                // convert container detials to json format
                string containerDetailsJson = JsonConvert.SerializeObject(containerDetails, Formatting.Indented);
                // Console.WriteLine($"Container details: {containerDetailsJson}");
                string usersList = await RunCommandInContainer(new List<string> { "cat", "/users.txt" }, containerDetails.ID);
                if (usersList != null && usersList.Contains(Uid))
                {
                    Console.WriteLine($"Container: {containerDetails.ID}, Image: {containerDetails.Image}, Users: {usersList}");
                }
                else
                {
                    Console.WriteLine("User not in the list");
                    throw new Exception("User not in the list");
                }
                ServerInstance container = new ServerInstance(containerDetailsJson, DisplayFields);

                // Additional processing can be done here if needed

                Console.WriteLine("Container details retrieved successfully");
                return container;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error3 : {e.Message}");
                return new ServerInstance();
            }
        }

        // Run container
        public async Task StartContainer(string containerId)
        {
            Console.WriteLine("Running container...");
            try
            {
                // check if container is paused
                var container = await _dockerClient.Containers.InspectContainerAsync(containerId);
                if (container.State.Status == "paused")
                {
                    Console.WriteLine("Container is paused, start the container");
                    await _dockerClient.Containers.UnpauseContainerAsync(containerId);
                }
                else
                    await _dockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
                Console.WriteLine("Container started successfully");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error4 : {e.Message}");
            }
        }

        // Execute commands in the container
        // TODO: Not all commands will work with this method
        public async Task<string> RunCommandInContainer(List<string> command, string containerId)
        {


            Console.WriteLine($"Client: {_dockerClient}, Container ID: {containerId}, Command: {string.Join(" ", command)}");
            try
            {

                string shellCommand = command != null ? string.Join(" ", command) : string.Empty;
                if (!IsTxtFileOperationValid(shellCommand))
                {
                    throw new Exception("Command validation failed: Only .txt file operations (read, write, create) are allowed.");
                }
                var execCreateResponse = await _dockerClient.Exec.ExecCreateContainerAsync(containerId, new ContainerExecCreateParameters
                {
                    AttachStdin = true,
                    AttachStdout = true,
                    AttachStderr = true,
                    Tty = false,
                    WorkingDir = "/",
                    Cmd = new List<string> { "sh", "-c", shellCommand }
                });
                using (var stream = await _dockerClient.Exec.StartAndAttachContainerExecAsync(execCreateResponse.ID, false))
                {
                    var outputBuilder = new StringBuilder();
                    var buffer = new byte[4096];
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
        public async Task PauseContainer(string containerId)
        {
            Console.WriteLine("Pausing container...");
            try
            {
                await _dockerClient.Containers.PauseContainerAsync(containerId);
                Console.WriteLine("Container paused successfully");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error6 : {e.Message}");
            }
        }


        public async Task RemoveUserOrPauseContainer(string ContainerId, string Uid)
        {
            // get users list in this container 
            var usersList = await RunCommandInContainer(new List<string> { "cat", "/users.txt" }, ContainerId);
            // remove user from the list
            // trim anyspace 

            var updatedUsersList = usersList
                .Split(',')
                .Where(x => !x.Contains(Uid)) // Filter out items containing Uid
                .Where(x => !string.IsNullOrWhiteSpace(x)) // Remove spaces or empty lines
                .Select(x => x.Trim()) // Optional: Remove extra spaces around entries
                .ToList();
            // remove any spaces or empty lines 


            if (updatedUsersList.Count == 0)
            {
                await PauseContainer(ContainerId);
                Console.WriteLine("Container paused successfully");
            }
            else
            {
                await RunCommandInContainer(new List<string> { $"echo \"{string.Join(",", updatedUsersList)}\" > users.txt" }, ContainerId);

                Console.WriteLine("User removed from the list of users, container still running");
            }

        }
        // get container id from container name

        public async Task<string> GetContainerId(string containerName)
        {
            IList<ContainerListResponse> containersList = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters
            {
            });
            foreach (var container in containersList)
            {
                // check if the string is part of the name 
                foreach (var name in container.Names)
                {
                    if (name.Contains(containerName))
                    {
                        return container.ID;
                    }
                }
            }
            return string.Empty;
        }

        public async Task<string> ClientExercisePassed(string uid, string container_id, string ip)
        {
            // log inputs 
            Console.WriteLine($"Client evaluation for uid: {uid} and container id: {container_id} mn kalb el ClientExercisePassed");
            // get container id from container name

            // remove user's ip and uid from users_ip.txt
            string ipList = await RunCommandInContainer(new List<string> { "cat", "/users_ip.txt" }, container_id);
            if (ipList != null)
            {
                // remove ip the given one 
                var updatedIpList = ipList
                    .Split('\n')
                    .Where(x => !x.Contains(ip)) // Filter out items containing ip
                    .Where(x => !string.IsNullOrWhiteSpace(x)) // Remove spaces or empty lines
                    .Select(x => x.Trim()) // Optional: Remove extra spaces around entries
                    .ToList();
                Console.WriteLine("User removed from container's list of ip addresses");
                Console.WriteLine($"Updated ip list: {string.Join("\n", updatedIpList)}");
            }
            // remove user from the list of users
            await RemoveUserOrPauseContainer(container_id, uid);
            return "Client exercise passed successfully";

        }
        public int FindAvailablePort()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listener.Start();
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }

        }
        private bool IsTxtFileOperationValid(string command)
        {
            // Remove extra spaces and newlines for consistent validation
            command = command.Trim();

            // Define allowed operations for .txt files
            var allowedPatterns = new List<string>
                {
                    @"^cat\s+[\s\S]+\.txt$",                // Read a .txt file
                    @"^echo\s+[\s\S]+\s+>\s+[\w/]+\.txt$",  // Create/overwrite a .txt file
                    @"^echo\s+[\s\S]+\s+>>\s+[\w/]+\.txt$", // Append to a .txt file
                    @"^ls\s+[\w/]+\.txt$",                   // List .txt files (optional)
                };


            // Check if the command matches any allowed pattern
            foreach (var pattern in allowedPatterns)
            {
                if (Regex.IsMatch(command, pattern))
                {
                    return true;
                }
            }

            return false; // Command does not match allowed operations
        }
    }
}