#if NET_4_6
using System;
using BeatThat.Requests;
using UnityEngine;
using UnityEngine.UI;

public class Example_WebRequestsAsync : MonoBehaviour
{

    public string m_imageURL;
    public Text m_imageURLText;
    public RawImage m_image;

    public string m_loadedImageUrl;

    public const string DOG_API = "https://dog.ceo/api/breeds/image/random";

    [Serializable]
    struct DogItem
    {
        public string status;
        public string message; // this dog api happens to store the image url in a property called 'message'
    }

    private void Start()
    {
        UpdateData(null);
        UpdateImage(null);
        GetNewDog();
    }

    public async void GetNewDog()
    {
        Debug.Log("[" + Time.frameCount + "] GetNewDog before...");
        var result = await new WebRequest<DogItem>(DOG_API).ExecuteAsync();
        Debug.Log("[" + Time.frameCount + "] GetNewDog completed...");
        UpdateData(result.message);
     
    }

    private void UpdateData(string dogUrl)
    {
        m_imageURL = dogUrl;
        m_imageURLText.text = string.Format("Image Url: {0}", dogUrl ?? "none");
        if (!string.IsNullOrEmpty(m_imageURL) && m_imageURL != m_loadedImageUrl)
        {
            LoadImage();
        }
    }

    private async void LoadImage()
    {
        if (string.IsNullOrEmpty(m_imageURL))
        {
            return;
        }

        m_loadedImageUrl = m_imageURL;
        UpdateData(m_imageURL); // will disable the load button

        try {
            var result = await new WebRequest<Texture2D>(m_imageURL).ExecuteAsync();
            UpdateImage(result);
        }
        catch(Exception e) {
            Debug.LogError("error loading dog image:" + e.Message);
        }
    }

    private void UpdateImage(Texture2D image)
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
#endif