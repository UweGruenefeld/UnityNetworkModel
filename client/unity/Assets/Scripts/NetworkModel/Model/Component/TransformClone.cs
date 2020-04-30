/**
 * 
 * Transform Clone Script of Unity Network Model
 *
 * @file TransformClone.cs
 * @author Uwe Gruenefeld
 * @version 2020-04-30
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable Transform
    /// </summary>
    [Serializable]
    internal class TransformClone : AbstractComponent
    {
        public Vector3 p, s;
        public Quaternion r;
        public string t;

        public TransformClone(Component component, ResourceStore resourceStore) : base(component)
        {
            Transform transform = (Transform)component;

            this.p = transform.localPosition;
            this.r = transform.localRotation;
            this.s = transform.localScale;
            this.t = transform.tag;
        }

        public override bool Apply(Component component, ResourceStore resourceStore)
        {
            Transform transform = (Transform)component;

            // If there was no change on client side then use server values
            if (!transform.hasChanged)
            {
                transform.localPosition = this.p;
                transform.localRotation = this.r;
                transform.localScale = this.s;
                transform.tag = this.t;

                // Avoid triggering update of changes
                transform.hasChanged = false;
            }
            return true;
        }
    }
}