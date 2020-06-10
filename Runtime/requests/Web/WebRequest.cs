using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BeatThat.ConvertTypeExt;
using BeatThat.Serializers;
using BeatThat.Service;
using UnityEngine;
using UnityEngine.Networking;

namespace BeatThat.Requests {
    public class WebRequest : RequestBase, UnityHTTPRequest {

        public WebRequest (string url, HttpVerb httpVerb = HttpVerb.GET, float delay = 0f, UnityHTTPRequestRunner runner = null) {
            this.url = url;
            this.delay = delay;
            this.httpVerb = httpVerb;
            this.runner = runner;
        }

        [Obsolete ("use constructor that requires only url param")]
        public WebRequest (UnityHTTPRequestRunner runner, string url, HttpVerb httpVerb = HttpVerb.GET, float delay = 0f) {
            this.runner = runner;
            this.url = url;
            this.delay = delay;
            this.httpVerb = httpVerb;
        }

        public void ToRawUpload (byte[] data, string contentType, HttpVerb verb = HttpVerb.POST) {
            this.httpVerb = verb;
            this.uploadHandler = new UploadHandlerRaw (data);
            this.uploadHandler.contentType = contentType;
        }

        public void SetBody (string body, string contentType = "application/json", HttpVerb verb = HttpVerb.POST) {
            ToRawUpload (Encoding.UTF8.GetBytes (body), contentType, verb);
        }

        private UploadHandler uploadHandler { get; set; }

        public HttpVerb httpVerb { get; protected set; }

        public long GetResponseCode () {
            return this.www != null ? this.www.responseCode : 0L;
        }

        public string GetResponseText () {
            return this.www != null && this.www.isDone ? this.www.downloadHandler.text : null;
        }

        /// <summary>
        /// Ensures that url (WWWForm) form are ready to send.
        /// Base implementation calls the action 'prepareDelegate' (if it is not null);
        /// </summary>
        public void Prepare () {
            if (this.prepareDelegate != null) {
                this.prepareDelegate (this);
            }
            this.www = PrepareRequest();
            CopyHeadersTo (this.www);
        }

        virtual protected UnityWebRequest PrepareRequest() 
        {
            switch (this.httpVerb) {
                case HttpVerb.GET:
                    return UnityWebRequest.Get (this.url);
                case HttpVerb.POST:
                    if (this.uploadHandler != null) {
                        return PrepareRawUpload (this.uploadHandler, "POST");
                    } else {
                        if (this.form == null) {
                            ForcePost (); // TODO: shouldn't need a form to POST
                        }
                        return UnityWebRequest.Post (this.url, this.form);
                    }
                case HttpVerb.PUT:
                    if (this.uploadHandler == null) {
                        throw new NotSupportedException ("PUT currently supported only with call to ToRawUpload, which should assign an UploadHandler for the request");
                    }
                    return PrepareRawUpload (this.uploadHandler, "PUT");
                default:
                    throw new NotSupportedException (this.httpVerb.ToString ()); // add later
            }
        }

        private UnityWebRequest PrepareRawUpload (UploadHandler h, string method) {
            var req = new UnityWebRequest ();
            req.url = this.url;
            req.method = method;
            req.downloadHandler = new DownloadHandlerBuffer ();
            req.uploadHandler = this.uploadHandler;
            return req;
        }

        private void CopyHeadersTo (UnityWebRequest webRequest = null) {
            if (m_headers == null) {
                return;
            }
            webRequest = webRequest ?? this.www;
            foreach (var h in m_headers) {
                webRequest.SetRequestHeader (h.Key, h.Value);
            }
        }

        public Action<WebRequest> prepareDelegate { get; set; }

        public void SetContentType (string contentType) {
            SetHeader ("Content-Type", contentType);
        }

        public void SetHeader (string name, string value) {
            if (m_headers == null) {
                m_headers = new Dictionary<string, string> (); // TODO: pool
            }
            m_headers[name] = value;
        }
        private IDictionary<string, string> m_headers;

