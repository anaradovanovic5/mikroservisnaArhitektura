namespace EventService.EventSourcing
{
    public class DogadjajSnapshot
    {
        public long Id { get; set; }
        public int AggregateId { get; set; }
        public string NazivDogadjaja { get; set; } = "";
        public string Agenda { get; set; } = "";
        public DateTime Datum { get; set; }
        public double Trajanje { get; set; }
        public double Cena { get; set; }
        public int LokacijaId { get; set; }
        public int VrstaId { get; set; }
        public bool Otkazan { get; set; }
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}