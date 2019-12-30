using System;
using System.Reflection;

namespace UnityEngine
{
    /// <summary>
    /// Handles conversion between serializable and non-serializable types of Assets and Components
    /// </summary>
    class Serializer
    {
        AssetStore assetStore;
        NetworkModel config;

        public Serializer(NetworkModel config)
        {
            this.config = config;
        }

        /// <summary>
        /// Requires access to AssetStore because some Assets themselves contain other Assets.
        /// </summary>
        /// <param name="assetStore"></param>
        public void SetAssetStore(AssetStore assetStore)
        {
            this.assetStore = assetStore;
        }

        /// <summary>
        /// Convert a Unity Component type to a NetworkModel Serializer.Component type
        /// </summary>
        /// <param name="type">Type of Unity Component</param>
        /// <returns>Type of NetworkModel Serializer.Component</returns>
        internal Type ToSerializableType(Type type)
        {
            return Type.GetType("UnityEngine.Serializer+" + type.Name);
        }

        /// <summary>
        /// Convert a NetworkModel Serializer.Component type to a Unity Component type
        /// </summary>
        /// <param name="type">Type of NetworkModel Serializer.Component</param>
        /// <returns>Type of Unity Component</returns>
        internal Type ToCommonType(Type type)
        {
            return Type.GetType("UnityEngine." + type.Name + ", UnityEngine");
        }

        /// <summary>
        /// Get the matching Unity Component type from a Serializer.Component; Returns Script type if Component is script
        /// </summary>
        /// <param name="comp"></param>
        /// <returns>Type of Unity Component</returns>
        internal Type GetCommonType(Serializer.Component comp)
        {
            Type type = comp.GetType();
            if (type == typeof(Serializer.Script))
            {
                Serializer.Script script = (Serializer.Script)comp;
                type = Type.GetType(script.ScriptType());
            }
            else
            {
                type = ToCommonType(type);
            }
            return type;
        }

        /// <summary>
        /// Convert a Unity Component to a NetworkModel Serializer.Component
        /// </summary>
        /// <param name="component">Unity Component</param>
        /// <returns>NetworkModel Serializer.Component</returns>
        internal Component ToSerializableComponent(UnityEngine.Component component)
        {
            Type type = component.GetType();

            if (type.IsSubclassOf(typeof(MonoBehaviour)))
            {
                type = typeof(Script);
            }
            else
            {
                type = ToSerializableType(type);
            }

            if (type != null)
            {
                return (Component)Activator.CreateInstance(type, new System.Object[] { component, assetStore });
            }
            return null;
        }

        /// <summary>
        /// Convert a Unity Object to a NetworkModel Serializer.Asset
        /// </summary>
        /// <param name="asset">Unity Object</param>
        /// <returns>NetworkModel Serializer.Asset</returns>
        internal Asset ToSerializableAsset(Object asset)
        {
            Type type = ToSerializableType(asset.GetType());
            if (type != null)
            {
                return (Asset)Activator.CreateInstance(type, new System.Object[] { asset, assetStore });
            }
            return null;
        }

        /// <summary>
        /// Checks whether Type of Unity Component has a matching NetworkModel Serializer.Component and if the matching public bool is set to true.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal bool ShouldBeSerialized(Type type)
        {
            if (type.IsSubclassOf(typeof(MonoBehaviour)) && config.SCRIPTS)
            {
                return true;
            }
            else if (ToSerializableType(type) != null)
            {
                try
                {
                    return ((bool)typeof(NetworkModel).GetField(type.Name.ToUpper()).GetValue(config));
                }
                catch (Exception)
                {
                    Debug.LogWarning("NetworkModel: Serializable class for " + type + "exists but no public bool variable to enable/disable it.");
                    return true;
                }
            }
            else
            {
                if (config.DEBUGSEND)
                {
                    Debug.Log("NetworkModel: Type " + type + " is unknown and cannot be synchronized.");
                }
                return false;
            }

        }

