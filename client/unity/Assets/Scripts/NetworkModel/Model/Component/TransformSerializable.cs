/**
 * 
 * Transform Clone Script of Unity Network Model
 *
 * @file TransformClone.cs
 * @author Uwe Gruenefeld
 * @version 2020-05-04
 *
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable Transform
    /// </summary>
    [Serializable]
    internal class TransformSerializable : AbstractComponent
    {
        public Vector3 p, s;    // LocalPosition, LocalScale
        public Quaternion r;    // LocalRotation
        public string t;        // Tags

        /// <summary>
        /// Creates serializable Transform from Unity component
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        public TransformSerializable(Injector injector, Component component) : base(injector, component)
        {
            Transform transform = (Transform)component;

            this.p = CompressionUtility.Compress(component.gameObject, transform.localPosition);
            this.s = CompressionUtility.Compress(component.gameObject, transform.localScale);
            this.r = transform.localRotation;
            this.t = transform.tag;
        }

        /// <summary>
        /// Return common type of component
        /// </summary>
        /// <returns></returns>
        public override Type commonType { get { return typeof(Transform); } }

        /// <summary>
        /// Applies parameters saved in the serialized Transform to a Unity Transform
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public override bool Apply(Injector injector, Component component)
        {
            Transform transform = (Transform)component;

            // If there was no change on client side then use server values
            if (!transform.hasChanged)
            {
                transform.localPosition = this.p;
                transform.localRotation = this.r;
                transform.localScale = this.s;
                transform.tag = this.t;

                // Avoid triggering update of changes
                transform.hasChanged = false;
            }
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
                this.p.GetHashCode(),
                this.s.GetHashCode(),
                this.r.GetHashCode(),
                this.t.GetHashCode()
            );
        }
    }
}