using System;

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

        public int Id { get; set; }
        public bool Lamp1 { get; set; }
        public bool Lamp2 { get; set; }
        public bool Lamp3 { get; set; }
        public bool Speakers { get; set; }
        public DateTime Date { get; set; }

        public override string ToString()
        {
            return string.Format("[{4:R}: Lampa1: {0}, Lampa2: {1}, Lampa3: {2}, Głośniki: {3}]", Lamp1, Lamp2, Lamp3, Speakers, Date);
        }
    }
}
