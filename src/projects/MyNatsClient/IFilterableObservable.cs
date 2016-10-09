using System;

namespace MyNatsClient
{
    public interface IFilterableObservable<out T> : IObservable<T>
    {
        IDisposable Subscribe(IObserver<T> observer, Func<T, bool> filter);
    }
}