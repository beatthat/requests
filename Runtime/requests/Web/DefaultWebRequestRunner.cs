using System.Collections;
using BeatThat.Service;
using UnityEngine;
using UnityEngine.Networking;

namespace BeatThat.Requests
{

    /// <summary>
    /// TODO: make all (or most) WWWRequest's safe to dispose immediately following completion. Will require different handling of downloaded textures, audioclips, etc.
    /// </summary>
    [RegisterService(typeof(WebRequestRunner))]
	public class DefaultWebRequestRunner : MonoBehaviour, WebRequestRunner 
	{
		public bool m_logSend = 
#if WEBREQUEST_LOG_SEND || (UNITY_EDITOR && !WEBREQUEST_LOG_SEND_DISABLED)
            true;
#else
            false;
#endif
        
		public bool m_logCompleted = 
#if WEBREQUEST_LOG_COMPLETE || (UNITY_EDITOR && !WEBREQUEST_LOG_COMPLETE_DISABLED)
            true;
#else
            false;
#endif
        
        public bool m_logResponses =
#if WEBREQUEST_LOG_RESPONSES || (UNITY_EDITOR && !WEBREQUEST_LOG_RESPONSES_DISABLED)
            true;
#else
            false;
#endif

		public bool m_disableLogError = 
#if WEBREQUEST_DISABLE_LOG_ERROR 
            true;
#else
        false;
#endif
        
		public void Execute(WebRequest req)
		{
			req.OnQueued();
			StartCoroutine(DoExecute(req));
		}

#pragma warning disable 414
        private static WaitForEndOfFrame WAIT_FOR_END_OF_FRAME = new WaitForEndOfFrame();
#pragma warning restore 414

		private IEnumerator DoExecute(WebRequest req)
		{
			if(req.delay > 0f) {
				yield return new WaitForSeconds(req.delay);
			}

			if(req.status == RequestStatus.CANCELLED || req.status == RequestStatus.DONE) {
				yield break;
			}

			req.Prepare();

			var www = req.www;

#pragma warning disable 219
            var token = www.SendWebRequest();
#pragma warning restore 219

			var timeStart = Time.realtimeSinceStartup;

			if(m_logSend) {
				Debug.Log("[" + Time.frameCount + "] " + GetType() + " executing " + www.method + " '" + www.url + "'");
			}

			req.OnSent();

#if UNITY_IOS
			// TODO: Cannot be interrupted by WWW.Dispose() on iOS. Need to retest if this is necessary with WebRequest
			while (req.status == RequestStatus.IN_PROGRESS && !www.isDone) { 
                yield return WAIT_FOR_END_OF_FRAME; 
			}
#else
			yield return token; 
#endif

			if(req.status == RequestStatus.CANCELLED) {
				yield break;
			}

			if(!string.IsNullOrEmpty(www.error)) {
				if(!m_disableLogError) {
					Debug.LogError("[" + Time.frameCount + "] " + GetType() + " error executing " + www.method 
                                   + " '" + www.url + "': " + www.error 
                                   + " [" + ((Time.realtimeSinceStartup - timeStart) * 1000) + "ms]"
                                   + ((m_logResponses)? Response2LogText(www): ""));
				}
				req.OnError(www.error);
				yield break;
			}

			if(!www.isDone) {
				if(!m_disableLogError) {
					Debug.LogError("[" + Time.frameCount + "] " + GetType() + " req for url failed to complete [" + www.url + "] [" 
                                   + ((Time.realtimeSinceStartup - timeStart) * 1000) + "ms]"
                                   + ((m_logResponses) ? Response2LogText(www) : ""));
				}
				req.OnError("failed to complete request");
				yield break;
			}

			string error;
			if(www.IsError(out error)) {
                if (!m_disableLogError) {
                    Debug.LogError("[" + Time.frameCount + "] " + GetType() + " error response executing " + www.method
                                   + " '" + www.url + "': " + error
                                   + " [" + ((Time.realtimeSinceStartup - timeStart) * 1000) + "ms]"
                                   + ((m_logResponses) ? Response2LogText(www) : ""));
                }

				req.OnError(error);
				yield break;
			}

            if(m_logResponses) {
                Debug.Log("[" + Time.frameCount + "] " + GetType() + " COMPLETED " + www.method + " '" + www.url 
                          + "' [" + ((Time.realtimeSinceStartup - timeStart) * 1000) + "ms]"
                          + "\nresponse:\n" + Response2LogText(www));
            }
			else if(m_logCompleted) {
				Debug.Log("[" + Time.frameCount + "] " + GetType() + " COMPLETED " + www.method + " '" + www.url + "' [" + ((Time.realtimeSinceStartup - timeStart) * 1000) + "ms]");
			}
				
			req.OnDone();
		}


        private string Response2LogText(UnityWebRequest www)
        {
            var contentType = www.GetResponseHeader("content-type");
            if(string.IsNullOrEmpty(contentType)) {
                return "[no content type]";
            }
            if(contentType.IndexOf("text", System.StringComparison.Ordinal) == -1
               && contentType.IndexOf("json", System.StringComparison.Ordinal) == -1
               && contentType.IndexOf("xml", System.StringComparison.Ordinal) == -1
              ){
                return contentType;
            }
            return www.downloadHandler.text;
        }
	}

}

