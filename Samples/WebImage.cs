using UnityEngine;
using UnityEngine.UI;

namespace BeatThat.Requests.Examples
{
    /// <summary>
    /// Takes an image url as input, loads a texture from url and displays it on a ui raw image

    /// </summary>
    public class WebImage : MonoBehaviour
    {

        public string m_imageURL;
        public Text m_imageURLText;
        public RawImage m_image;

        public string m_loadedImageUrl;

        void Start()
        {
            LoadAndDisplayImage(null);
            DisplayImage(null);
        }

        public void LoadAndDisplayImage(string imgUrl)
        {
            m_imageURL = imgUrl;

            if (m_imageURLText != null)
            {
                m_imageURLText.text = string.Format("Image Url: {0}", imgUrl ?? "none");
            }

            if (!string.IsNullOrEmpty(m_imageURL) && m_imageURL != m_loadedImageUrl)
            {
                LoadImage();
            }
        }

        private void LoadImage()
        {
            if (string.IsNullOrEmpty(m_imageURL))
            {
                return;
            }

            m_loadedImageUrl = m_imageURL;
            LoadAndDisplayImage(m_imageURL); // will disable the load button

            new WebRequest<Texture2D>(m_imageURL).Execute(result =>
            {
                if (result.hasError)
                {
                    Debug.LogError("error loading dog image:" + result.error);
                    return;
                }

                DisplayImage(result.item);
            });
        }

        private void DisplayImage(Texture2D image)
        {
            if (image == null)
            {
                m_image.enabled = false;
                return;
            }

            m_image.texture = image;
            m_image.enabled = true;
        }



    }

}
