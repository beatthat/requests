using System.Collections;
using BeatThat.Defines;
using BeatThat.NetworkNotifications;
using BeatThat.Service;
using UnityEngine;
using UnityEngine.Networking;

namespace BeatThat.Requests
{

    /// <summary>
    /// TODO: make all (or most) WWWRequest's safe to dispose immediately following completion. Will require different handling of downloaded textures, audioclips, etc.
    /// </summary>
    [EditDefine(new string[] {
        "WEBREQUEST_LOG_SEND_IN_EDITOR",
        "WEBREQUEST_LOG_SEND_DISABLED", 
        "WEBREQUEST_LOG_SEND_ON_DEVICE"
    }, "enables/disables logging on send. Default is to log in Unity Editor but not on devices.")]

    [EditDefine(new string[] {
        "WEBREQUEST_LOG_COMPLETE_IN_EDITOR",
        "WEBREQUEST_LOG_COMPLETE_DISABLED",
        "WEBREQUEST_LOG_COMPLETE_ON_DEVICE"
    }, "enables/disables logging on complete. Default is to log in Unity Editor but not on devices.")]

    [EditDefine(new string[] {
        "WEBREQUEST_LOG_RESPONSES_IN_EDITOR",
        "WEBREQUEST_LOG_RESPONSES_DISABLED",
        "WEBREQUEST_LOG_RESPONSES_ON_DEVICE"
    }, "enables/disables logging of responses. Default is to log in Unity Editor but not on devices.")]

    [EditDefine("WEBREQUEST_LOG_ERRORS_DISABLED",
                "enables/disables logging of errors. Default is to log in Unity Editor but not on devices.")]
    [RegisterService(typeof(UnityHTTPRequestRunner))]
	public class DefaultUnityHTTPRequestRunner : MonoBehaviour, UnityHTTPRequestRunner 
	{
		public bool m_logSend = 
#if WEBREQUEST_LOG_SEND_ON_DEVICE || (UNITY_EDITOR && !WEBREQUEST_LOG_SEND_DISABLED)
            true;
#else
            false;
#endif
        
		public bool m_logCompleted = 
#if WEBREQUEST_LOG_COMPLETE_ON_DEVICE || (UNITY_EDITOR && !WEBREQUEST_LOG_COMPLETE_DISABLED)
            true;
#else
            false;
#endif
        
        public bool m_logResponses =
#if WEBREQUEST_LOG_RESPONSES_ON_DEVICE || (UNITY_EDITOR && !WEBREQUEST_LOG_RESPONSES_DISABLED)
            true;
#else
            false;
#endif

        public bool m_logErrors = 
#if !WEBREQUEST_LOG_ERRORS_DISABLED
            true;
#else
            false;
#endif

        
		public void Execute(UnityHTTPRequest req)
		{
			req.OnQueued();
			StartCoroutine(DoExecute(req));
		}

#pragma warning disable 414
        private static WaitForEndOfFrame WAIT_FOR_END_OF_FRAME = new WaitForEndOfFrame();
#pragma warning restore 414

		private IEnumerator DoExecute(UnityHTTPRequest req)
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

            if(!www.uri.IsFile) {
                NetworkNotification.WebRequestStarted(www);
            }

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

            if(www.isNetworkError) {
                NetworkNotification.WebRequestNetworkError(www);
            }
            else if(!www.uri.IsFile){
                NetworkNotification.WebRequestReceivedResponse(www);
            }

			if(!string.IsNullOrEmpty(www.error)) {
				if(m_logErrors) {
					Debug.LogError("[" + Time.frameCount + "] " + GetType() + " error executing " + www.method 
                                   + " '" + www.url + "': " + www.error 
                                   + " [" + ((Time.realtimeSinceStartup - timeStart) * 1000) + "ms]"
                                   + ((m_logResponses)? Response2LogText(www): ""));
				}
                req.OnError(www.responseCode == 401 ? Response2LogText(www) : www.error);
				yield break;
			}

			if(!www.isDone) {
				if(m_logErrors) {
					Debug.LogError("[" + Time.frameCount + "] " + GetType() + " req for url failed to complete [" + www.url + "] [" 
                                   + ((Time.realtimeSinceStartup - timeStart) * 1000) + "ms]"
                                   + ((m_logResponses) ? Response2LogText(www) : ""));
				}
				req.OnError("failed to complete request");
				yield break;
			}

			string error;
			if(www.IsError(out error)) {
                if (m_logErrors) {
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


            var dh = www.downloadHandler as DownloadHandlerBuffer;
            if(dh == null) {
                return "content-length: " + www.GetResponseHeader("content-length");
            }

            return dh.text;
        }
	}

}

