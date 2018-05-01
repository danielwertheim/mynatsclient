using System;

namespace MyNatsClient
{
    public interface INatsObservable<out T> : IObservable<T> { }
}