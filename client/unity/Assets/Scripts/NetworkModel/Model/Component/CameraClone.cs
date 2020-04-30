/**
 * 
 * Camera Clone Script of Unity Network Model
 *
 * @file CameraClone.cs
 * @author Uwe Gruenefeld
 * @version 2020-04-30
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable Camera
    /// </summary>
    [Serializable]
    internal class CameraClone : AbstractComponent
    {
        public float d, n, f, v;
        public Color b;
        public CameraClearFlags c;

        public CameraClone(Component component, ResourceStore resourceStore) : base(component)
        {
            Camera camera = (Camera)component;

            this.d = camera.depth;
            this.n = camera.nearClipPlane;
            this.f = camera.farClipPlane;
            this.v = camera.fieldOfView;
            this.b = camera.backgroundColor;
            this.c = camera.clearFlags;
        }

        public override bool Apply(Component component, ResourceStore resourceStore)
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
    }
}