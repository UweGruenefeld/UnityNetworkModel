/**
 * 
 * Mesh Collider Clone Script of Unity Network Model
 *
 * @file MeshColliderClone.cs
 * @author Uwe Gruenefeld
 * @version 2020-04-30
 *
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable MeshCollider
    /// </summary>
    [Serializable]
    internal class MeshColliderSerializable : AbstractComponent
    {
        public string m;    // SharedMesh

        /// <summary>
        /// Creates serializable MeshCollider from Unity component
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        public MeshColliderSerializable(Injector injector, Component component) : base(injector, component)
        {
            MeshCollider meshCollider = (MeshCollider)component;

            // Check if Mesh is attached to MeshCollider
            if (meshCollider.sharedMesh == null)
                this.m = "null";
            else
            {
                // If mesh is attached, then get reference or unique name from ResourceStore
                meshCollider.sharedMesh.name = injector.resourceStore.GetReferenceName(meshCollider.sharedMesh);
                if (!injector.resourceStore.Contains(meshCollider.sharedMesh.name))
                {
                    // If Mesh resource does not exist add it to the ResourceStore
                    injector.resourceStore.Add(meshCollider.sharedMesh);
                }
                // Store the new Material name
                this.m = meshCollider.sharedMesh.name;
            }
        }

        /// <summary>
        /// Return common type of component
        /// </summary>
        /// <returns></returns>
        public override Type commonType { get { return typeof(MeshCollider); } }

        /// <summary>
        /// Applies parameters saved in the serialized MeshCollider to a Unity MeshCollider
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public override bool Apply(Injector injector, Component component)
        {
            // Get reference to component
            MeshCollider meshCollider = (MeshCollider)component;

            // Check if Mesh is null
            if(this.m == null || this.m == "null")
            {
                meshCollider.material = null;
                meshCollider.sharedMaterial = null;
                return true;
            }

            // Find resource in Store
            ResourceNode resourceNode;
            if (!injector.resourceStore.TryGet(this.m, typeof(Mesh), out resourceNode))
            {
                // If resource not found, then Unity component cannot be created
                return false;
            }

            // Apply the Material to the Component
            meshCollider.sharedMesh = (Mesh)resourceNode.resource;

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