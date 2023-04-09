namespace MieszkanieOswieceniaBot
{
    public interface IRelaySensorEntry<out T>
    {
        int Id { get; }
        T RelaySensor { get; }
        string FriendlyName { get; }
    }
}