using System;
using BeatThat.Requests;
using UnityEngine;
using UnityEngine.UI;

public class Example_WebRequests : MonoBehaviour {

    public string m_imageURL;
    public Text m_imageURLText;
    public RawImage m_image;

    public string m_loadedImageUrl;

    public const string DOG_API = "https://dog.ceo/api/breeds/image/random";

	[Serializable] struct DogItem
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

    public void GetNewDog()
    {
        new WebItemRequest<DogItem>(DOG_API).Execute(result =>
        {
            if(result.hasError) {
                Debug.LogError("error loading dog data:" + result.error);
                return;
            }

            UpdateData(result.item.message); 
        });
    }

    private void UpdateData(string dogUrl)
    {
        m_imageURL = dogUrl;
        m_imageURLText.text = string.Format("Image Url: {0}", dogUrl?? "none");
        if(!string.IsNullOrEmpty(m_imageURL) && m_imageURL != m_loadedImageUrl) {
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
        UpdateData(m_imageURL); // will disable the load button

        new WebItemRequest<Texture2D>(m_imageURL).Execute(result =>
        {
            if (result.hasError)
            {
                Debug.LogError("error loading dog image:" + result.error);
                return;
            }

            UpdateImage(result.item);
        });
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