        /// <summary>
        /// Utility function to create order-dependent hash from two hashes. Collissions unlikely iff original hashes are already well-distibuted.
        /// </summary>
        /// <param name="h1"></param>
        /// <param name="h2"></param>
        /// <returns></returns>
        internal static long CombineHashes(long h1, long h2)
        {
            return h1 * 31 + h2;
        }

        /// <summary>
        /// Super class for all serializable Components
        /// </summary>
        [Serializable]
        internal abstract class Component
        {

            /// <summary>
            /// Creates serializable Component from Unity component
            /// </summary>
            /// <param name="component"></param>
            public Component(UnityEngine.Component component)
            {
            }

            /// <summary>
            /// Applies parameters saved in the serialized component to a unity component
            /// </summary>
            /// <param name="component"></param>
            /// <param name="assetStore"></param>
            /// <returns></returns>
            abstract public bool Apply(UnityEngine.Component component, AssetStore assetStore);

            /// <summary>
            /// Returns value-based, order-dependent hash. Needs to be overwritten for components containing arrays
            /// </summary>
            /// <returns></returns>
            public virtual long GetHash()
            {
                long hash = 17;
                FieldInfo[] fields = this.GetType().GetFields();
                foreach (FieldInfo field in fields)
                {
                    if (!field.FieldType.IsArray)
                    {
                        hash = CombineHashes(hash, field.GetValue(this).GetHashCode());
                    }
                }
                return hash;
            }
        }

        /// <summary>
        /// Serializable Script
        /// </summary>
        [Serializable]
        internal class Script : Component
        {
            public string type;
            public string value;

            public Script(UnityEngine.Component component, AssetStore assetStore) : base(component)
            {
                this.type = component.GetType().AssemblyQualifiedName;
                this.value = JsonUtility.ToJson(component);
            }

            public override bool Apply(UnityEngine.Component component, AssetStore assetStore)
            {
                JsonUtility.FromJsonOverwrite(this.value, component);
                return true;
            }

            public string ScriptType()
            {
                return this.type;
            }
        }

        /// <summary>
        /// Serializable Transform
        /// </summary>
        [Serializable]
        internal class Transform : Component
        {
            public Vector3 p, s;
            public Quaternion r;
            public string t;

            public Transform(UnityEngine.Component component, AssetStore assetStore) : base(component)
            {
                UnityEngine.Transform transform = (UnityEngine.Transform)component;

                this.p = transform.localPosition;
                this.r = transform.localRotation;
                this.s = transform.localScale;
                this.t = transform.tag;
            }

            public override bool Apply(UnityEngine.Component component, AssetStore assetStore)
            {
                UnityEngine.Transform transform = (UnityEngine.Transform)component;

                // If there was no change on client side then use server values
                if (!transform.hasChanged)
                {
                    transform.localPosition = this.p;
                    transform.localRotation = this.r;
                    transform.localScale = this.s;
                    transform.tag = this.t;

                    // Avoid triggering update of changes
                    transform.hasChanged = false;
                }
                return true;
            }
        }

        /// <summary>
        /// Serializable Camera
        /// </summary>
        [Serializable]
        internal class Camera : Component
        {
            public float d, n, f, v;
            public Color b;
            public CameraClearFlags c;

            public Camera(UnityEngine.Component component, AssetStore assetStore) : base(component)
            {
                UnityEngine.Camera camera = (UnityEngine.Camera)component;

                this.d = camera.depth;
                this.n = camera.nearClipPlane;
                this.f = camera.farClipPlane;
                this.v = camera.fieldOfView;
                this.b = camera.backgroundColor;
                this.c = camera.clearFlags;
            }

            public override bool Apply(UnityEngine.Component component, AssetStore assetStore)
            {
                UnityEngine.Camera camera = (UnityEngine.Camera)component;

                camera.depth = this.d;
                camera.nearClipPlane = this.n;
                camera.farClipPlane = this.f;
                camera.fieldOfView = this.v;
                camera.backgroundColor = this.b;
                camera.clearFlags = this.c;

                return true;
            }
        }

        /// <summary>
        /// Serializable Light
        /// </summary>
        [Serializable]
        internal class Light : Component
        {
            public LightType t;
            public Color c;
            public float i, b;

