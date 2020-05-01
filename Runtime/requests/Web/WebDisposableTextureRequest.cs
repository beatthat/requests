
using BeatThat.Disposables;
using UnityEngine;
using UnityEngine.Networking;

namespace BeatThat.Requests
{
    /// <summary>
    /// TODO: change so that the request can be disposed immediately and the loaded clip disposed elsewhere/later
    /// </summary>
    public class WebDisposableTextureRequest : WebRequest<Disposable<Texture2D>>
    {
        public WebDisposableTextureRequest(
            string url,
            HttpVerb httpVerb = HttpVerb.GET,
            float delay = 0f,
            UnityHTTPRequestRunner runner = null) : base(url, httpVerb: httpVerb, delay: delay, runner: runner) { }

		override public void Prepare()
        {
            this.www = new UnityWebRequest(this.url);
            www.method = this.httpVerb.ToUnityWebRequestVerb();
            var dh = new UnityEngine.Networking.DownloadHandlerTexture(false);
            www.downloadHandler = dh;
            CopyHeadersTo(this.www);
        }

        override protected void DoOnDone()
        {
            this.item = new DownloadedAssetDisposable<Texture2D>(
				(this.www.downloadHandler as DownloadHandlerTexture).texture);
        }
    }
}



