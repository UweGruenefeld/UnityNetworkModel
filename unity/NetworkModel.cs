/**
 * Network Model Script
 *
 * Attach this script to a gameobject in Unity
 * and all children gameobjects will be
 * synchronized to the specified server
 * 
 * Currently supported components are
 * - Transform
 * - Camera
 * - Light
 * - MeshFilter
 * - MeshRenderer
 * - MeshCollider
 * - BoxCollider
 * - SphereCollider
 * - Scripts
 * 
 * Currently supported assets are
 * - Mesh
 * //- Material
 * //- Texture2D
 * //- RenderTexture
 * 
 * To add new synchronizable components or assets, search for
 * TEMPLATE FOR NEW COMPONENT
 * or
 * TEMPLATE FOR NEW ASSET
 *
 * @file NetworkModel.cs
 * @author Uwe Gruenefeld
 * @author Tobias Lunte
 * @version 2019-06-30
 **/
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using WebSocketSharp;
using System.Text;

namespace UnityEngine
{
    /// <summary>
    /// Implements MonoBehaviour and connects to server via websocket
    /// </summary>
    public class NetworkModel : MonoBehaviour
    {
        // Config variables
        // Connection
        public string IP = "127.0.0.1";
        public int PORT = 8080;
        public float RECONNECT = 1f;
        public float PERIOD = 0.1f;
        // Update
        public bool RECEIVE = true;
        public bool SEND = true;
        // Sender
        public bool TIMESTAMP = false;
        public bool TRANSFORM = true;
        public bool CAMERA = true;
        public bool LIGHT = true;
        public bool MESHFILTER = true;
        public bool MESHRENDERER = true;
        public bool MESHCOLLIDER = true;
        public bool BOXCOLLIDER = true;
        public bool SPHERECOLLIDER = true;
        public bool SCRIPTS = true;
        //public bool MESH = true;
        //public bool MATERIAL = true;
        //public bool TEXTURE = true;
        // Receiver
        public bool EXISTINGCOMP = false;
        public bool EXISTINGASSETS = false;
        // Debugging
        public bool DEBUGSEND = false;
        public bool DEBUGREC = false;

        // Values from previous iteration for detecting changes
        private bool OLD_RECEIVE;


        // Components
        private Serializer serializer;
        private GOStore goStore;
        private AssetStore assetStore;
        private Connection connection;
        private WorldModel worldModel;

        private float time;
        //private EventHandler<MessageEventArgs> receiveHandler;

        private void Start()
        {
            //Set up components
            this.serializer = new Serializer(this);
            this.goStore = new GOStore(serializer, this);
            this.goStore.Add("root", this.gameObject);
            this.assetStore = new AssetStore(serializer, this);
            this.assetStore.Add("null", null);
            this.serializer.SetAssetStore(assetStore);
            //this.goStore.Add(this.gameObject);
            this.connection = new Connection(this);
            this.worldModel = WorldModel.CreateWorldModel(this.gameObject, goStore, assetStore, connection, serializer, this);
            //this.receiver = new Receiver(goStore, this);
            //this.sender = new Sender(connection, this);

            //Set up handler for received messages
            this.connection.OnMessage += (sender, message) => worldModel.ReceiveRequest(message.Data);
            //this.receiveHandler = (sender, message) => worldModel.ReceiveRequest(message.Data);
            //if (this.RECEIVE)
            //{
            //    this.connection.OnMessage += receiveHandler;
            //    //this.connection.AddMessageHandler(receiveHandler);
            //}
            //this.OLD_RECEIVE = this.RECEIVE;
        }

        private void Update()
        {
            //check if update period has passed
            this.time += Time.deltaTime;
            if (this.time > this.PERIOD)
            {
                this.time = 0f;
                //try to establish connection, only proceed if successful
                if (connection.EnsureIsAlive())
                {
                    ////en-/disable handler for received messages
                    //if (this.RECEIVE != this.OLD_RECEIVE)
                    //{
                    //    if (this.RECEIVE)
                    //    {
                    //        this.connection.OnMessage += receiveHandler;
                    //        //this.connection.AddMessageHandler(receiveHandler);
                    //    }
                    //    else
                    //    {
                    //        this.connection.OnMessage -= receiveHandler;
                    //        //this.connection.RemoveMessageHandler(receiveHandler);
                    //    }
                    //    this.OLD_RECEIVE = this.RECEIVE;
                    //}
                    //apply change-requests that were received from the server since the last update
                    if (this.RECEIVE)
                    {
                        worldModel.ApplyChanges();
                    }
                    //send change-requests for modifications since last update to the server
                    if (this.SEND)
                    {
                        worldModel.TrackChanges();
                    }
                };
            }
        }
    }


    class Connection : WebSocket
    {
        private NetworkModel config;

        // Variables for websocket connection
        //private WebSocket webSocket;
        private float lastAttempt;

