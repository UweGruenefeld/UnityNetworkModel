/**
 * 
 * Abstract Component Script of Unity Network Model
 *
 * @file AbstractComponent.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-04-30
 *
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Abstract class for all serializable Components
    /// </summary>
    [Serializable]
    internal abstract class AbstractComponent
    {
        protected Injector injector;

        /// <summary>
        /// Creates serializable Component from Unity component
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        public AbstractComponent(Injector injector, Component component)
        {
            this.injector = injector;
        }

        /// <summary>
        /// Return common type of component
        /// </summary>
        /// <returns></returns>
        abstract public Type commonType { get; }

        /// <summary>
        /// Applies parameters saved in the serialized component to a unity component
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        abstract public bool Apply(Injector injector, Component component);

        /// <summary>
        /// Returns value-based, order-dependent hash. Needs to be overwritten for components
        /// </summary>
        /// <returns></returns>
        abstract public long GetHash();
    }
}

/*
 * TEMPLATE FOR NEW COMPONENT
 * 
 * [Serializable]
 * internal class NAMEOFCOMPONENTSerializable : AbstractComponent
 * {
 *     CLASS VARIABLES TO SYNCHRONIZE
 *     
 *     // Prepare component for sending to server
 *     public NAMEOFCOMPONENTSerializable(Injector injector, Component component) : base(injector, component)
 *     {
 *          SAVE VARIABLES FROM COMPONENT IN CLASS VARIABLES
 *     }
 *     
 *     // Apply received values to component
 *     public override void Apply (Injector injector, Component component) : base(injector, component)
 *     {
 *          RESTORE CLASS VARIABLES INTO VARIABLES FROM COMPONENT
 *     }
 *
 *     // Returns value-based, order-dependent hash. Needs to be overwritten for components
 *     // public override long GetHash()
 *     // {
 *     //     COMBINE ALL VARIABLE HASHES
 *     // }
 * }
 *
 */