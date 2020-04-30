/**
 * 
 * Serializer Script of Unity Network Model
 *
 * @file Serializer.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-04-30
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Handles conversion between serializable and non-serializable types of Components and Resources
    /// </summary>
    internal class Serializer
    {
        private NetworkModel config;
        private Bundle bundle;

        public Serializer(NetworkModel config, Bundle bundle)
        {
            this.config = config;
            this.bundle = bundle;
        }

        /// <summary>
        /// Convert a Unity Component type to a UnityNetworkModel.AbstractComponent type
        /// </summary>
        /// <param name="type">Type of Unity Component</param>
        /// <returns>Type of UnityNetworkModel.AbstractComponent</returns>
        internal Type ToSerializableType(Type type)
        {
            return Type.GetType("UnityNetworkModel." + type.Name + "Clone");
        }

        /// <summary>
        /// Convert a NetworkModel UnityNetworkModel.AbstractComponent type to a Unity Component type
        /// </summary>
        /// <param name="type">Type of UnityNetworkModel.Component</param>
        /// <returns>Type of Unity Component</returns>
        internal Type ToCommonType(Type type)
        {
            return Type.GetType("UnityEngine." + type.Name.Replace("Clone", "") + ", UnityEngine");
        }

        /// <summary>
        /// Get the matching Unity Component type from a UnityNetworkModel.Component; Returns Script type if Component is script
        /// </summary>
        /// <param name="comp"></param>
        /// <returns>Type of Unity Component</returns>
        internal Type GetCommonType(AbstractComponent comp)
        {
            Type type = comp.GetType();
            if (type == typeof(ScriptClone))
            {
                ScriptClone script = (ScriptClone)comp;
                type = Type.GetType(script.ScriptType());
            }
            else
            {
                type = ToCommonType(type);
            }
            return type;
        }

        /// <summary>
        /// Convert a Unity Component to a NetworkModel UnityNetworkModel.Component
        /// </summary>
        /// <param name="component">Unity Component</param>
        /// <returns>UnityNetworkModel.AbstractComponent</returns>
        internal AbstractComponent ToSerializableComponent(Component component)
        {
            Type type = component.GetType();

            if (type.IsSubclassOf(typeof(MonoBehaviour)))
            {
                type = typeof(ScriptClone);
            }
            else
            {
                type = ToSerializableType(type);
            }

            if (type != null)
            {
                return (AbstractComponent)Activator.CreateInstance(type, new System.Object[] { component, this.bundle.resourceStore });
            }
            return null;
        }

        /// <summary>
        /// Convert a Unity Object to a NetworkModel UnityNetworkModel.AbstractResource
        /// </summary>
        /// <param name="asset">Unity Object</param>
        /// <returns>UnityNetworkModel.AbstractResource</returns>
        internal AbstractResource ToSerializableResource(UnityEngine.Object resource)
        {
            Type type = ToSerializableType(resource.GetType());
            if (type != null)
            {
                return (AbstractResource)Activator.CreateInstance(type, new System.Object[] { resource, this.bundle.resourceStore });
            }
            return null;
        }

        /// <summary>
        /// Checks whether Type of Unity Component has a matching NUnityNetworkModel.AbstractComponent and if the matching public bool is set to true.
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
        /// Utility function to create order-dependent hash from two hashes. Collisions unlikely if original hashes are already well-distibuted.
        /// </summary>
        /// <param name="h1"></param>
        /// <param name="h2"></param>
        /// <returns></returns>
        internal static long CombineHashes(long h1, long h2)
        {
            return h1 * 31 + h2;
        }
    }
}