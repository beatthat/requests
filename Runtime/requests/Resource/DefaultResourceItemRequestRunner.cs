using System.Collections;
using BeatThat.Service;
using UnityEngine;

namespace BeatThat.Requests
{
    [RegisterService]
    public class DefaultResourceItemRequestRunner : MonoBehaviour, ResourceItemRequestRunner
    {
        public void Execute(ResourceItemRequest request)
        {
            StartCoroutine(RunRequest(request));
        }

        private IEnumerator RunRequest(ResourceItemRequest request)
        {
            var resReq = Resources.LoadAsync(request.path, request.GetResourceType());

            request.OnStarted(resReq);

            yield return resReq;

            if (resReq.asset == null)
            {
                request.OnError("Failed to load resource at path '" + request.path + "'");
                yield break;
            }

            request.SetAsset(resReq.asset);

            var asset = request.GetAsset();

            if (asset == null)
            {
                request.OnError("Failed to cast resource at path '" 
                                + request.path + "' to type " 
                                + request.GetResourceType());
                yield break;
            }

            request.SetItem(request.assetObjectToItemObj(asset));

            if (request.GetItem() == null)
            {
                request.OnError("Failed to convert resource at path '" + request.path
                                  + "' from asset type" + request.GetResourceType()
                                  + " to item type " + request.GetItemType());

                yield break;
            }


            request.OnDone();
        }
    }
}