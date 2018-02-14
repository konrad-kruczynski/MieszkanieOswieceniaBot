using System;

namespace MieszkanieOswieceniaBot
{
    public interface ISample<T>
    {
        DateTime Date { get; }
        int Id { get; }
        bool CanSampleBeSquashed(T t);
    }
}
