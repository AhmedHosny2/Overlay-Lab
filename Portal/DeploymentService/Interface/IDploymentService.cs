using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Portal.Models;
namespace Portal.DeploymentService.Interface
{
    public interface IDeploymentService
    {
        public DockerClient CreateDockerClient();
        public Task EnsureDockerImageExists(DockerClient client, string ImageName);
        public Task<string> GetOrCreateContainerForUser(DockerClient client, string ImageName, string exerciseName, string Uid, string port, string ip, bool? isCLient, string? clientPort, int? MaxUsers);
        public Task<string> InitializeContainer(DockerClient client, string ImageName, string exerciseName, string Uid, string port, string ip, bool? isCLient, string? clientPort);
        public Task<IList<string>> ListUsersContainer(DockerClient client, string Uid);
        public Task<ServerInstance> FetchContainerDetails(DockerClient client, string exerciseName, List<string> DisplayFields, string Uid, string ip);
        public Task StartContainer(DockerClient client, string ContainerId);

        public Task<string> RunCommandInContainer(DockerClient client, List<string> Command, string ContainerId);
        public Task PauseContainer(DockerClient client, string ContainerId);
        public Task RemoveUserOrPauseContainer(DockerClient client, string ContainerId, string Uid);
        public Task<string> ClientExercisePassed(string uid, string container_id);
    }
}