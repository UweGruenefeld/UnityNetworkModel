using System;
using System.Collections.Generic;

namespace UnityEngine
{
    /// <summary>
    /// Collects all known GameObjects and tracks the last known state of their parent and components.
    /// </summary>
    internal class GOStore : Dictionary<string, GOStore.GONode>
    {
        private Serializer serializer;
        private NetworkModel config;

        public GOStore(Serializer serializer, NetworkModel config)
        {
            this.serializer = serializer;
            this.config = config;
        }

        /// <summary>
        /// Adds GO to store
        /// </summary>
        /// <param name="go"></param>
        public void Add(GameObject go)
        {
            this.Add(go.name, go);
        }

        /// <summary>
        /// Adds GO to store with specific name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="go"></param>
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
                    Transform tmp = FindTransform(config.transform, name);
                    if (tmp != null)
                    {
                        go = tmp.gameObject;
                    }
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
        /// Stores specific GameObject, tracking its last known parent and components
        /// </summary>
        public class GONode
        {
            public GameObject gameObject;
            private Serializer serializer;
            public int parent;
            public Dictionary<Type, long> hashes;

            /// <summary>
            /// Wraps a GameObject into a Node
            /// </summary>
            /// <param name="gameObject"></param>
            /// <param name="serializer"></param>
            public GONode(GameObject gameObject, Serializer serializer)
            {
                this.gameObject = gameObject;
                this.serializer = serializer;
                this.hashes = new Dictionary<Type, long>();
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
                        hashes.Add(component.GetType(), 0);
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
}