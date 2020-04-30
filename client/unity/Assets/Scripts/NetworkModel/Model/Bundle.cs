/**
 * 
 * Bundle Script of Unity Network Model
 *
 * @file Bundle.cs
 * @author Uwe Gruenefeld
 * @version 2020-04-30
 **/
namespace UnityNetworkModel
{
    internal class Bundle
    {
        public Serializer serializer;
        public ObjectStore objectStore;
        public ResourceStore resourceStore;
        public Connection connection;
        public Model model;


        // Store old receiveChannels to check for changes
        public string OLDRECEIVECHANNELS = "";

        // Time since last update loop
        public float time = 0;
    }
}