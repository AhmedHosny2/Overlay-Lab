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
        public Task EnsureDockerImageExists(string ImageName);
        public Task<string> GetOrCreateContainerForUser(string ImageName, string exerciseName, string Uid, string port, string ip, bool? isCLient, string? clientPort, int? MaxUsers);
        public Task<string> InitializeContainer(string ImageName, string exerciseName, string Uid, string port, string ip, bool? isCLient, string? clientPort);
        public Task<IList<string>> ListUsersContainer(string Uid);
        public Task<ServerInstance> FetchContainerDetails(string exerciseName, List<string> DisplayFields, string Uid, string ip);
        public Task StartContainer(string ContainerId);

        public Task<string> RunCommandInContainer(List<string> Command, string ContainerId);
        public Task PauseContainer(string ContainerId);
        public Task RemoveUserOrPauseContainer(string ContainerId, string Uid);
        public Task<string> ClientExercisePassed(string uid, string container_id, string ip);
    }
}