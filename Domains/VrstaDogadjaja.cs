namespace DogadjajiApp.Domains
{
    public class VrstaDogadjaja
    {
        public int VrstaId { get; set; }
        public string Naziv { get; set; }
        public string Opis { get; set; }

        public ICollection<Dogadjaj> Dogadjaji { get; set; }
    }
}
