using System;
using System.IO;
using BeatThat.ConvertTypeExt;
using BeatThat.Pools;
using BeatThat.Serializers;
using UnityEngine;

namespace BeatThat.Requests
{
    public class WebItemRequest<T> : WebRequestBase, Request<T> 
	{

        public WebItemRequest(string url, HttpVerb httpVerb = HttpVerb.GET, float delay = 0f, Reader<T> format = null, WebRequestRunner runner = null) : base(url, httpVerb, delay, runner)
        {
            this.format = format;
        }

        [Obsolete("use constructor that requires only url")]public WebItemRequest(ReadItemDelegate<T> itemReader, WebRequestRunner runner, string url, HttpVerb httpVerb = HttpVerb.GET, float delay = 0f) : base(runner, url, httpVerb, delay)
        {
            this.itemReader = itemReader;
        }

        [Obsolete("use constructor that requires only url")]
        public WebItemRequest(Reader<T> format, WebRequestRunner runner, string url, HttpVerb httpVerb = HttpVerb.GET, float delay = 0f) : base(runner, url, httpVerb, delay)
        {
            this.format = format;
        }

		public object GetItem() { return this.item; } 
		public T item { get; protected set; }
		public Reader<T> format { get; set; }
        public ReadItemDelegate<T> itemReader { get; set; }

		virtual public void Execute(Action<Request<T>> callback, bool callbackOnCancelled = false) 
		{
			if(callback == null) {
				Execute();
				return;
			}

            RequestExecutionPool<T>.Get().Execute(this, callback, callbackOnCancelled);
		}

		sealed override protected void AfterDisposeWWW()
		{
			var disposeFormat = this.format as IDisposable;
			if(disposeFormat != null) {
				disposeFormat.Dispose();
			}

			this.format = null;
			this.itemReader = null;

			// TODO: this is wrong. Dispose should dispose request resources (e.g. buffers) but not be the means to destroy a loaded item
			var obj = this.item as UnityEngine.Object;
			this.item = default(T);
			if(obj != null) {
				UnityEngine.Object.DestroyImmediate(obj);
			}

			this.item = default(T);

			AfterDisposeItem();
		}

		virtual protected void AfterDisposeItem() {}

		override protected void DoOnDone()
		{
			ReadItem();	
		}

		protected bool ReadItem()
		{
            if(typeof(T) == typeof(string) && (this.itemReader == null && this.format == null)) {
                T text;
                if(this.www.downloadHandler.text.TryConvertTo<T>(out text)) {
                    this.item = text;
                    return true;
                }
            }

			try {
#pragma warning disable XS0001 // Find APIs marked as TODO in Mono
                using (var s = new MemoryStream(this.www.downloadHandler.data))
                {
#pragma warning restore XS0001 // Find APIs marked as TODO in Mono
                    if (this.itemReader != null) {
                        this.item = this.itemReader(s);
                    }
                    else if(this.format != null) {
                        this.item = this.format.ReadOne(s);
                    }
                    else {
                        var ctype = this.www.GetResponseHeader("content-type");
                        if(ctype == null || ctype.IndexOf("json", StringComparison.Ordinal) == -1) {
                            this.error = "No item reader set and response is not json";
                            return false;
                        }

                        var r = StaticObjectPool<JsonReader<T>>.Get();
                        try {
                            this.item = r.ReadOne(s);
                        }
                        finally {
                            StaticObjectPool<JsonReader<T>>.Return(r);
                        }
                    }                     

				}

				return true;
			}
			catch(Exception e) {
				Debug.LogError("Failed to parse item results: url=" + this.www.url 
					+ ", response=" + this.www.downloadHandler.text + ", error=" + e.Message);

				this.error = "format";

				#if UNITY_EDITOR
				throw e;
				#else 
				return false;
				#endif
			}
		}
	}
}

