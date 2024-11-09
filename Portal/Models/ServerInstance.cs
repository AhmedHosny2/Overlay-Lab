namespace Portal.Models
{
    public class ServerInstance
    {
        public string InstanceId { get; set; }
        public string ServerType { get; set; }
        public string Status { get; set; }
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public DateTime Created { get; set; }
    }
}