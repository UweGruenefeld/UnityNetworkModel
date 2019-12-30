using System;
using System.Collections.Specialized;

namespace UnityEngine
{
    /// <summary>
    /// Collects all known Assets and tracks their last known state.
    /// </summary>
    class AssetStore : OrderedDictionary/*<string, AssetStore.AssetNode>*/
    {
        private Serializer serializer;
        private NetworkModel config;

        public AssetStore(Serializer serializer, NetworkModel config)
        {
            this.serializer = serializer;
            this.config = config;
        }

        /// <summary>
        /// Adds Asset to store
        /// </summary>
        /// <param name="asset"></param>
        internal void Add(Object asset)
        {
            this.Add(asset.name, asset);
        }

        /// <summary>
        /// Adds Asset to store with specific name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="asset"></param>
        internal void Add(string name, Object asset)
        {
            this.Add(name, new AssetNode(asset, name, serializer));
        }

        /// <summary>
        /// Tries to find an Asset of correct name and type by priority
        /// 1) A matching tracked Asset from the store.
        /// 2) A matching Asset of the correct type from the Resources folder (if using existing assets is enabled).
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="type"></param>
        /// <param name="node"></param>
        /// <returns>Returns true if a matching Asset was found.</returns>
        internal bool TryGet(string assetName, Type type, out AssetNode node)
        {
            if (this.Contains(assetName))
            {
                AssetNode tmp = (AssetNode)this[assetName];
                if (tmp.asset != null && tmp.asset.GetType() == type)
                {
                    node = tmp;
                    return true;
                }
            }
            if (config.EXISTINGASSETS)
            {
                Object asset = Resources.Load("NetworkModel/" + assetName, type);
                if (asset != null)
                {
                    node = new AssetNode(asset, assetName, serializer);
                    Add(assetName, node);
                    return true;
                }
            }

            node = null;
            return false;
        }

        /// <summary>
        /// Determine name that a Asset should be stored under in the store. Will return current storage name if Asset is already present or new free name if not. 
        /// Asset's name should be set to returned name if it is to be stored.
        /// </summary>
        /// <param name="asset"></param>
        /// <returns>Name the Asset should be stored under</returns>
        internal string GetReferenceName(Object asset)
        {
            if (asset == null)
            {
                return "null";
            }
            string refName = asset.name;
            while (this.Contains(refName) && ((AssetNode)this[refName]).asset.GetInstanceID() != asset.GetInstanceID())
            {
                refName = refName + "_" + asset.GetInstanceID().ToString();
            }
            return refName;
        }

        /// <summary>
        /// Stores specific Asset, tracking its last known state through a hash
        /// </summary>
        public class AssetNode
        {
            public Object asset;
            public string name;
            private Serializer serializer;
            public Type type;
            public long hash = 0;

            /// <summary>
            /// Wraps an Asset into a Node
            /// </summary>
            /// <param name="asset"></param>
            /// <param name="serializer"></param>
            public AssetNode(Object asset, string name, Serializer serializer)
            {
                this.name = name;
                if (asset != null)
                {
                    this.asset = asset;
                    this.type = asset.GetType();
                }
                this.serializer = serializer;
            }

            /// <summary>
            /// Updates stored hash of Asset to match current hash
            /// </summary>
            public void UpdateHash()
            {
                hash = serializer.ToSerializableAsset(asset).GetHash();
            }
        }
    }

}