            public Light(UnityEngine.Component component, AssetStore assetStore) : base(component)
            {
                UnityEngine.Light light = (UnityEngine.Light)component;

                this.t = light.type;
                this.c = light.color;
                this.i = light.intensity;
                this.b = light.bounceIntensity;
            }

            public override bool Apply(UnityEngine.Component component, AssetStore assetStore)
            {
                UnityEngine.Light light = (UnityEngine.Light)component;

                light.type = this.t;
                light.color = this.c;
                light.intensity = this.i;
                light.bounceIntensity = this.b;

                return true;
            }
        }

        /// <summary>
        /// Serializable MeshFilter
        /// </summary>
        [Serializable]
        internal class MeshFilter : Component
        {
            public string m;

            public MeshFilter(UnityEngine.Component component, AssetStore assetStore) : base(component)
            {
                UnityEngine.MeshFilter meshFilter = (UnityEngine.MeshFilter)component;
                if (meshFilter.sharedMesh == null)
                {
                    this.m = "null";
                }
                else
                {
                    meshFilter.sharedMesh.name = assetStore.GetReferenceName(meshFilter.sharedMesh);
                    if (!assetStore.Contains(meshFilter.sharedMesh.name))
                    {
                        assetStore.Add(meshFilter.sharedMesh);
                    }
                    this.m = meshFilter.sharedMesh.name;
                }
            }

            public override bool Apply(UnityEngine.Component component, AssetStore assetStore)
            {
                AssetStore.AssetNode assetNode;
                if (!assetStore.TryGet(this.m, typeof(UnityEngine.Mesh), out assetNode))
                {
                    return false;
                }

                UnityEngine.MeshFilter meshFilter = (UnityEngine.MeshFilter)component;
                meshFilter.sharedMesh = (UnityEngine.Mesh)assetNode.asset;

                return true;
            }
        }

        /// <summary>
        /// Serializable MeshRenderer
        /// </summary>
        [Serializable]
        internal class MeshRenderer : Component
        {
            public string m;

            public MeshRenderer(UnityEngine.Component component, AssetStore assetStore) : base(component)
            {
                UnityEngine.MeshRenderer meshRenderer = (UnityEngine.MeshRenderer)component;
                if (meshRenderer.sharedMaterial == null)
                {
                    this.m = "null";
                }
                else
                {
                    meshRenderer.sharedMaterial.name = assetStore.GetReferenceName(meshRenderer.sharedMaterial);
                    if (!assetStore.Contains(meshRenderer.sharedMaterial.name))
                    {
                        assetStore.Add(meshRenderer.sharedMaterial);
                    }
                    this.m = meshRenderer.sharedMaterial.name;
                }
            }

            public override bool Apply(UnityEngine.Component component, AssetStore assetStore)
            {
                AssetStore.AssetNode assetNode;
                if (!assetStore.TryGet(this.m, typeof(UnityEngine.Material), out assetNode))
                {
                    return false;
                }

                UnityEngine.MeshRenderer meshRenderer = (UnityEngine.MeshRenderer)component;
                meshRenderer.material = (UnityEngine.Material)assetNode.asset;

                return true;
            }
        }

        /// <summary>
        /// Serializable LineRenderer
        /// </summary>
        [Serializable]
        internal class LineRenderer : Component
        {
            public string m;
            public Vector3[] p;
            public float w;
            public bool l;
            public int ca, co;
            public LineTextureMode t;
            public Color cs, ce;

            public LineRenderer(UnityEngine.Component component, AssetStore assetStore) : base(component)
            {
                UnityEngine.LineRenderer lineRenderer = (UnityEngine.LineRenderer)component;
                if (lineRenderer.sharedMaterial == null)
                {
                    this.m = "null";
                }
                else
                {
                    lineRenderer.sharedMaterial.name = assetStore.GetReferenceName(lineRenderer.sharedMaterial);
                    if (!assetStore.Contains(lineRenderer.sharedMaterial.name))
                    {
                        assetStore.Add(lineRenderer.sharedMaterial);
                    }
                    this.m = lineRenderer.sharedMaterial.name;
                }
                this.p = new Vector3[lineRenderer.positionCount];
                lineRenderer.GetPositions(this.p);
                this.w = lineRenderer.widthMultiplier;
                this.l = lineRenderer.loop;
                this.ca = lineRenderer.numCapVertices;
                this.co = lineRenderer.numCornerVertices;
                this.t = lineRenderer.textureMode;
                this.cs = lineRenderer.startColor;
                this.ce = lineRenderer.endColor;
            }

