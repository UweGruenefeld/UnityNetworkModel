/**
 * 
 * Main Script of Unity Network Model
 * 
 * Attach this script to a GameObject in Unity
 * All children of that GameObject can be synchronized with the server
 * 
 * Currently supported components are
 * - BoxCollider
 * - Camera
 * - Light
 * - LineRenderer
 * - MeshCollider
 * - MeshFilter
 * - MeshRenderer
 * - Script
 * - SphereCollider
 * - Transform
 * 
 * Currently supported ressources are
 * - Material
 * - Mesh
 * - Texture2D
 *
 * @file NetworkModel.cs
 * @author Uwe Gruenefeld, Tobias Lunte
 * @version 2020-05-04
 **/
using System.Collections;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Configuration class of the UnityNetworkModel
    /// </summary>
    // Update the name of the component in the title and component menu
    [AddComponentMenu("NetworkModel/NetworkModel Configuration")]
    public class NetworkModelConfiguration : MonoBehaviour
    {
        // Connection
        public string IP = "127.0.0.1";     // Cannot be changed during runtime
        public int PORT = 8080;             // Cannot be changed during runtime
        public float RECONNECT = 1;
        public float DELAY = 0.1f;

        // Settings
        public bool TIME = false; 
        public bool SEND = true;
        public bool RECEIVE = true;
        public bool EXISTINGOBJECTS = false;
        public bool EXISTINGRESOURCES = false;        

        // Channel
        public string SENDCHANNELS = "default";     // Cannot be changed during runtime
        public string RECEIVECHANNELS = "default";  // Cannot be changed during runtime

        // Rules Resources

        // Material
        public bool enableMaterial = true;
        public bool sendMaterial = true;
        public bool receiveMaterial = true;

        // Mesh
        public bool enableMesh = true;
        public bool sendMesh = true;
        public bool receiveMesh = true;

        // Texture2D
        public bool enableTexture2D  = true;
        public bool sendTexture2D = true;
        public bool receiveTexture2D = true;

        // Debugging
        public LogType DEBUGLEVEL = LogType.DISABLED;
        public bool DEBUGSEND = false;
        public bool DEBUGRECEIVE = false;

        // Injector
        private Injector injector;

        // Variables for coroutines
        private int numberOfCoroutines;
        private bool isCoroutineRunning;

        /// <summary>
        /// Start method from MonoBehaviour
        /// </summary>
        void Start()
        {
            // Similar to dependency injection
            this.injector = new Injector(this);

            // Storage for (game)objects and resources
            this.injector.objectStore = new ObjectStore(this.injector);
            // Add the GameObject which contains this script as root GameObject to the ObjectStore
            this.injector.objectStore.Add(Model.ROOT_NAME, this.gameObject);
            this.injector.resourceStore = new ResourceStore(this.injector);
            this.injector.resourceStore.Add("null", null);

            // Handles serialization
            this.injector.serializer = new Serializer(this.injector);

            // Handles the complete data model
            this.injector.model = Model.CreateModel(this.injector);

            // Handles the connection to the server
            this.injector.connection = new Connection(this.injector);
            this.injector.connection.OnMessage += (sender, message) => this.injector.connection.ReceiveRequest(message.Data);

            // Handles subscriptions
            this.injector.subscriptions = new Subscriptions(this.injector);
            

            // Initialize variables for coroutines
            this.numberOfCoroutines = 0;
            this.isCoroutineRunning = false;
        }

        /// <summary>
        /// Update method from MonoBehaviour
        /// </summary>
        void Update()
        {
            // Start synchronization coroutine
            if(!this.isCoroutineRunning)
                StartCoroutine(Synchronize());
        }

        /// <summary>
        /// Coroutine to synchronize NetworkModel with server
        /// </summary>
        IEnumerator Synchronize()
        {
            // Coroutine running
            this.isCoroutineRunning = true;
            this.numberOfCoroutines++;

            // Try to establish connection, only proceed if successful
            if (this.injector.connection.EnsureIsAlive())
            {
                // If subscriptions are not initialized, then initialize them
                if(!this.injector.subscriptions.isInitialized)
                    this.injector.subscriptions.Initialize();

                // Subscribe and unsubscribe from receiving updates on channel
                this.injector.subscriptions.UpdateReceivingChannel();

                // Apply change-requests that were received from the server since the last update
                if (this.RECEIVE)
                    this.injector.model.ApplyChanges();

                yield return null;

                // Subscribe and unsubscribe from sending updates on channel 
                this.injector.subscriptions.UpdateSendingChannel();

                // Send change-requests for modifications since last update to the server
                if (this.SEND)
                    this.injector.model.TrackChanges();
            };

            // Insert time between updates
            yield return new WaitForSeconds(this.DELAY);

            // Check if this is the last coroutine
            this.numberOfCoroutines--;
            this.isCoroutineRunning = this.numberOfCoroutines != 0;

            // Finish coroutine
            yield return null;
        }
    }
}
