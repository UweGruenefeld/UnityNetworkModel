/**
 * 
 * Model Script of Unity Network Model
 *
 * @file Model.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-05-05
 *
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
    internal class Model : MonoBehaviour
    {
        internal static string ROOT_NAME = "root";
        internal static int ATTEMPTS_TO_UPDATE = 10;

        private Injector injector;

        // Queue of requests to be applied during the next update loop
        private Queue<Request> requestQueue;
        // Queue of requests that need to be retried
        private Queue<Request> nextRequestQueue;
        // Queue of hierarchy changes to be applied
        private Queue<HierarchyUpdate> hierarchyQueue;

        /// <summary>
        /// Faux-constructor since Monobehaviours may not be constructed traditionally.
        /// </summary>
        /// <param name="injector"></param>
        /// <returns>new model</returns>
        internal static Model CreateModel(Injector injector)
        {
            Model model = injector.configuration.gameObject.AddComponent<Model>();

            model.injector = injector;

            model.requestQueue = new Queue<Request>();
            model.nextRequestQueue = new Queue<Request>();
            model.hierarchyQueue = new Queue<HierarchyUpdate>();

            return model;
        }

        /// <summary>
        /// Add Request to queue
        /// </summary>
        /// <param name="request"></param>
        internal void AddRequest(Request request)
        {
            this.requestQueue.Enqueue(request);
        }

        /// <summary>
        /// Check synchronized part of scene for changes in GameObjects, Components or Resources.
        /// </summary>
        internal void TrackChanges()
        {
            // Create lists of resources and components that have pending requests
            List<NameTypePair> blockedResources = new List<NameTypePair>();
            List<NameTypePair> blockedComponents = new List<NameTypePair>();

            // Check pending requests for components and resources
            foreach(Request request in requestQueue.ToArray())
            {
                switch(request.messageType)
                {
                    // Check if request is pending for resource
                    case Request.RESOURCE:
                        if(request.resource != null)
                            blockedResources.Add(new NameTypePair(request.name, request.resource.commonType));
                        else if(request.resourceType != null)
                            blockedResources.Add(new NameTypePair(request.name, request.resourceType));
                        break;

                    // Check if request is pending for component
                    case Request.COMPONENT:
                        if(request.components != null)
                            // Component requests can contain multiple components
                            foreach(AbstractComponent component in request.components)
                                blockedComponents.Add(new NameTypePair(request.name, component.commonType));
                        else if(request.componentTypes != null)
                            // Component requests can contain multiple components
                            foreach(Type type in request.componentTypes)
                                blockedComponents.Add(new NameTypePair(request.name, type));
                        break;
                }
            }

            // Add untracked GameObjects to ObjectStore as Objects without Components
            foreach (Transform transform in this.gameObject.GetComponentsInChildren<Transform>())
            {
                // Make sure that GameObject with NetworkModel attached does not get synced to Server
                if (transform != this.gameObject.transform)
                {
                    // Get existing reference or create a unique reference for GameObject
                    transform.gameObject.name = this.injector.objectStore.GetReferenceName(transform.gameObject);

                    // Check if GameObject is already in ObjectStore
                    if (!this.injector.objectStore.ContainsKey(transform.gameObject.name))
                        this.injector.objectStore.Add(transform.gameObject);
                }
            }

            // List of GameObjects that have been deleted on this client
            List<string> objectsToDelete = new List<string>();

            // Iterate over GameObject Store
            foreach (KeyValuePair<string, ObjectNode> objectEntry in this.injector.objectStore)
            {
                // Never synchronize the root gameobject containing the NetworkModel
                string referenceName = objectEntry.Key;
                if (referenceName == Model.ROOT_NAME)
                    continue;

                // If Element does not exist anymore (or the name was changed), then add it to the list to schedule the deletion
                // (When changing the name of a GameObject, the ObjectNode of that GameOBject gets destroyed and a new ObjectNode is created)
                ObjectNode node = objectEntry.Value;
                if (node.gameObject == null || node.gameObject.name != referenceName)
                    objectsToDelete.Add(referenceName);

                // Else, check for changes in hierarchy and components
                else
                {
                    // Check if parent of gameObject has changed compared to last known state
                    if (node.gameObject.transform.parent.GetInstanceID() != node.parentID)
                    {
                        // Send updated parent information to server
                        this.injector.connection.SendRequest(Request.UpdateObject(this.injector, node.gameObject));
                        node.gameObject.transform.hasChanged = false;
                    }

                    // Update Node with current values from represented GameObject
                    node.Update();

                    // List of Components that have been deleted on this client
                    List<Type> deletedComponents = new List<Type>();
                    // List of Components that have been deleted and inform the server about it
                    List<Type> sendDeletedComponents = new List<Type>();

                    // List of Components that have been changed on this client
                    List<AbstractComponent> updatedComponents = new List<AbstractComponent>();
                    // List of Components that have been changed and inform the server about it
                    List<AbstractComponent> sendUpdatedComponents = new List<AbstractComponent>();

                    // Iterate over tracked components of the node
                    foreach (KeyValuePair<Type, long> hashEntry in node.hashes)
                    {
                        // If component is blocked from tracking changes, then continue with next component
                        if(blockedComponents.Contains(new NameTypePair(node.gameObject.name, hashEntry.Key)))
                        {
                            LogUtility.Log(this.injector, LogType.INFORMATION, "Component " + hashEntry.Key + " update blocked");
                            continue;
                        }

                        // Get the component which is pointed at in this loop
                        Component component = node.gameObject.GetComponent(hashEntry.Key);

                        // If component is allowed to send, then track changes
                        bool send = RuleUtility.FindComponentRule(this.injector, node.gameObject, component.GetType(), UpdateType.SEND);

                        // If Component does not exist anymore, then schedule it for deletion
                        if (component == null)
                        {
                            // Delete references to component on client
                            Type type = this.injector.serializer.ToSerializableType(hashEntry.Key);
                            deletedComponents.Add(type);

                            // Inform the server about the change
                            if (send)
                                sendDeletedComponents.Add(type);

                            continue;
                        }

                        // Transform UnityEngine Component to NetworkModel AbstractComponent
                        AbstractComponent serializedComponent = this.injector.serializer.ToSerializableComponent(component);

                        // If Component does not exist as a serializable Component
                        if(serializedComponent == null)
                            continue;

                        // Generate hash of component and compare it to the previously stored value; if not equal, then add it to update list
                        if (serializedComponent.GetHash() != hashEntry.Value)
                        {
                            // Update hash stored for component on client
                            updatedComponents.Add(serializedComponent);

                            // Inform the server about the change
                            if (send)
                                sendUpdatedComponents.Add(serializedComponent);
                        }
                    }

                    // Components scheduled for Delete
                    foreach (Type type in deletedComponents)
                        node.RemoveComponent(this.injector.serializer.ToCommonType(type));

                    // Components scheduled for Update
                    foreach (AbstractComponent component in updatedComponents)
                        node.UpdateComponent(component.commonType);

                    // Check if there are deleted components the server needs to be informed about
                    if (sendDeletedComponents.Count > 0)
                        this.injector.connection.SendRequest(Request.DeleteComponents(this.injector, referenceName, sendDeletedComponents));

                    // Check if there are updated components the server needs to be informed about
                    if (sendUpdatedComponents.Count > 0)
                    {
                        this.injector.connection.SendRequest(Request.UpdateComponents(this.injector, referenceName, sendUpdatedComponents));
                        node.gameObject.transform.hasChanged = false;
                    }
                }
            }

            // Delete scheduled GameObjects and send Delete Request
            foreach (string referenceName in objectsToDelete)
            {
                this.injector.objectStore.Remove(referenceName);
                this.injector.connection.SendRequest(Request.DeleteObject(this.injector, referenceName));
            }

            // List of Resources that have been deleted on this client
            List<string> resourcesToDelete = new List<string>();
            
            // Iterate over Resource Store
            for (int i = 0; i < this.injector.resourceStore.Count; i++)
            {
                // Get the resource which is pointed at in this loop
                ResourceNode node = (ResourceNode)this.injector.resourceStore[i];
                string resourceName = node.name;

                // If resource name is null, then check next resource
                if (resourceName == "null")
                    continue;

                // If resource is blocked from tracking changes, then continue with next resource
                if(blockedResources.Contains(new NameTypePair(resourceName, node.type)))
                {
                    LogUtility.Log(this.injector, LogType.INFORMATION, "Resource " + node.type + " update blocked");
                    continue;
                }

                // If resource is allowed to send, then track changes
                bool send = RuleUtility.FindResourceRule(this.injector, node.type, UpdateType.SEND);

                // If Element does not exist anymore, then schedule it for deletion
                if (node.resource == null || node.resource.name != resourceName || node.resource.GetType() != node.type)
                {
                    // Delete references to resource on client
                    resourcesToDelete.Add(resourceName);

                    // Inform the server about the change
                    if (send)
                        this.injector.connection.SendRequest(Request.DeleteResource(this.injector, resourceName));
                }
                // Else, check for changes
                else
                {
                    // Transform UnityEngine Resource to NetworkModel AbstractResource
                    AbstractResource serializedResource = this.injector.serializer.ToSerializableResource(node.resource);
                    if (serializedResource.GetHash() != node.hash)
                    {
                        // Inform the server about the change
                        if (send)
                            this.injector.connection.SendRequest(Request.UpdateResource(this.injector, resourceName,
                                this.injector.serializer.ToSerializableType(node.type), serializedResource));

                        // Update hash stored for resource on client
                        node.UpdateHash();
                    }
                }
            }

            // Delete resources scheduled for deleting from ResourceStore
            foreach (string resourceName in resourcesToDelete)
                this.injector.resourceStore.Remove(resourceName);
        }

        /// <summary>
        /// Apply outstanding changes to scene.
        /// </summary>
        internal void ApplyChanges()
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

                // If request does not contain the name of a subscribed channel, then continue with next request
                if (!this.injector.subscriptions.receiveChannelList.Contains(request.channel))
                    continue;

                // Continue dependent on content of request
                switch (request.messageType + request.updateType)
                {
                    // Update GameObject    
                    case Request.OBJECT + Request.UPDATE:

                        // Enqueue GameObject to be sorted into hierarchy, creating it if neccessary.
                        ObjectNode node = this.injector.objectStore.GetOrCreate(request.name);
                        node.gameObject.SetActive(false);
                        node.gameObject.transform.hasChanged = false;
                        this.hierarchyQueue.Enqueue(new HierarchyUpdate(node, request.parent));
                        break;

                    // Delete GameObject
                    case Request.OBJECT + Request.DELETE:
                        DeleteObject(request.name);
                        break;

                    // Update Resource
                    case Request.RESOURCE + Request.UPDATE:
                        UpdateResource(request);
                        break;

                    // Delete Resource
                    case Request.RESOURCE + Request.DELETE:
                        DeleteResource(request);
                        break;

                    // Update Component
                    case Request.COMPONENT + Request.UPDATE:
                        UpdateComponents(request);
                        break;

                    // Delete Component
                    case Request.COMPONENT + Request.DELETE:
                        DeleteComponents(request);
                        break;
                }
            }

            // Schedule currently unfulfillable requests for next update.
            while (nextRequestQueue.Count > 0)
                requestQueue.Enqueue(nextRequestQueue.Dequeue());
        }

        /// <summary>
        /// Explicitly delete Gameobject from scene and remove it from tracked store.
        /// </summary>
        /// <param name="name"></param>
        private void DeleteObject(string name)
        {
            ObjectNode node;
            if (this.injector.objectStore.TryGetValue(name, out node))
            {
                Destroy(node.gameObject);
                this.injector.objectStore.Remove(name);
            }
        }

        /// <summary>
        /// Update content of Resource, creating it if necessary.
        /// </summary>
        /// <param name="request"></param>
        private void UpdateResource(Request request)
        {
            // Get Unity Type of Resource
            Type commonType = this.injector.serializer.ToCommonType(request.resourceType);

            // If resource is not allowed to receive update, then drop request
            if(!RuleUtility.FindResourceRule(this.injector, commonType, UpdateType.RECEIVE))
                return;

            ResourceNode node;
            if (this.injector.resourceStore.TryGet(request.name, commonType, out node))
            {
                // If Resource exists, try to apply updates. If successful, request is fulfilled.
                if (request.resource.Apply(this.injector, node.resource))
                {
                    node.UpdateHash();
                    LogUtility.Log(this.injector, LogType.INFORMATION, "Resource update sucessful");
                    return;
                }
            }
            else
            {
                // If Resource does not exist, create it 
                UnityEngine.Object resource = request.resource.Construct();
                resource.name = request.name;

                // Try to apply updates; if successful, request is fulfilled
                if (request.resource.Apply(this.injector, resource))
                {
                    node = this.injector.resourceStore.Add(request.name, resource);
                    node.UpdateHash();
                    LogUtility.Log(this.injector, LogType.INFORMATION, "Resource " + commonType + " update sucessful");
                    return;
                }
            }

            // Resource creation was not successful in this update
            
            // If this request was handled less times than allowed, then try in an later update loop
            if (request.iteration < Model.ATTEMPTS_TO_UPDATE)
            {
                request.iteration++;
                nextRequestQueue.Enqueue(request);
                LogUtility.Log(this.injector, LogType.INFORMATION, "Resource  " + commonType + " update unsucessful because of missing references");
            }
            // Else, report failed resource update
            else
                LogUtility.Log(this.injector, LogType.WARNING, "Request to update resource " + commonType + " for " + request.name + " dropped");
        }

        /// <summary>
        /// Remove Resource from tracked store. Will only be garbage-collected if no other resource uses it.
        /// </summary>
        /// <param name="request"></param>
        private void DeleteResource(Request request)
        {
            // Get Unity Type of Resource
            Type commonType = this.injector.serializer.ToCommonType(request.resourceType);

            // If resource is not allowed to receive update, then drop request
            if(!RuleUtility.FindResourceRule(this.injector, commonType, UpdateType.RECEIVE))
                return;

            // If ResourceStore contains Resource, then remove it
            if (this.injector.resourceStore.Contains(name))
                this.injector.resourceStore.Remove(name);
        }

        /// <summary>
        /// Update components in GameObject, creating them if necessary.
        /// </summary>
        /// <param name="request"></param>
        private void UpdateComponents(Request request)
        {
            // Find a node of the correct gameobject for the request to use, creating it if it doesn't exist yet.
            ObjectNode node = this.injector.objectStore.GetOrCreate(request.name);

            // Update the components
            foreach (AbstractComponent serializedComponent in request.components)
            {
                // Get component type
                Type type = this.injector.serializer.GetCommonType(serializedComponent);

                // Check if type is not null
                if (type != null)
                {
                    // If component is not allowed to receive update, then drop request
                    if(!RuleUtility.FindComponentRule(this.injector, node.gameObject, type, UpdateType.RECEIVE))
                        continue;

                    // Get or create the component
                    Component component = node.GetOrCreateComponent(type);

                    // Try to apply updates; if successful, request is fulfilled
                    if (serializedComponent.Apply(this.injector, component))
                    {
                        node.UpdateComponent(type);
                        LogUtility.Log(this.injector, LogType.INFORMATION, "Component " + type + " update sucessful");
                        continue;
                    }

                    // Component creation could not add values in this update
                    
                    // Create Request to try in a later update
                    Request newRequest = Request.UpdateComponents(this.injector, request.name, new List<AbstractComponent>() { serializedComponent });
                    newRequest.channel = request.channel; // Save channel information

                    // If this request was handled less times than allowed, then try in an later update loop
                    if (request.iteration < Model.ATTEMPTS_TO_UPDATE)
                    {
                        // Integrate the iterations of the old request
                        newRequest.iteration = request.iteration + 1;
                        nextRequestQueue.Enqueue(newRequest);
                        LogUtility.Log(this.injector, LogType.INFORMATION, "Component " + type + " update unsucessful because missing references");
                    }
                    // Else, report failed resource update
                    else
                        LogUtility.Log(this.injector, LogType.WARNING, "Request to update component " + type + " for " + request.name + " dropped");
                }
            }
        }

        /// <summary>
        /// Explicitly delete Component from Gameobject and remove it from tracked store.
        /// </summary>
        /// <param name="name">Name of containing gameobject</param>
        /// <param name="type">Type of component to remove</param>
        private void DeleteComponents(Request request)
        {
             ObjectNode node;
            if (this.injector.objectStore.TryGetValue(request.name, out node))
            {
                foreach (Type t in request.componentTypes)
                {
                    Type type = this.injector.serializer.ToCommonType(t);
                    if (type != null)
                    {
                        // If component is not allowed to receive update, then drop request
                        if(!RuleUtility.FindComponentRule(this.injector, node.gameObject, type, UpdateType.RECEIVE))
                        continue;

                        // Remove component
                        Component component = node.gameObject.GetComponent(type);
                        node.RemoveComponent(this.injector.serializer.ToCommonType(type));
                        Destroy(component);
                    }
                }
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
                if (this.injector.objectStore.TryGetValue(hierarchyUpdate.parent, out node))
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
                    hierarchyUpdate.node.Update();
                }
                else
                    // else, re-enqueue request for later
                    this.hierarchyQueue.Enqueue(hierarchyUpdate);
            }
        }
    }
}