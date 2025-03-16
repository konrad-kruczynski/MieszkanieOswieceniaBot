namespace MieszkanieOswieceniaBot
{
    public interface IEntry<out T> : IEntryBase
    {
        T Element { get; }
    }
}