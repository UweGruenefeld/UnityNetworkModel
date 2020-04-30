/**
 * 
 * Sphere Collider Clone Script of Unity Network Model
 *
 * @file SphereColliderClone.cs
 * @author Tobias Lunte, Uwe Gruenefeld
 * @version 2020-04-30
 **/
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Serializable SphereCollider
    /// </summary>
    internal class SphereColliderClone : AbstractComponent
    {
        public Vector3 c;
        public float r;

        public SphereColliderClone(Component component, ResourceStore resourceStore) : base(component)
        {
            SphereCollider spherecollider = (SphereCollider)component;

            this.c = spherecollider.center;
            this.r = spherecollider.radius;
        }

        public override bool Apply(Component component, ResourceStore resourceStore)
        {
            SphereCollider spherecollider = (SphereCollider)component;

            spherecollider.center = this.c;
            spherecollider.radius = this.r;

            return true;
        }
    }
}