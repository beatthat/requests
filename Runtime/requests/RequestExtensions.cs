using System;

namespace BeatThat.Requests
{
    public static class RequestExtensions
	{
		/// <summary>
		/// Execute the request and call the (optional) callback when the request terminates, successful or otherwise.
		/// </summary>
		public static void Execute(this Request r, Action<Request> callback)
		{
			if(callback == null) {
				r.Execute();
				return;
			}
			RequestExecutionPool.Get().Execute(r, callback);
		}

		/// <summary>
		/// Execute the request and call the (optional) callback when the request terminates, successful or otherwise.
		/// </summary>
		public static void Execute<T>(this Request<T> r, Action<Request<T>> callback)
		{
			if(callback == null) {
				r.Execute();
				return;
			}
			RequestExecutionPool<T>.Get().Execute(r, callback);
		}
	}

}


