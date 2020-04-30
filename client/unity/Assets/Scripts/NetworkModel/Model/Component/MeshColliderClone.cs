/**
 * 
 * Mesh Collider Clone Script of Unity Network Model
 *
 * @file MeshColliderClone.cs
 * @author Uwe Gruenefeld
 * @version 2020-04-30
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable MeshCollider
    /// </summary>
    [Serializable]
    internal class MeshColliderClone : AbstractComponent
    {
        public string m;

        public MeshColliderClone(Component component, ResourceStore resourceStore) : base(component)
        {
            MeshCollider meshCollider = (MeshCollider)component;
            if (meshCollider.sharedMesh == null)
            {
                this.m = "null";
            }
            else
            {
                meshCollider.sharedMesh.name = resourceStore.GetReferenceName(meshCollider.sharedMesh);
                if (!resourceStore.Contains(meshCollider.sharedMesh.name))
                {
                    resourceStore.Add(meshCollider.sharedMesh);
                }
                this.m = meshCollider.sharedMesh.name;
            }
        }

        public override bool Apply(Component component, ResourceStore resourceStore)
        {
            ResourceNode resourceNode;
            if (!resourceStore.TryGet(this.m, typeof(Mesh), out resourceNode))
            {
                return false;
            }

            MeshCollider meshCollider = (MeshCollider)component;
            meshCollider.sharedMesh = (Mesh)resourceNode.resource;

            return true;
        }
    }
}