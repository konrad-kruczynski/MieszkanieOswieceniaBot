using System;

namespace MieszkanieOswieceniaBot
{
    public sealed class RelaySample : ISample<RelaySample>
    {
        public RelaySample(int relayId, bool state)
        {
            RelayId = relayId;
            Date = DateTime.Now;
            State = state;
        }

        public RelaySample()
        {

        }

        public int Id { get; set; }
        public int RelayId { get; set; }
        public DateTime Date { get; set; }
        public bool State { get; set; }

        public bool CanSampleBeSquashed(RelaySample sample)
        {
            return !sample.State && !State;
        }

        public override string ToString() => $"{RelayId}: {State}";
    }
}
