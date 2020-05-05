/**
 * 
 * Network Model Rule of Unity Network Model
 *
 * @file NetworkModelRule.cs
 * @author Uwe Gruenefeld
 * @version 2020-05-04
 *
 **/
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Rule class of the UnityNetworkModel
    /// </summary>
    // Update the name of the component in the title and component menu
    [AddComponentMenu("NetworkModel/NetworkModel Rule")] 
    public class NetworkModelRule : MonoBehaviour
    {
        // Scope
        public bool applyToObject = false;
        public bool applyToChildren = true;

        // Settings
        public int decimalPlaces = 5;

        // Rules Components

        // BoxCollider
        public bool enableBoxCollider;
        public bool sendBoxCollider;
        public bool receiveBoxCollider;

        // Camera
        public bool enableCamera;
        public bool sendCamera;
        public bool receiveCamera;

        // Light
        public bool enableLight;
        public bool sendLight;
        public bool receiveLight;

        // LineRenderer
        public bool enableLineRenderer;
        public bool sendLineRenderer;
        public bool receiveLineRenderer;

        // MeshCollider
        public bool enableMeshCollider;
        public bool sendMeshCollider;
        public bool receiveMeshCollider;

        // MeshFilter
        public bool enableMeshFilter;
        public bool sendMeshFilter;
        public bool receiveMeshFilter;

        // MeshRenderer
        public bool enableMeshRenderer;
        public bool sendMeshRenderer;
        public bool receiveMeshRenderer;

        // Script
        public bool enableScript;
        public bool sendScript;
        public bool receiveScript;

        // SphereCollider
        public bool enableSphereCollider;
        public bool sendSphereCollider;
        public bool receiveSphereCollider;

        // Transform
        public bool enableTransform;
        public bool sendTransform;
        public bool receiveTransform;
    }
}