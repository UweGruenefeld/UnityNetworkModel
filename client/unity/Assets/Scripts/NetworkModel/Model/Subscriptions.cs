/**
 * 
 * Subscriptions Script of Unity Network Model
 *
 * @file Subscriptions.cs
 * @author Uwe Gruenefeld
 * @version 2020-05-05
 **/
using System.Collections.Generic;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Class to manage subscriptions for sending and receiving updates
    /// </summary>
    internal class Subscriptions : AbstractInjector
    {
        private string sendChannels;
        private string receiveChannels;

        public ISet<string> sendChannelList;
        public ISet<string> receiveChannelList;

        public bool isInitialized;

        /// <summary>
        /// Constructor for new Subscriptions class
        /// </summary>
        /// <param name="injector"></param>
        // Injector for dependency injection
        internal Subscriptions(Injector injector) : base(injector) 
        {
            this.sendChannels = "";
            this.receiveChannels = "";

            this.sendChannelList = null;
            this.receiveChannelList = null;

            this.isInitialized = false;
        }

        /// <summary>
        /// Method to initialize subscriptions (afterwards channels cannot be changed)
        /// </summary>
        internal void Initialize()
        {
            this.sendChannels = this.injector.configuration.SENDCHANNELS;
            this.receiveChannels = this.injector.configuration.RECEIVECHANNELS;

            this.EnableSend();
            this.EnableReceive();

            this.isInitialized = true;

            LogUtility.Log(this.injector, LogType.INFORMATION, "Successful subscribed to channels");
        }

        /// <summary>
        /// Enable/Disable sending updates
        /// </summary>
        internal void UpdateSendingChannel()
        {
            // If sending updates is enabled and send channels contains none, then add all channel
            if(this.injector.configuration.SEND && this.sendChannelList.Count <= 0)
                this.EnableSend();
            
            // If sending updates is disabled and send channels contains some, then remove all channel
            if(!this.injector.configuration.SEND && this.sendChannelList.Count > 0)
                this.DisableSend();
        }

        /// <summary>
        /// Enable/Disable receiving updates
        /// </summary>
        internal void UpdateReceivingChannel()
        {
            // If receiving updates is enabled and receive channels is subscribed to none, then subscribe to all channel
            if(this.injector.configuration.RECEIVE && this.receiveChannelList.Count <= 0)
                this.EnableReceive();
            
            // If receiving updates is disabled and receive channels is subscribed to some, then unsubscribe from all channel
            if(!this.injector.configuration.RECEIVE && this.receiveChannelList.Count > 0)
                this.DisableReceive();
        }

        /// <summary>
        /// Enable sending updates
        /// </summary>
        private void EnableSend()
        {
            this.sendChannelList = new HashSet<string>(this.sendChannels.Split(','));
        }

        /// <summary>
        /// Disable sending updates
        /// </summary>
        private void DisableSend()
        {
            this.sendChannelList = new HashSet<string>();
        }

        /// <summary>
        /// Enable receiving updates
        /// </summary>
        private void EnableReceive()
        {
            this.receiveChannelList = new HashSet<string>(this.receiveChannels.Split(','));

            foreach (string channel in this.receiveChannelList)
                this.Subscribe(channel);
        }

        /// <summary>
        /// Disable receiving updates
        /// </summary>
        private void DisableReceive()
        {
            foreach (string channel in this.receiveChannelList)
                this.Unsubscribe(channel);
            this.receiveChannelList = new HashSet<string>();
        }

        /// <summary>
        /// Subscribe to channel on server
        /// </summary>
        /// <param name="channel"></param>
        private void Subscribe(string channel)
        {
            if (channel != "")
                this.injector.connection.SendAsync("{\"type\":\"cs\", \"channel\":\"" + channel + "\"}", null);
        }

        /// <summary>
        /// Unsubscribe from channel on server
        /// </summary>
        /// <param name="channel"></param>
        private void Unsubscribe(string channel)
        {
            if (channel != "")
                this.injector.connection.SendAsync("{\"type\":\"cl\", \"channel\":\"" + channel + "\"}", null);
        }
    }
}