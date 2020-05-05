/**
 * 
 * Abstract Resource Script of Unity Network Model
 *
 * @file AbstractResourceClone.cs
 * @author Tobias Lunte, Uwe Gruenefeld
 * @version 2020-05-04
 *
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Abstract class for all serializable Resources
    /// </summary>
    [Serializable]
    internal abstract class AbstractResource
    {
        protected Injector injector;

        /// <summary>
        /// Creates serializable Resource from Unity Resource
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="resource"></param>
        public AbstractResource(Injector injector, UnityEngine.Object resource)
        {
            this.injector = injector;
        }

        /// <summary>
        /// Return common type of resource
        /// </summary>
        /// <returns></returns>
        abstract public Type commonType { get; }

        /// <summary>
        /// Applies parameters stored in serialized Resource to Unity Resource
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        abstract public bool Apply(Injector injector, UnityEngine.Object resource);

        /// <summary>
        /// Create Unity resource of matching type, setting final parameters where necessary. Needs to be overwritten for resources using final parameters
        /// </summary>
        /// <returns></returns>
        public virtual UnityEngine.Object Construct()
        {
            return (UnityEngine.Object)Activator.CreateInstance(commonType);
        }

        /// <summary>
        /// Returns value-based, order-dependent hash. Needs to be overwritten for resources
        /// </summary>
        /// <returns></returns>
        abstract public long GetHash();
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
 *     // Prepare resource for sending to server
 *     public NAMEOFRESOURCEClone(Injector injector, System.Object resource) : base(injector, resource)
 *     {
 *          SAVE VARIABLES FROM RESOURCE IN CLASS VARIABLES
 *     }
 *     
 *     // Apply received values to resource
 *     public override void Apply (Injector injector, System.Object resource)
 *     {
 *          RESTORE CLASS VARIABLES INTO VARIABLES FROM RESOURCE
 *     }
 *     
 *     // Override if parameters need to be set during initialization
 *     // public override Object Construct()
 *     // {
 *     //      
 *     // }
 *     
 *     // Returns value-based, order-dependent hash. Needs to be overwritten for resources
 *     public override long GetHash()
 *     {
 *          COMBINE ALL VARIABLE HASHES
 *     }
 * }
 *
 */
