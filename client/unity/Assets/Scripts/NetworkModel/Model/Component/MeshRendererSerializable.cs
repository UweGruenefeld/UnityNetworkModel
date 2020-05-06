/**
 * 
 * Mesh Renderer Clone Script of Unity Network Model
 *
 * @file MeshRendererClone.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-05-04
 *
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable MeshRenderer
    /// </summary>
    [Serializable]
    internal class MeshRendererSerializable : AbstractComponent
    {
        public string m;    // SharedMesh

        /// <summary>
        /// Creates serializable MeshRenderer from Unity component
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        public MeshRendererSerializable(Injector injector, Component component) : base(injector, component)
        {
            // Check if Mesh is attached to MeshFilter
            MeshRenderer meshRenderer = (MeshRenderer)component;
            if (meshRenderer.sharedMaterial == null)
                this.m = "null";
            else
            {
                // If mesh is attached, then get reference or unique name from ResourceStore
                meshRenderer.sharedMaterial.name = injector.resourceStore.GetReferenceName(meshRenderer.sharedMaterial);
                if (!injector.resourceStore.Contains(meshRenderer.sharedMaterial.name))
                {
                    // If Mesh resource does not exist add it to the ResourceStore
                    injector.resourceStore.Add(meshRenderer.sharedMaterial);
                }
                // Store the new Material name
                this.m = meshRenderer.sharedMaterial.name;
            }
        }

        /// <summary>
        /// Return common type of component
        /// </summary>
        /// <returns></returns>
        public override Type commonType { get { return typeof(MeshRenderer); } }

        /// <summary>
        /// Applies parameters saved in the serialized MeshRenderer to a Unity MeshRenderer
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public override bool Apply(Injector injector, Component component)
        {
            // Get reference to component
            MeshRenderer meshRenderer = (MeshRenderer)component;

            // Check if Material is null
            if(this.m == null || this.m == "null")
            {
                meshRenderer.material = null;
                meshRenderer.sharedMaterial = null;
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
            meshRenderer.material = (Material)resourceNode.resource;

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