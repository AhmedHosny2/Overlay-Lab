// create interface for deployment service

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
        public DockerClient ConnectToDocker();
        public Task CheckOrCreateImage(DockerClient client, string ImageName);
        public Task<string> CreateContainer(DockerClient client, string ImageName, string DeploymentName);
        public Task<IList<ServerInstance>> ListContainers(DockerClient client);
        public Task RunContainer(DockerClient client, string ContainerId);

        public Task<StringBuilder> ExecuteCommand(DockerClient client, List<string> Command, string ContainerId);
        public Task PauseContainer(DockerClient client, string ContainerId);
        // todo delete container 
    }
}