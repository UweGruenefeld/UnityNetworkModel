/**
 * 
 * Sphere Collider Clone Script of Unity Network Model
 *
 * @file SphereColliderClone.cs
 * @author Tobias Lunte, Uwe Gruenefeld
 * @version 2020-05-04
 *
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable SphereCollider
    /// </summary>
    internal class SphereColliderSerializable : AbstractComponent
    {
        public Vector3 c;   // Center
        public float r;     // Radius

        /// <summary>
        /// Creates serializable SphereCollider from Unity component
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        public SphereColliderSerializable(Injector injector, Component component) : base(injector, component)
        {
            SphereCollider spherecollider = (SphereCollider)component;

            this.c = CompressionUtility.Compress(component.gameObject, spherecollider.center);
            this.r = CompressionUtility.Compress(component.gameObject, spherecollider.radius);
        }

        /// <summary>
        /// Return common type of component
        /// </summary>
        /// <returns></returns>
        public override Type commonType { get { return typeof(SphereCollider); } }

        /// <summary>
        /// Applies parameters saved in the serialized SphereCollider to a Unity SphereCollider
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public override bool Apply(Injector injector, Component component)
        {
            SphereCollider spherecollider = (SphereCollider)component;

            spherecollider.center = this.c;
            spherecollider.radius = this.r;

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
                this.r.GetHashCode()
            );
        }
    }
}