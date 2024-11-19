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
        public List<string> Args { get; set; } = new List<string>();
        public string Image { get; set; } = string.Empty;
        public string ResolvConfPath { get; set; } = string.Empty;
        public string HostnamePath { get; set; } = string.Empty;
        public string HostsPath { get; set; } = string.Empty;
        public string LogPath { get; set; } = string.Empty;
        public long RestartCount { get; set; }
        public string Driver { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string MountLabel { get; set; } = string.Empty;
        public string ProcessLabel { get; set; } = string.Empty;
        public string AppArmorProfile { get; set; } = string.Empty;
        public List<string> ExecIDs { get; set; } = new List<string>();
        public long? SizeRw { get; set; }
        public long? SizeRootFs { get; set; }
        public List<MountPoint> Mounts { get; set; } = new List<MountPoint>();

        // Properties from State
        public string StateStatus { get; set; } = string.Empty;
        public bool StateRunning { get; set; }
        public bool StatePaused { get; set; }
        public bool StateRestarting { get; set; }
        public bool StateOOMKilled { get; set; }
        public bool StateDead { get; set; }
        public long StatePid { get; set; }
        public long StateExitCode { get; set; }
        public string StateError { get; set; } = string.Empty;
        public string StateStartedAt { get; set; } = string.Empty;
        public string StateFinishedAt { get; set; } = string.Empty;

        // Properties from HostConfig
        public List<string> HostConfigBinds { get; set; } = new List<string>();
        public string HostConfigContainerIDFile { get; set; } = string.Empty;
        public LogConfig HostConfigLogConfig { get; set; }
        public string HostConfigNetworkMode { get; set; } = string.Empty;
        public Dictionary<string, IList<PortBinding>> HostConfigPortBindings { get; set; } = new Dictionary<string, IList<PortBinding>>();
        public RestartPolicy HostConfigRestartPolicy { get; set; }
        public bool HostConfigAutoRemove { get; set; }
        public string HostConfigVolumeDriver { get; set; } = string.Empty;
        public List<string> HostConfigVolumesFrom { get; set; } = new List<string>();
        // Include other HostConfig properties as needed

        // Properties from GraphDriver
        public string GraphDriverName { get; set; } = string.Empty;
        public Dictionary<string, string> GraphDriverData { get; set; } = new Dictionary<string, string>();

        // Properties from Config
        public string ConfigHostname { get; set; } = string.Empty;
        public string ConfigDomainname { get; set; } = string.Empty;
        public string ConfigUser { get; set; } = string.Empty;
        public bool ConfigAttachStdin { get; set; }
        public bool ConfigAttachStdout { get; set; }
        public bool ConfigAttachStderr { get; set; }
        public Dictionary<string, object> ConfigExposedPorts { get; set; } = new Dictionary<string, object>();
        public bool ConfigTty { get; set; }
        public bool ConfigOpenStdin { get; set; }
        public bool ConfigStdinOnce { get; set; }
        public List<string> ConfigEnv { get; set; } = new List<string>();
        public List<string> ConfigCmd { get; set; } = new List<string>();
        public HealthConfig ConfigHealthcheck { get; set; }
        public bool ConfigArgsEscaped { get; set; }
        public string ConfigImage { get; set; } = string.Empty;
        public Dictionary<string, object> ConfigVolumes { get; set; } = new Dictionary<string, object>();
        public string ConfigWorkingDir { get; set; } = string.Empty;
        public List<string> ConfigEntrypoint { get; set; } = new List<string>();
        public bool? ConfigNetworkDisabled { get; set; }
        public string ConfigMacAddress { get; set; } = string.Empty;
        public List<string> ConfigOnBuild { get; set; } = new List<string>();
        public Dictionary<string, string> ConfigLabels { get; set; } = new Dictionary<string, string>();
        public string ConfigStopSignal { get; set; } = string.Empty;
        public long? ConfigStopTimeout { get; set; }
        public List<string> ConfigShell { get; set; } = new List<string>();

        // Properties from NetworkSettings
        public string NetworkSettingsBridge { get; set; } = string.Empty;
        public string NetworkSettingsSandboxID { get; set; } = string.Empty;
        public bool NetworkSettingsHairpinMode { get; set; }
        public string NetworkSettingsLinkLocalIPv6Address { get; set; } = string.Empty;
        public long NetworkSettingsLinkLocalIPv6PrefixLen { get; set; }
        public IDictionary<string, IList<PortBinding>> NetworkSettingsPorts { get; set; }
        public string NetworkSettingsSandboxKey { get; set; } = string.Empty;
        public IList<Address> NetworkSettingsSecondaryIPAddresses { get; set; } = new List<Address>();
        public IList<Address> NetworkSettingsSecondaryIPv6Addresses { get; set; } = new List<Address>();
        public string NetworkSettingsEndpointID { get; set; } = string.Empty;
        public string NetworkSettingsGateway { get; set; } = string.Empty;
        public string NetworkSettingsGlobalIPv6Address { get; set; } = string.Empty;
        public long NetworkSettingsGlobalIPv6PrefixLen { get; set; }
        public string NetworkSettingsIPAddress { get; set; } = string.Empty;
        public long NetworkSettingsIPPrefixLen { get; set; }
        public string NetworkSettingsIPv6Gateway { get; set; } = string.Empty;
        public string NetworkSettingsMacAddress { get; set; } = string.Empty;
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
                this.Image = containerInspectResponse.Image ?? string.Empty;
                
                    this.Created = containerInspectResponse.Created;
                

                // Path and Args
                this.Path = containerInspectResponse.Path ?? string.Empty;
                this.Args = containerInspectResponse.Args?.ToList() ?? new List<string>();

                // State
                var state = containerInspectResponse.State;
                if (state != null)
                {
                    this.Status = state.Status ?? string.Empty;
                    this.StateStatus = state.Status ?? string.Empty;
                    this.StateRunning = state.Running;
                    this.StatePaused = state.Paused;
                    this.StateRestarting = state.Restarting;
                    this.StateOOMKilled = state.OOMKilled;
                    this.StateDead = state.Dead;
                    this.StatePid = state.Pid;
                    this.StateExitCode = state.ExitCode;
                    this.StateError = state.Error ?? string.Empty;
                    this.StateStartedAt = state.StartedAt ?? string.Empty;
                    this.StateFinishedAt = state.FinishedAt ?? string.Empty;
                }

                // Additional Paths
                this.ResolvConfPath = containerInspectResponse.ResolvConfPath ?? string.Empty;
                this.HostnamePath = containerInspectResponse.HostnamePath ?? string.Empty;
                this.HostsPath = containerInspectResponse.HostsPath ?? string.Empty;
                this.LogPath = containerInspectResponse.LogPath ?? string.Empty;

                // Restart Count and Driver
                this.RestartCount = containerInspectResponse.RestartCount;
                this.Driver = containerInspectResponse.Driver ?? string.Empty;
                this.Platform = containerInspectResponse.Platform ?? string.Empty;
                this.MountLabel = containerInspectResponse.MountLabel ?? string.Empty;
                this.ProcessLabel = containerInspectResponse.ProcessLabel ?? string.Empty;
                this.AppArmorProfile = containerInspectResponse.AppArmorProfile ?? string.Empty;
                this.ExecIDs = containerInspectResponse.ExecIDs?.ToList() ?? new List<string>();

                // HostConfig
                var hostConfig = containerInspectResponse.HostConfig;
                if (hostConfig != null)
                {
                    this.HostConfigBinds = hostConfig.Binds?.ToList() ?? new List<string>();
                    this.HostConfigContainerIDFile = hostConfig.ContainerIDFile ?? string.Empty;
                    this.HostConfigLogConfig = hostConfig.LogConfig;
                    this.HostConfigNetworkMode = hostConfig.NetworkMode ?? string.Empty;
                    // this.HostConfigPortBindings = hostConfig.PortBindings ?? new Dictionary<string, IList<PortBinding>>();
                    this.HostConfigRestartPolicy = hostConfig.RestartPolicy;
                    this.HostConfigAutoRemove = hostConfig.AutoRemove;
                    this.HostConfigVolumeDriver = hostConfig.VolumeDriver ?? string.Empty;
                    this.HostConfigVolumesFrom = hostConfig.VolumesFrom?.ToList() ?? new List<string>();
                    // Map other HostConfig properties as needed
                }

                // GraphDriver
                var graphDriver = containerInspectResponse.GraphDriver;
                if (graphDriver != null)
                {
                    this.GraphDriverName = graphDriver.Name ?? string.Empty;
                    this.GraphDriverData = (Dictionary<string, string>)(graphDriver.Data ?? new Dictionary<string, string>());
                }

                // Sizes and Mounts
                this.SizeRw = containerInspectResponse.SizeRw;
                this.SizeRootFs = containerInspectResponse.SizeRootFs;
                this.Mounts = containerInspectResponse.Mounts?.ToList() ?? new List<MountPoint>();

                // Config
                var config = containerInspectResponse.Config;
                if (config != null)
                {
                    this.ConfigHostname = config.Hostname ?? string.Empty;
                    this.ConfigDomainname = config.Domainname ?? string.Empty;
                    this.ConfigUser = config.User ?? string.Empty;
                    this.ConfigAttachStdin = config.AttachStdin;
                    this.ConfigAttachStdout = config.AttachStdout;
                    this.ConfigAttachStderr = config.AttachStderr;
                    // this.ConfigExposedPorts = config.ExposedPorts ?? new Dictionary<string, object>();
                    this.ConfigTty = config.Tty;
                    this.ConfigOpenStdin = config.OpenStdin;
                    this.ConfigStdinOnce = config.StdinOnce;
                    this.ConfigEnv = config.Env?.ToList() ?? new List<string>();
                    this.ConfigCmd = config.Cmd?.ToList() ?? new List<string>();
                    this.ConfigHealthcheck = config.Healthcheck;
                    this.ConfigArgsEscaped = config.ArgsEscaped;
                    this.ConfigImage = config.Image ?? string.Empty;
                    // this.ConfigVolumes = config.Volumes ?? new Dictionary<string, object>();
                    this.ConfigWorkingDir = config.WorkingDir ?? string.Empty;
                    this.ConfigEntrypoint = config.Entrypoint?.ToList() ?? new List<string>();
                    this.ConfigNetworkDisabled = config.NetworkDisabled;
                    this.ConfigMacAddress = config.MacAddress ?? string.Empty;
                    this.ConfigOnBuild = config.OnBuild?.ToList() ?? new List<string>();
                    // this.ConfigLabels = config.Labels ?? new Dictionary<string, string>();
                    this.ConfigStopSignal = config.StopSignal ?? string.Empty;
                    // this.ConfigStopTimeout = config.StopTimeout;
                    this.ConfigShell = config.Shell?.ToList() ?? new List<string>();
                }

                // NetworkSettings
                var netSettings = containerInspectResponse.NetworkSettings;
                if (netSettings != null)
                {
                    this.NetworkSettingsBridge = netSettings.Bridge ?? string.Empty;
                    this.NetworkSettingsSandboxID = netSettings.SandboxID ?? string.Empty;
                    this.NetworkSettingsHairpinMode = netSettings.HairpinMode;
                    this.NetworkSettingsLinkLocalIPv6Address = netSettings.LinkLocalIPv6Address ?? string.Empty;
                    this.NetworkSettingsLinkLocalIPv6PrefixLen = netSettings.LinkLocalIPv6PrefixLen;
                    this.NetworkSettingsPorts = netSettings.Ports ?? new Dictionary<string, IList<PortBinding>>();
                    this.NetworkSettingsSandboxKey = netSettings.SandboxKey ?? string.Empty;
                    this.NetworkSettingsSecondaryIPAddresses = netSettings.SecondaryIPAddresses?.ToList() ?? new List<Address>();
                    this.NetworkSettingsSecondaryIPv6Addresses = netSettings.SecondaryIPv6Addresses?.ToList() ?? new List<Address>();
                    this.NetworkSettingsEndpointID = netSettings.EndpointID ?? string.Empty;
                    this.NetworkSettingsGateway = netSettings.Gateway ?? string.Empty;
                    this.NetworkSettingsGlobalIPv6Address = netSettings.GlobalIPv6Address ?? string.Empty;
                    this.NetworkSettingsGlobalIPv6PrefixLen = netSettings.GlobalIPv6PrefixLen;
                    this.NetworkSettingsIPAddress = netSettings.IPAddress ?? string.Empty;
                    this.NetworkSettingsIPPrefixLen = netSettings.IPPrefixLen;
                    this.NetworkSettingsIPv6Gateway = netSettings.IPv6Gateway ?? string.Empty;
                    this.NetworkSettingsMacAddress = netSettings.MacAddress ?? string.Empty;
                    this.NetworkSettingsNetworks = netSettings.Networks ?? new Dictionary<string, EndpointSettings>();
                }

                // Set IPAddress and Port
                var firstNetwork = netSettings?.Networks?.Values.FirstOrDefault();
                if (firstNetwork != null)
                {
                    this.IpAddress = firstNetwork.IPAddress ?? string.Empty;
                 // get port from config 
                 if(config.ExposedPorts.Count > 0)
                 {
                     var port = config.ExposedPorts.Keys.First();
                     this.Port = port;
                 }
                }
            }
        }
    }
}