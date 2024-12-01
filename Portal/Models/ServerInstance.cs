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
        public string ID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty; // Mapped from Image
        public string IpAddress { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public DateTime Created { get; set; } = DateTime.MinValue;


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

        public ServerInstance(ContainerInspectResponse containerInspectResponse, List<string> displayFields)
        {
            if (containerInspectResponse == null || displayFields == null)
                return;

            var serverInstanceType = typeof(ServerInstance);
            // set port 
            this.Port = containerInspectResponse.Config.ExposedPorts.Keys.First();
            this.Image = containerInspectResponse.Config.Image;
            foreach (var field in displayFields)
            {
                Console.WriteLine("Field: " + field);
                // Get the property info of the field in ServerInstance
                var serverInstanceProperty = serverInstanceType.GetProperty(field);
                Console.WriteLine("ServerInstanceProperty: " + serverInstanceProperty);
                if (serverInstanceProperty == null)
                    continue; // Skip if the property doesn't exist

                // Get the value from ContainerInspectResponse
                var value = GetPropertyValue(containerInspectResponse, field);
                Console.WriteLine("value : " + value);

                // Set the value to the ServerInstance property if not null
                if (value != null)
                {
                    try
                    {
                        // Convert the value to the property type if necessary
                        var convertedValue = Convert.ChangeType(value, serverInstanceProperty.PropertyType);
                        serverInstanceProperty.SetValue(this, convertedValue);
                    }
                    catch
                    {
                        // Handle conversion errors if necessary
                    }
                }
            }

            Console.WriteLine("ServerInstance created");

        }

        public static object GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null || string.IsNullOrEmpty(propertyName))
                return null;

            var parts = propertyName.Split('.');
            foreach (var part in parts)
            {
                if (obj == null)
                    return null;

                var type = obj.GetType();
                var propertyInfo = type.GetProperty(part);
                if (propertyInfo == null)
                    return null;

                obj = propertyInfo.GetValue(obj, null);
            }
            return obj;
        }
    }
}