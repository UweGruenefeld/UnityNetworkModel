/**
 * 
 * Object Node Script of Unity Network Model
 *
 * @file ObjectNode.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-05-04
 **/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Stores specific GameObject, tracking its last known parent and components
    /// </summary>
    internal class ObjectNode : AbstractInjector
    {
        public GameObject gameObject;
        public int parentID;
        public Dictionary<Type, long> hashes;

        /// <summary>
        /// Wraps a GameObject into a Node
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="gameObject"></param>
        internal ObjectNode(Injector injector, GameObject gameObject) : base(injector)
        {
            this.gameObject = gameObject;
            this.hashes = new Dictionary<Type, long>();
        }

        /// <summary>
        /// Update ObjectNode with current values from represented GameObject
        /// </summary>
        internal void Update()
        {
            // Update stored parent with current parent ID
            this.parentID = gameObject.transform.parent.GetInstanceID();

            // Add all Components to Dictonary containing hashes
            foreach (Component component in gameObject.GetComponents(typeof(Component)))
            {
                // If Component does not exist in Dictonary, then add it
                if (!this.hashes.ContainsKey(component.GetType()))
                    this.hashes.Add(component.GetType(), 0);
            }
        }

        /// <summary>
        /// Update specific component hash to current state
        /// </summary>
        /// <param name="type">Type of component to update</param>
        internal void UpdateComponent(Type type)
        {
            Component component = gameObject.GetComponent(type);

            if (component != null)
                this.hashes[type] = this.injector.serializer.ToSerializableComponent(component).GetHash();
        }

        /// <summary>
        /// Remove tracking for component of specific Type
        /// </summary>
        /// <param name="type"></param>
        internal void RemoveComponent(Type type)
        {
            this.hashes.Remove(type);
        }



/*
        /// <summary>
        /// Add missing hashes to component Dictionary as empty == unknown
        /// </summary>
        internal void PopulateHashes()
        {
            foreach (Component component in gameObject.GetComponents(typeof(Component)))
            {
                if (HierarchyUtility.FindRule(this.injector, this.gameObject, component.GetType(), UpdateType.SEND) && !hashes.ContainsKey(component.GetType()))
                {
                    hashes.Add(component.GetType(), 0);
                }
            }
        }

        /// <summary>
        /// Update stored parent to current state
        /// </summary>
        internal void UpdateParent()
        {
            this.parent = gameObject.transform.parent.GetInstanceID();
        }

        /// <summary>
        /// Update specific component hash to current state
        /// </summary>
        /// <param name="type">Type of component to update</param>
        internal void UpdateHash(Type type)
        {
            if (HierarchyUtility.FindRule(this.injector, this.gameObject, type, UpdateType.SEND))
            {
                Component component = gameObject.GetComponent(type);
                if (component != null)
                {
                    hashes[type] = this.injector.serializer.ToSerializableComponent(component).GetHash();
                }
            }
        }

        /// <summary>
        /// Update specific component hash to current state
        /// UpdateHash(Type type) should always be preferred, if possible.
        /// </summary>
        /// <param name="component">serialized component to update</param>
        internal void UpdateHash(AbstractComponent component)
        {
            hashes[this.injector.serializer.GetCommonType(component)] = component.GetHash();
        }


        */

        /// <summary>
        /// Returns matching Component by priority
        /// 1) Existing matching component from the gameObject
        /// 2) New matching component added to the gameObject
        /// </summary>
        /// <param name="type"></param>
        /// <returns>Component of matching Type</returns>
        internal Component GetOrCreateComponent(Type type)
        {
            // Get component of specified type
            Component component = gameObject.GetComponent(type);

            // If no component was found, then add component
            if (component == null)
                    component = gameObject.AddComponent(type);

            // Return the component
            return component;
        }
    }
}