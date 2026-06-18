namespace RegistrationService
{
    public class RabbitMqOptions
    {
        public const string SectionName = "RabbitMq";
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string Exchange { get; set; } = "dogadjaji.events";
        public string RoutingKey { get; set; } = "dogadjaj.created";
        public string Queue { get; set; } = "dogadjaj.created.queue";
        public ushort PrefetchCount { get; set; } = 1;
    }
}
