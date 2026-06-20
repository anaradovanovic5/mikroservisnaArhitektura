namespace EventService.Domains
{
    public class Dogadjaj
    {
        public int DogadjajId { get; set; }
        public string NazivDogadjaja { get; set; }
        public string Agenda { get; set; }
        public DateTime Datum { get; set; }
        public double Trajanje { get; set; }
        public double Cena { get; set; }
        public bool Otkazan { get; set; }
        public int LokacijaId { get; set; }   
        public int VrstaId { get; set; }
        public VrstaDogadjaja VrstaDogadjaja { get; set; }
    }
}
