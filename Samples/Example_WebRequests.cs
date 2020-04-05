using System;
using UnityEngine;

namespace BeatThat.Requests.Examples
{
    public class Example_WebRequests : MonoBehaviour
    {
        public WebImage m_webImage;

        void Start()
        {
            m_webImage = (m_webImage != null) ? m_webImage : GetComponentInChildren<WebImage>();
            m_webImage.LoadAndDisplayImage(null);
            GetNewDog();
        }

        public void GetNewDog()
        {
            new WebRequest<DogAPI.DogItem>(DogAPI.END_POINT).Execute(result =>
            {
                if (result.hasError)
                {
                    Debug.LogError("error loading dog data:" + result.error);
                    return;
                }
                m_webImage.LoadAndDisplayImage(result.item.message);
            });
        }
    }
}