            public override bool Apply(UnityEngine.Component component, AssetStore assetStore)
            {
                AssetStore.AssetNode assetNode;
                if (!assetStore.TryGet(this.m, typeof(UnityEngine.Material), out assetNode))
                {
                    return false;
                }

                UnityEngine.LineRenderer lineRenderer = (UnityEngine.LineRenderer)component;
                lineRenderer.material = (UnityEngine.Material)assetNode.asset;
                lineRenderer.positionCount = this.p.Length;
                lineRenderer.SetPositions(this.p);
                lineRenderer.widthMultiplier = this.w;
                lineRenderer.loop = this.l;
                lineRenderer.numCapVertices = this.ca;
                lineRenderer.numCornerVertices = this.co;
                lineRenderer.textureMode = this.t;
                lineRenderer.startColor = this.cs;
                lineRenderer.endColor = this.ce;

                return true;
            }

            public override long GetHash()
            {
                long hash = base.GetHash();
                foreach (Vector3 val in this.p)
                {
                    hash = CombineHashes(hash, val.GetHashCode());
                }
                return hash;
            }
        }

        /// <summary>
        /// Serializable MeshCollider
        /// </summary>
        [Serializable]
        internal class MeshCollider : Component
        {
            public string m;

            public MeshCollider(UnityEngine.Component component, AssetStore assetStore) : base(component)
            {
                UnityEngine.MeshCollider meshCollider = (UnityEngine.MeshCollider)component;
                if (meshCollider.sharedMesh == null)
                {
                    this.m = "null";
                }
                else
                {
                    meshCollider.sharedMesh.name = assetStore.GetReferenceName(meshCollider.sharedMesh);
                    if (!assetStore.Contains(meshCollider.sharedMesh.name))
                    {
                        assetStore.Add(meshCollider.sharedMesh);
                    }
                    this.m = meshCollider.sharedMesh.name;
                }
            }

            public override bool Apply(UnityEngine.Component component, AssetStore assetStore)
            {
                AssetStore.AssetNode assetNode;
                if (!assetStore.TryGet(this.m, typeof(UnityEngine.Mesh), out assetNode))
                {
                    return false;
                }

                UnityEngine.MeshCollider meshCollider = (UnityEngine.MeshCollider)component;
                meshCollider.sharedMesh = (UnityEngine.Mesh)assetNode.asset;

                return true;
            }
        }

        /// <summary>
        /// Serializable BoxCollider
        /// </summary>
        internal class BoxCollider : Component
        {
            public Vector3 c, s;

            public BoxCollider(UnityEngine.Component component, AssetStore assetStore) : base(component)
            {
                UnityEngine.BoxCollider boxcollider = (UnityEngine.BoxCollider)component;

                this.c = boxcollider.center;
                this.s = boxcollider.size;
            }

            public override bool Apply(UnityEngine.Component component, AssetStore assetStore)
            {
                UnityEngine.BoxCollider boxcollider = (UnityEngine.BoxCollider)component;

                boxcollider.center = this.c;
                boxcollider.size = this.s;

                return true;
            }
        }

        /// <summary>
        /// Serializable SphereCollider
        /// </summary>
        internal class SphereCollider : Component
        {
            public Vector3 c;
            public float r;

            public SphereCollider(UnityEngine.Component component, AssetStore assetStore) : base(component)
            {
                UnityEngine.SphereCollider spherecollider = (UnityEngine.SphereCollider)component;

                this.c = spherecollider.center;
                this.r = spherecollider.radius;
            }

            public override bool Apply(UnityEngine.Component component, AssetStore assetStore)
            {
                UnityEngine.SphereCollider spherecollider = (UnityEngine.SphereCollider)component;

                spherecollider.center = this.c;
                spherecollider.radius = this.r;

                return true;
            }
        }

