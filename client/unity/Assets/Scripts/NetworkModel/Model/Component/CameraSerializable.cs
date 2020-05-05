/**
 * 
 * Serializable Camera of Unity Network Model
 *
 * @file CameraSerializable.cs
 * @author Uwe Gruenefeld
 * @version 2020-05-02
 *
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable Camera
    /// </summary>
    [Serializable]
    internal class CameraSerializable : AbstractComponent
    {
        public float d, n, f, v;    // Depth, NearClipPlane, FarClipPlane, FieldOfView
        public Color b;             // BackgroundColor
        public CameraClearFlags c;  // ClearFlags

        /// <summary>
        /// Creates serializable Camera from Unity component
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        public CameraSerializable(Injector injector, Component component) : base(injector, component)
        {
            Camera camera = (Camera)component;

            this.d = CompressionUtility.Compress(component.gameObject, camera.depth);
            this.n = CompressionUtility.Compress(component.gameObject, camera.nearClipPlane);
            this.f = CompressionUtility.Compress(component.gameObject, camera.farClipPlane);
            this.v = CompressionUtility.Compress(component.gameObject, camera.fieldOfView);
            this.b = CompressionUtility.Compress(component.gameObject, camera.backgroundColor);
            this.c = camera.clearFlags;
        }

        /// <summary>
        /// Return common type of component
        /// </summary>
        /// <returns></returns>
        public override Type commonType { get { return typeof(Camera); } }

        /// <summary>
        /// Applies parameters saved in the serialized Camera to a Unity Camera
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public override bool Apply(Injector injector, Component component)
        {
            Camera camera = (Camera)component;

            camera.depth = this.d;
            camera.nearClipPlane = this.n;
            camera.farClipPlane = this.f;
            camera.fieldOfView = this.v;
            camera.backgroundColor = this.b;
            camera.clearFlags = this.c;

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
                this.d.GetHashCode(),
                this.n.GetHashCode(),
                this.f.GetHashCode(),
                this.v.GetHashCode(),
                this.b.GetHashCode(),
                this.c.GetHashCode()
            );
        }
    }
}