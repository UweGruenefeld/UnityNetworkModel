/**
 * 
 * Model Script of Unity Network Model
 *
 * @file Model.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-04-30
 **/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Models known state of synchronezed part of the scene. Applies changes as they are requested (if receiving) 
    /// and periodically checks for new changes to request (if sending).
    /// </summary>
    class Model : MonoBehaviour
    {
        GameObject root;

        private NetworkModel config;
        private Bundle bundle;

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
        /// <param name="objectStore"></param>
        /// <param name="resourceStore"></param>
        /// <param name="connection"></param>
        /// <param name="serializer"></param>
        /// <param name="config">NetworkModel</param>
        /// <returns>New Model attached to root</returns>
        public static Model CreateModel(NetworkModel config, Bundle bundle, GameObject root)
        {
            Model model = root.AddComponent<Model>();

            model.config = config;
            model.bundle = bundle;
            //this.root = root;
            //model.root = root;

            model.requestQueue = new Queue<Request>();
            model.nextRequestQueue = new Queue<Request>();
            model.hierarchyQueue = new Queue<HierarchyUpdate>();

            return model;
        }

        public void AddRequest(Request request)
        {
            this.requestQueue.Enqueue(request);
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
        /// Changes to the existence or content of GameObjects, Components and Resources
        /// </summary>
        private void UpdateObjects()
        {
            // List of outstanding requests from server
            while (requestQueue.Count > 0)
            {
                // Get next Request
                Request request = this.requestQueue.Dequeue();
                switch (request.messageType + request.updateType)
                {
                    case Request.RESOURCE + Request.UPDATE:
                        UpdateResource(request);
                        break;
                    case Request.RESOURCE + Request.DELETE:
                        DeleteResource(request.name);
                        break;
                    case Request.OBJECT + Request.UPDATE:
                        // Enqueue GameObject to be sorted into hierarchy, creating it if neccessary.
                        ObjectNode node = this.bundle.objectStore.GetOrCreate(request.name);
                        node.gameObject.SetActive(false);
                        node.gameObject.transform.hasChanged = false;
                        this.hierarchyQueue.Enqueue(new HierarchyUpdate(node, request.parent));
                        break;
                    case Request.OBJECT + Request.DELETE:
                        DeleteObject(request.name);
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
                ObjectNode node;
                if (this.bundle.objectStore.TryGetValue(hierarchyUpdate.parent, out node))
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
        /// Update content of Resource, creating it if necessary.
        /// </summary>
        /// <param name="request"></param>
        private void UpdateResource(Request request)
        {
            ResourceNode node;
            if (this.bundle.resourceStore.TryGet(request.name, this.bundle.serializer.ToCommonType(request.resourceType), out node))
            {
                // If Resource exists, try to apply updates. If successful, request is fulfilled.
                if (request.resource.Apply(node.resource, this.bundle.resourceStore))
                {
                    return;
                }
            }
            else
            {
                // If Resource doesn't exist, create it and try to apply updates. If successful, request is fulfilled.
                UnityEngine.Object resource = request.resource.Construct();
                if (request.resource.Apply(resource, this.bundle.resourceStore))
                {
                    resource.name = request.name;
                    this.bundle.resourceStore.Add(resource);
                    return;
                }
            }

            // If creation / update wasn't successful above, schedule the request to try again later; either in the current or a following update loop.
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
            // Find a node of the correct gameobject for the request to use, creating it if it doesn't exist yet.
            ObjectNode node = this.bundle.objectStore.GetOrCreate(request.name);

            // Update the components
            foreach (AbstractComponent serializedComponent in request.components)
            {
                Type type = this.bundle.serializer.GetCommonType(serializedComponent);
                if (type != null)
                {
                    Component component = node.GetOrCreateComponent(type);
                    if (serializedComponent.Apply(component, this.bundle.resourceStore))
                    {
                        node.UpdateHash(type);
                    }
                    else
                    {
                        Request newReq = Request.UpdateComponents(request.name, new List<AbstractComponent>() { serializedComponent }, config);
                        if (request.iteration < 5)
                        {
                            newReq.iteration = request.iteration + 1;
                            requestQueue.Enqueue(newReq);
                        }
                        else
                        {
                            nextRequestQueue.Enqueue(newReq);
                            Debug.LogWarning("NetworkModel: Missing resource update needed for " + request.name + "." + type);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove Resource from tracked store. Will only be garbage-collected if no other resource uses it.
        /// </summary>
        /// <param name="name"></param>
        private void DeleteResource(string name)
        {
            if (this.bundle.resourceStore.Contains(name))
            {
                this.bundle.resourceStore.Remove(name);
            }
        }

        /// <summary>
        /// Explicitly delete Gameobject from scene and remove it from tracked store.
        /// </summary>
        /// <param name="name"></param>
        private void DeleteObject(string name)
        {
            ObjectNode node;
            if (this.bundle.objectStore.TryGetValue(name, out node))
            {
                Destroy(node.gameObject);
                this.bundle.objectStore.Remove(name);
            }
        }

        /// <summary>
        /// Explicitly delete Component from Gameobject and remove it from tracked store.
        /// </summary>
        /// <param name="name">Name of containing gameobject</param>
        /// <param name="type">Type of component to remove</param>
        private void DeleteComponent(string name, Type type)
        {
            ObjectNode node;
            if (this.bundle.objectStore.TryGetValue(name, out node))
            {
                type = this.bundle.serializer.ToCommonType(type);
                if (type != null)
                {
                    Component comp = node.gameObject.GetComponent(type);
                    node.RemoveHash(this.bundle.serializer.ToCommonType(type));
                    Destroy(comp);
                }
            }
        }

        /// <summary>
        /// Check synchronized part of scene for changes in GameObjects, Components or Resources.
        /// </summary>
        internal void TrackChanges()
        {
            // Add untracked elements to goStore as empty
            foreach (Transform transform in gameObject.GetComponentsInChildren<Transform>())
            {
                if (transform != gameObject.transform)
                {
                    transform.gameObject.name = this.bundle.objectStore.GetReferenceName(transform.gameObject);
                    if (!this.bundle.objectStore.ContainsKey(transform.gameObject.name))
                    {
                        this.bundle.objectStore.Add(transform.gameObject);
                    }
                }
            }

            // Iterate over GameObject Store
            List<string> objectsToDelete = new List<string>();
            foreach (KeyValuePair<string, ObjectNode> objectEntry in this.bundle.objectStore)
            {
                // Never synchronize the root gameobject containing the NetworkModel
                string referenceName = objectEntry.Key;
                if (referenceName == "root")
                {
                    continue;
                }

                // If Element doesn't exist anymore, schedule it for deletion
                ObjectNode node = objectEntry.Value;
                if (node.gameObject == null || node.gameObject.name != referenceName)
                {
                    objectsToDelete.Add(referenceName);
                }

                // Else, check for changes in hierarchy and components
                else
                {
                    // Check if parent of gameObject has changed compared to last known state
                    if (node.gameObject.transform.parent.GetInstanceID() != node.parent)
                    {
                        this.bundle.connection.SendRequest(Request.UpdateObject(node.gameObject, config));
                        node.gameObject.transform.hasChanged = false;
                        node.UpdateParent();
                    }

                    // Add missing components to the node's dictionary of tracked components as unknown
                    node.PopulateHashes();

                    // Iterate over tracked components of the node. 
                    List<Type> compToDelete = new List<Type>();
                    List<AbstractComponent> compToUpdate = new List<AbstractComponent>();
                    foreach (KeyValuePair<Type, long> hashEntry in node.hashes)
                    {
                        Component comp = node.gameObject.GetComponent(hashEntry.Key);

                        // If Component doesn't exist anymroe, schedule it for deletion
                        if (comp == null)
                        {
                            Type type = this.bundle.serializer.ToSerializableType(hashEntry.Key);
                            compToDelete.Add(type);
                        }

                        // Else if component has changed in way tracked by serializer, schedule it for update
                        else
                        {
                            AbstractComponent serComp = this.bundle.serializer.ToSerializableComponent(comp);
                            if (serComp.GetHash() != hashEntry.Value)
                            {
                                compToUpdate.Add(serComp);
                            }
                        }
                    }

                    // Delete scheduled components and send Delete Request
                    foreach (Type type in compToDelete)
                    {
                        node.RemoveHash(this.bundle.serializer.ToCommonType(type));
                    }

                    if (compToDelete.Count > 0)
                    {
                        this.bundle.connection.SendRequest(Request.DeleteComponents(referenceName, compToDelete, config));
                    }

                    // Update scheduled components and send Update Request
                    foreach (AbstractComponent comp in compToUpdate)
                    {
                        node.UpdateHash(comp);
                    }

                    if (compToUpdate.Count > 0)
                    {
                        this.bundle.connection.SendRequest(Request.UpdateComponents(referenceName, compToUpdate, config));
                        node.gameObject.transform.hasChanged = false;
                    }
                }
            }

            // Delete scheduled GameObjects and send Delete Request
            foreach (string referenceName in objectsToDelete)
            {
                this.bundle.objectStore.Remove(referenceName);
                this.bundle.connection.SendRequest(Request.DeleteObject(referenceName, config));
            }

            // Missing Resources were added while updating components (in AbstractComponent..ctor's)
            // Iterate over resourceStore. More Resources may be added to the end of the OrderedDictionary resourceStore during iteration; so a for (int i) loop is used instead of enumerators.
            List<string> resourcesToDelete = new List<string>();
            for (int i = 0; i < this.bundle.resourceStore.Count; i++)
            {
                ResourceNode node = (ResourceNode)this.bundle.resourceStore[i];
                string resourceName = node.name;
                if (resourceName == "null")
                {
                    continue;
                }

                // If Element doesn't exist anymore, schedule it for deletion
                if (node.resource == null || node.resource.name != resourceName || node.resource.GetType() != node.type)
                {
                    resourcesToDelete.Add(resourceName);
                }

                // Else, check for changes
                else
                {
                    AbstractResource serResource = this.bundle.serializer.ToSerializableResource(node.resource);
                    if (serResource.GetHash() != node.hash)
                    {
                        this.bundle.connection.SendRequest(
                            Request.UpdateResource(resourceName, this.bundle.serializer.ToSerializableType(node.type), this.bundle.serializer.ToSerializableResource(node.resource), config));
                        node.UpdateHash();
                    }
                }
            }
            // Delete scheduled Resources and send Delete Request
            foreach (string resourceName in resourcesToDelete)
            {
                this.bundle.resourceStore.Remove(resourceName);
                this.bundle.connection.SendRequest(Request.DeleteResource(resourceName, config));
            }
        }

        struct HierarchyUpdate
        {
            public ObjectNode node;
            public string parent;

            /// <summary>
            /// Describes a changed parent for a GameObject
            /// </summary>
            /// <param name="node">ObjectNode</param>
            /// <param name="parent">The name of the new parent GameObject</param>
            public HierarchyUpdate(ObjectNode node, string parent)
            {
                this.node = node;
                this.parent = parent;
            }
        }
    }
}