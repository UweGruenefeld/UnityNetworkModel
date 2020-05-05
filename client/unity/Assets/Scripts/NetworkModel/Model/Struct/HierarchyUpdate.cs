/**
 * 
 * Hierarchy Update Struct of Unity Network Model
 *
 * @file HierarchyUpdate.cs
 * @author Uwe Gruenefeld
 * @version 2020-05-04
 *
 **/
namespace UnityNetworkModel
{
    struct HierarchyUpdate
    {
        public ObjectNode node;
        public string parent;

        /// <summary>
        /// Describes a changed parent for a GameObject
        /// </summary>
        /// <param name="node">ObjectNode</param>
        /// <param name="parent">The name of the new parent GameObject</param>
        public HierarchyUpdate(ObjectNode node, string parent)
        {
            this.node = node;
            this.parent = parent;
        }
    }
}