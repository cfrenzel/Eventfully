using System.Threading.Tasks;

namespace Eventfully
{
    public interface ICountingSemaphore
    {
        int MaxConcurrentOwners { get; }
        string Name { get; }
        int TimeoutInSeconds { get; }

        Task<bool> TryAcquire(string ownerId);
        Task<bool> TryRelease(string ownerId);
        Task<bool> TryRenew(string ownerId);
    }
}