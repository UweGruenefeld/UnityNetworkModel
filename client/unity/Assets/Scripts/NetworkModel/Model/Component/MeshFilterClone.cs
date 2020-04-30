/**
 * 
 * Mesh Filter Clone Script of Unity Network Model
 *
 * @file MeshFilterClone.cs
 * @author Uwe Gruenefeld
 * @version 2020-04-30
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable MeshFilter
    /// </summary>
    [Serializable]
    internal class MeshFilterClone : AbstractComponent
    {
        public string m;

        public MeshFilterClone(Component component, ResourceStore resourceStore) : base(component)
        {
            MeshFilter meshFilter = (MeshFilter)component;
            if (meshFilter.sharedMesh == null)
            {
                this.m = "null";
            }
            else
            {
                meshFilter.sharedMesh.name = resourceStore.GetReferenceName(meshFilter.sharedMesh);
                if (!resourceStore.Contains(meshFilter.sharedMesh.name))
                {
                    resourceStore.Add(meshFilter.sharedMesh);
                }
                this.m = meshFilter.sharedMesh.name;
            }
        }

        public override bool Apply(Component component, ResourceStore resourceStore)
        {
            ResourceNode resourceNode;
            if (!resourceStore.TryGet(this.m, typeof(Mesh), out resourceNode))
            {
                return false;
            }

            MeshFilter meshFilter = (MeshFilter)component;
            meshFilter.sharedMesh = (Mesh)resourceNode.resource;

            return true;
        }
    }
}