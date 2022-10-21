namespace Eventfully.Transports
{
    public interface ISupportQueues<T>
    {
        T ConfigureQueue(string name);
    }
}