        /*
         * TEMPLATE FOR NEW COMPONENT
         * 
         * [Serializable]
         * internal class NAMEOFCOMPONENT : Component
         * {
         *     CLASS VARIABLES TO SYNCHRONIZE
         *     
         *     // Prepare component for sending to server
         *     public NAMEOFCOMPONENT(Component component, AssetStore assetStore) : base(component)
         *     {
         *          SAVE VARIABLES FROM COMPONENT IN CLASS VARIABLES
         *     }
         *     
         *     // Apply received values to component
         *     public override void Apply (Component component, AssetStore assetStore)
         *     {
         *          RESTORE CLASS VARIABLES INTO VARIABLES FROM COMPONENT
         *     }
         *
         *     // Override Hash if component contains array. Apply loop for each array.
         *     // public override long GetHash()
         *     // {
         *     //     long hash = base.GetHash();
         *     //     foreach (ELEMENT val in ARRAY)
         *     //     {
         *     //         hash = CombineHashes(hash, val.GetHashCode());
         *     //     }
         *     //     return hash;
         *     // }
         * }
         *
         */


        /// <summary>
        /// Super class for all serializable Assets
        /// </summary>
        [Serializable]
        internal abstract class Asset
        {
            protected AssetStore assetStore;
            protected abstract Type commonType { get; }

            /// <summary>
            /// Creates serializable Asset from Unity asset
            /// </summary>
            /// <param name="asset"></param>
            /// <param name="assetStore"></param>
            public Asset(System.Object asset, AssetStore assetStore)
            {
                this.assetStore = assetStore;
            }

            /// <summary>
            /// Applies parameters stored in serialized asset to Unity asset
            /// </summary>
            /// <param name="asset"></param>
            /// <param name="assetStore"></param>
            /// <returns></returns>
            abstract public bool Apply(System.Object asset, AssetStore assetStore);

            /// <summary>
            /// Create Unity asset of matching type, setting final parameters where necessary. Needs to be overwritten for assets using final parameters
            /// </summary>
            /// <returns></returns>
            public virtual Object Construct()
            {
                return (Object)Activator.CreateInstance(commonType);
            }

            /// <summary>
            /// Returns value-based, order-dependent hash. Needs to be overwritten for assets containing arrays
            /// </summary>
            /// <returns></returns>
            public virtual long GetHash()
            {
                long hash = 17;
                FieldInfo[] fields = this.GetType().GetFields();
                foreach (FieldInfo field in fields)
                {
                    if (!field.FieldType.IsArray)
                    {
                        hash = CombineHashes(hash, field.GetValue(this).GetHashCode());
                    }
                }
                return hash;
            }
        }

        /// <summary>
        /// Serializable Mesh
        /// </summary>
        internal class Mesh : Asset
        {
            protected override Type commonType { get { return typeof(UnityEngine.Mesh); } }

            public Vector3[] v, n;
            public Vector2[] u;
            public int[] t;

            public Mesh(System.Object asset, AssetStore assetStore) : base(asset, assetStore)
            {
                UnityEngine.Mesh mesh = (UnityEngine.Mesh)asset;

                this.v = mesh.vertices;
                this.n = mesh.normals;
                this.u = mesh.uv;
                this.t = mesh.triangles;
            }

            public override bool Apply(System.Object asset, AssetStore assetStore)
            {
                UnityEngine.Mesh mesh = (UnityEngine.Mesh)asset;

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
                    hash = CombineHashes(hash, val.GetHashCode());
                }
                foreach (Vector3 val in this.n)
                {
                    hash = CombineHashes(hash, val.GetHashCode());
                }
                foreach (Vector2 val in this.u)
                {
                    hash = CombineHashes(hash, val.GetHashCode());
                }
                foreach (int val in this.t)
                {
                    hash = CombineHashes(hash, val.GetHashCode());
                }
                return hash;
            }
        }

        [Serializable]
        internal class Material : Asset
        {
            protected override Type commonType { get { return typeof(UnityEngine.Material); } }

