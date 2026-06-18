namespace EventService.Domains
{
    public class OutboxMessage
    {
        public long Id { get; set; }
        public string EventType { get; set; }
        public string Payload { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
