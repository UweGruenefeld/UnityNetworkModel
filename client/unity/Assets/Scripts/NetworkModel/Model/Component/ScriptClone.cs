/**
 * 
 * Script Clone Script of Unity Network Model
 *
 * @file ScriptClone.cs
 * @author Uwe Gruenefeld
 * @version 2020-04-30
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable Script
    /// </summary>
    [Serializable]
    internal class ScriptClone : AbstractComponent
    {
        public string t;
        public string v;

        public ScriptClone(Component component, ResourceStore resourceStore) : base(component)
        {
            this.t = component.GetType().AssemblyQualifiedName;
            this.v = JsonUtility.ToJson(component);
        }

        public override bool Apply(Component component, ResourceStore resourceStore)
        {
            JsonUtility.FromJsonOverwrite(this.v, component);
            return true;
        }

        public string ScriptType()
        {
            return this.t;
        }
    }
}