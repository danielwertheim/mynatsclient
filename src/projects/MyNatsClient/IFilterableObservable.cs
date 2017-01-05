using System;

namespace MyNatsClient
{
    public interface IFilterableObservable<out T> : IObservable<T> where T : class
    {
        IDisposable Subscribe(IObserver<T> observer, Func<T, bool> filter);
    }
}