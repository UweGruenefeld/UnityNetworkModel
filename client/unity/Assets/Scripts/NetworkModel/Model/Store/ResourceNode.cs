/**
 * 
 * Resource Node Script of Unity Network Model
 *
 * @file ResourceNode.cs
 * @author Tobias Lunte, Uwe Gruenefeld
 * @version 2020-04-30
 **/
using System;

namespace UnityNetworkModel 
{
    /// <summary>
    /// Stores specific Resource, tracking its last known state through a hash
    /// </summary>
    internal class ResourceNode
    {
        public UnityEngine.Object resource;
        public string name;
        private Serializer serializer;
        public Type type;
        public long hash = 0;

        /// <summary>
        /// Wraps a Resource into a Node
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="name"></param>
        /// <param name="serializer"></param>

        public ResourceNode(UnityEngine.Object resource, string name, Serializer serializer)
        {
            if (resource != null)
            {
                this.resource = resource;
                this.type = resource.GetType();
            }
            this.name = name;
            this.serializer = serializer;
        }

        /// <summary>
        /// Updates stored hash of Asset to match current hash
        /// </summary>
        public void UpdateHash()
        {
            hash = serializer.ToSerializableResource(this.resource).GetHash();
        }
    }
}