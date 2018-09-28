using UnityEngine;

namespace BeatThat.Requests
{
    public interface ResourceItemRequestRunner
    {
        void Execute(ResourceItemRequest request);
    }
}