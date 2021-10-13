using System;
using System.Collections.Generic;

namespace SnapTestDocuments
{

    internal class SubDisposable<T> : IDisposable
    {
        private bool disposedValue;
        private readonly List<IObserver<T>> viewers;
        private readonly IObserver<T> view;

        public SubDisposable(IObserver<T> observer, List<IObserver<T>> observers)
        {
            viewers = observers;
            view = observer;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    viewers.Remove(view);



                    // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                    // TODO: set large fields to null
                    disposedValue = true;
                }
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~SubDisposable()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    internal class SubjectItem<T> : IObservable<T>, IObserver<T>
    {
        private readonly List<IObserver<T>> observers = new List<IObserver<T>>();

        void IObserver<T>.OnCompleted()
        {
            foreach (var observer in observers)
            {
                observer.OnCompleted();
            }
        }

        void IObserver<T>.OnError(Exception error)
        {
            foreach (var observer in observers) { observer.OnError(error); }
        }

        void IObserver<T>.OnNext(T value)
        {
            foreach (var observer in observers) { observer.OnNext(value); }
        }

        IDisposable IObservable<T>.Subscribe(IObserver<T> obs)
        {
            observers.Add(obs);
            return new SubDisposable<T>(obs, observers);
        }
    }
}
