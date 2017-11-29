using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot
{
    public class TemperatureSample
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Temperature { get; set; }
    }
}
