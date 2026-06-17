namespace DogadjajiApp.Domains
{
    public class DogadjajPredavac
    {
        public int Id { get; set; }
        public DateTime DatumIVreme { get; set; }
        public string Tema { get; set; }

        public int DogadjajId { get; set; }
        public Dogadjaj Dogadjaj { get; set; }

        public int PredavacId { get; set; }
        public Predavac Predavac { get; set; }
    }
}
