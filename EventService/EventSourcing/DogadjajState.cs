using EventService.EventSourcing.Events;
using System.Text.Json;

namespace EventService.EventSourcing
{
   
    public class DogadjajState
    {
        public int DogadjajId { get; set; }
        public string NazivDogadjaja { get; set; } = "";
        public string Agenda { get; set; } = "";
        public DateTime Datum { get; set; }
        public double Trajanje { get; set; }
        public double Cena { get; set; }
        public int LokacijaId { get; set; }
        public int VrstaId { get; set; }
        public bool Otkazan { get; set; }
        public int Version { get; set; }

        public void Primeni(string eventType, string payloadJson)
        {
            switch (eventType)
            {
                case "DogadjajKreiran":
                    var kreiran = JsonSerializer.Deserialize<DogadjajKreiranEvent>(payloadJson)!;
                    DogadjajId = kreiran.DogadjajId;
                    NazivDogadjaja = kreiran.NazivDogadjaja;
                    Agenda = kreiran.Agenda;
                    Datum = kreiran.Datum;
                    Trajanje = kreiran.Trajanje;
                    Cena = kreiran.Cena;
                    LokacijaId = kreiran.LokacijaId;
                    VrstaId = kreiran.VrstaId;
                    break;

                case "DogadjajNazivIzmenjen":
                    var naziv = JsonSerializer.Deserialize<DogadjajNazivIzmenjenEvent>(payloadJson)!;
                    NazivDogadjaja = naziv.NoviNaziv;
                    break;

                case "DogadjajCenaPromenjena":
                    var cena = JsonSerializer.Deserialize<DogadjajCenaPromenjenaEvent>(payloadJson)!;
                    Cena = cena.NovaCena;
                    break;

                case "DogadjajTerminPromenjen":
                    var termin = JsonSerializer.Deserialize<DogadjajTerminPromenjenEvent>(payloadJson)!;
                    Datum = termin.NoviDatum;
                    break;

                case "DogadjajOtkazan":
                    Otkazan = true;
                    break;
            }

            Version++;
        }
    }
}