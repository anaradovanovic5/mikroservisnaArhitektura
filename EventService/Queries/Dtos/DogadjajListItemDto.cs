namespace EventService.Queries.Dtos
{
    public class DogadjajListItemDto
    {
        public int DogadjajId { get; set; }
        public string NazivDogadjaja { get; set; } = "";
        public DateTime Datum { get; set; }
        public double Cena { get; set; }
        public bool Otkazan { get; set; }
    }
}