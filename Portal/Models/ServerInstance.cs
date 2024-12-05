using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Docker.DotNet.Models;
using System.Text.Json.Nodes;

namespace Portal.Models
{
    public class ServerInstance
    {
        // Basic Container Information
        public string ID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty; // Mapped from Image
        public string IpAddress { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public DateTime Created { get; set; } = DateTime.MinValue;
        public Dictionary<string, string> map = new Dictionary<string, string>();
        // Additional Attributes from ContainerInspectResponse
        public string Config { get; set; } = string.Empty;
        // public string MountLabel { get; set; } = string.Empty;
        // public string ProcessLabel { get; set; } = string.Empty;
        // public List<MountPoint> Mounts { get; set; } = new List<MountPoint>();

        // Properties from State
        public string StateStatus { get; set; } = string.Empty;

        // Properties from HostConfig

        // Properties from NetworkSettings
        public NetworkSettings NetworkSettingsNetworks { get; set; }

        // Constructor to map ContainerInspectResponse to ServerInstance
        public ServerInstance()
        {
        }

        public ServerInstance(string containerInspectResponse, List<string> displayFields)
        {
            if (containerInspectResponse == null || displayFields == null)
                return;

            var serverInstanceType = typeof(ServerInstance);
            // in all cases get image and port number by defualt 

            // this.Port = containerInspectResponse.Config.ExposedPorts.Keys.First();
            // this.Image = containerInspectResponse.Config.Image;
            // get image port and id from jsgon 
            var Allports = GetValueFromJson(containerInspectResponse, "Config.ExposedPorts.*");
            Allports = Allports.Split('/')[0];
            // remove " and {
            Allports = Allports.Replace("\"", "");
            Allports = Allports.Replace("{", "");
            Allports = Allports.Replace("}", "");
            this.Port = Allports;
            this.Image = GetValueFromJson(containerInspectResponse, "Config.Image");
            this.ID = GetValueFromJson(containerInspectResponse, "Id");
            this.IpAddress = GetValueFromJson(containerInspectResponse, "NetworkSettings.IPAddress");

            foreach (var field in displayFields)
            {
                Console.WriteLine("Field: " + field);
                // get display filed key, last part of the string after the last dot if not * 
                var key = field.Split('.').Last();
                if (key == "*")
                {
                    // get elment befor last 
                    key = field.Split('.').Reverse().Skip(1).First();
                }
                Console.WriteLine("Key: " + key);
                // get value from json
                var value = GetValueFromJson(containerInspectResponse, field);
                Console.WriteLine("Value: " + value);
                // if (value != null)
                // {
                //     // set value to the object 
                //     serverInstanceType.GetProperty(key)?.SetValue(this, value);
                // }
                map[key] = value;



            }
            // log map
            foreach (var item in map)
            {
                Console.WriteLine("Key: " + item.Key + " Value: " + item.Value);
            }

            Console.WriteLine("ServerInstance created");

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