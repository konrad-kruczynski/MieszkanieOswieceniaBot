using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot
{
    public class TemperatureSample : ISample<TemperatureSample>
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Temperature { get; set; }

        public bool CanSampleBeSquashed(TemperatureSample t)
        {
            return Temperature == t.Temperature;
        }

        public override string ToString()
        {
            return string.Format("[{1:R}: {2:##.#}°C]", Id, Date, Temperature);
        }
    }
}
