namespace EventService.Domains
{

    public class EventStoreEntry
    {
        public long Id { get; set; }
        public int AggregateId { get; set; }      
        public string EventType { get; set; }      
        public string Payload { get; set; }        
        public int Version { get; set; }          
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}