        public Connection(NetworkModel config) : base("ws://" + config.IP + ":" + config.PORT)
        {
            this.config = config;
            //this.webSocket = new WebSocket("ws://" + config.IP + ":" + config.PORT);
            lastAttempt = -config.RECONNECT;
        }

        public bool EnsureIsAlive()
        {
            // If the websocket is not alive try to connect to the server
            if (!this.IsAlive)
            {
                // Check if last attempt to connect is at least one second ago
                if (Time.realtimeSinceStartup > this.lastAttempt + config.RECONNECT)
                {
                    if (config.DEBUGSEND || config.DEBUGREC)
                        Debug.Log("NetworkModel: Attempt to connect to server");

                    try
                    {
                        // Connect to the server
                        this.ConnectAsync();
                        this.lastAttempt = Time.realtimeSinceStartup;
                    }
                    catch (Exception)
                    {
                        Debug.LogError("NetworkModel: Unable to connect to server");
                    }
                }
            }

            // Return if websocket is alive
            return this.IsAlive;
        }

        //public void AddMessageHandler(EventHandler<MessageEventArgs> handler)
        //{
        //    webSocket.OnMessage += handler;
        //}

        //public void RemoveMessageHandler(EventHandler<MessageEventArgs> handler)
        //{
        //    webSocket.OnMessage -= handler;
        //}

        internal void SendRequest(Request request)
        {
            string json = JsonUtility.ToJson(request);
            if (config.DEBUGSEND)
                Debug.Log("NetworkModel: Sent message " + json);
            this.SendAsync(json, null);
        }
    }

    class WorldModel : MonoBehaviour
    {
        GameObject root;
        private GOStore goStore;
        private AssetStore assetStore;
        private Connection connection;
        private Serializer serializer;
        private NetworkModel config;

        private Queue<Request> requestQueue;
        private Queue<HierarchyUpdate> hierarchyQueue;

        public static WorldModel CreateWorldModel(GameObject root, GOStore goStore, AssetStore assetStore, Connection connection, Serializer serializer, NetworkModel config)
        {
            WorldModel wm = root.AddComponent<WorldModel>();

            //this.root = root;
            wm.goStore = goStore;
            wm.assetStore = assetStore;
            wm.connection = connection;
            wm.serializer = serializer;
            wm.config = config;
            wm.requestQueue = new Queue<Request>();
            wm.hierarchyQueue = new Queue<HierarchyUpdate>();

            return wm;
        }

        public void ApplyChanges()
        {
            ApplyRequests();
            UpdateHierarchy();
        }

        private void ApplyRequests()
        {
            if (requestQueue.Count > 0)
            {
                Debug.Log(requestQueue.Count);
            }
            // Handle requests from server
            while (requestQueue.Count > 0)
            {
                // Get next Request
                Request request = this.requestQueue.Dequeue();
                switch (request.objectType + request.updateType)
                {
                    //case Request.ASSET:
                    //    break;
                    case Request.ASSET + Request.UPDATE:
                        UpdateAsset(request);
                        break;
                    case Request.ASSET + Request.DELETE:
                        DeleteAsset(request.name);
                        break;
                    case Request.GAMEOBJECT + Request.UPDATE:
                        //check if node has correct parent; accounting for differences in root go name.
                        GOStore.GONode node = goStore.GetOrCreate(request.name);
                        //if ((!(request.parent == "root" && node.gameObject.transform.parent == goStore["root"].gameObject.transform))
                        //    && (node.gameObject.transform.parent.name != request.parent))
                        //{
                        node.gameObject.SetActive(false);
                        node.gameObject.transform.hasChanged = false;
                        this.hierarchyQueue.Enqueue(new HierarchyUpdate(node, request.parent));
                        //}
                        break;
                    case Request.COMPONENT + Request.UPDATE:
                        UpdateComponents(request);
                        break;
                    case Request.GAMEOBJECT + Request.DELETE:
                        DeleteGameObject(request.name);
                        break;
                    case Request.COMPONENT + Request.DELETE:
                        foreach (Type type in request.componentTypes)
                        {
                            DeleteComponent(request.name, type);
                        }
                        break;
                }
            }
        }

        private void UpdateHierarchy()
        {
            // List of gameobjects that need an hierarchy update
            while (hierarchyQueue.Count > 0)
            {
                // Get first request
                HierarchyUpdate hierarchyUpdate = this.hierarchyQueue.Dequeue();

                // Check if requested parent is known. If so, set go to be child of parent
                GOStore.GONode node;
                if (goStore.TryGetValue(hierarchyUpdate.parent, out node))
                {
                    Transform parent = node.gameObject.transform;
                    Transform transform = hierarchyUpdate.node.gameObject.transform;

                    // Save transform values
                    Vector3 position = transform.localPosition;
                    Quaternion rotation = transform.localRotation;
                    Vector3 scale = transform.localScale;

                    transform.parent = parent;

                    // Restore transform values
                    transform.localPosition = position;
                    transform.localRotation = rotation;
                    transform.localScale = scale;

                    hierarchyUpdate.node.gameObject.SetActive(true);
                    hierarchyUpdate.node.gameObject.transform.hasChanged = false;
                    hierarchyUpdate.node.UpdateParent();
                }
                else
                // else, re-enqueue request for later
                {
                    this.hierarchyQueue.Enqueue(hierarchyUpdate);
                }
            }
        }

