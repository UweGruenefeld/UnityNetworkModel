/**
 * 
 * Object Store Script of Unity Network Model
 *
 * @file ObjectStore.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-04-30
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
        private NetworkModel config;
        private Bundle bundle;

        public ObjectStore(NetworkModel config, Bundle bundle)
        {
            this.config = config;
            this.bundle = bundle;
        }

        /// <summary>
        /// Adds GameObject to store
        /// </summary>
        /// <param name="go"></param>
        public void Add(GameObject gameObject)
        {
            this.Add(gameObject.name, gameObject);
        }

        /// <summary>
        /// Adds GameObject to store with specific name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="go"></param>
        public void Add(string name, GameObject gameObject)
        {
            this.Add(name, new ObjectNode(gameObject, this.bundle.serializer));
        }

        /// <summary>
        /// Return a GameObject of the correct name by priority
        /// 1) A matching tracked GameObject from the store.
        /// 2) A matching GameObject from tracked part of the scene (if using existing components is enabled).
        /// 3) A new empty GameObject of the correct name.
        /// </summary>
        /// <param name="name">Name of the gameObject</param>
        /// <returns>GameObject of matching name</returns>
        public ObjectNode GetOrCreate(string name)
        {
            ObjectNode node;

            // Name of gameobject is not existing
            if (!this.TryGetValue(name, out node))
            {
                GameObject gameObject = null;
                if (config.EXISTINGCOMPENTS)
                {
                    Transform tmp = FindTransform(config.transform, name);
                    if (tmp != null)
                    {
                        gameObject = tmp.gameObject;
                    }
                }
                if (gameObject == null)
                {
                    gameObject = new GameObject();
                    gameObject.SetActive(false);
                    gameObject.name = name;
                    gameObject.transform.parent = this["root"].gameObject.transform;
                }
                node = new ObjectNode(gameObject, this.bundle.serializer);
                Add(name, node);
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
        /// Determine name that a gameObject should be stored under in the store. Will return current storage name if gameObject is already present or new free name if not. 
        /// GameObject's name should be set to returned name if it is to be stored.
        /// </summary>
        /// <param name="go"></param>
        /// <returns>Name the GameObject should be stored under</returns>
        public string GetReferenceName(GameObject gameObject)
        {
            string refName = gameObject.name;
            while (this.ContainsKey(refName) && this[refName].gameObject.GetInstanceID() != gameObject.GetInstanceID())
            {
                refName = refName + "_" + gameObject.GetInstanceID().ToString();
            }
            return refName;
        }
    }
}