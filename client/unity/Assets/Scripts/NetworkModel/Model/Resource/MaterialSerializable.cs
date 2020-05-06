/**
 * 
 * Material Clone Script of Unity Network Model
 *
 * @file MaterialClone.cs
 * @author Tobias Lunte, Uwe Gruenefeld
 * @version 2020-05-04
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    [Serializable]
    internal class MaterialSerializable : AbstractResource
    {
        public Color c;         // Color of Material
        public string t;        // Reference name for texture resource
        public Vector2 o, s;    // Texture offset and scale
        public string n;        // Shader name
        public string[] k;      // Shader keywords

        /// <summary>
        /// Creates serializable Material from Unity resource
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="resource"></param>
        public MaterialSerializable(Injector injector, UnityEngine.Object resource) : base(injector, resource)
        {
            Material material = (Material)resource;

            // Check if Color attribute exists
            if (material.HasProperty("_Color"))
                this.c = material.color;

            // Save the used Main Texture
            if (!material.HasProperty("_MainTex") || material.mainTexture == null)
                this.t = "null";
            else
            {
                // Get reference or unique name for the material resource
                material.mainTexture.name = injector.resourceStore.GetReferenceName(material.mainTexture);

                // Check if resource is already in ResourceStore
                if (!injector.resourceStore.Contains(material.mainTexture.name))
                    injector.resourceStore.Add(material.mainTexture);

                // Store the Texture name
                this.t = material.mainTexture.name;
            }

            // Check if TextureOffset attribute exists
            if(material.HasProperty("_TextureOffset"))
                this.o = CompressionUtility.Compress(this.injector.configuration.gameObject, material.mainTextureOffset);

            // Check if TextureScale exists
            if(material.HasProperty("_TextureScale"))
                this.s = CompressionUtility.Compress(this.injector.configuration.gameObject, material.mainTextureScale);

            this.n = material.shader.name;
            this.k = material.shaderKeywords;
        }

        /// <summary>
        /// Return common type of resource
        /// </summary>
        /// <returns></returns>
        public override Type commonType { get { return typeof(Material); } }

        /// <summary>
        /// Applies parameters saved in the serialized Material to a Unity Material
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public override bool Apply(Injector injector, UnityEngine.Object resource)
        {
            Material material = (Material)resource;

            // Find Texture2D resource in Store
            ResourceNode node = null;
            if (this.t != "null" && !injector.resourceStore.TryGet(this.t, typeof(Texture2D), out node))
            {
                // If Texture2D resource that belongs to this Material is not found, then Unity component cannot be created
                return false;
            }

            // Apply Material color
            if (material.HasProperty("_Color"))
            {
                material.color = this.c;
            }

            // Apply texture
            if (this.t != "null")
            {
                material.mainTextureOffset = this.o;
                material.mainTextureScale = this.s;
                material.mainTexture = (Texture2D)node.resource;
            }

            // Apply shader values
            Shader shader = Shader.Find(this.n);
            if (shader != null)
            {
                material.shader = shader;
                material.shaderKeywords = this.k;
            }
            
            return true;
        }

        /// <summary>
        /// Construct a Unity Material
        /// </summary>
        /// <returns></returns>
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


        /// <summary>
        /// Returns Hash value of this serializable class
        /// </summary>
        /// <returns></returns>
        public override long GetHash()
        {
            // Combine the Hashes of all variables
            long hash = HashUtility.CombineHashes(
                this.c.GetHashCode(),
                this.t.GetHashCode(),
                this.o.GetHashCode(),
                this.s.GetHashCode(),
                this.n.GetHashCode()
            );

            foreach (string keyword in this.k)
                hash = HashUtility.Combine2Hashes(hash, keyword.GetHashCode());
            
            return hash;
        }
    }
}