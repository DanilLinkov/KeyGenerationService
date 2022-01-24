using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace KeyGenerationService.BackgroundTasks.BackgroundTaskQueues
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<int> _channel;
        
        public BackgroundTaskQueue()
        {
            _channel = Channel.CreateBounded<int>(10);
        }
        
        public async ValueTask AddToQueueAsync(int value)
        {
            await _channel.Writer.WriteAsync(value);
        }

        public IAsyncEnumerable<int> DequeueAllAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}