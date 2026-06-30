namespace LocationService.Domains
{
    public class Lokacija
    {
        public int LokacijaId { get; set; }
        public string NazivLokacije { get; set; }
        public string Adresa { get; set; }
        public int Kapacitet { get; set; }
        public int BrojRezervacija { get; set; } = 0; // koliko je puta lokacija trenutno rezervisana (Saga kompenzacija)
    }
}