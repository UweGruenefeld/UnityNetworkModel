/**
 * 
 * Injector Script of Unity Network Model
 *
 * @file Injector.cs
 * @author Uwe Gruenefeld
 * @version 2020-05-03
 **/
namespace UnityNetworkModel
{
    /// <summary>
    /// Injector that handles Dependencies for Network Model Configuration
    /// </summary>
    internal class Injector
    {
        // Configuration script visible in Inspector
        public NetworkModelConfiguration configuration;

        // Store for Objects representations
        public ObjectStore objectStore;
        // Store for Resource representations
        public ResourceStore resourceStore;

        // Model of the current Instance
        public Model model;

        // Serializer allows to serialize and deserialize Unity components and resources
        public Serializer serializer;

        // Handles subscriptions on send and receive channels
        public Subscriptions subscriptions;
        
        // Handles connection to server
        public Connection connection;

        /// <summary>
        /// Constructs an Injektor containing references to interfaces
        /// </summary>
        /// <param name="configuration"></param>
        internal Injector(NetworkModelConfiguration configuration)
        {
            this.configuration = configuration;
        }
    }
}