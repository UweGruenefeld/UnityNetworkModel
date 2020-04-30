/**
 * 
 * Mesh Clone Script of Unity Network Model
 *
 * @file MeshClone.cs
 * @author Tobias Lunte, Uwe Gruenefeld
 * @version 2020-04-30
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable Mesh
    /// </summary>
    internal class MeshClone : AbstractResource
    {
        protected override Type commonType { get { return typeof(Mesh); } }

        public Vector3[] v, n;
        public Vector2[] u;
        public int[] t;

        public MeshClone(System.Object resource, ResourceStore resourceStore) : base(resource, resourceStore)
        {
            Mesh mesh = (Mesh)resource;

            this.v = mesh.vertices;
            this.n = mesh.normals;
            this.u = mesh.uv;
            this.t = mesh.triangles;
        }

        public override bool Apply(System.Object resource, ResourceStore resourceStore)
        {
            Mesh mesh = (Mesh)resource;

            mesh.vertices = this.v;
            mesh.normals = this.n;
            mesh.uv = this.u;
            mesh.triangles = this.t;

            return true;
        }

        public override long GetHash()
        {
            long hash = 17;
            foreach (Vector3 val in this.v)
            {
                hash = Serializer.CombineHashes(hash, val.GetHashCode());
            }
            foreach (Vector3 val in this.n)
            {
                hash = Serializer.CombineHashes(hash, val.GetHashCode());
            }
            foreach (Vector2 val in this.u)
            {
                hash = Serializer.CombineHashes(hash, val.GetHashCode());
            }
            foreach (int val in this.t)
            {
                hash = Serializer.CombineHashes(hash, val.GetHashCode());
            }
            return hash;
        }
    }
}