using BeatThat.Pools;
using BeatThat.Properties;
using System;

namespace BeatThat.Requests
{


	public static class RequestExtensions
	{
		public static bool IsQueuedOrInProgress(this Request r)
		{
			return r.status == RequestStatus.QUEUED || r.status == RequestStatus.IN_PROGRESS;
		}

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


		/// <summary>
		/// Utility for cases where a service stores the 'active' request, 
		/// e.g. to prevent duplicate concurrent requests.
		/// If the passed request is the same ref as the passed ref, then nulls the ref
		/// If not, clears the request (this is why it's a ref arg)
		/// </summary>
		public static bool ClearIfMatches<T>(this Request r, ref T rRef) where T : class
		{
			if(r == null) {
				return false;
			}
			if(Object.ReferenceEquals(r, rRef)) {
				rRef = null;
				return true;
			}
			return false;
		}
			
	}

}


