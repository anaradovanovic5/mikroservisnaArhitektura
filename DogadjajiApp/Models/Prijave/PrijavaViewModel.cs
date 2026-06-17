namespace DogadjajiApp.Models.Prijave
{
    public class PrijavaViewModel
    {
        public int Id { get; set; }
        public string ImeUcesnika { get; set; }
        public string PrezimeUcesnika { get; set; }
        public string EmailUcesnika { get; set; }
        public DateTime DatumPrijave { get; set; }
        public int DogadjajId { get; set; }
        public string NazivDogadjaja { get; set; }
    }
}
