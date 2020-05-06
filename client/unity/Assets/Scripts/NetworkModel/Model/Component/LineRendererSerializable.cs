/**
 * 
 * Serializable LineRenderer of Unity Network Model
 *
 * @file LineRendererSerializable.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-05-02
 *
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable LineRenderer
    /// </summary>
    [Serializable]
    internal class LineRendererSerializable : AbstractComponent
    {
        public string m;            // SharedMaterial
        public Vector3[] p;         // ArrayOfPositions
        public float w;             // WidthMultiplier
        public bool l;              // Loop
        public int ca, co;          // NumberOfCapVertices, NumberOfCornerVertices
        public LineTextureMode t;   // TextureMode
        public Color cs, ce;        // StartColor, EndColor

        /// <summary>
        /// Creates serializable LineRenderer from Unity component
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        public LineRendererSerializable(Injector injector, Component component) : base(injector, component)
        {
            LineRenderer lineRenderer = (LineRenderer)component;

            // Save the used Shared Material
            if (lineRenderer.sharedMaterial == null)
                this.m = "null";    // No Material set
            else
            {
                // Get reference or unique name for the material resource
                lineRenderer.sharedMaterial.name = injector.resourceStore.GetReferenceName(lineRenderer.sharedMaterial);

                // Check if resource is already in ResourceStore
                if (!injector.resourceStore.Contains(lineRenderer.sharedMaterial.name))
                    injector.resourceStore.Add(lineRenderer.sharedMaterial);

                // Store the new Material name
                this.m = lineRenderer.sharedMaterial.name;
            }

            this.p = new Vector3[lineRenderer.positionCount];
            lineRenderer.GetPositions(this.p);

            this.p = CompressionUtility.Compress(component.gameObject, this.p);
            this.w = CompressionUtility.Compress(component.gameObject, lineRenderer.widthMultiplier);
            this.l = lineRenderer.loop;
            this.ca = lineRenderer.numCapVertices;
            this.co = lineRenderer.numCornerVertices;
            this.t = lineRenderer.textureMode;
            this.cs = CompressionUtility.Compress(component.gameObject, lineRenderer.startColor);
            this.ce = CompressionUtility.Compress(component.gameObject, lineRenderer.endColor);
        }

        /// <summary>
        /// Return common type of component
        /// </summary>
        /// <returns></returns>
        public override Type commonType { get { return typeof(LineRenderer); } }

        /// <summary>
        /// Applies parameters saved in the serialized LineRenderer to a Unity LineRenderer
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public override bool Apply(Injector injector, Component component)
        {
            // Get reference to component
            LineRenderer lineRenderer = (LineRenderer)component;

            lineRenderer.positionCount = this.p.Length;
            lineRenderer.SetPositions(this.p);
            lineRenderer.widthMultiplier = this.w;
            lineRenderer.loop = this.l;
            lineRenderer.numCapVertices = this.ca;
            lineRenderer.numCornerVertices = this.co;
            lineRenderer.textureMode = this.t;
            lineRenderer.startColor = this.cs;
            lineRenderer.endColor = this.ce;

            // Check if Material is null
            if(this.m == null || this.m == "null")
            {
                lineRenderer.material = null;
                lineRenderer.sharedMaterial = null;
                return true;
            }

            // Find resource in Store
            ResourceNode resourceNode;
            if (!injector.resourceStore.TryGet(this.m, typeof(Material), out resourceNode))
            {
                // If resource not found, then Unity component cannot be created
                return false;
            }

            // Apply the Material to the Component
            lineRenderer.material = (Material)resourceNode.resource;

            return true;
        }

        /// <summary>
        /// Returns Hash value of this serializable class
        /// </summary>
        /// <returns></returns>
        public override long GetHash()
        {
            // Combine the Hashes of all variables
            long hash = HashUtility.CombineHashes(
                this.m.GetHashCode(),
                this.w.GetHashCode(),
                this.l.GetHashCode(),
                this.ca.GetHashCode(),
                this.co.GetHashCode(),
                this.t.GetHashCode(),
                this.cs.GetHashCode(),
                this.ce.GetHashCode()
            );

            foreach (Vector3 position in this.p)
                hash = HashUtility.Combine2Hashes(hash, position.GetHashCode());
            
            return hash;
        }
    }
}