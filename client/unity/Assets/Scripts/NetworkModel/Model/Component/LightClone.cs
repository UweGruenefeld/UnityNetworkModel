/**
 * 
 * Light Clone Script of Unity Network Model
 *
 * @file LightClone.cs
 * @author Uwe Gruenefeld
 * @version 2020-04-30
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable Light
    /// </summary>
    [Serializable]
    internal class LightClone : AbstractComponent
    {
        public LightType t;
        public Color c;
        public float i, b;

        public LightClone(Component component, ResourceStore resourceStore) : base(component)
        {
            Light light = (Light)component;

            this.t = light.type;
            this.c = light.color;
            this.i = light.intensity;
            this.b = light.bounceIntensity;
        }

        public override bool Apply(Component component, ResourceStore resourceStore)
        {
            Light light = (Light)component;

            light.type = this.t;
            light.color = this.c;
            light.intensity = this.i;
            light.bounceIntensity = this.b;

            return true;
        }
    }
}