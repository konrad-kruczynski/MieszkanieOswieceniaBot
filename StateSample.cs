using System;
using System.Linq;
using System.Text;

namespace MieszkanieOswieceniaBot
{
    public class StateSample : ISample
    {
        public StateSample(bool[] stateArray)
        {
            Lamp1 = stateArray[0];
            Lamp2 = stateArray[1];
            Lamp3 = stateArray[2];
            Speakers = stateArray[3];
            Date = DateTime.Now;
        }

        public StateSample()
        {
            
        }

        public bool IsEffectivelyMeaningless
        {
            get
            {
                return !GetStateArray().Any();
            }
        }

        public bool[] GetStateArray()
        {
            return new[] { Lamp1, Lamp2, Lamp3, Speakers };
        }

        public int Id { get; set; }
        public bool Lamp1 { get; set; }
        public bool Lamp2 { get; set; }
        public bool Lamp3 { get; set; }
        public bool Speakers { get; set; }
        public DateTime Date { get; set; }

        public override string ToString()
        {
            var result = new StringBuilder();
            result.Append(Lamp1 ? "○" : "●");
            result.Append(Lamp2 ? "○" : "●");
            result.Append(Lamp3 ? "○" : "●");
            result.Append(Speakers ? "○" : "●");
            return string.Format("[{0:R}: {1}]", Date, result);
        }
    }
}
