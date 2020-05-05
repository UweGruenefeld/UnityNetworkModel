/**
 * 
 * Mesh Clone Script of Unity Network Model
 *
 * @file MeshClone.cs
 * @author Tobias Lunte, Uwe Gruenefeld
 * @version 2020-05-04
 *
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable Mesh
    /// </summary>
    internal class MeshSerializable : AbstractResource
    {        
        public Vector3[] v, n;  // Vertices, Normals
        public Vector2[] u;     // UV-Texture Coordinates
        public int[] t;         // Triangles

        /// <summary>
        /// Creates serializable Mesh from Unity resource
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="resource"></param>
        public MeshSerializable(Injector injector, UnityEngine.Object resource) : base(injector, resource)
        {
            Mesh mesh = (Mesh)resource;

            this.v = CompressionUtility.Compress(this.injector.configuration.gameObject, mesh.vertices);
            this.n = CompressionUtility.Compress(this.injector.configuration.gameObject, mesh.normals);
            this.u = CompressionUtility.Compress(this.injector.configuration.gameObject, mesh.uv);
            this.t = mesh.triangles;
        }

        /// <summary>
        /// Return common type of resource
        /// </summary>
        /// <returns></returns>
        public override Type commonType { get { return typeof(Mesh); } }

        /// <summary>
        /// Applies parameters saved in the serialized Mesh to a Unity Mesh
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public override bool Apply(Injector injector, UnityEngine.Object resource)
        {
            Mesh mesh = (Mesh)resource;

            mesh.vertices = this.v;
            mesh.normals = this.n;
            mesh.uv = this.u;
            mesh.triangles = this.t;

            return true;
        }

        /// <summary>
        /// Returns Hash value of this serializable class
        /// </summary>
        /// <returns></returns>
        public override long GetHash()
        {
            // Combine the Hashes of all variables
            long hash = 17;
            foreach (Vector3 val in this.v)
            {
                hash = HashUtility.Combine2Hashes(hash, val.GetHashCode());
            }
            foreach (Vector3 val in this.n)
            {
                hash = HashUtility.Combine2Hashes(hash, val.GetHashCode());
            }
            foreach (Vector2 val in this.u)
            {
                hash = HashUtility.Combine2Hashes(hash, val.GetHashCode());
            }
            foreach (int val in this.t)
            {
                hash = HashUtility.Combine2Hashes(hash, val.GetHashCode());
            }
            return hash;
        }
    }
}