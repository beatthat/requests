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

        override public void Prepare()
        {
            this.downloadFile = this.downloadToTmpFile ?
                new FileInfo(this.file.FullName + "." + Guid.NewGuid().ToString()) :
                this.file;
            
            if (!this.downloadFile.Directory.Exists)
            {
                this.downloadFile.Directory.Create();
            }

            this.www = new UnityWebRequest(this.url);
            www.method = this.httpVerb.ToUnityWebRequestVerb();
            var dh = new DownloadHandlerFile(this.downloadFile.FullName);
            dh.removeFileOnAbort = true;
            www.downloadHandler = dh;
            CopyHeadersTo(this.www);
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

