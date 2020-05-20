using System;
using System.IO;
using UnityEngine.Networking;

namespace BeatThat.Requests
{
    /// <summary>
    /// Web request that performs a download directly to a file.
    /// <param></param>
    /// </summary>
    public class WebFileDownloadRequest : WebRequest
    {
        public WebFileDownloadRequest(
            string url,
            string file,
            HttpVerb httpVerb = HttpVerb.GET,
            bool downloadToTmpFile = true,
            float delay = 0f,
            UnityHTTPRequestRunner runner = null)
            : this(url, new FileInfo(file), httpVerb, downloadToTmpFile, delay, runner)
        {
        }

        public WebFileDownloadRequest(
            string url, 
            FileInfo file, 
            HttpVerb httpVerb = HttpVerb.GET, 
            bool downloadToTmpFile = true,
            float delay = 0f, 
            UnityHTTPRequestRunner runner = null)
            : base(url, httpVerb, delay, runner)
        {
            this.file = file;
            this.downloadToTmpFile = downloadToTmpFile;
        }

        protected bool downloadToTmpFile { get; private set; }
        protected FileInfo file { get; private set; }
        protected FileInfo downloadFile { get; private set; }

        override protected UnityWebRequest PrepareRequest()
        {
            this.downloadFile = this.downloadToTmpFile 
                ? new FileInfo(this.file.FullName + "." + Guid.NewGuid().ToString())
                : this.file;
            if (!this.downloadFile.Directory.Exists)
            {
                this.downloadFile.Directory.Create();
            }
            var req = new UnityWebRequest(this.url);
            req.method = this.httpVerb.ToUnityWebRequestVerb();
            var dh = new DownloadHandlerFile(this.downloadFile.FullName);
            dh.removeFileOnAbort = true;
            req.downloadHandler = dh;
            return req;
        }

        protected void MoveDownloadFileToTarget()
        {
            if (!this.downloadToTmpFile)
            {
                return;
            }

            if (!this.file.Directory.Exists)
            {
                this.file.Directory.Create();
            }

            if(this.file.Exists) {
                this.file.Delete();
            }

            this.downloadFile.MoveTo(this.file.FullName);

        }

        override protected void DoOnDone()
        {
            MoveDownloadFileToTarget();
        }
    }
}

