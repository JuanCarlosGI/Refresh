using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core
{
    public interface IUpdateSubscriber
    {
        Task WaitAndUpdateAsync(CancellationToken token);
    }

    public interface IUpdater
    {
        Task Update(CancellationToken token, string repo);
    }
}
