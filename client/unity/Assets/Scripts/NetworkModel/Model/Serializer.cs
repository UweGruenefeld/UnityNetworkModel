/**
 * 
 * Serializer Script of Unity Network Model
 *
 * @file Serializer.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-05-05
 *
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Handles conversion between serializable and non-serializable types of Components and Resources
    /// </summary>
    internal class Serializer : AbstractInjector
    {
        // Injector for dependency injection
        internal Serializer(Injector injector) : base(injector) {}

        /// <summary>
        /// Convert a Unity Component type to a AbstractComponent type
        /// </summary>
        /// <param name="type">Type of Unity Component</param>
        /// <returns>Type of AbstractComponent</returns>
        internal Type ToSerializableType(Type type)
        {
            return Type.GetType("UnityNetworkModel." + type.Name + "Serializable");
        }

        /// <summary>
        /// Convert a NetworkModel AbstractComponent type to a Unity Component type
        /// </summary>
        /// <param name="type">Type of AbstractComponent</param>
        /// <returns>Type of Unity Component</returns>
        internal Type ToCommonType(Type type)
        {
            return Type.GetType("UnityEngine." + type.Name.Replace("Serializable", "") + ", UnityEngine");
        }

        /// <summary>
        /// Get the matching Unity Component type from a AbstractComponent; Returns Script type if Component is script
        /// </summary>
        /// <param name="component"></param>
        /// <returns>Type of Unity Component</returns>
        internal Type GetCommonType(AbstractComponent component)
        {
            Type type = component.GetType();
            if (type == typeof(ScriptSerializable))
            {
                ScriptSerializable script = (ScriptSerializable)component;
                type = Type.GetType(script.ScriptType());
            }
            else
                type = ToCommonType(type);
            
            return type;
        }

        /// <summary>
        /// Convert a Unity Component to a NetworkModel AbstractComponent
        /// </summary>
        /// <param name="component">Unity Component</param>
        /// <returns>AbstractComponent</returns>
        internal AbstractComponent ToSerializableComponent(Component component)
        {
            Type type = component.GetType();

            if (type.IsSubclassOf(typeof(MonoBehaviour)))
                type = typeof(ScriptSerializable);
            else
                type = ToSerializableType(type);

            if (type != null)
                return (AbstractComponent)Activator.CreateInstance(type, new System.Object[] { this.injector, component });
            
            return null;
        }

        /// <summary>
        /// Convert a UnityEngine Object to a NetworkModel AbstractResource
        /// </summary>
        /// <param name="resource">UnityEngine Object</param>
        /// <returns>AbstractResource</returns>
        internal AbstractResource ToSerializableResource(UnityEngine.Object resource)
        {
            Type type = ToSerializableType(resource.GetType());
            if (type != null)
                return (AbstractResource)Activator.CreateInstance(type, new System.Object[] { this.injector, resource });
            
            return null;
        }
    }
}