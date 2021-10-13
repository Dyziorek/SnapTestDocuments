using System;
using System.Threading.Tasks;

namespace SnapTestDocuments
{
    internal class Observator<T> : IObserver<T>
    {
        T itemWork;

        TaskCompletionSource<T> done = new TaskCompletionSource<T>();

        void IObserver<T>.OnCompleted()
        {
            done.SetResult(itemWork);
        }

        void IObserver<T>.OnError(Exception error)
        {
            done.SetException(error);
        }

        void IObserver<T>.OnNext(T value)
        {
            itemWork = value;
        }
    }
}
