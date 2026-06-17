namespace DogadjajiApp.Models.Dogadjaji
{
    public class DogadjajViewModel
    {
        public int Id { get; set; }
        public string Naziv { get; set; }
        public string Agenda { get; set; }
        public DateTime DatumVreme { get; set; }
        public int TrajanjePominuta { get; set; }
        public decimal CenaKotizacije { get; set; }
        public int LokacijaId { get; set; }
        public string NazivLokacije { get; set; }
        public List<int> IzabranPredavaciIds { get; set; } = new();
        public List<string> ImenaPredavaca { get; set; } = new();
    }
}
