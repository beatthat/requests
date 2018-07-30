using System;
using System.Collections.Generic;
using System.IO;
using BeatThat.ConvertTypeExt;
using BeatThat.Pools;
using BeatThat.Serializers;
using BeatThat.Service;
using UnityEngine;
using UnityEngine.Networking;

namespace BeatThat.Requests
{
    public class WebRequest : RequestBase, UnityHTTPRequest 
	{
		
        [Obsolete("use constructor that requires only url param")]
		public WebRequest(UnityHTTPRequestRunner runner, string url, HttpVerb httpVerb = HttpVerb.GET, float delay = 0f)
		{
			this.runner = runner;
			this.url = url;
			this.delay = delay;
			this.httpVerb = httpVerb;
		}

        public WebRequest(string url, HttpVerb httpVerb = HttpVerb.GET, float delay = 0f, UnityHTTPRequestRunner runner = null)
        {
            this.url = url;
            this.delay = delay;
            this.httpVerb = httpVerb;
            this.runner = runner;
        }

		public HttpVerb httpVerb { get; protected set; }

		public long GetResponseCode()
		{
			return this.www != null? this.www.responseCode: 0L;
		}

		public string GetResponseText()
		{
			return this.www != null && this.www.isDone? this.www.downloadHandler.text: null;
		}
			
		/// <summary>
		/// Ensures that url (WWWForm) form are ready to send.
		/// Base implementation calls the action 'prepareDelegate' (if it is not null);
		/// </summary>
		virtual public void Prepare() 
		{
			if(this.prepareDelegate != null) {
				this.prepareDelegate(this);
			}

			switch(this.httpVerb) {
			case HttpVerb.GET:
				this.www = UnityWebRequest.Get(this.url);
				break;
			case HttpVerb.POST:
				if(this.form == null) {
					ForcePost(); // TODO: shouldn't need a form to POST
				}
				this.www = UnityWebRequest.Post (this.url, this.form);
				break;
			default:
				throw new NotSupportedException(this.httpVerb.ToString()); // add later
			}

			if(m_headers != null) {
				foreach(var h in m_headers) {
					this.www.SetRequestHeader(h.Key, h.Value);
				}
			}
		}

		public Action<WebRequest> prepareDelegate { get; set; }

		public void SetHeader(string name, string value)
		{
			if(m_headers == null) {
				m_headers = new Dictionary<string, string>(); // TODO: pool
			}
			m_headers[name] = value;
		}
		private IDictionary<string,string> m_headers;

		override public float progress 
		{
			get {
				switch(this.status) {
				case RequestStatus.IN_PROGRESS:
					if(this.www == null) {
						return 0f;
					}
					return this.www.downloadProgress;
				case RequestStatus.DONE:
					return 1f;
				default:
					return 0f;
				}
			}
		}

		override public float uploadProgress 
		{
			get {
				switch(this.status) {
				case RequestStatus.IN_PROGRESS:
					if(this.www == null) {
						return 0f;
					}
					return this.www.uploadProgress;
				case RequestStatus.DONE:
					return 1f;
				default:
					return 0f;
				}
			}
		}

		public float delay { get; set; }
		public string url { get; set; }
		public WWWForm form { get; set; }

		public void ForcePost()
		{
			PostForm(true).AddField("force", "post"); // TODO: figure out how UnityWebRequest wants you to send an empty post
		}

		public void AddPostField(string name, string value)
		{
			PostForm(true).AddField(name, value);
		}

		public void AddPostBinaryField(string name, byte[] value, string fileName = null, string mimeType = null)
		{
			PostForm(true).AddBinaryData(name, value, fileName, mimeType);
		}

		protected WWWForm PostForm(bool create)
		{
			if(this.form == null && create) {
				this.form = new WWWForm();
			}
			return this.form;
		}

		sealed override protected void DisposeRequest()
		{
			if(this.www != null) {
				this.www.Dispose();
				this.www = null;
			}
			AfterDisposeWWW();
		}

		virtual protected void AfterDisposeWWW()
		{
		}

		override protected void ExecuteRequest()
		{
			this.runner.Execute(this);
		}

		public void OnQueued()
		{
			UpdateToQueued();
		}

		public void OnSent()
		{
			this.www = www;
			UpdateToInProgress();
			DoOnSent();
		}

		virtual protected void DoOnSent()
		{
		}

		public void OnError(string err)
		{
			this.error = err;

			DoOnError();

			if(!this.hasError) {
				// DoOnError might have decided there is no error after all
				CompleteRequest();
				return;
			}
				
			CompleteWithError(this.error);
		}

		public bool isNetworkError 
		{ 
			get {
				#if UNITY_2017_1_OR_NEWER
				return this.www.isNetworkError;
				#else
				// terrible hack
				return this.www != null && !string.IsNullOrEmpty(this.www.error) && this.www.error.IndexOf("No Internet Connection") >= 0; 
				#endif
			}
		}


