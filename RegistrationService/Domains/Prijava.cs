namespace RegistrationService.Domains
{
    public class Prijava
    {
        public int Id { get; set; }
        public string ImeUcesnika { get; set; }
        public string PrezimeUcesnika { get; set; }
        public string EmailUcesnika { get; set; }
        public DateTime DatumPrijave { get; set; }
        public int DogadjajId { get; set; }
    }
}
