using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Socean.Rpc.Core.Client
{
    public class QueryContextAwaiter : IAwaiter, IDisposable
    {
        public QueryContextAwaiter()
        {
            this._timer = new Timer(TimerCallback, this, Timeout.Infinite, Timeout.Infinite);
        }


        public static readonly Action CallbackCompleted = () => { };

        public const int FalseCode = 0;
        public const int TrueCode = 1;

        private volatile int _isDisposed = FalseCode;
        private volatile Action _continuation;
        private volatile Exception _resultException;
        private readonly Timer _timer;

        public bool IsCompleted => this._continuation == CallbackCompleted;

        private static void TimerCallback(object state)
        {
            var source = (QueryContextAwaiter)state;
            source.SetSignal(new TimeoutException());
        }

        public int GetResult()
        {
            if (IsCompleted == false)
                throw new Exception("get result failed,task is running");

            if (this._resultException != null)
                throw this._resultException;

            return 0;
        }

        private void ExecuteContinuation(Action continuation)
        {
            continuation();
        }

        public void Reset()
        {
            if (_isDisposed == TrueCode)
                throw new Exception("awaiter has been disposed");

            _continuation = null;
            _resultException = null;
        }

        public void OnCompleted(Action continuation)
        {
            var originContinuation = Interlocked.CompareExchange(ref this._continuation, continuation, null);
            if (originContinuation == CallbackCompleted)
            {
                this.ExecuteContinuation(continuation);
            }
        }

        public void SetSignal(Exception exception = null)
        {
            var originContinuation = Interlocked.Exchange(ref this._continuation, CallbackCompleted);
            if (originContinuation == CallbackCompleted)
            {
                return;
            }
            else if (originContinuation == null)
            {
                this._timer.Change(Timeout.Infinite, Timeout.Infinite);
                this._resultException = exception;
            }
            else
            {
                this._timer.Change(Timeout.Infinite, Timeout.Infinite);
                this._resultException = exception;

                this.ExecuteContinuation(originContinuation);
            }
        }

        public void SetSignalTimeout(int milliseconds)
        {
            if (milliseconds < 0)
                throw new ArgumentException(nameof(milliseconds));

            if (IsCompleted)
                return;

            this._timer.Change(milliseconds, Timeout.Infinite);
        }

        public void Dispose()
        {
            var origin = Interlocked.Exchange(ref this._isDisposed, TrueCode);
            if (origin == FalseCode)
            {
                this._timer.Change(Timeout.Infinite, Timeout.Infinite);
                this._timer.Dispose();
            }
        }
    }

    public interface IAwaiter : INotifyCompletion
    {
        bool IsCompleted { get; }

        int GetResult();
    }
}
