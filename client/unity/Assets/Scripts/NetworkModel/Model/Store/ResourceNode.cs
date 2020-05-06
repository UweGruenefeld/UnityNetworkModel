/**
 * 
 * Resource Node Script of Unity Network Model
 *
 * @file ResourceNode.cs
 * @author Tobias Lunte, Uwe Gruenefeld
 * @version 2020-04-30
 *
 **/
using System;

namespace UnityNetworkModel 
{
    /// <summary>
    /// Stores specific Resource, tracking its last known state through a hash
    /// </summary>
    internal class ResourceNode : AbstractInjector
    {
        public UnityEngine.Object resource;
        public string name;
        public Type type;
        public long hash = 0;

        /// <summary>
        /// Wraps a Resource into a Node
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="resource"></param>
        /// <param name="name"></param>
        internal ResourceNode(Injector injector, UnityEngine.Object resource, string name) : base(injector)
        {
            // Check if resource is null
            if (resource != null)
            {
                // Store resource information
                this.resource = resource;
                this.type = resource.GetType();
            }
            this.name = name;
        }

        /// <summary>
        /// Updates stored hash of Resource to match current hash
        /// </summary>
        internal void UpdateHash()
        {
            this.hash = this.injector.serializer.ToSerializableResource(this.resource).GetHash();
        }
    }
}