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
 * - Material
 * - Texture2D
 * 
 * To add new synchronizable components or assets, search for
 * TEMPLATE FOR NEW COMPONENT
 * or
 * TEMPLATE FOR NEW ASSET
 *
 * @file NetworkModel.cs
 * @author Uwe Gruenefeld
 * @author Tobias Lunte
 * @version 2019-07-22
 **/
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using WebSocketSharp;
using System.Text;
using System.Collections;

namespace UnityEngine
{
    /// <summary>
    /// Manages relations between disparate components
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
        // Receiver
        public string RECEIVECHANNELS = "default";
        public bool EXISTINGCOMP = false;
        public bool EXISTINGASSETS = false;
        // Debugging
        public string SENDCHANNEL = "default";
        public bool DEBUGSEND = false;
        public bool DEBUGREC = false;

        // Components
        private Serializer serializer;
        private GOStore goStore;
        private AssetStore assetStore;
        private Connection connection;
        private WorldModel worldModel;

        // Old Values to perceive changes
        private string OLDRECEIVECHANNELS = "";

        private float time;

        private void Start()
        {
            //Set up components
            this.serializer = new Serializer(this);
            this.goStore = new GOStore(serializer, this);
            this.goStore.Add("root", this.gameObject);
            this.assetStore = new AssetStore(serializer, this);
            this.assetStore.Add("null", null);
            this.serializer.SetAssetStore(assetStore);
            this.connection = new Connection(this);
            this.worldModel = WorldModel.CreateWorldModel(this.gameObject, goStore, assetStore, connection, serializer, this);

            //Set up handler for received messages
            this.connection.OnMessage += (sender, message) => worldModel.ReceiveRequest(message.Data);
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
                    //apply change-requests that were received from the server since the last update
                    if (this.RECEIVE)
                    {
                        worldModel.ApplyChanges();

                        if (RECEIVECHANNELS != OLDRECEIVECHANNELS)
                        {
                            ISet<string> _rChannels = new HashSet<string>(RECEIVECHANNELS.Split(','));
                            ISet<string> _oldRChannels = new HashSet<string>(OLDRECEIVECHANNELS.Split(','));
                            ISet<string> rChannels = new HashSet<string>();
                            ISet<string> oldRChannels = new HashSet<string>();
                            foreach (string channel in _rChannels)
                            {
                                rChannels.Add(channel.Trim());
                            }
                            foreach (string channel in _oldRChannels)
                            {
                                oldRChannels.Add(channel.Trim());
                            }
                            ISet<string> newRChannels = new HashSet<string>(rChannels);
                            newRChannels.ExceptWith(oldRChannels);
                            oldRChannels.ExceptWith(rChannels);

                            foreach (string channel in oldRChannels)
                            {
                                if (channel != "")
                                    connection.SendAsync("{\"subscriptionChange\":\"unsubscribe\", \"channel\":\"" + channel + "\"}", null);
                            }
                            foreach (string channel in newRChannels)
                            {
                                if (channel != "")
                                    connection.SendAsync("{\"subscriptionChange\":\"subscribe\", \"channel\":\"" + channel + "\"}", null);
                            }
                            OLDRECEIVECHANNELS = RECEIVECHANNELS;
                        }
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

    /// <summary>
    /// Manages connection to webserver. Provides Sender and Receiver capabilities.
    /// </summary>
    class Connection : WebSocket
    {
        private NetworkModel config;

        // Variables for websocket connection
        private float lastAttempt;

        public Connection(NetworkModel config) : base("ws://" + config.IP + ":" + config.PORT)
        {
            this.config = config;
            lastAttempt = -config.RECONNECT;
        }

        public bool EnsureIsAlive()
        {
            // If the websocket is not alive try to connect to the server
            if (!this.IsAlive)
            {
                // Check if last attempt to connect is at least one Reconnect Period ago
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

        internal void SendRequest(Request request)
        {
            string json = JsonUtility.ToJson(request);
            if (config.DEBUGSEND)
                Debug.Log("NetworkModel: Sent message " + json);
            this.SendAsync(json, null);
        }
    }

    /// <summary>
    /// Models known state of synchronezed part of the scene. Applies changes as they are requested (if receiving) and periodically checks for new changes to request (if sending).
    /// </summary>
    class WorldModel : MonoBehaviour
    {
        GameObject root;
        private GOStore goStore;
        private AssetStore assetStore;
        private Connection connection;
        private Serializer serializer;
        private NetworkModel config;

        private Queue<Request> requestQueue;
        private Queue<Request> nextRequestQueue;
        private Queue<HierarchyUpdate> hierarchyQueue;

        /// <summary>
        /// Faux-constructor since Monobehaviours may not be constructed traditionally.
        /// </summary>
        /// <param name="root">Same GameObject as NetworkModel</param>
        /// <param name="goStore"></param>
        /// <param name="assetStore"></param>
        /// <param name="connection"></param>
        /// <param name="serializer"></param>
        /// <param name="config">NetworkModel</param>
        /// <returns>New WorldModel attached to root</returns>
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
            wm.nextRequestQueue = new Queue<Request>();
            wm.hierarchyQueue = new Queue<HierarchyUpdate>();

            return wm;
        }

        /// <summary>
        /// Apply outstanding changes to scene.
        /// </summary>
        public void ApplyChanges()
        {
            UpdateObjects();
            UpdateHierarchy();
        }

        /// <summary>
        /// Changes to the existance or content of Assets, GameObjects and Components
        /// </summary>
        private void UpdateObjects()
        {
            // List of outstanding requests from server
            while (requestQueue.Count > 0)
            {
                // Get next Request
                Request request = this.requestQueue.Dequeue();
                switch (request.objectType + request.updateType)
                {
                    case Request.ASSET + Request.UPDATE:
                        UpdateAsset(request);
                        break;
                    case Request.ASSET + Request.DELETE:
                        DeleteAsset(request.name);
                        break;
                    case Request.GAMEOBJECT + Request.UPDATE:
                        // Enqueue GameObject to be sorted into hierarchy, creating it if neccessary.
                        GOStore.GONode node = goStore.GetOrCreate(request.name);
                        node.gameObject.SetActive(false);
                        node.gameObject.transform.hasChanged = false;
                        this.hierarchyQueue.Enqueue(new HierarchyUpdate(node, request.parent));
                        break;
                    case Request.GAMEOBJECT + Request.DELETE:
                        DeleteGameObject(request.name);
                        break;
                    case Request.COMPONENT + Request.UPDATE:
                        UpdateComponents(request);
                        break;
                    case Request.COMPONENT + Request.DELETE:
                        foreach (Type type in request.componentTypes)
                        {
                            DeleteComponent(request.name, type);
                        }
                        break;
                }
            }

            // Schedule currently unfulfillable requests for next update.
            while (nextRequestQueue.Count > 0)
            {
                requestQueue.Enqueue(nextRequestQueue.Dequeue());
            }
        }

        /// <summary>
        /// Changes to the hierarchy between GameObjects
        /// </summary>
        private void UpdateHierarchy()
        {
            // List of gameobjects that need an hierarchy update
            while (hierarchyQueue.Count > 0)
            {
                // Get first request
                HierarchyUpdate hierarchyUpdate = this.hierarchyQueue.Dequeue();

                // Check if requested parent is known. If so, set gameObject to be child of parent
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

        /// <summary>
        /// Update content of Asset, creating it if necessary.
        /// </summary>
        /// <param name="request"></param>
        private void UpdateAsset(Request request)
        {
            AssetStore.AssetNode node;
            if (assetStore.TryGet(request.name, serializer.ToCommonType(request.assetType), out node))
            {
                // if Asset exists, try to apply updates. If successful, request is fulfilled.
                if (request.asset.Apply(node.asset, assetStore))
                {
                    return;
                }
            }
            else
            {
                // if Asset doesn't exist, create it and try to apply updates. If successful, request is fulfilled.
                Object asset = request.asset.Construct();
                if (request.asset.Apply(asset, assetStore))
                {
                    asset.name = request.name;
                    assetStore.Add(asset);
                    return;
                }
            }

            // if creation / update wasn't successful above, schedule the request to try again later; either in the current or a following update loop.
            if (request.iteration < 5)
            {
                request.iteration++;
                requestQueue.Enqueue(request);
            }
            else
            {
                request.iteration = 0;
                nextRequestQueue.Enqueue(request);
                Debug.LogWarning("NetworkModel: Missing asset update needed for " + request.name);
            }
        }

        /// <summary>
        /// Update components in GameObject, creating them if necessary.
        /// </summary>
        /// <param name="request"></param>
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
                    if (serializedComponent.Apply(component, assetStore))
                    {
                        node.UpdateHash(type);
                    }
                    else
                    {
                        Request newReq = Request.UpdateComponents(request.name, new List<Serializer.Component>() { serializedComponent }, config);
                        if (request.iteration < 5)
                        {
                            newReq.iteration = request.iteration + 1;
                            requestQueue.Enqueue(newReq);
                        }
                        else
                        {
                            nextRequestQueue.Enqueue(newReq);
                            Debug.LogWarning("NetworkModel: Missing asset update needed for " + request.name + "." + type);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove Asset from tracked store. Will only be garbage-collected if no other resource uses it.
        /// </summary>
        /// <param name="name"></param>
        private void DeleteAsset(string name)
        {
            if (this.assetStore.Contains(name))
            {
                this.assetStore.Remove(name);
            }
        }

        /// <summary>
        /// Explicitly delete Gameobject from scene and remove it from tracked store.
        /// </summary>
        /// <param name="name"></param>
        private void DeleteGameObject(string name)
        {
            GOStore.GONode node;
            if (this.goStore.TryGetValue(name, out node))
            {
                Destroy(node.gameObject);
                this.goStore.Remove(name);
            }
        }

        /// <summary>
        /// Explicitly delete Component from Gameobject and remove it from tracked store.
        /// </summary>
        /// <param name="name">Name of containing gameobject</param>
        /// <param name="type">Type of component to remove</param>
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

        /// <summary>
        /// Check synchronized part of scene for changes in GameObjects, Components or Assets.
        /// </summary>
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
                // Never synchronize the root gameobject containing the NetworkModel.
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
                    // check if parent of gameObject has changed compared to last known state
                    if (node.gameObject.transform.parent.GetInstanceID() != node.parent)
                    {
                        connection.SendRequest(Request.UpdateGO(node.gameObject, config));
                        node.gameObject.transform.hasChanged = false;
                        node.UpdateParent();
                    }

                    //Add missing components to the node's dictionary of tracked components as unknown
                    node.PopulateHashes();

                    // Iterate over tracked components of the node. 
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
            // Iterate over assetStore. More Assets may be added to the end of the OrderedDictionary assetStore during iteration; so a for (int i) loop is used instead of enumerators.
            List<string> assetsToDelete = new List<string>();
            for (int i = 0; i < assetStore.Count; i++)
            {
                AssetStore.AssetNode node = (AssetStore.AssetNode)assetStore[i];
                string assetName = node.name;
                if (assetName == "null")
                {
                    continue;
                }

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
            // Delete scheduled Assets and send Delete Request
            foreach (string assetName in assetsToDelete)
            {
                assetStore.Remove(assetName);
                connection.SendRequest(Request.DeleteAsset(assetName, config));
            }
        }

        /// <summary>
        /// Handler for new Requests from the server. Simply enqueues them to be applied during the next ApplyChanges().
        /// </summary>
        /// <param name="json"></param>
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

    /// <summary>
    /// Collects all known GameObjects and tracks the last known state of their parent and components.
    /// </summary>
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

        /// <summary>
        /// Return a GameObject of the correct name by priority
        /// 1) A matching tracked GameObject from the store.
        /// 2) A matching GameObject from tracked part of the scene (if using existing components is enabled).
        /// 3) A new empty GameObject of the correct name.
        /// </summary>
        /// <param name="name">Name of the gameObject</param>
        /// <returns>GameObject of matching name</returns>
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
            }

            return node;
        }

        /// <summary>
        /// Determine name that a gameObject should be stored under in the store. Will return current storage name if gameObject is already present or new free name if not. 
        /// GameObject's name should be set to returned name if it is to be stored.
        /// </summary>
        /// <param name="go">Original name of gameObject</param>
        /// <returns>Name the GameObject should be stored under</returns>
        public string GetReferenceName(GameObject go)
        {
            string refName = go.name;
            while (this.ContainsKey(refName) && this[refName].gameObject.GetInstanceID() != go.GetInstanceID())
            {
                refName = refName + "_" + go.GetInstanceID().ToString();
            }
            return refName;
        }

        /// <summary>
        /// Performs a depth-first search for a Transform of the correct Name among all descendants of parent transform.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <returns>Transform of matching name</returns>
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

        /// <summary>
        /// Stores specific GameObject, tracking its last known parent and components
        /// </summary>
        public class GONode
        {
            public GameObject gameObject;
            private Serializer serializer;
            public int parent;
            public Dictionary<Type, string> hashes;

            /// <summary>
            /// Wraps a GameObject into a Node
            /// </summary>
            /// <param name="gameObject"></param>
            /// <param name="serializer"></param>
            public GONode(GameObject gameObject, Serializer serializer)
            {
                this.gameObject = gameObject;
                this.serializer = serializer;
                this.hashes = new Dictionary<Type, string>();
            }

            /// <summary>
            /// Add missing hashes to component Dictionary as empty == unknown
            /// </summary>
            public void PopulateHashes()
            {
                foreach (Component component in gameObject.GetComponents(typeof(Component)))
                {
                    if (serializer.ShouldBeSerialized(component.GetType()) && !hashes.ContainsKey(component.GetType()))
                    {
                        hashes.Add(component.GetType(), "");
                    }
                }
            }

            /// <summary>
            /// Update stored parent to current state
            /// </summary>
            public void UpdateParent()
            {
                this.parent = gameObject.transform.parent.GetInstanceID();
            }

            /// <summary>
            /// Update specific component hash to current state
            /// </summary>
            /// <param name="type">Type of component to update</param>
            public void UpdateHash(Type type)
            {
                if (serializer.ShouldBeSerialized(type))
                {
                    Component component = gameObject.GetComponent(type);
                    if (component != null)
                    {
                        hashes[type] = serializer.ToSerializableComponent(component).GetHash();
                    }
                }
            }

            /// <summary>
            /// Update specific component hash to current state
            /// UpdateHash(Type type) should always be preferred, if possible.
            /// </summary>
            /// <param name="comp">serialized component to update</param>
            public void UpdateHash(Serializer.Component comp)
            {
                hashes[serializer.GetCommonType(comp)] = comp.GetHash();
            }

            /// <summary>
            /// Remove tracking for component of specific Type
            /// </summary>
            /// <param name="type"></param>
            public void RemoveHash(Type type)
            {
                hashes.Remove(type);
            }

            /// <summary>
            /// Returns matching Component by priority
            /// 1) Existing matching component from the gameObject
            /// 2) New matching component added to the gameObject
            /// </summary>
            /// <param name="type"></param>
            /// <returns>Component of matching Type</returns>
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

    /// <summary>
    /// Collects all known Assets and tracks their last known state.
    /// </summary>
    class AssetStore : OrderedDictionary/*<string, AssetStore.AssetNode>*/
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
            this.Add(name, new AssetNode(asset, name, serializer));
        }

        /// <summary>
        /// Tries to find an Asset of correct name and type
        /// 1) A matching tracked Asset from the store.
        /// 2) A matching Asset of the correct type from the Resources folder (if using existing assets is enabled).
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="type"></param>
        /// <param name="node"></param>
        /// <returns>Returns true if a matching Asset was found.</returns>
        internal bool TryGet(string assetName, Type type, out AssetNode node)
        {
            if (this.Contains(assetName))
            {
                AssetNode tmp = (AssetNode)this[assetName];
                if (tmp.asset.GetType() == type)
                {
                    node = tmp;
                    return true;
                }
            }
            if (config.EXISTINGASSETS)
            {
                Object asset = Resources.Load("NetworkModel/" + assetName, type);
                if (asset != null)
                {
                    node = new AssetNode(asset, assetName, serializer);
                    Add(assetName, node);
                    return true;
                }
            }

            node = null;
            return false;
        }

        /// <summary>
        /// Determine name that a Asset should be stored under in the store. Will return current storage name if Asset is already present or new free name if not. 
        /// Asset's name should be set to returned name if it is to be stored.
        /// </summary>
        /// <param name="asset"></param>
        /// <returns>Name the Asset should be stored under</returns>
        internal string GetReferenceName(Object asset)
        {
            if (asset == null)
            {
                return "null";
            }
            string refName = asset.name;
            while (this.Contains(refName) && ((AssetNode)this[refName]).asset.GetInstanceID() != asset.GetInstanceID())
            {
                refName = refName + "_" + asset.GetInstanceID().ToString();
            }
            return refName;
        }

        /// <summary>
        /// Stores specific Asset, tracking its last known state
        /// </summary>
        public class AssetNode
        {
            public Object asset;
            public string name;
            private Serializer serializer;
            public Type type;
            public string hash = "";

            /// <summary>
            /// Wraps an Asset into a Node
            /// </summary>
            /// <param name="asset"></param>
            /// <param name="serializer"></param>
            public AssetNode(Object asset, string name, Serializer serializer)
            {
                this.name = name;
                if (asset != null)
                {
                    this.asset = asset;
                    this.type = asset.GetType();
                }
                this.serializer = serializer;
            }

            /// <summary>
            /// Updates stored state of Asset to match current state
            /// </summary>
            public void UpdateHash()
            {
                hash = serializer.ToSerializableAsset(asset).GetHash();
            }
        }
    }

    /// <summary>
    /// Request for changes to Assets, GameObjects and Components to be sent between Client and Server
    /// </summary>
    [Serializable]
    class Request : ISerializationCallbackReceiver
    {
        // Types of request
        public const string ASSET = "a";
        public const string GAMEOBJECT = "g";
        public const string COMPONENT = "c";

        public const string UPDATE = "u";
        public const string DELETE = "d";

        // Parameters used by all requests
        [NonSerialized]
        public string objectType;
        [NonSerialized]
        public string updateType;
        public string type;
        public string name;
        public string channel;
        public long timestamp;

        [NonSerialized]
        public int iteration;

        // Human-readable parameters for specific types of request
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

        // Serialized parameters that human-readable parameters are mapped to while sending over the network
        public string StringParam1;
        public string StringParam2;
        public List<string> ListParam1;
        public List<string> ListParam2;

        /// <summary>
        /// Common constructor used by static Request-generating methdos
        /// </summary>
        /// <param name="objectType"></param>
        /// <param name="updateType"></param>
        /// <param name="name"></param>
        /// <param name="config"></param>
        private Request(string objectType, string updateType, string name, NetworkModel config)
        {
            this.objectType = objectType;
            this.updateType = updateType;
            this.name = name;
            this.channel = config.SENDCHANNEL.Trim();
            this.iteration = 0;
            if (config.TIMESTAMP)
            {
                this.timestamp = DateTime.Now.ToUniversalTime().ToBinary();
            }
            else
            {
                this.timestamp = 0;
            }
        }

        /// <summary>
        /// Create Request to update a specific Asset
        /// </summary>
        /// <param name="name"></param>
        /// <param name="assetType"></param>
        /// <param name="asset"></param>
        /// <param name="config"></param>
        /// <returns>Request</returns>
        internal static Request UpdateAsset(string name, Type assetType, Serializer.Asset asset, NetworkModel config)
        {
            Request req = new Request(ASSET, UPDATE, name, config);
            req.asset = asset;
            req.assetType = assetType;
            return req;
        }

        /// <summary>
        /// Create Request to delete a specific Asset
        /// </summary>
        /// <param name="name"></param>
        /// <param name="config"></param>
        /// <returns>Request</returns>
        internal static Request DeleteAsset(string name, NetworkModel config)
        {
            Request req = new Request(ASSET, DELETE, name, config);
            return req;
        }

        /// <summary>
        /// Create Request to update specific GameObject
        /// </summary>
        /// <param name="go"></param>
        /// <param name="config"></param>
        /// <returns>Request</returns>
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

        /// <summary>
        /// Create Request to delete specific GameObject
        /// </summary>
        /// <param name="name"></param>
        /// <param name="config"></param>
        /// <returns>Request</returns>
        public static Request DeleteGO(string name, NetworkModel config)
        {
            Request req = new Request(GAMEOBJECT, DELETE, name, config);
            return req;
        }

        /// <summary>
        /// Create Request to update a List of Components belonging to a specific GameObject
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comps"></param>
        /// <param name="config"></param>
        /// <returns>Request</returns>
        public static Request UpdateComponents(string name, List<Serializer.Component> comps, NetworkModel config)
        {
            Request req = new Request(COMPONENT, UPDATE, name, config);
            req.components = comps;
            return req;
        }

        /// <summary>
        /// Create Request to delete a List of Components belonging to a specific GameObject
        /// </summary>
        /// <param name="name"></param>
        /// <param name="types"></param>
        /// <param name="config"></param>
        /// <returns>Request</returns>
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
        /// Super class for all serializable Components
        /// </summary>
        [Serializable]
        internal abstract class Component
        {

            public Component(UnityEngine.Component component)
            {
            }

            abstract public bool Apply(UnityEngine.Component component, AssetStore assetStore);

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

            public Asset(System.Object asset, AssetStore assetStore)
            {
                this.assetStore = assetStore;
            }

            abstract public bool Apply(System.Object asset, AssetStore assetStore);

            public virtual Object Construct()
            {
                return (Object)Activator.CreateInstance(commonType);
            }

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

            public override string GetHash()
            {
                StringBuilder hash = new StringBuilder();
                hash.Append(base.GetHash());
                foreach (string val in this.k)
                {
                    hash.Append(val.ToString());
                }
                return hash.ToString();
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
                // ImageConversion.LoadImage(texture, this.p);
                return true;
            }

            public override Object Construct()
            {
                return new UnityEngine.Texture2D(this.w, this.h);
            }

            public override string GetHash()
            {
                UInt64 colorHash = 1;
                UInt64 p = 31;
                foreach (Color c in this.p)
                {
                    unchecked
                    {
                        colorHash *= p;
                        colorHash += (UInt64)c.r*255;
                        colorHash *= p;
                        colorHash += (UInt64)c.b * 255;
                        colorHash *= p;
                        colorHash += (UInt64)c.g * 255;
                    }
                }

                return base.GetHash() + colorHash;
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
         *     
         *     // public override string GetHash() {}
         *     // {
         *     //    OVERRIDE TO CONSIDER VALUES INSIDE ARRAYS
         *     // v}
         * }
         *
         */
    }


    /// <summary>
    /// Editor interface for public variables
    /// </summary>
#if UNITY_EDITOR
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
                nwm.SENDCHANNEL = EditorGUILayout.TextField(new GUIContent("Send on channel", "Singular channel on which to broadcast changes. May not contain \",\"."), nwm.SENDCHANNEL);
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
                nwm.RECEIVECHANNELS = EditorGUILayout.TextField(new GUIContent("Receive on channels", "List of channels from which to receive changes, separated by \",\"."), nwm.RECEIVECHANNELS);
                nwm.EXISTINGCOMP = EditorGUILayout.Toggle(new GUIContent("Use existing components", "Attempt to find existing GameObjects of the correct name before creating new ones. Names of GameObject descendants of NetworkModel must be unique when using this option."), nwm.EXISTINGCOMP);
                nwm.EXISTINGASSETS = EditorGUILayout.Toggle(new GUIContent("Use existing assets", "Attempt to find existing Assets of the correct name and type in \"/Resources/NetworkModel\" before creating new ones. Names of Assets in the folder must be unique when using this option."), nwm.EXISTINGASSETS);
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
#endif
}