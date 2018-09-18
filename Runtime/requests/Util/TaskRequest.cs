#if NET_4_6
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeatThat.Requests
{
    /// <summary>
    /// If async/await is available (if .NET >=4.6), 
    /// then you should probably be using async/await and Task instead of Requests.
    /// 
    /// Code that supports async/await but also needs to legacy-support Request versions
    /// of the same methods can use this TaskRequest class
    /// to have a Request simply wrap an async method.
    /// </summary>
    public class TaskRequest<T> : RequestBase, Request<T>
    {
        public TaskRequest(Task<T> task)
        {
            this.task = task;   
        }
        public T item { get; private set; }

        public object GetItem()
        {
            return this.item;
        }

        protected override async void ExecuteRequest()
        {
            if (HandleDoneStates())
            {
                return;
            }

            try
            {
                var cancelToken = this.cancellationTokenSource.Token;
                cancelToken.ThrowIfCancellationRequested();

                this.item = await this.task;
                CompleteRequest();
            }
#pragma warning disable 168
            catch (Exception e)
#pragma warning restore 168
            {
                HandleDoneStates();
            }
        }

        override protected void BeforeCancel()
        {
            base.BeforeCancel();
            this.cancellationTokenSource.Cancel();
        }


        private bool HandleDoneStates()
        {
            if(this.task.IsCompleted) {
                this.item = this.task.Result;
                return true;
            }

            if(this.task.IsCanceled) {
                CompleteRequest(RequestStatus.CANCELLED);
                return true;
            }

            if(this.task.IsFaulted) {
                CompleteWithError(this.task.Exception.GetBaseException().Message);
                return true;
            }

            return false;
        }

        private Task<T> task { get; set; }
        private CancellationTokenSource cancellationTokenSource { get; set; }

    }
}
#endif