using BeatThat.Properties;

using System;

namespace BeatThat.Requests
{
    [Obsolete("use LocalRequest and pass error to constructor")]
	public class LocalErrorRequest : RequestBase 
	{
		public LocalErrorRequest(string error) 
		{
			this.allowCompleteFromCompletedStatus = true;
			this.error = error;
			UpdateStatus(RequestStatus.DONE);
		}

		override public float progress { get { return 0f; } }

		override protected void ExecuteRequest() 
		{
			CompleteWithError(this.error);
		} 
	}

    [Obsolete("use LocalRequest and pass error to constructor")]
    public class LocalErrorRequest<T> : LocalErrorRequest, Request<T> 
	{
		public LocalErrorRequest(string error) : base(error) {}

		public object GetItem() { return this.item; } 
		public T item { get { return default(T); } }
	}
}
