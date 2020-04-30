/**
 * 
 * Material Clone Script of Unity Network Model
 *
 * @file MaterialClone.cs
 * @author Tobias Lunte, Uwe Gruenefeld
 * @version 2020-04-30
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    [Serializable]
    internal class MaterialClone : AbstractResource
    {
        protected override Type commonType { get { return typeof(Material); } }

        public Color c;
        public string t;        // Reference name for texture resource
        public Vector2 o, s;    // Texture offset and scale
        public string n;        // Shader name
        public string[] k;      // Shader keywords

        // Prepare component for sending to server
        public MaterialClone(System.Object resource, ResourceStore resourceStore) : base(resource, resourceStore)
        {
            Material material = (Material)resource;

            if (material.HasProperty("_Color"))
            {
                this.c = material.color;
            }
            this.o = material.mainTextureOffset;
            this.s = material.mainTextureScale;
            this.n = material.shader.name;
            this.k = material.shaderKeywords;

            if (material.mainTexture == null)
            {
                this.t = "null";
            }
            else
            {
                material.mainTexture.name = resourceStore.GetReferenceName(material.mainTexture);
                if (!resourceStore.Contains(material.mainTexture.name))
                {
                    resourceStore.Add(material.mainTexture);
                }
                this.t = material.mainTexture.name;
            }
        }

        // Apply received values to resource
        public override bool Apply(System.Object resource, ResourceStore resourceStore)
        {
            ResourceNode node = null;
            if (this.t != "null" && !resourceStore.TryGet(this.t, typeof(Texture2D), out node))
            {
                return false;
            }

            Material material = (Material)resource;
            Shader shader = Shader.Find(this.n);
            if (shader != null)
            {
                material.shader = shader;
                material.shaderKeywords = this.k;
            }
            if (material.HasProperty("_Color"))
            {
                material.color = this.c;
            }
            if (this.t != "null")
            {
                material.mainTextureOffset = this.o;
                material.mainTextureScale = this.s;
                material.mainTexture = (Texture2D)node.resource;
            }

            return true;
        }

        public override UnityEngine.Object Construct()
        {
            Shader shader = Shader.Find(this.n);
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            System.Object[] args = new System.Object[] { shader };
            return (UnityEngine.Object)Activator.CreateInstance(commonType, args);
        }

        public override long GetHash()
        {
            long hash = 17;

            // TODO hash all values
            hash = Serializer.CombineHashes(hash, c.GetHashCode());
            
            foreach (string val in this.k)
            {
                hash = Serializer.CombineHashes(hash, val.GetHashCode());
            }
            return hash;
        }
    }
}