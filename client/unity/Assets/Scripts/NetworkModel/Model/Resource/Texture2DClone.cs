/**
 * 
 * Texture2D Clone Script of Unity Network Model
 *
 * @file Texture2DClone.cs
 * @author Tobias Lunte, Uwe Gruenefeld
 * @version 2020-04-30
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    [Serializable]
    internal class Texture2DClone : AbstractResource
    {
        protected override Type commonType { get { return typeof(Texture2D); } }

        public int w, h; //width, height
        public Color[] p; //pixels

        // Prepare component for sending to server
        public Texture2DClone(System.Object resource, ResourceStore resourceStore) : base(resource, resourceStore)
        {
            Texture2D texture = (Texture2D)resource;

            this.w = texture.width;
            this.h = texture.height;
            this.p = texture.GetPixels();
        }

        // Apply received values to resource
        public override bool Apply(System.Object resource, ResourceStore resourceStore)
        {
            Texture2D texture = (Texture2D)resource;

            texture.SetPixels(this.p);
            texture.Apply();
            return true;
        }

        public override UnityEngine.Object Construct()
        {
            return new Texture2D(this.w, this.h);
        }

        public override long GetHash()
        {
            long hash = base.GetHash();
            foreach (Color c in this.p)
            {
                hash = Serializer.CombineHashes(hash, c.r.GetHashCode());
                hash = Serializer.CombineHashes(hash, c.g.GetHashCode());
                hash = Serializer.CombineHashes(hash, c.b.GetHashCode());
            }
            return hash;
        }
    }
}