        private void UpdateAsset(Request request)
        {
            AssetStore.AssetNode node = assetStore.getOrCreate(request.name, serializer.ToCommonType(request.assetType));
            request.asset.Apply(node.asset, assetStore);
        }

        private void UpdateComponents(Request request)
        {
            //Find a node of the correct gameobject for the request to use, creating it if it doesn't exist yet.
            GOStore.GONode node = goStore.GetOrCreate(request.name);

            // Update the components
            foreach (Serializer.Component serializedComponent in request.components)
            {
                Type type = serializer.GetCommonType(serializedComponent);
                if (type != null)
                {
                    Component component = node.GetOrCreateComponent(type);
                    serializedComponent.Apply(component, assetStore);
                    node.UpdateHash(type);
                }
            }
        }

        private void DeleteAsset(string name)
        {
            if (this.assetStore.ContainsKey(name))
            {
                this.assetStore.Remove(name);
            }
        }

        private void DeleteGameObject(string name)
        {
            GOStore.GONode node;
            if (this.goStore.TryGetValue(name, out node))
            {
                Destroy(node.gameObject);
                this.goStore.Remove(name);
            }
        }

        private void DeleteComponent(string name, Type type)
        {
            GOStore.GONode node;
            if (this.goStore.TryGetValue(name, out node))
            {
                type = serializer.ToCommonType(type);
                if (type != null)
                {
                    Component comp = node.gameObject.GetComponent(type);
                    node.RemoveHash(serializer.ToCommonType(type));
                    Destroy(comp);
                }
            }
        }

        internal void TrackChanges()
        {
            // Add untracked elements to goStore as empty
            foreach (Transform transform in gameObject.GetComponentsInChildren<Transform>())
            {
                if (transform != gameObject.transform)
                {
                    transform.gameObject.name = goStore.GetReferenceName(transform.gameObject);
                    if (!goStore.ContainsKey(transform.gameObject.name))
                    {
                        goStore.Add(transform.gameObject);
                    }
                }
            }

            // Iterate over goStore
            List<string> goToDelete = new List<string>();
            foreach (KeyValuePair<string, GOStore.GONode> goEntry in goStore)
            {
                string referenceName = goEntry.Key;
                if (referenceName == "root")
                {
                    continue;
                }
                GOStore.GONode node = goEntry.Value;
                // If Element doesn't exist anymore, schedule it for deletion
                if (node.gameObject == null || node.gameObject.name != referenceName)
                {
                    goToDelete.Add(referenceName);
                }
                // Else, check for changes in hierarchy and components
                else
                {
                    if (node.gameObject.transform.parent.GetInstanceID() != node.parent)
                    {
                        connection.SendRequest(Request.UpdateGO(node.gameObject, config));
                        node.gameObject.transform.hasChanged = false;
                        node.UpdateParent();
                    }

                    //Add untracked components to componentDict as empty
                    node.PopulateHashes();

                    // Iterate over compDict
                    List<Type> compToDelete = new List<Type>();
                    List<Serializer.Component> compToUpdate = new List<Serializer.Component>();
                    foreach (KeyValuePair<Type, string> hashEntry in node.hashes)
                    {
                        Component comp = node.gameObject.GetComponent(hashEntry.Key);

                        //If Component doesn't exist anymroe, schedule it for deletion
                        if (comp == null)
                        {
                            Type type = serializer.ToSerializableType(hashEntry.Key);
                            compToDelete.Add(type);
                        }
                        //Else if component has changed in way tracked by serializer, schedule it for update
                        else
                        {
                            Serializer.Component serComp = serializer.ToSerializableComponent(comp);
                            if (serComp.GetHash() != hashEntry.Value)
                            {
                                compToUpdate.Add(serComp);
                            }
                        }
                    }
                    // Delete scheduled components and send Delete Request
                    foreach (Type type in compToDelete)
                    {
                        node.RemoveHash(serializer.ToCommonType(type));
                    }
                    if (compToDelete.Count > 0)
                    {
                        connection.SendRequest(Request.DeleteComponents(referenceName, compToDelete, config));
                    }
                    // Update scheduled components and send Update Request
                    foreach (Serializer.Component comp in compToUpdate)
                    {
                        node.UpdateHash(comp);
                    }
                    if (compToUpdate.Count > 0)
                    {
                        connection.SendRequest(Request.UpdateComponents(referenceName, compToUpdate, config));
                        node.gameObject.transform.hasChanged = false;
                    }
                }
            }

            // Delete scheduled GameObjects and send Delete Request
            foreach (string referenceName in goToDelete)
            {
                goStore.Remove(referenceName);
                connection.SendRequest(Request.DeleteGO(referenceName, config));
            }

            // Missing Assets were added while updating components (in Serializer.Component..ctor's)
            // Iterate over assetStore
            List<string> assetsToDelete = new List<string>();
            foreach (KeyValuePair<string, AssetStore.AssetNode> assetEntry in assetStore)
            {
                string assetName = assetEntry.Key;
                if (assetName == "null")
                {
                    continue;
                }
                AssetStore.AssetNode node = assetEntry.Value;

                // If Element doesn't exist anymore, schedule it for deletion
                if (node.asset == null || node.asset.name != assetName || node.asset.GetType() != node.type)
                {
                    assetsToDelete.Add(assetName);
                }
                // Else, check for changes
                else
                {
                    Serializer.Asset serAsset = serializer.ToSerializableAsset(node.asset);
                    if (serAsset.GetHash() != node.hash)
                    {
                        connection.SendRequest(Request.UpdateAsset(assetName, serializer.ToSerializableType(node.type), serializer.ToSerializableAsset(node.asset), config));
                        node.UpdateHash();
                    }
                }
            }
            foreach (string assetName in assetsToDelete)
            {
                assetStore.Remove(assetName);
                connection.SendRequest(Request.DeleteAsset(assetName, config));
            }
        }

