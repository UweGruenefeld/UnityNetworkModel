/**
 * 
 * Object Node Script of Unity Network Model
 *
 * @file ObjectNode.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-04-30
 **/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Stores specific GameObject, tracking its last known parent and components
    /// </summary>
    internal class ObjectNode
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
        public ObjectNode(GameObject gameObject, Serializer serializer)
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
        public void UpdateHash(AbstractComponent comp)
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
                    component = gameObject.AddComponent(type);

            return component;
        }
    }
}