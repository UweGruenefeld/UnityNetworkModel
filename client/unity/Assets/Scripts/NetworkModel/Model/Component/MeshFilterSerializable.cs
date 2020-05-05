/**
 * 
 * Mesh Filter Clone Script of Unity Network Model
 *
 * @file MeshFilterClone.cs
 * @author Uwe Gruenefeld
 * @version 2020-05-04
 *
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable MeshFilter
    /// </summary>
    [Serializable]
    internal class MeshFilterSerializable : AbstractComponent
    {
        public string m;    // SharedMesh

        /// <summary>
        /// Creates serializable MeshFilter from Unity component
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        public MeshFilterSerializable(Injector injector, Component component) : base(injector, component)
        {
            // Check if Mesh is attached to MeshFilter
            MeshFilter meshFilter = (MeshFilter)component;
            if (meshFilter.sharedMesh == null)
            {
                this.m = "null";
            }
            else
            {
                // If mesh is attached, then get reference or unique name from ResourceStore
                meshFilter.sharedMesh.name = injector.resourceStore.GetReferenceName(meshFilter.sharedMesh);
                if (!injector.resourceStore.Contains(meshFilter.sharedMesh.name))
                {
                    // If Mesh resource does not exist add it to the ResourceStore
                    injector.resourceStore.Add(meshFilter.sharedMesh);
                }
                // Store the new Material name
                this.m = meshFilter.sharedMesh.name;
            }
        }

        /// <summary>
        /// Return common type of component
        /// </summary>
        /// <returns></returns>
        public override Type commonType { get { return typeof(MeshFilter); } }

        /// <summary>
        /// Applies parameters saved in the serialized MeshFilter to a Unity MeshFilter
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public override bool Apply(Injector injector, Component component)
        {
            // Find resource in Store
            ResourceNode resourceNode;
            if (!injector.resourceStore.TryGet(this.m, typeof(Mesh), out resourceNode))
            {
                // If resource not found, then Unity component cannot be created
                // TODO check if really necessary
                return false;
            }

            MeshFilter meshFilter = (MeshFilter)component;
            meshFilter.sharedMesh = (Mesh)resourceNode.resource;

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
                this.m.GetHashCode()
            );
        }
    }
}