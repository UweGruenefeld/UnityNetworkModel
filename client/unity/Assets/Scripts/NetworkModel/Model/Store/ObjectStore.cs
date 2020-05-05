/**
 * 
 * Object Store Script of Unity Network Model
 *
 * @file ObjectStore.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-04-30
 *
 **/
using System.Collections.Generic;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Collects all known GameObjects and tracks the last known state of their parent and components.
    /// </summary>
    internal class ObjectStore : Dictionary<string, ObjectNode>
    {
        private Injector injector;

        /// <summary>
        /// Create a new ObjectStore
        /// </summary>
        /// <param name="injector"></param>
        internal ObjectStore(Injector injector)
        {
            this.injector = injector;
        }

        /// <summary>
        /// Adds GameObject to store
        /// </summary>
        /// <param name="gameobject"></param>
        internal void Add(GameObject gameObject)
        {
            this.Add(gameObject.name, gameObject);
        }

        /// <summary>
        /// Adds GameObject to store with specific name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="gameObject"></param>
        internal void Add(string name, GameObject gameObject)
        {
            this.Add(name, new ObjectNode(this.injector, gameObject));
        }

        /// <summary>
        /// Return a GameObject of the correct name by priority
        /// 1) A matching tracked GameObject from the store.
        /// 2) A matching GameObject from tracked part of the scene (if using existing components is enabled).
        /// 3) A new empty GameObject of the correct name.
        /// </summary>
        /// <param name="name">Name of the gameObject</param>
        /// <returns>GameObject of matching name</returns>
        internal ObjectNode GetOrCreate(string name)
        {
            ObjectNode node;

            // Check if name of Gameobject is not existing
            if (!this.TryGetValue(name, out node))
            {
                // Create a new GameObject
                GameObject gameObject = null;

                // Should exisitng objects be preferred
                if (this.injector.configuration.EXISTINGOBJECTS)
                {
                    // Try to find transform of specified GameObject
                    Transform transform = FindTransform(this.injector.configuration.transform, name);
                    if (transform != null)
                        gameObject = transform.gameObject;
                }

                // If no GameObject was found
                if (gameObject == null)
                {
                    // Create new GameOBject
                    gameObject = new GameObject();
                    gameObject.SetActive(false);
                    gameObject.name = name;

                    // Attach new GameObject to root GameObject that contains the NetworkModel Configuration
                    gameObject.transform.parent = this[Model.ROOT_NAME].gameObject.transform;
                }

                // Create new ObjectNode for GameObject
                node = new ObjectNode(this.injector, gameObject);

                // Add it to the ObjectStore
                this.Add(name, node);
            }

            return node;
        }

        /// <summary>
        /// Performs a depth-first search for a Transform of the correct Name among all descendants of parent transform.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <returns>Transform of matching name</returns>
        private Transform FindTransform(Transform parent, string name)
        {
            // Is the name of the parent equal to the name of the searched GameObject
            if (parent.name.Equals(name))
                return parent;
            
            // Search through all children
            foreach (Transform child in parent)
            {
                // Recrusive find transform
                Transform result = FindTransform(child, name);
                if (result != null)
                    return result;
            }

            // No Transform found
            return null;
        }

        /// <summary>
        /// Determine name that a gameObject should be stored under in the store. Will return current storage name if gameObject is already present or new free name if not. 
        /// GameObject's name should be set to returned name if it is to be stored.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns>Name the GameObject should be stored under</returns>
        internal string GetReferenceName(GameObject gameObject)
        {
            // Check if object is null
            if (gameObject == null)
                return "null";

            // Find the name of the Object
            string referenceName = gameObject.name;
            while (this.ContainsKey(referenceName) && this[referenceName].gameObject.GetInstanceID() != gameObject.GetInstanceID())
                referenceName = referenceName + "_" + gameObject.GetInstanceID().ToString();
            
            // Return the Object name
            return referenceName;
        }
    }
}