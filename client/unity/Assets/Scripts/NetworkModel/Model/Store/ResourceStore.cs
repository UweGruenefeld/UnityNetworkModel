/**
 * 
 * Resource Store Script of Unity Network Model
 *
 * @file ResourceStore.cs
 * @author Tobias Lunte, Uwe Gruenefeld
 * @version 2020-05-05
 *
 **/
using System;
using System.Collections.Specialized;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Collects all known Resources and tracks their last known state.
    /// </summary>
    internal class ResourceStore : OrderedDictionary
    {
        private Injector injector;

        /// <summary>
        /// Create a new ResourceStore
        /// </summary>
        /// <param name="injector"></param>
        internal ResourceStore(Injector injector)
        {
            this.injector = injector;
        }

        /// <summary>
        /// Adds Resource to store
        /// </summary>
        /// <param name="resource"></param>
        internal ResourceNode Add(UnityEngine.Object resource)
        {
            ResourceNode node = new ResourceNode(this.injector, resource, resource.name);
            this.Add(resource.name, node);
            return node;
        }

        /// <summary>
        /// Adds Resource to store with specific name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        internal ResourceNode Add(string name, UnityEngine.Object resource)
        {
            ResourceNode node = new ResourceNode(this.injector, resource, name);
            this.Add(name, node);
            return node;
        }

        /// <summary>
        /// Tries to find a Resource of correct name and type by priority
        /// 1) A matching tracked Resource from the store.
        /// 2) A matching Resource of the correct type from the Resources folder (if using existing resources is enabled).
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="type"></param>
        /// <param name="node"></param>
        /// <returns>Returns true if a matching Resource was found.</returns>
        internal bool TryGet(string resourceName, Type type, out ResourceNode node)
        {
            // Check if ResourceStore contains Resource with name
            if (this.Contains(resourceName))
            {
                // Get ResourceNode for resource with name
                ResourceNode resource = (ResourceNode)this[resourceName];

                // Check if ResourceNode is of the correct type
                if (resource.resource != null && resource.resource.GetType() == type)
                {
                    node = resource;
                    return true;
                }
            }

            // Should existing resources be preferred
            if (this.injector.configuration.EXISTINGRESOURCES)
            {
                // Try to load exsting resource
                UnityEngine.Object resource = Resources.Load("NetworkModel/" + resourceName, type);

                // Check if loading of resource failed
                if (resource != null)
                {
                    // Create a new ResourceNode
                    node = new ResourceNode(this.injector, resource, resourceName);
                    Add(resourceName, node);
                    return true;
                }
            }

            // No ResourceNode found
            node = null;
            return false;
        }

        /// <summary>
        /// Determine name that a Resource should be stored under in the store. Will return current storage name if Resource is already present or new free name if not. 
        /// Resource's name should be set to returned name if it is to be stored.
        /// </summary>
        /// <param name="resource"></param>
        /// <returns>Name the Resource should be stored under</returns>
        internal string GetReferenceName(UnityEngine.Object resource)
        {
            // Check if resource is null
            if (resource == null)
                return "null";
            
            // Find the name of the Resource
            string referenceName = resource.name;
            while (this.Contains(referenceName) && ((ResourceNode)this[referenceName]).resource.GetInstanceID() != resource.GetInstanceID())
                referenceName = referenceName + "_" + resource.GetInstanceID().ToString();
            
            // Return the Resource name
            return referenceName;
        }
    }

}