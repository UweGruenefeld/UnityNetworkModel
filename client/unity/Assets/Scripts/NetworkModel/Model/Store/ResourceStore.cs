/**
 * 
 * Resource Store Script of Unity Network Model
 *
 * @file ResourceStore.cs
 * @author Tobias Lunte, Uwe Gruenefeld
 * @version 2020-04-30
 **/
using System;
using System.Collections.Specialized;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Collects all known Resources and tracks their last known state.
    /// </summary>
    internal class ResourceStore : OrderedDictionary/*<string, AssetStore.AssetNode>*/
    {
        private NetworkModel config;
        private Bundle bundle;

        public ResourceStore(NetworkModel config, Bundle bundle)
        {
            this.config = config;
            this.bundle = bundle;
        }

        /// <summary>
        /// Adds Resource to store
        /// </summary>
        /// <param name="resource"></param>
        internal void Add(UnityEngine.Object resource)
        {
            this.Add(resource.name, resource);
        }

        /// <summary>
        /// Adds Resource to store with specific name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="resource"></param>
        internal void Add(string name, UnityEngine.Object ressource)
        {
            this.Add(name, new ResourceNode(ressource, name, this.bundle.serializer));
        }

        /// <summary>
        /// Tries to find a Resource of correct name and type by priority
        /// 1) A matching tracked Resource from the store.
        /// 2) A matching Resource of the correct type from the Resources folder (if using existing assets is enabled).
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="type"></param>
        /// <param name="node"></param>
        /// <returns>Returns true if a matching Resource was found.</returns>
        internal bool TryGet(string resourceName, Type type, out ResourceNode node)
        {
            if (this.Contains(resourceName))
            {
                ResourceNode tmp = (ResourceNode)this[resourceName];
                if (tmp.resource != null && tmp.resource.GetType() == type)
                {
                    node = tmp;
                    return true;
                }
            }
            if (config.EXISTINGRESOURCES)
            {
                UnityEngine.Object resource = Resources.Load("NetworkModel/" + resourceName, type);
                if (resource != null)
                {
                    node = new ResourceNode(resource, resourceName, this.bundle.serializer);
                    Add(resourceName, node);
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
        internal string GetReferenceName(UnityEngine.Object resource)
        {
            if (resource == null)
            {
                return "null";
            }
            string refName = resource.name;
            while (this.Contains(refName) && ((ResourceNode)this[refName]).resource.GetInstanceID() != resource.GetInstanceID())
            {
                refName = refName + "_" + resource.GetInstanceID().ToString();
            }
            return refName;
        }
    }

}