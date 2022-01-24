using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KeyGenerationService.BackgroundTasks.BackgroundTaskQueues
{
    public interface IBackgroundTaskQueue
    {
        ValueTask AddToQueueAsync(int value);
        IAsyncEnumerable<int> DequeueAllAsync(CancellationToken cancellationToken);
    }
}