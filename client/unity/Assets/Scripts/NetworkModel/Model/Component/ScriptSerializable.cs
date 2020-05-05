/**
 * 
 * Script Clone Script of Unity Network Model
 *
 * @file ScriptClone.cs
 * @author Uwe Gruenefeld
 * @version 2020-05-04
 *
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable Script
    /// </summary>
    [Serializable]
    internal class ScriptSerializable : AbstractComponent
    {
        public string t;    // Type
        public string v;    // Values

        /// <summary>
        /// Creates serializable Script from Unity component
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        public ScriptSerializable(Injector injector, Component component) : base(injector, component)
        {
            this.t = component.GetType().AssemblyQualifiedName;
            this.v = JsonUtility.ToJson(component);
        }

        /// <summary>
        /// Return common type of component
        /// </summary>
        /// <returns></returns>
        public override Type commonType { get { return typeof(MonoBehaviour); } }

        /// <summary>
        /// Applies parameters saved in the serialized MonoBehaviour to a Unity MonoBehaviour
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public override bool Apply(Injector injector, Component component)
        {
            JsonUtility.FromJsonOverwrite(this.v, component);
            return true;
        }

        /// <summary>
        /// Return Script type
        /// </summary>
        /// <returns></returns>
        public string ScriptType()
        {
            return this.t;
        }

        /// <summary>
        /// Returns Hash value of this serializable class
        /// </summary>
        /// <returns></returns>
        public override long GetHash()
        {
            // Combine the Hashes of all variables
            return HashUtility.CombineHashes(
                this.t.GetHashCode(),
                this.v.GetHashCode()
            );
        }
    }
}