        internal void ReceiveRequest(string json)
        {
            if (config.DEBUGREC)
                Debug.Log("NetworkModel: Received message " + json);
            this.requestQueue.Enqueue(JsonUtility.FromJson<Request>(json));
        }

        struct HierarchyUpdate
        {
            public GOStore.GONode node;
            public string parent;

            /// <summary>
            /// Describes a changed parent for a GameObject
            /// </summary>
            /// <param name="node">GONode</param>
            /// <param name="parent">The name of the new parent GameObject</param>
            public HierarchyUpdate(GOStore.GONode node, string parent)
            {
                this.node = node;
                this.parent = parent;
            }
        }
    }

    class GOStore : Dictionary<string, GOStore.GONode>
    {
        private Serializer serializer;
        private NetworkModel config;

        public GOStore(Serializer serializer, NetworkModel config)
        {
            this.serializer = serializer;
            this.config = config;
        }

        public void Add(GameObject go)
        {
            this.Add(go.name, go);
        }

        public void Add(string name, GameObject go)
        {
            this.Add(name, new GONode(go, serializer));
        }

        public GONode GetOrCreate(string name)
        {

            GONode node;

            // Name of gameobject is not existing
            if (!this.TryGetValue(name, out node))
            {
                GameObject go = null;
                if (config.EXISTINGCOMP)
                {
                    go = FindTransform(config.transform, name).gameObject;
                }
                if (go == null)
                {
                    go = new GameObject();
                    go.SetActive(false);
                    go.name = name;
                    go.transform.parent = this["root"].gameObject.transform;
                }
                node = new GONode(go, serializer);
                Add(name, node);
                //go.transform.hasChanged = false;
            }

            return node;
        }

        public string GetReferenceName(GameObject go)
        {
            string refName = go.name;
            while (this.ContainsKey(refName) && this[refName].gameObject.GetInstanceID() != go.GetInstanceID())
            {
                refName = refName + "_" + go.GetInstanceID().ToString();
            }
            return refName;
        }

