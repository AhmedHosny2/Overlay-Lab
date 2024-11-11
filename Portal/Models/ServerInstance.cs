namespace Portal.Models
{
    public class ServerInstance
    {
        public string InstanceId { get; set; } = string.Empty;
        public string ServerType { get; set; }= string.Empty;
        public string Status { get; set; }= string.Empty;
        public string IpAddress { get; set; }= string.Empty;
        public string Port { get; set; }= string.Empty;
        public DateTime Created { get; set; }= DateTime.MinValue;
        public string Name { get; set; }= string.Empty;
        
    }
}