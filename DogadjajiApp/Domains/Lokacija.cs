namespace DogadjajiApp.Domains
{
    public class Lokacija
    {
        public int LokacijaId { get; set; }
        public string NazivLokacije { get; set; }
        public string Adresa { get; set; }
        public int Kapacitet { get; set; }

        public ICollection<Dogadjaj> Dogadjaji { get; set; }
    }
}
