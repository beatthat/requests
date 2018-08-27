#if NET_4_6
using System;
using UnityEngine;

namespace BeatThat.Requests.Examples
{
    public class Example_WebRequestsAsync : MonoBehaviour
    {

        public WebImage m_webImage;

        void Start()
        {
            m_webImage = (m_webImage != null) ? m_webImage : GetComponentInChildren<WebImage>();
            m_webImage.LoadAndDisplayImage(null);
            GetNewDog();
        }

        public async void GetNewDog()
        {
            try {
                var result = await new WebRequest<DogAPI.DogItem>(DogAPI.END_POINT).ExecuteAsync();
                m_webImage.LoadAndDisplayImage(result.message);
            }
            catch(Exception e) {
                Debug.LogError(e.Message);
            }
        }

    }
}
#endif