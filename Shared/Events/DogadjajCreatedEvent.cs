using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Events
{
    public class DogadjajCreatedEvent
    {
        public int DogadjajId { get; set; }
        public string NazivDogadjaja { get; set; }
        public DateTime Datum { get; set; }
    }
}
