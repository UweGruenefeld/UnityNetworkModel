/**
 * 
 * Box Collider Clone Script of Unity Network Model
 *
 * @file BoxColliderClone.cs
 * @author Uwe Gruenefeld
 * @version 2020-04-30
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable BoxCollider
    /// </summary>
    [Serializable]
    internal class BoxColliderClone : AbstractComponent
    {
        public Vector3 c, s;

        public BoxColliderClone(Component component, ResourceStore resourceStore) : base(component)
        {
            BoxCollider boxcollider = (BoxCollider)component;

            this.c = boxcollider.center;
            this.s = boxcollider.size;
        }

        public override bool Apply(Component component, ResourceStore resourceStore)
        {
            BoxCollider boxcollider = (BoxCollider)component;

            boxcollider.center = this.c;
            boxcollider.size = this.s;

            return true;
        }
    }
}