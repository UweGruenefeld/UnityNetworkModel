/**
 * 
 * Serializable BoxCollider of Unity Network Model
 *
 * @file BoxColliderSerializable.cs
 * @author Uwe Gruenefeld
 * @version 2020-05-02
 *
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable BoxCollider
    /// </summary>
    [Serializable]
    internal class BoxColliderSerializable : AbstractComponent
    {
        public Vector3 c, s;    // Center, Size

        /// <summary>
        /// Creates serializable BoxCollider from Unity component
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        public BoxColliderSerializable(Injector injector, Component component) : base(injector, component)
        {
            BoxCollider boxcollider = (BoxCollider)component;

            this.c = CompressionUtility.Compress(component.gameObject, boxcollider.center);
            this.s = CompressionUtility.Compress(component.gameObject, boxcollider.size);
        }

        /// <summary>
        /// Return common type of component
        /// </summary>
        /// <returns></returns>
        public override Type commonType { get { return typeof(BoxCollider); } }

        /// <summary>
        /// Applies parameters saved in the serialized BoxCollider to a Unity BoxCollider
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public override bool Apply(Injector injector, Component component)
        {
            BoxCollider boxcollider = (BoxCollider)component;

            boxcollider.center = this.c;
            boxcollider.size = this.s;

            return true;
        }

        /// <summary>
        /// Returns Hash value of this serializable class
        /// </summary>
        /// <returns></returns>
        public override long GetHash()
        {
            // Combine the Hashes of all variables
            return HashUtility.CombineHashes(
                this.c.GetHashCode(),
                this.s.GetHashCode()
            );
        }
    }
}