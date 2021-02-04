using System;

namespace Socean.Rpc.Core.Client
{
    public class QueryContextTaskCompletionSource : ITaskCompletionSource, IAwaitableTask, IDisposable
    {
        private readonly QueryContextAwaiter _awaiter = new QueryContextAwaiter();

        public IAwaiter GetAwaiter()
        {
            return _awaiter;
        }



        public IAwaitableTask Task => this;

        public void Reset()
        {
            _awaiter.Reset();
        }

        public void SetSignal()
        {
            _awaiter.SetSignal();
        }

        public void SetSignalTimeout(int milliseconds)
        {
            _awaiter.SetSignalTimeout(milliseconds);
        }

        public void Dispose()
        {
            _awaiter.Dispose();
        }
    }

    public interface ITaskCompletionSource
    {
        IAwaitableTask Task { get; }

        void Reset();

        void SetSignal();

        void SetSignalTimeout(int milliseconds);
    }

    public interface IAwaitableTask
    {
        IAwaiter GetAwaiter();
    }
}
