using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Docker.DotNet.Models;

namespace Portal.Models
{
    public class ServerInstance
    {
        // Basic Container Information
        public string InstanceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ServerType { get; set; } = string.Empty; // Mapped from Image
        public string IpAddress { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public DateTime Created { get; set; } = DateTime.MinValue;

        // Additional Attributes from ContainerInspectResponse
        public string Path { get; set; } = string.Empty;
        public long RestartCount { get; set; }
        // public string MountLabel { get; set; } = string.Empty;
        // public string ProcessLabel { get; set; } = string.Empty;
        // public List<MountPoint> Mounts { get; set; } = new List<MountPoint>();

        // Properties from State
        public string StateStatus { get; set; } = string.Empty;

        // Properties from HostConfig

        // Properties from NetworkSettings
        public IDictionary<string, EndpointSettings> NetworkSettingsNetworks { get; set; } = new Dictionary<string, EndpointSettings>();

        // Constructor to map ContainerInspectResponse to ServerInstance
        public ServerInstance()
        {
        }

        public ServerInstance(ContainerInspectResponse containerInspectResponse)
        {
            if (containerInspectResponse != null)
            {
                // Basic Information
                this.InstanceId = containerInspectResponse.ID ?? string.Empty;
                this.Name = containerInspectResponse.Name ?? string.Empty;

                this.Created = containerInspectResponse.Created;
                this.ServerType = containerInspectResponse.Config.Image ?? string.Empty;

                // Path and Args
                this.Path = containerInspectResponse.Path ?? string.Empty;

                // State
                var state = containerInspectResponse.State;
                if (state != null)
                {
                    this.Status = state.Status ?? string.Empty;
                }

                this.RestartCount = containerInspectResponse.RestartCount;
                // this.MountLabel = containerInspectResponse.MountLabel ?? string.Empty;


                // NetworkSettings
                var netSettings = containerInspectResponse.NetworkSettings;
                if (netSettings != null)
                {

                    this.NetworkSettingsNetworks = netSettings.Networks ?? new Dictionary<string, EndpointSettings>();
                }

                // Set IPAddress and Port
                var firstNetwork = netSettings?.Networks?.Values.FirstOrDefault();
                if (firstNetwork != null)
                {
                    this.IpAddress = firstNetwork.IPAddress ?? string.Empty;
                    // get port from config 
                    if (containerInspectResponse.Config.ExposedPorts.Count > 0)
                    {
                        var port = containerInspectResponse.Config.ExposedPorts.Keys.First();
                        this.Port = port;
                    }
                }
            }
        }
    }
}