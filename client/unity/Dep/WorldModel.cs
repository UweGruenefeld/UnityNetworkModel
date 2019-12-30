using System;
using System.Collections.Generic;

namespace UnityEngine
{
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

        // Queue of requests to be applied during the next update loop
        private Queue<Request> requestQueue;
        // Queue of requests that need to be retried
        private Queue<Request> nextRequestQueue;
        // Queue of hierarchy changes to be applied
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
        /// Changes to the existence or content of Assets, GameObjects and Components
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
                    foreach (KeyValuePair<Type, long> hashEntry in node.hashes)
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
}