        private Transform FindTransform(Transform parent, string name)
        {
            if (parent.name.Equals(name))
            {
                return parent;
            }
            foreach (Transform child in parent)
            {
                Transform result = FindTransform(child, name);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        public class GONode
        {
            public GameObject gameObject;
            private Serializer serializer;
            public int parent;
            public Dictionary<Type, string> hashes;

            /// <summary>
            /// Transforms a GameObject to a Node
            /// </summary>
            /// <param name="gameObject"></param>
            public GONode(GameObject gameObject, Serializer serializer)
            {
                this.gameObject = gameObject;
                this.serializer = serializer;
                this.hashes = new Dictionary<Type, string>();
            }

            public void PopulateHashes()
            {
                foreach (Component component in gameObject.GetComponents(typeof(Component)))
                {
                    if (serializer.IsSerializable(component.GetType()) && !hashes.ContainsKey(component.GetType()))
                    {
                        hashes.Add(component.GetType(), "");
                    }
                }
            }

            public void UpdateParent()
            {
                this.parent = gameObject.transform.parent.GetInstanceID();
            }

            public void UpdateHash(Type type)
            {
                if (serializer.IsSerializable(type))
                {
                    Component component = gameObject.GetComponent(type);
                    if (component != null)
                    {
                        hashes[type] = serializer.ToSerializableComponent(component).GetHash();
                    }
                }
            }

            public void UpdateHash(Serializer.Component comp)
            {
                hashes[serializer.GetCommonType(comp)] = comp.GetHash();
            }

            public void RemoveHash(Type type)
            {
                hashes.Remove(type);
            }

            public Component GetOrCreateComponent(Type type)
            {
                Component component = gameObject.GetComponent(type);
                if (component == null)
                {
                    component = gameObject.AddComponent(type);
                }
                return component;
            }
        }
    }

    class AssetStore : Dictionary<string, AssetStore.AssetNode>
    {
        private Serializer serializer;
        private NetworkModel config;

        public AssetStore(Serializer serializer, NetworkModel config)
        {
            this.serializer = serializer;
            this.config = config;
        }

        internal void Add(Object asset)
        {
            this.Add(asset.name, asset);
        }

        internal void Add(string name, Object asset)
        {
            this.Add(name, new AssetNode(asset, serializer));
        }

        internal AssetNode getOrCreate(string assetName, Type type)
        {
            AssetNode node;

            if (!this.TryGetValue(assetName, out node))
            {
                Object asset = null;
                if (config.EXISTINGASSETS)
                {
                    asset = Resources.Load(assetName, type);
                }
                if (asset == null)
                {
                    asset = (Object)Activator.CreateInstance(type);
                    asset.name = assetName;
                }
                node = new AssetNode(asset, serializer);
                Add(assetName, node);
            }
            return node;
        }

        internal string GetReferenceName(Object asset)
        {
            if (asset == null)
            {
                return "null";
            }
            string refName = asset.name;
            while (this.ContainsKey(refName) && this[refName].asset.GetInstanceID() != asset.GetInstanceID())
            {
                refName = refName + "_" + asset.GetInstanceID().ToString();
            }
            //if (!this.ContainsKey(assetName))
            //{
            //    this.Add(assetName, new AssetNode(asset));
            //}
            return refName;
        }




        public class AssetNode
        {
            public Object asset;
            private Serializer serializer;
            public Type type;
            public string hash;

            public AssetNode(Object asset, Serializer serializer)
            {
                if (asset != null)
                {
                    this.asset = asset;
                    this.type = asset.GetType();
                }
                this.serializer = serializer;
            }

            public void UpdateHash()
            {
                hash = serializer.ToSerializableAsset(asset).GetHash();
            }
        }
    }

    [Serializable]
    class Request : ISerializationCallbackReceiver
    {
        public const string ASSET = "a";
        public const string GAMEOBJECT = "g";
        public const string COMPONENT = "c";

        public const string UPDATE = "u";
        public const string DELETE = "d";

        [NonSerialized]
        public string objectType;
        [NonSerialized]
        public string updateType;
        public string type;
        public string name;
        public long timestamp;

        [NonSerialized]
        public string parent;
        [NonSerialized]
        public Serializer.Asset asset;
        [NonSerialized]
        public Type assetType;
        [NonSerialized]
        public List<Serializer.Component> components;
        [NonSerialized]
        public List<Type> componentTypes;

        public string StringParam1;
        public string StringParam2;
        public List<string> ListParam1;
        public List<string> ListParam2;

        public Request(string objectType, string updateType, string name, NetworkModel config)
        {
            this.objectType = objectType;
            this.updateType = updateType;
            this.name = name;
            if (config.TIMESTAMP)
            {
                this.timestamp = DateTime.Now.ToUniversalTime().ToBinary();
            }
            else
            {
                this.timestamp = 0;
            }
        }

        //public static Request AssetRequest()
        //{

        //}

        internal static Request UpdateAsset(string name, Type assetType, Serializer.Asset asset, NetworkModel config)
        {
            Request req = new Request(ASSET, UPDATE, name, config);
            req.asset = asset;
            req.assetType = assetType;
            return req;
        }

        internal static Request DeleteAsset(string name, NetworkModel config)
        {
            Request req = new Request(ASSET, DELETE, name, config);
            return req;
        }

        internal static Request UpdateGO(GameObject go, NetworkModel config)
        {
            Request req = new Request(GAMEOBJECT, UPDATE, go.name, config);
            req.parent = go.transform.parent.name;
            if (req.parent == config.gameObject.name)
            {
                req.parent = "root";
            }
            return req;
        }

        public static Request DeleteGO(string name, NetworkModel config)
        {
            Request req = new Request(GAMEOBJECT, DELETE, name, config);
            return req;
        }

        public static Request UpdateComponents(string name, List<Serializer.Component> comps, NetworkModel config)
        {
            Request req = new Request(COMPONENT, UPDATE, name, config);
            req.components = comps;
            return req;
        }

        public static Request DeleteComponents(string name, List<Type> types, NetworkModel config)
        {
            Request req = new Request(COMPONENT, DELETE, name, config);
            req.componentTypes = types;
            return req;
        }

        /// <summary>
        /// Before the request is send to the server
        /// </summary>
        public void OnBeforeSerialize()
        {
            this.type = this.objectType + this.updateType;
            switch (type)
            {
                case ASSET + UPDATE:
                    this.StringParam1 = assetType.ToString();
                    this.StringParam2 = JsonUtility.ToJson(asset);
                    break;
                case GAMEOBJECT + UPDATE:
                    this.StringParam1 = this.parent;
                    break;
                case COMPONENT + UPDATE:
                    this.ListParam1 = new List<string>();
                    this.ListParam2 = new List<string>();
                    foreach (Serializer.Component component in this.components)
                    {
                        this.ListParam1.Add(component.GetType().ToString());
                        this.ListParam2.Add(JsonUtility.ToJson(component));
                    }
                    break;
                case COMPONENT + DELETE:
                    this.ListParam1 = new List<string>();
                    foreach (Type type in this.componentTypes)
                    {
                        this.ListParam1.Add(type.ToString());
                    }
                    break;
            }
        }

        /// <summary>
        /// After the response is received from the server
        /// </summary>
        public void OnAfterDeserialize()
        {
            this.objectType = this.type.Substring(0, 1);
            this.updateType = this.type.Substring(1, 1);
            switch (type)
            {
                case ASSET + UPDATE:
                    this.assetType = Type.GetType(StringParam1);
                    this.asset = (Serializer.Asset)JsonUtility.FromJson(this.StringParam2, assetType);
                    break;
                case GAMEOBJECT + UPDATE:
                    this.parent = this.StringParam1;
                    break;
                case COMPONENT + UPDATE:
                    this.components = new List<Serializer.Component>();
                    for (int i = 0; i < this.ListParam1.Count && i < this.ListParam2.Count; i++)
                    {
                        Type type = Type.GetType(this.ListParam1[i]);
                        this.components.Add((Serializer.Component)JsonUtility.FromJson(this.ListParam2[i], type));
                    }
                    break;
                case COMPONENT + DELETE:
                    this.componentTypes = new List<Type>();
                    foreach (string typeName in ListParam1)
                    {
                        this.componentTypes.Add(Type.GetType(typeName));
                    }
                    break;
            }
        }
    }

    class Serializer
    {
        AssetStore assetStore;
        NetworkModel config;

        public Serializer(NetworkModel config)
        {
            this.config = config;
        }

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

        internal Asset ToSerializableAsset(Object asset)
        {
            Type type = ToSerializableType(asset.GetType());
            if (type != null)
            {
                return (Asset)Activator.CreateInstance(type, new System.Object[] { asset, assetStore });
            }
            return null;
        }

        internal bool IsSerializable(Type type)
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
        /// Super class for all serializable Components
        /// </summary>
        [Serializable]
        internal abstract class Component
        {

            public Component(UnityEngine.Component component)
            {
            }

            abstract public void Apply(UnityEngine.Component component, AssetStore assetStore);

            public virtual string GetHash()
            {
                string hash = "";
                FieldInfo[] fields = this.GetType().GetFields();
                foreach (FieldInfo field in fields)
                {
                    hash += field.GetValue(this).ToString() + ":";
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

            public override void Apply(UnityEngine.Component component, AssetStore assetStore)
            {
                JsonUtility.FromJsonOverwrite(this.value, component);
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

            public override void Apply(UnityEngine.Component component, AssetStore assetStore)
            {
                UnityEngine.Transform transform = (UnityEngine.Transform)component;

                // If there was no change on client side than use server values
                if (!transform.hasChanged)
                {
                    transform.localPosition = this.p;
                    transform.localRotation = this.r;
                    transform.localScale = this.s;
                    transform.tag = this.t;

                    // Avoid triggering update of changes
                    transform.hasChanged = false;
                }
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

            public override void Apply(UnityEngine.Component component, AssetStore assetStore)
            {
                UnityEngine.Camera camera = (UnityEngine.Camera)component;

                camera.depth = this.d;
                camera.nearClipPlane = this.n;
                camera.farClipPlane = this.f;
                camera.fieldOfView = this.v;
                camera.backgroundColor = this.b;
                camera.clearFlags = this.c;
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

            public override void Apply(UnityEngine.Component component, AssetStore assetStore)
            {
                UnityEngine.Light light = (UnityEngine.Light)component;

                light.type = this.t;
                light.color = this.c;
                light.intensity = this.i;
                light.bounceIntensity = this.b;
            }
        }

        /// <summary>
        /// Serializable MeshFilter
        /// </summary>
        [Serializable]
        internal class MeshFilter : Component
        {
            public string n;

            public MeshFilter(UnityEngine.Component component, AssetStore assetStore) : base(component)
            {
                UnityEngine.MeshFilter meshFilter = (UnityEngine.MeshFilter)component;
                if (meshFilter.sharedMesh == null)
                {
                    this.n = "null";
                }
                else
                {
                    meshFilter.sharedMesh.name = assetStore.GetReferenceName(meshFilter.sharedMesh);
                    if (!assetStore.ContainsKey(meshFilter.sharedMesh.name))
                    {
                        assetStore.Add(meshFilter.sharedMesh);
                    }
                    this.n = meshFilter.sharedMesh.name;
                }
            }

            public override void Apply(UnityEngine.Component component, AssetStore assetStore)
            {
                UnityEngine.MeshFilter meshFilter = (UnityEngine.MeshFilter)component;

                meshFilter.sharedMesh = (UnityEngine.Mesh)assetStore.getOrCreate(this.n, typeof(UnityEngine.Mesh)).asset;
            }
        }

        /// <summary>
        /// Serializable MeshRenderer
        /// </summary>
        [Serializable]
        internal class MeshRenderer : Component
        {
            public string s;

            public MeshRenderer(UnityEngine.Component component, AssetStore assetStore) : base(component)
            {
                UnityEngine.MeshRenderer meshRenderer = (UnityEngine.MeshRenderer)component;
                Material material = meshRenderer.material;

                this.s = material.shader.name;
            }

            public override void Apply(UnityEngine.Component component, AssetStore assetStore)
            {
                UnityEngine.MeshRenderer meshRenderer = (UnityEngine.MeshRenderer)component;
                Shader shader = Shader.Find(this.s);

                if (shader != null)
                {
                    Material material = new Material(shader);
                    meshRenderer.material = material;
                }
            }
        }

        /// <summary>
        /// Serializable MeshCollider
        /// </summary>
        [Serializable]
        internal class MeshCollider : Component
        {
            public string n;

            public MeshCollider(UnityEngine.Component component, AssetStore assetStore) : base(component)
            {
                UnityEngine.MeshCollider meshCollider = (UnityEngine.MeshCollider)component;
                if (meshCollider.sharedMesh == null)
                {
                    this.n = "null";
                }
                else
                {
                    meshCollider.sharedMesh.name = assetStore.GetReferenceName(meshCollider.sharedMesh);
                    if (!assetStore.ContainsKey(meshCollider.sharedMesh.name))
                    {
                        assetStore.Add(meshCollider.sharedMesh);
                    }
                    this.n = meshCollider.sharedMesh.name;
                }
            }

            public override void Apply(UnityEngine.Component component, AssetStore assetStore)
            {
                UnityEngine.MeshCollider meshCollider = (UnityEngine.MeshCollider)component;
                meshCollider.sharedMesh = (UnityEngine.Mesh)assetStore.getOrCreate(this.n, typeof(UnityEngine.Mesh)).asset;
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

            public override void Apply(UnityEngine.Component component, AssetStore assetStore)
            {
                UnityEngine.BoxCollider boxcollider = (UnityEngine.BoxCollider)component;

                boxcollider.center = this.c;
                boxcollider.size = this.s;
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

            public override void Apply(UnityEngine.Component component, AssetStore assetStore)
            {
                UnityEngine.SphereCollider spherecollider = (UnityEngine.SphereCollider)component;

                spherecollider.center = this.c;
                spherecollider.radius = this.r;
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

            public Asset(System.Object asset, AssetStore assetStore)
            {
                this.assetStore = assetStore;
            }

            abstract public void Apply(System.Object asset, AssetStore assetStore);

            public virtual string GetHash()
            {
                string hash = "";
                FieldInfo[] fields = this.GetType().GetFields();
                foreach (FieldInfo field in fields)
                {
                    hash += field.GetValue(this).ToString() + ":";
                }
                Debug.Log("hash : " + hash);
                return hash;

                //StringBuilder hash = new StringBuilder();
                //FieldInfo[] fields = this.GetType().GetFields();
                //foreach (FieldInfo field in fields)
                //{
                //    hash.Append(field.GetValue(this).ToString() + ":");
                //}
                //return hash.ToString();
            }
        }

        /// <summary>
        /// Serializable Mesh
        /// </summary>
        internal class Mesh : Asset
        {

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

            public override void Apply(System.Object asset, AssetStore assetStore)
            {
                UnityEngine.Mesh mesh = (UnityEngine.Mesh)asset;

                mesh.vertices = this.v;
                mesh.normals = this.n;
                mesh.uv = this.u;
                mesh.triangles = this.t;
            }

            public override string GetHash()
            {
                StringBuilder hash = new StringBuilder();
                hash.Append("v");
                foreach (Vector3 val in this.v)
                {
                    hash.Append(val.ToString());
                }
                hash.Append("n");
                foreach (Vector3 val in this.n)
                {
                    hash.Append(val.ToString());
                }
                hash.Append("u");
                foreach (Vector2 val in this.u)
                {
                    hash.Append(val.ToString());
                }
                hash.Append("v");
                foreach (int val in this.t)
                {
                    hash.Append(val.ToString());
                }
                return hash.ToString();
            }
        }

        /*
         * TEMPLATE FOR NEW ASSET
         * 
         * [Serializable]
         * internal class NAMEOFASSET : Asset
         * {
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
         * }
         *
         */
    }


    /// <summary>
    /// Editor interface for public variables
    /// </summary>
    [CustomEditor(typeof(NetworkModel))]
    public class MyScriptEditor : Editor
    {
        override public void OnInspectorGUI()
        {
            var nwm = target as NetworkModel;
            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
            nwm.IP = EditorGUILayout.TextField("IP", nwm.IP);
            nwm.PORT = EditorGUILayout.IntField("Port", nwm.PORT);
            nwm.RECONNECT = EditorGUILayout.Slider(new GUIContent("Reconnect Period", "Minimum time in seconds between reconnect attempts. Will never be faster than update period."), nwm.RECONNECT, 0.5f, 5.0f);
            nwm.PERIOD = EditorGUILayout.Slider(new GUIContent("Update Period", "Minimum time in seconds between two updates."), nwm.PERIOD, 0.0f, 10.0f);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Update direction", EditorStyles.boldLabel);
            nwm.SEND = EditorGUILayout.Toggle("Send", nwm.SEND);
            nwm.RECEIVE = EditorGUILayout.Toggle("Receive", nwm.RECEIVE);
            EditorGUILayout.Space();
            if (nwm.SEND)
            {
                EditorGUILayout.LabelField("Sender properties", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                nwm.TIMESTAMP = EditorGUILayout.Toggle("Update with timestamp", nwm.TIMESTAMP);
                EditorGUILayout.LabelField(new GUIContent("Update components", "For which components should updates be sent"), EditorStyles.boldLabel);
                nwm.TRANSFORM = EditorGUILayout.Toggle(new GUIContent("Transform", "localPosition, localRotation, localScale, tag of gameObject"), nwm.TRANSFORM);
                nwm.CAMERA = EditorGUILayout.Toggle(new GUIContent("Camera", "depth, nearClipPlane, farClipPlane, fieldOfView, backgroundColor, clearFlags"), nwm.CAMERA);
                nwm.LIGHT = EditorGUILayout.Toggle(new GUIContent("Light", "type, color, intensity, bounceIntensity"), nwm.LIGHT);
                nwm.MESHFILTER = EditorGUILayout.Toggle(new GUIContent("MeshFilter", "mesh"), nwm.MESHFILTER);
                nwm.MESHRENDERER = EditorGUILayout.Toggle(new GUIContent("MeshRenderer", "material"), nwm.MESHRENDERER);
                nwm.MESHCOLLIDER = EditorGUILayout.Toggle(new GUIContent("MeshCollider", "mesh"), nwm.MESHCOLLIDER);
                nwm.BOXCOLLIDER = EditorGUILayout.Toggle(new GUIContent("BoxCollider", "center, size"), nwm.BOXCOLLIDER);
                nwm.SPHERECOLLIDER = EditorGUILayout.Toggle(new GUIContent("SphereCollider", "center, radius"), nwm.SPHERECOLLIDER);
                EditorGUILayout.LabelField("Update scripts", EditorStyles.boldLabel);
                nwm.SCRIPTS = EditorGUILayout.Toggle(new GUIContent("Scripts", "Scripts have to be [Serializable] and in the namespace UnityEngine. A matching script Resource must be available in the receiving client."), nwm.SCRIPTS);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
            if (nwm.RECEIVE)
            {
                EditorGUILayout.LabelField("Receiver properties", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                nwm.EXISTINGCOMP = EditorGUILayout.Toggle(new GUIContent("Use existing components", "Attempt to find existing GameObjects of the correct name before creating new ones. Names of GameObject descendants of NetworkModel must be unique when using this option."), nwm.EXISTINGCOMP);
                nwm.EXISTINGASSETS = EditorGUILayout.Toggle(new GUIContent("Use existing assets", "Attempt to find existing Assets of the correct name and type in \"/Resources/\" before creating new ones. Names of Assets in the folder must be unique when using this option."), nwm.EXISTINGASSETS);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
            EditorGUILayout.LabelField("Debugging", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            if (nwm.SEND)
            {
                nwm.DEBUGSEND = EditorGUILayout.Toggle("Debug Sending", nwm.DEBUGSEND);
            }
            if (nwm.RECEIVE)
            {
                nwm.DEBUGREC = EditorGUILayout.Toggle("Debug Receiving", nwm.DEBUGREC);
            }
            EditorGUI.indentLevel--;
        }
    }
}