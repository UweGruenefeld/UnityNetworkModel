/**
 * 
 * Serializable Light of Unity Network Model
 *
 * @file LightSerializable.cs
 * @author Uwe Gruenefeld
 * @version 2020-05-02
 *
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable Light
    /// </summary>
    [Serializable]
    internal class LightSerializable : AbstractComponent
    {
        public LightType t;     // Type
        public Color c;         // Color
        public float i, b;      // Intensity, BounceIntensity

        /// <summary>
        /// Creates serializable Light from Unity component
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        public LightSerializable(Injector injector, Component component) : base(injector, component)
        {
            Light light = (Light)component;

            this.t = light.type;
            this.c = CompressionUtility.Compress(component.gameObject, light.color);
            this.i = CompressionUtility.Compress(component.gameObject, light.intensity);
            this.b = CompressionUtility.Compress(component.gameObject, light.bounceIntensity);
        }

        /// <summary>
        /// Return common type of component
        /// </summary>
        /// <returns></returns>
        public override Type commonType { get { return typeof(Light); } }

        /// <summary>
        /// Applies parameters saved in the serialized Light to a Unity Light
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public override bool Apply(Injector injector, Component component)
        {
            Light light = (Light)component;

            light.type = this.t;
            light.color = this.c;
            light.intensity = this.i;
            light.bounceIntensity = this.b;

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
                this.t.GetHashCode(),
                this.c.GetHashCode(),
                this.i.GetHashCode(),
                this.b.GetHashCode()
            );
        }
    }
}