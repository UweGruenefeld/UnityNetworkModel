/**
 * 
 * Texture2D Clone Script of Unity Network Model
 *
 * @file Texture2DClone.cs
 * @author Tobias Lunte, Uwe Gruenefeld
 * @version 2020-05-04
 *
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    [Serializable]
    internal class Texture2DSerializable : AbstractResource
    {
        public int w, h;    // Width, Height
        public Color[] p;   // Pixels

        /// <summary>
        /// Creates serializable Texture2D from Unity resource
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="resource"></param>
        public Texture2DSerializable(Injector injector, UnityEngine.Object resource) : base(injector, resource)
        {
            Texture2D texture = (Texture2D)resource;

            this.w = texture.width;
            this.h = texture.height;

            try
            {
                this.p = CompressionUtility.Compress(this.injector.configuration.gameObject, texture.GetPixels());
            }
            catch(Exception) {}
        }

        /// <summary>
        /// Return common type of resource
        /// </summary>
        /// <returns></returns>
        public override Type commonType { get { return typeof(Texture2D); } }

        /// <summary>
        /// Applies parameters saved in the serialized Texture2D to a Unity Texture2D
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public override bool Apply(Injector injector, UnityEngine.Object resource)
        {
            Texture2D texture = (Texture2D)resource;

            if(this.p != null && this.p.Length > 0)
            {
                texture.SetPixels(this.p);
                texture.Apply();
            }

            return true;
        }

        /// <summary>
        /// Construct a Unity Texture2D
        /// </summary>
        /// <returns></returns>
        public override UnityEngine.Object Construct()
        {
            return new Texture2D(this.w, this.h);
        }

        /// <summary>
        /// Returns Hash value of this serializable class
        /// </summary>
        /// <returns></returns>
        public override long GetHash()
        {
            // Combine the Hashes of all variables
            long hash = HashUtility.CombineHashes(
                this.w.GetHashCode(),
                this.h.GetHashCode()
            );

            if(this.p != null)
                foreach (Color color in this.p)
                    hash = HashUtility.Combine2Hashes(hash, color.GetHashCode());
            
            return hash;
        }
    }
}