        override public float progress {
            get {
                switch (this.status) {
                    case RequestStatus.IN_PROGRESS:
                        if (this.www == null) {
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

        override public float uploadProgress {
            get {
                switch (this.status) {
                    case RequestStatus.IN_PROGRESS:
                        if (this.www == null) {
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

        public void ForcePost () {
            PostForm (true).AddField ("force", "post"); // TODO: figure out how UnityWebRequest wants you to send an empty post
        }

        public void AddPostField (string name, string value) {
            PostForm (true).AddField (name, value);
        }

        public void AddPostBinaryField (string name, byte[] value, string fileName = null, string mimeType = null) {
            PostForm (true).AddBinaryData (name, value, fileName, mimeType);
        }

        protected WWWForm PostForm (bool create) {
            if (this.form == null && create) {
                this.form = new WWWForm ();
            }
            return this.form;
        }

        sealed override protected void DisposeRequest () {
            if (this.www != null) {
                this.www.Dispose ();
                this.www = null;
            }
            AfterDisposeWWW ();
        }

        virtual protected void AfterDisposeWWW () { }

        override protected void ExecuteRequest () {
            this.runner.Execute (this);
        }

        public void OnQueued () {
            UpdateToQueued ();
        }

        public void OnSent () {
            this.www = www;
            UpdateToInProgress ();
            DoOnSent ();
        }

        virtual protected void DoOnSent () { }

        public void OnError (string err) {
            this.error = err;

            DoOnError ();

            if (!this.hasError) {
                // DoOnError might have decided there is no error after all
                CompleteRequest ();
                return;
            }

            CompleteWithError (this.error);
        }

        public bool isNetworkError {
            get {
#if UNITY_2017_1_OR_NEWER
                return this.www != null && this.www.isNetworkError;
#else
                // terrible hack
                return this.www != null && !string.IsNullOrEmpty (this.www.error) && this.www.error.IndexOf ("No Internet Connection") >= 0;
#endif
            }
        }

        public void OnDone () {
            if (this.status != RequestStatus.IN_PROGRESS) {
                Debug.LogError ("[" + Time.frameCount + "] " + GetType () + "::OnDone called in invalid status " + this.status + " url=" + this.url);
                return;
            }

            DoOnDone ();

            CompleteRequest ();
        }

        virtual protected void DoOnDone () { }

        virtual protected void DoOnError () { }

        public UnityWebRequest www { get; private set; }

        public UnityHTTPRequestRunner runner {
            get {
                return m_runner != null ? m_runner : (m_runner = Services.Require<UnityHTTPRequestRunner> ());
            }
            set {
                m_runner = value;
            }
        }
        private UnityHTTPRequestRunner m_runner;

    }

    public class WebRequest<T> : WebRequest, Request<T> {

        public WebRequest (string url, HttpVerb httpVerb = HttpVerb.GET, float delay = 0f, Reader<T> format = null, UnityHTTPRequestRunner runner = null) : base (url, httpVerb, delay, runner) {
            this.format = format;
        }

        [Obsolete ("use constructor that requires only url")]
        public WebRequest (ReadItemDelegate<T> itemReader, UnityHTTPRequestRunner runner, string url, HttpVerb httpVerb = HttpVerb.GET, float delay = 0f) : base (runner, url, httpVerb, delay) {
            this.itemReader = itemReader;
        }

        [Obsolete ("use constructor that requires only url")]
        public WebRequest (Reader<T> format, UnityHTTPRequestRunner runner, string url, HttpVerb httpVerb = HttpVerb.GET, float delay = 0f) : base (runner, url, httpVerb, delay) {
            this.format = format;
        }

        public object GetItem () { return this.item; }
        public T item { get; protected set; }
        public Reader<T> format { get; set; }
        public ReadItemDelegate<T> itemReader { get; set; }

        virtual public void Execute (Action<Request<T>> callback, bool callbackOnCancelled = false) {
            if (callback == null) {
                Execute ();
                return;
            }

            RequestExecutionPool<T>.Get ().Execute (this, callback, callbackOnCancelled);
        }

        override protected UnityWebRequest PrepareRequest () {
            var req = base.PrepareRequest ();
            if (typeof (Texture).IsAssignableFrom (typeof (T))) {
                req.downloadHandler = new DownloadHandlerTexture ();
            }
            return req;
        }

        sealed override protected void AfterDisposeWWW () {
            var disposeFormat = this.format as IDisposable;
            if (disposeFormat != null) {
                disposeFormat.Dispose ();
            }
            this.format = null;
            this.itemReader = null;
            // TODO: this is wrong. Dispose should dispose request resources (e.g. buffers) but not be the means to destroy a loaded item
            var obj = this.item as UnityEngine.Object;
            this.item = default (T);
            if (obj != null) {
                UnityEngine.Object.DestroyImmediate (obj);
            }
            this.item = default (T);
            AfterDisposeItem ();
        }

        virtual protected void AfterDisposeItem () { }

        override protected void DoOnDone () {
            ReadItem ();
        }

        protected bool ReadItem () {
            if (typeof (T) == typeof (string) && (this.itemReader == null && this.format == null)) {
                T text;
                if (this.www.downloadHandler.text.TryConvertTo<T> (out text)) {
                    this.item = text;
                    return true;
                }
            }

            if (typeof (Texture2D).IsAssignableFrom (typeof (T)) && this.www.downloadHandler as DownloadHandlerTexture != null) {
                T texture;
                (this.www.downloadHandler as DownloadHandlerTexture).texture.TryConvertTo<T> (out texture);
                this.item = texture;
                return true;
            }

            try {
#pragma warning disable XS0001 // Find APIs marked as TODO in Mono
                using (var s = new MemoryStream (this.www.downloadHandler.data)) {
#pragma warning restore XS0001 // Find APIs marked as TODO in Mono
                    if (this.itemReader != null) {
                        this.item = this.itemReader (s);
                    } else if (this.format != null) {
                        this.item = this.format.ReadOne (s);
                    } else {
                        // We don't have a handler for the response but
                        // if there's any hint that it's json, try to process it as json
                        var ctype = this.www.GetResponseHeader ("content-type");
                        if ((ctype == null || ctype.IndexOf ("json", StringComparison.Ordinal) == -1) &&
                            !this.url.EndsWith ("json", StringComparison.Ordinal)) {
                            this.error = "No item reader set and response is not json";
                            return false;
                        }
                        Serializer<T> reader;
                        string err;
                        if (!SerializerConfig.GetDefaultSerializer<T> (out reader, out err)) {
                            this.error = err;
                            return false;
                        }
                        this.item = reader.ReadOne (s);
                    }
                }
                return true;
            } catch (Exception e) {
#if UNITY_EDITOR || DEBUG_UNSTRIP
                Debug.LogError ("Failed to parse item results: url=" + this.www.url +
                    ", response=" + this.www.downloadHandler.text +
                    ", error=" + e.Message + "\n" + e.StackTrace);

#endif

                this.error = e.Message;
                return false;
            }
        }
    }
}