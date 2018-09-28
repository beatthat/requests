using System;
using System.Collections;
using BeatThat.ConvertTypeExt;
using BeatThat.Service;
using UnityEngine;

namespace BeatThat.Requests
{
    public abstract class ResourceItemRequest : RequestBase
    {
        public ResourceItemRequest(string path, 
                                   AssetObjToItemObjDelegate assetToItem,
                                   ResourceItemRequestRunner runner = null)
        {
            this.path = path;
            this.assetObjectToItemObj = assetToItem;
            this.runner = runner;
        }

        public delegate object AssetObjToItemObjDelegate(UnityEngine.Object asset);

        protected ResourceRequest req { get; set; }
        public AssetObjToItemObjDelegate assetObjectToItemObj { get; protected set; }
        public string path { get; protected set; }


        abstract public Type GetResourceType();
        abstract public Type GetItemType();
        abstract public void SetAsset(UnityEngine.Object a);
        abstract public UnityEngine.Object GetAsset();
        abstract public object GetItem();
        abstract public void SetItem(object i);

        override public float progress
        {
            get
            {
                switch (this.status)
                {
                    case RequestStatus.IN_PROGRESS:
                        if (this.req == null)
                        {
                            return 0f;
                        }
                        return this.req.progress;
                    case RequestStatus.DONE:
                        return 1f;
                    default:
                        return 0f;
                }
            }
        }

        private GameObject runner4SingleExecution { get; set; }

        override protected void AfterCancel()
        {
            DestroyRunner();

            UnityEngine.Object assetToUnload = GetAsset();
            SetAsset(null);
            SetItem(null);

            // NOTE: handling of Dispose/Cancel is wrong (dispose should not unload asset)?

            // only unload if the object type is a file asset, e.g. a texure or audio file
            if (assetToUnload != null && !(assetToUnload is GameObject) && !(assetToUnload is Component))
            {
                Resources.UnloadAsset(assetToUnload);
            }
        }

        override protected void AfterCompletionCallback()
        {
            DestroyRunner();
        }

        private void DestroyRunner()
        {
            var r = this.runner4SingleExecution;
            this.runner4SingleExecution = null;
            if (r)
            {
                UnityEngine.Object.DestroyImmediate(r);
            }
        }

        override protected void ExecuteRequest()
        {
            this.runner.Execute(this);
        }

        public ResourceItemRequestRunner runner
        {
            get
            {
                if(m_runner != null) {
                    return m_runner;
                }
                if((m_runner = Services.Require<ResourceItemRequestRunner>()) != null) {
                    return m_runner;
                }
                this.runner4SingleExecution = new GameObject("ResourceLoader-" + this.path);
                m_runner = this.runner4SingleExecution.AddComponent<DefaultResourceItemRequestRunner>();
                return m_runner;
            }
            set
            {
                m_runner = value;
            }
        }
        private ResourceItemRequestRunner m_runner;

        public void OnStarted(ResourceRequest r)
        {
            this.req = r;
            UpdateToInProgress();
            OnResourceStarted();
        }

        virtual protected void OnResourceStarted()
        {
        }

        public void OnDone()
        {
            if (this.status != RequestStatus.IN_PROGRESS)
            {
#if UNITY_EDITOR || DEBUG_UNSTRIP
                Debug.LogWarning("[" + Time.frameCount + "] " + GetType() 
                               + "::OnDone called in status " + this.status + " path=" + this.path);
#endif
                return;
            }

            OnResourceDone();

            CompleteRequest();
        }

        virtual protected void OnResourceDone() { }

        public void OnError(string err)
        {
            this.error = err;

            OnResourceError();

            if (!this.hasError)
            {
                // DoOnError might have decided there is no error after all
                CompleteRequest();
                return;
            }

            CompleteWithError(this.error);
        }

        virtual protected void OnResourceError()
        {
        }

        protected IEnumerator RunExecute()
        {
            UpdateToInProgress();

            this.req = Resources.LoadAsync(this.path, GetResourceType());

            yield return req;

            if (req.asset == null)
            {
                CompleteWithError("Failed to load resource at path '" + this.path + "'");
                yield break;
            }

            SetAsset(this.req.asset);
            if (GetAsset() == null)
            {
                CompleteWithError("Failed to cast resource at path '" + this.path + "' to type " + GetResourceType());
                yield break;
            }

            SetItem(this.assetObjectToItemObj(GetAsset()));
                    
            if (GetItem() == null)
            {
                CompleteWithError("Failed to convert resource at path '" + this.path
                                  + "' from asset type" + GetResourceType()
                                  + " to item type " + GetItemType());

                yield break;
            }


            CompleteRequest();
        }
    }

    public class ResourceItemRequest<ItemType, ResourceType> : ResourceItemRequest, Request<ItemType>
        where ResourceType : UnityEngine.Object
    {
        public delegate ItemType AssetToItemDelegate(ResourceType asset);

        public ResourceItemRequest(string path, 
                                   AssetToItemDelegate assetToItem,
                                   ResourceItemRequestRunner runner = null)
            : base(path, a => assetToItem(a as ResourceType), runner)
        {
            this.path = path;
            this.assetToItem = assetToItem;
        }

        protected AssetToItemDelegate assetToItem { get; set; }

        override public object GetItem() { return this.item; }
        override public Type GetResourceType() { return typeof(ResourceType); }
        override public Type GetItemType() { return typeof(ItemType); }
        override public void SetAsset(UnityEngine.Object a)
        {
            if(a == null) {
                this.asset = null;
                return;
            }

            ResourceType asType;
            if (a.TryConvertTo<ResourceType>(out asType))
            {
                this.asset = asType;
            }
        }
        override public UnityEngine.Object GetAsset() { return this.asset; }
        override public void SetItem(object i) 
        {
            if(i == null) {
                this.item = default(ItemType);
                return;
            }

            ItemType asType;
            if(i.TryConvertTo<ItemType>(out asType)) {
                this.item = asType;
            }
        }

		public ResourceType asset { get; private set; }
		public ItemType item { get; private set; }


        virtual public void Execute(Action<Request<ItemType>> callback)
        {
            if (callback == null)
            {
                Execute();
                return;
            }

            RequestExecutionPool<ItemType>.Get().Execute(this, callback);
        }

	}

	public class ResourceItemRequest<T> : ResourceItemRequest<T, T> where T : UnityEngine.Object
	{
        public ResourceItemRequest(string path, 
                                   ResourceItemRequestRunner runner = null) 
            : base(path, ResourceItemRequest<T>.AssetToItem, runner) {}

		private static T AssetToItem(T asset) { return asset; }
	}
}


