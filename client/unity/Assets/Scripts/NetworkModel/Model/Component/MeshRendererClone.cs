/**
 * 
 * Mesh Renderer Clone Script of Unity Network Model
 *
 * @file MeshRendererClone.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-04-30
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable MeshRenderer
    /// </summary>
    [Serializable]
    internal class MeshRendererClone : AbstractComponent
    {
        public string m;

        public MeshRendererClone(Component component, ResourceStore resourceStore) : base(component)
        {
            MeshRenderer meshRenderer = (MeshRenderer)component;
            if (meshRenderer.sharedMaterial == null)
            {
                this.m = "null";
            }
            else
            {
                meshRenderer.sharedMaterial.name = resourceStore.GetReferenceName(meshRenderer.sharedMaterial);
                if (!resourceStore.Contains(meshRenderer.sharedMaterial.name))
                {
                    resourceStore.Add(meshRenderer.sharedMaterial);
                }
                this.m = meshRenderer.sharedMaterial.name;
            }
        }

        public override bool Apply(Component component, ResourceStore resourceStore)
        {
            ResourceNode resourceNode;
            if (!resourceStore.TryGet(this.m, typeof(Material), out resourceNode))
            {
                return false;
            }

            MeshRenderer meshRenderer = (MeshRenderer)component;
            meshRenderer.material = (Material)resourceNode.resource;

            return true;
        }
    }
}