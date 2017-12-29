using System;

namespace MyNatsClient
{
    public interface INatsObservable<out T> : IObservable<T> where T : class
    {
        /// <summary>
        /// Sets a handler that will be called whenever a subscribed
        /// observer generates an exception.
        /// </summary>
        Action<T, Exception> OnException { set; }
    }
}