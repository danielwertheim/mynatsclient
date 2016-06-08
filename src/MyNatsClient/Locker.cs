using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyNatsClient
{
    /// <summary>
    /// See here for more info http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10266988.aspx
    /// </summary>
    public class Locker : IDisposable
    {
        private readonly Task<IDisposable> _releaserTask;
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private bool _isDisposed;

        public Locker()
        {
            _releaserTask = Task.FromResult((IDisposable)new Releaser(_semaphore));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            _isDisposed = true;
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed || !disposing)
                return;

            _semaphore?.Dispose();
            _semaphore = null;
        }

        public IDisposable Lock()
        {
            _semaphore.Wait();

            return _releaserTask.Result;
        }

        public Task<IDisposable> LockAsync(CancellationToken token)
        {
            var wait = _semaphore.WaitAsync(token);

            return wait.IsCompleted
                ? _releaserTask
                : wait.ContinueWith(
                    (_, state) => (IDisposable)state,
                    _releaserTask.Result,
                    token,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }

        private sealed class Releaser : IDisposable
        {
            private readonly SemaphoreSlim _toRelease;

            internal Releaser(SemaphoreSlim toRelease)
            {
                _toRelease = toRelease;
            }

            public void Dispose()
            {
                _toRelease.Release();
            }
        }
    }
}