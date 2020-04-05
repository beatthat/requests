A Request is an executable async operation that can return status and optionally a retrieved object. Under the covers, requests are frequently HTTP calls, but from the Request-user perspective, that is an implementation detail.

## Install

From your unity project folder:

    npm init
    npm install beatthat/requests --save

The package and all its dependencies will be installed under Assets/Plugins/packages/beatthat.

In case it helps, a quick video of the above: https://youtu.be/Uss_yOiLNw8

## USAGE

The examples below use this dogs api:

```c#
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
```

Make a GET request with callback

```c#
new WebRequest<DogAPI.DogItem>(DogAPI.END_POINT).Execute(result =>
{
    if (result.hasError)
    {
        Debug.LogError("error loading dog data:" + result.error);
        return;
    }
    Debug.Log("got a url (dog api returns url as 'message'):" + result.item.message);
});
```

Make a GET request async (requires API Level >= 4.x)

```c#
public async void GetNewDog()
{
    try {
        var result = await new WebRequest<DogAPI.DogItem>(DogAPI.END_POINT).ExecuteAsync();
        Debug.Log("got a url (dog api returns url as 'message'):" + result.item.message);
    }
    catch(Exception e) {
        Debug.LogError(e.Message);
    }
}
```

#### Change the underlying json lib to Newtonsoft (or other)

The default serializer uses Unity's `JSONUtility` class to read and write json. Unity's JSONUtility is efficient, but it has a number of serious limitations, including these:

- no support for `Dictionary` types
- no support for dates
- no support for C# properties (as opposed to public fields)
- top level object cannot be an array (must be an object)

You can change the json impl to newtonsoft as follows:

- add the newtonsoft [JsonNet](https://www.newtonsoft.com/json) dll to `Assets/Plugins` (NOTE: as of this writing Unity only works with the 2.0 version and you will need to make some link.xml entries if using iOS)
- Install via terminal `cd your-unity-project-root && npm install --save beatthat/serializers-newtonsoft`
- put the code snippet below somewhere early in execution of your game

```c#
BeatThat.Serializers.SerializerConfig.SetDefaultSerializer(
    new BeatThat.Serializers.Newtonsoft.JsonNetSerializerFactory());
```
