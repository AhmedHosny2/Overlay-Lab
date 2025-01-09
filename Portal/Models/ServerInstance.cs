using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Docker.DotNet.Models;
using System.Text.Json.Nodes;
using System.Net.NetworkInformation;

namespace Portal.Models
{
    public class ServerInstance
    {
        // Basic Container Information
        public string ID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public DateTime Created { get; set; } = DateTime.MinValue;
        public Dictionary<string, string> map = new Dictionary<string, string>();
        // Additional Attributes from ContainerInspectResponse
        public string Config { get; set; } = string.Empty;
        public string StateStatus { get; set; } = string.Empty;
        public NetworkSettings NetworkSettingsNetworks { get; set; }
        public ServerInstance()
        {
        }

        public ServerInstance(string containerInspectResponse, List<string> displayFields, string ip)
        {
            if (containerInspectResponse == null || displayFields == null)
                return;

            var portBindingsJson = GetValueFromJson(containerInspectResponse, "HostConfig.PortBindings");
            if (string.IsNullOrEmpty(portBindingsJson))
            {
                Console.WriteLine("PortBindings structure is null or empty.");
                return;
            }

            try
            {
                // Deserialize the PortBindings JSON into a dictionary
                var portBindingsObject = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, string>>>>(portBindingsJson);

                if (portBindingsObject != null && portBindingsObject.Count > 0)
                {
                    var dynamicPortKey = portBindingsObject.Keys.FirstOrDefault();

                    if (!string.IsNullOrEmpty(dynamicPortKey))
                    {
                        var hostPortEntry = portBindingsObject[dynamicPortKey]?.FirstOrDefault();
                        if (hostPortEntry != null && hostPortEntry.ContainsKey("HostPort"))
                        {
                            this.Port = hostPortEntry["HostPort"];
                            Console.WriteLine($"Extracted HostPort for dynamic container port {dynamicPortKey}: {this.Port}");
                        }
                        else
                        {
                            Console.WriteLine($"No HostPort found for the dynamic container port {dynamicPortKey}.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No dynamic port key found in PortBindings.");
                    }
                }
                else
                {
                    Console.WriteLine("PortBindings structure is null or empty.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing PortBindings: {ex.Message}");
            }

            this.Image = GetValueFromJson(containerInspectResponse, "Config.Image");
            this.ID = GetValueFromJson(containerInspectResponse, "Id");
            this.IpAddress = GetVmIpAddress();
            foreach (var field in displayFields)
            {
                var key = field.Split('.').Last();
                if (key == "*")
                {
                    key = field.Split('.').Reverse().Skip(1).First();
                }
                var value = GetValueFromJson(containerInspectResponse, field);
                map[key] = value;
            }
         
        }

        public static string GetVmIpAddress()
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())

            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (var ipAddress in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (ipAddress.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                            ipAddress.Address.ToString().StartsWith("10."))
                        {
                            return ipAddress.Address.ToString();
                        }
                    }
                }
            }
            return "No VM IP starting with 10. found.";
        }
        public static string GetValueFromJson(string json, string keyPath)
        {
            try
            {
                var jsonObject = JsonNode.Parse(json);

                if (jsonObject == null)
                    return null;

                var keys = keyPath.Split('.');
                JsonNode currentNode = jsonObject;

                foreach (var key in keys)
                {
                    if (key == "*")
                    {
                        return currentNode.ToString();
                    }
                    currentNode = currentNode?[key];

                    if (currentNode == null)
                    {
                        return null;
                    }
                }

                return currentNode?.ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }


    }
}