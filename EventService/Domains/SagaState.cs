namespace EventService.Domains
{
    public class SagaState
    {
        public int Id { get; set; }
        public Guid SagaId { get; set; }
        public string Status { get; set; } = "Started"; // Started, Completed, Compensating, Failed
        public int? DogadjajId { get; set; }
        public int? LokacijaId { get; set; }
        public string? CurrentStep { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
