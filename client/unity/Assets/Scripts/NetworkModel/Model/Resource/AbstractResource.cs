/**
 * 
 * Abstract Resource Script of Unity Network Model
 *
 * @file AbstractResourceClone.cs
 * @author Tobias Lunte, Uwe Gruenefeld
 * @version 2020-04-30
 **/
using System;
using System.Reflection;

namespace UnityNetworkModel
{
    /// <summary>
    /// Super class for all serializable Assets
    /// </summary>
    [Serializable]
    internal abstract class AbstractResource
    {
        protected ResourceStore resourceStore;
        protected abstract Type commonType { get; }

        /// <summary>
        /// Creates serializable Asset from Unity asset
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="resourceStore"></param>
        public AbstractResource(System.Object resource, ResourceStore resourceStore)
        {
            this.resourceStore = resourceStore;
        }

        /// <summary>
        /// Applies parameters stored in serialized asset to Unity asset
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="resourceStore"></param>
        /// <returns></returns>
        abstract public bool Apply(System.Object resource, ResourceStore resourceStore);

        /// <summary>
        /// Create Unity resource of matching type, setting final parameters where necessary. Needs to be overwritten for resources using final parameters
        /// </summary>
        /// <returns></returns>
        public virtual UnityEngine.Object Construct()
        {
            return (UnityEngine.Object)Activator.CreateInstance(commonType);
        }

        /// <summary>
        /// Returns value-based, order-dependent hash. Needs to be overwritten for assets containing arrays
        /// </summary>
        /// <returns></returns>
        public virtual long GetHash()
        {
            long hash = 17;
            FieldInfo[] fields = this.GetType().GetFields();
            foreach (FieldInfo field in fields)
            {
                if (!field.FieldType.IsArray)
                {
                    hash = Serializer.CombineHashes(hash, field.GetValue(this).GetHashCode());
                }
            }
            return hash;
        }
    }
}

/*
 * TEMPLATE FOR NEW RESOURCE
 * 
 * [Serializable]
 * internal class NAMEOFRESOURCEClone : AbstractResource
 * {
 *     protected override Type commonType { get { return typeof(UnityEngine.NAMEOFRESOURCE); } }
 *
 *     CLASS VARIABLES TO SYNCHRONIZE
 *     
 *     // Prepare component for sending to server
 *     public NAMEOFRESOURCEClone(System.Object resource, ResourceStore resourceStore) : base(resource, resourceStore)
 *     {
 *          SAVE VARIABLES FROM COMPONENT IN CLASS VARIABLES
 *     }
 *     
 *     // Apply received values to resource
 *     public override void Apply (System.Object resource, ResourceStore resourceStore)
 *     {
 *          RESTORE CLASS VARIABLES INTO VARIABLES FROM COMPONENT
 *     }
 *     
 *     // Override if parameters need to be set during initialization
 *     // public override Object Construct()
 *     // {
 *     //      
 *     // }
 *     
 *     // Override if Resource contains arrays. Apply loop for each array
 *     // public override string GetHash() {}
 *     // {
 *     //      long hash = base.GetHash();
 *     //      foreach (ELEMENT val in ARRAY)
 *     //      {
 *     //          hash = Serializer.CombineHashes(hash, val.GetHashCode());
 *     //      }
 *     //      return hash;
 *     // }
 * }
 *
 */
