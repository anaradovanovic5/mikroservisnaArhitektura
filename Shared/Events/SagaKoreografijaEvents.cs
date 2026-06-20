namespace Shared.Events
{
    // EventService objavljuje — lokacija i registracija treba da reaguju
    public class DogadjajKreiranSagaEvent
    {
        public Guid SagaId { get; set; }
        public int DogadjajId { get; set; }
        public string NazivDogadjaja { get; set; } = "";
        public int LokacijaId { get; set; }
    }

    // LocationService objavljuje nakon uspesne rezervacije
    public class LokacijaRezervacisanaEvent
    {
        public Guid SagaId { get; set; }
        public int DogadjajId { get; set; }
        public int LokacijaId { get; set; }
    }

    // LocationService objavljuje ako rezervacija nije uspela
    public class LokacijaRezervacijaNeuspesnaEvent
    {
        public Guid SagaId { get; set; }
        public int DogadjajId { get; set; }
        public string Razlog { get; set; } = "";
    }

    // RegistrationService objavljuje nakon uspesne prijave
    public class PrijavaKreiranaEvent
    {
        public Guid SagaId { get; set; }
        public int DogadjajId { get; set; }
    }

    // RegistrationService objavljuje ako prijava nije uspela — LocationService treba da kompenzuje
    public class PrijavaNeuspesnaEvent
    {
        public Guid SagaId { get; set; }
        public int DogadjajId { get; set; }
        public int LokacijaId { get; set; }
        public string Razlog { get; set; } = "";
    }
}