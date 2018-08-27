using System;
using UnityEngine;

namespace BeatThat.Requests.Examples
{
    public static class DogAPI 
    {
        public const string END_POINT = "https://dog.ceo/api/breeds/image/random";

        [Serializable]
        public struct DogItem
        {
            public string status;
            public string message; // this dog api happens to store the image url in a property called 'message'
        }
    }
}
