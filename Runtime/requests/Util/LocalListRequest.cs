using BeatThat.Pools;
using BeatThat.Properties;
using System;
using System.Collections;
using UnityEngine;

namespace BeatThat.Requests
{
	public class LocalListRequest<T> : RequestBase, ListRequest<T> where T : class
	{


        /// <summary>
        /// Create a local request with an optional execution time.
        /// </summary>
        /// <param name="execDuration">
        /// If set > 0, then execution will take this duration instead of returning immediately.
        /// This is not implemented to be efficient. It's a convenience for test scenarios.
        /// </param>

        public LocalListRequest(T[] items, float execDuration = 0f)
        {
            this.allowCompleteFromCompletedStatus = true;
            this.execDuration = execDuration;
            if (execDuration <= 0f)
            {
                UpdateStatus(RequestStatus.DONE);
            }
		}
        public float execDuration { get; set; }


        public T[] items { get; private set; }

//		override public void Cancel() {}
		override protected void DisposeRequest() 
		{
			this.items = null;
		}

        override protected void ExecuteRequest()
        {
            if (this.execDuration <= 0f)
            {
                CompleteRequest();
                return;
            }

            UpdateToInProgress();
            var runner = new GameObject("LocalRequestRunner").AddComponent<RequestCoroutine>();
            runner.StartCoroutine(ExecuteWithDuration(runner));
        }

        /// <summary>
        /// Uses a one-off created GameObject to run a coroutine and fake delay the execution.
        /// This is sloppyish. Really intended only for test scenarios
        /// </summary>
        private IEnumerator ExecuteWithDuration(MonoBehaviour coroutineOwner)
        {
            yield return new WaitForSeconds(this.execDuration);
            CompleteRequest();
            UnityEngine.Object.Destroy(coroutineOwner.gameObject);
        }

        virtual public void Execute(Action<ListRequest<T>> callback) 
		{
			if(callback == null) {
				Execute();
				return;
			}

			ListRequestExecutionPool<T>.Get().Execute(this, callback);
		}
	}
}

