/**
 * 
 * Line Renderer Clone Script of Unity Network Model
 *
 * @file LineRendererClone.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-04-30
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable LineRenderer
    /// </summary>
    [Serializable]
    internal class LineRendererClone : AbstractComponent
    {
        public string m;
        public Vector3[] p;
        public float w;
        public bool l;
        public int ca, co;
        public LineTextureMode t;
        public Color cs, ce;

        public LineRendererClone(Component component, ResourceStore resourceStore) : base(component)
        {
            LineRenderer lineRenderer = (LineRenderer)component;

            if (lineRenderer.sharedMaterial == null)
            {
                this.m = "null";
            }
            else
            {
                lineRenderer.sharedMaterial.name = resourceStore.GetReferenceName(lineRenderer.sharedMaterial);
                if (!resourceStore.Contains(lineRenderer.sharedMaterial.name))
                {
                    resourceStore.Add(lineRenderer.sharedMaterial);
                }
                this.m = lineRenderer.sharedMaterial.name;
            }

            this.p = new Vector3[lineRenderer.positionCount];
            lineRenderer.GetPositions(this.p);

            this.w = lineRenderer.widthMultiplier;
            this.l = lineRenderer.loop;
            this.ca = lineRenderer.numCapVertices;
            this.co = lineRenderer.numCornerVertices;
            this.t = lineRenderer.textureMode;
            this.cs = lineRenderer.startColor;
            this.ce = lineRenderer.endColor;
        }

        public override bool Apply(Component component, ResourceStore resourceStore)
        {
            ResourceNode resourceNode;
            if (!resourceStore.TryGet(this.m, typeof(Material), out resourceNode))
            {
                return false;
            }

            LineRenderer lineRenderer = (LineRenderer)component;
            lineRenderer.material = (Material)resourceNode.resource;
            lineRenderer.positionCount = this.p.Length;
            lineRenderer.SetPositions(this.p);
            lineRenderer.widthMultiplier = this.w;
            lineRenderer.loop = this.l;
            lineRenderer.numCapVertices = this.ca;
            lineRenderer.numCornerVertices = this.co;
            lineRenderer.textureMode = this.t;
            lineRenderer.startColor = this.cs;
            lineRenderer.endColor = this.ce;

            return true;
        }

        public override long GetHash()
        {
            long hash = base.GetHash();
            foreach (Vector3 val in this.p)
            {
                hash = Serializer.CombineHashes(hash, val.GetHashCode());
            }
            return hash;
        }
    }
}