		public void OnDone()
		{
			if(this.status != RequestStatus.IN_PROGRESS) {
				Debug.LogError("[" + Time.frameCount + "] " + GetType() + "::OnDone called in inva status " + this.status + " url=" + this.url);
				return;
			}

			DoOnDone();

			CompleteRequest();
		}

		virtual protected void DoOnDone() {}

		virtual protected void DoOnError() {}

		public UnityWebRequest www { get; protected set; }

		public UnityHTTPRequestRunner runner 
        { 
            get {
                return m_runner != null ? m_runner : (m_runner = Services.Require<UnityHTTPRequestRunner>());
            } 
            set {
                m_runner = value;
            }
        }
        private UnityHTTPRequestRunner m_runner;

	}

    public class WebRequest<T> : WebRequest, Request<T>
    {

        public WebRequest(string url, HttpVerb httpVerb = HttpVerb.GET, float delay = 0f, Reader<T> format = null, UnityHTTPRequestRunner runner = null) : base(url, httpVerb, delay, runner)
        {
            this.format = format;
        }

        [Obsolete("use constructor that requires only url")]
        public WebRequest(ReadItemDelegate<T> itemReader, UnityHTTPRequestRunner runner, string url, HttpVerb httpVerb = HttpVerb.GET, float delay = 0f) : base(runner, url, httpVerb, delay)
        {
            this.itemReader = itemReader;
        }

        [Obsolete("use constructor that requires only url")]
        public WebRequest(Reader<T> format, UnityHTTPRequestRunner runner, string url, HttpVerb httpVerb = HttpVerb.GET, float delay = 0f) : base(runner, url, httpVerb, delay)
        {
            this.format = format;
        }

        public object GetItem() { return this.item; }
        public T item { get; protected set; }
        public Reader<T> format { get; set; }
        public ReadItemDelegate<T> itemReader { get; set; }

        virtual public void Execute(Action<Request<T>> callback, bool callbackOnCancelled = false)
        {
            if (callback == null)
            {
                Execute();
                return;
            }

            RequestExecutionPool<T>.Get().Execute(this, callback, callbackOnCancelled);
        }

        override public void Prepare()
        {
            base.Prepare();
            if (typeof(Texture).IsAssignableFrom(typeof(T)))
            {
                www.downloadHandler = new DownloadHandlerTexture();
            }
        }

        sealed override protected void AfterDisposeWWW()
        {
            var disposeFormat = this.format as IDisposable;
            if (disposeFormat != null)
            {
                disposeFormat.Dispose();
            }

            this.format = null;
            this.itemReader = null;

            // TODO: this is wrong. Dispose should dispose request resources (e.g. buffers) but not be the means to destroy a loaded item
            var obj = this.item as UnityEngine.Object;
            this.item = default(T);
            if (obj != null)
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }

            this.item = default(T);

            AfterDisposeItem();
        }

        virtual protected void AfterDisposeItem() { }

        override protected void DoOnDone()
        {
            ReadItem();
        }

        protected bool ReadItem()
        {
            if (typeof(T) == typeof(string) && (this.itemReader == null && this.format == null))
            {
                T text;
                if (this.www.downloadHandler.text.TryConvertTo<T>(out text))
                {
                    this.item = text;
                    return true;
                }
            }

            if (typeof(Texture2D).IsAssignableFrom(typeof(T)) && this.www.downloadHandler as DownloadHandlerTexture != null)
            {
                T texture;
                (this.www.downloadHandler as DownloadHandlerTexture).texture.TryConvertTo<T>(out texture);
                this.item = texture;
                return true;
            }

            try
            {
#pragma warning disable XS0001 // Find APIs marked as TODO in Mono
                using (var s = new MemoryStream(this.www.downloadHandler.data))
                {
#pragma warning restore XS0001 // Find APIs marked as TODO in Mono
                    if (this.itemReader != null)
                    {
                        this.item = this.itemReader(s);
                    }
                    else if (this.format != null)
                    {
                        this.item = this.format.ReadOne(s);
                    }
                    else
                    {
                        var ctype = this.www.GetResponseHeader("content-type");
                        if (ctype == null || ctype.IndexOf("json", StringComparison.Ordinal) == -1)
                        {
                            this.error = "No item reader set and response is not json";
                            return false;
                        }

                        var r = StaticObjectPool<JsonReader<T>>.Get();
                        try
                        {
                            this.item = r.ReadOne(s);
                        }
                        finally
                        {
                            StaticObjectPool<JsonReader<T>>.Return(r);
                        }
                    }

                }

                return true;
            }
            catch (Exception e)
            {
#if UNITY_EDITOR || DEBUG_UNSTRIP
                Debug.LogError("Failed to parse item results: url=" + this.www.url
                               + ", response=" + this.www.downloadHandler.text 
                               + ", error=" + e.Message + "\n" + e.StackTrace);
                
#endif

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