            public Color c;
            public string t; //Reference name for texture asset
            public Vector2 o, s; //texture offset and scale
            public string n; //Shader name
            public string[] k; //Shader keywords


            // Prepare component for sending to server
            public Material(System.Object asset, AssetStore assetStore) : base(asset, assetStore)
            {
                UnityEngine.Material material = (UnityEngine.Material)asset;

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
                    material.mainTexture.name = assetStore.GetReferenceName(material.mainTexture);
                    if (!assetStore.Contains(material.mainTexture.name))
                    {
                        assetStore.Add(material.mainTexture);
                    }
                    this.t = material.mainTexture.name;
                }
            }

            // Apply received values to asset
            public override bool Apply(System.Object asset, AssetStore assetStore)
            {
                AssetStore.AssetNode node = null;
                if (this.t != "null" && !assetStore.TryGet(this.t, typeof(UnityEngine.Texture2D), out node))
                {
                    return false;
                }

                UnityEngine.Material material = (UnityEngine.Material)asset;
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
                    material.mainTexture = (UnityEngine.Texture2D)node.asset;
                }

                return true;
            }

            public override Object Construct()
            {
                Shader shader = Shader.Find(this.n);
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }
                System.Object[] args = new System.Object[] { shader };
                return (Object)Activator.CreateInstance(commonType, args);
            }

            public override long GetHash()
            {
                long hash = 17;
                foreach (string val in this.k)
                {
                    hash = CombineHashes(hash, val.GetHashCode());
                }
                return hash;
            }
        }

        [Serializable]
        internal class Texture2D : Asset
        {
            protected override Type commonType { get { return typeof(UnityEngine.Texture2D); } }

            public int w, h; //width, height
            public Color[] p; //pixels

            // Prepare component for sending to server
            public Texture2D(System.Object asset, AssetStore assetStore) : base(asset, assetStore)
            {
                UnityEngine.Texture2D texture = (UnityEngine.Texture2D)asset;

                this.w = texture.width;
                this.h = texture.height;
                this.p = texture.GetPixels();

            }

            // Apply received values to asset
            public override bool Apply(System.Object asset, AssetStore assetStore)
            {
                UnityEngine.Texture2D texture = (UnityEngine.Texture2D)asset;

                texture.SetPixels(this.p);
                texture.Apply();
                return true;
            }

            public override Object Construct()
            {
                return new UnityEngine.Texture2D(this.w, this.h);
            }

            public override long GetHash()
            {
                long hash = base.GetHash();
                foreach (Color c in this.p)
                {
                    hash = CombineHashes(hash, c.r.GetHashCode());
                    hash = CombineHashes(hash, c.g.GetHashCode());
                    hash = CombineHashes(hash, c.b.GetHashCode());
                }
                return hash;
            }
        }

        /*
         * TEMPLATE FOR NEW ASSET
         * 
         * [Serializable]
         * internal class NAMEOFASSET : Asset
         * {
         *     protected override Type commonType { get { return typeof(UnityEngine.NAMEOFASSET); } }
         *
         *     CLASS VARIABLES TO SYNCHRONIZE
         *     
         *     // Prepare component for sending to server
         *     public NAMEOFASSET(System.Object asset, AssetStore assetStore) : base(asset, assetStore)
         *     {
         *          SAVE VARIABLES FROM COMPONENT IN CLASS VARIABLES
         *     }
         *     
         *     // Apply received values to asset
         *     public override void Apply (System.Object asset, AssetStore assetStore)
         *     {
         *          RESTORE CLASS VARIABLES INTO VARIABLES FROM COMPONENT
         *     }
         *     
         *     // Override if parameters need to be set during initialization
         *     // public override Object Construct()
         *     // {
         *     //      
         *     // }
         *     
         *     // Override if Asset contains arrays. Apply loop for each array
         *     // public override string GetHash() {}
         *     // {
         *     //      long hash = base.GetHash();
         *     //      foreach (ELEMENT val in ARRAY)
         *     //      {
         *     //          hash = CombineHashes(hash, val.GetHashCode());
         *     //      }
         *     //      return hash;
         *     // }
         * }
         *
         */
    }
}