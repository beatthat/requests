using UnityEngine;
using UnityEngine.Networking;

namespace BeatThat.Requests
{
    public enum HttpVerb { GET = 0, POST = 1, HEAD = 3, CREATE = 4, PUT = 5, DELETE = 6 }

	public interface UnityHTTPRequest : NetworkRequest, HasResponseCode, HasResponseText
	{
		/// <summary>
		/// Ensures that url (WWWForm) form are ready to send
		/// </summary>
		void Prepare();

		string url { get;  }
		WWWForm form { get; set; }

		UnityWebRequest www { get; }

		void SetHeader(string name, string value);

		void OnQueued();

		void OnSent();

		void OnError(string error);

		void OnDone();

		float delay { get; }
	}

    public static class HttpVerbExt
    {
        public static string ToUnityWebRequestVerb(this HttpVerb v)
        {
            switch(v) {
                case HttpVerb.CREATE:
                    return UnityWebRequest.kHttpVerbCREATE;
                case HttpVerb.DELETE:
                    return UnityWebRequest.kHttpVerbDELETE;
                case HttpVerb.GET:
                    return UnityWebRequest.kHttpVerbGET;
                case HttpVerb.HEAD:
                    return UnityWebRequest.kHttpVerbHEAD;
                case HttpVerb.POST:
                    return UnityWebRequest.kHttpVerbPOST;
                case HttpVerb.PUT:
                    return UnityWebRequest.kHttpVerbPUT;
                default:
                    return UnityWebRequest.kHttpVerbGET;
            }
        }
    }
}

