/**
 * 
 * Abstract Component Script of Unity Network Model
 *
 * @file AbstractComponent.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-04-30
 **/
using System;
using System.Reflection;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Super class for all serializable Components
    /// </summary>
    [Serializable]
    internal abstract class AbstractComponent
    {
        /// <summary>
        /// Creates serializable Component from Unity component
        /// </summary>
        /// <param name="component"></param>
        public AbstractComponent(Component component)
        {

        }

        /// <summary>
        /// Applies parameters saved in the serialized component to a unity component
        /// </summary>
        /// <param name="component"></param>
        /// <param name="resourceStore"></param>
        /// <returns></returns>
        abstract public bool Apply(Component component, ResourceStore resourceStore);

        /// <summary>
        /// Returns value-based, order-dependent hash. Needs to be overwritten for components containing arrays
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
 * TEMPLATE FOR NEW COMPONENT
 * 
 * [Serializable]
 * internal class NAMEOFCOMPONENTClone : AbstractComponent
 * {
 *     CLASS VARIABLES TO SYNCHRONIZE
 *     
 *     // Prepare component for sending to server
 *     public NAMEOFCOMPONENTClone(Component component, ResourceStore resourceStore) : base(component)
 *     {
 *          SAVE VARIABLES FROM COMPONENT IN CLASS VARIABLES
 *     }
 *     
 *     // Apply received values to component
 *     public override void Apply (Component component, ResourceStore resourceStore)
 *     {
 *          RESTORE CLASS VARIABLES INTO VARIABLES FROM COMPONENT
 *     }
 *
 *     // Override Hash if component contains array. Apply loop for each array.
 *     // public override long GetHash()
 *     // {
 *     //     long hash = base.GetHash();
 *     //     foreach (ELEMENT val in ARRAY)
 *     //     {
 *     //         hash = Serializer.CombineHashes(hash, val.GetHashCode());
 *     //     }
 *     //     return hash;
 *     // }
 * }
 *
 */