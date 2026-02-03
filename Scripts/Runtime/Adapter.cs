using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using Core;

using Integration.JSON;

using UI;

using Unity.Entities;

using UnityEngine;

using WebSocketSharp;

namespace Integration
{
    [Serializable]
    public class Adapter : Storage
    {
        [Space]
        [SerializeField] Processor Processor;

        [Space]
        [SerializeField] Indicator Indicator;

        protected EntityManager EntityManager;
        protected Platform Platform;

        protected ConcurrentQueue<SocketMessage> SocketMessages = new ConcurrentQueue<SocketMessage>();

        public void Load()
        {
            EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var platform = Load(Processor.name);
            if (string.IsNullOrEmpty(platform))
                return;

            Platform = JsonUtility.FromJson<Platform>(platform);
        }
        public void Reset(string name, string channel, string token)
        {
            Platform = new Platform
            {
                Name = name,

                Enabled = true,
                Token = token,
                Channel = channel,
            };
        }
        public async Task Connect()
        {
            if (!VerifyToken())
                return;

            Platform.ChannelID = await Processor.Connect(Platform);
            if (!string.IsNullOrEmpty(Platform.ChannelID))
                InitializeSocket(Processor.GetSocketURL());
        }
        public void IsAlive()
        {
            if (Platform != null &&
                 Platform.Socket != null)
                Log.Warning(this, $"{Platform.Name}'s Connection is: {Platform.Socket.IsAlive}");
        }
        public void Disconnect()
        {
            if (Platform.Socket != null &&
                 Platform.Socket.IsAlive)
                Platform.Socket.Close();
        }
        public void Invoke()
        {
            while (SocketMessages.TryDequeue(out var message))
            {
                Indicator?.Refresh();
                Processor.DetermineType(ref message);

                switch (message.type)
                {
                    case "session_keepalive":
                    Processor.OnPing(Platform);
                    break;
                    case "session_welcome":
                    if (message.payload.session != null)
                        Platform.SessionID = message.payload.session.id;

                    Processor.SubscribeToEvents(Platform);
                    break;
                    case "notification":
                    Processor.Invoke(message, EntityManager);
                    break;
                }
            }
        }

        void InitializeSocket(string url)
        {
            Log.Info(this, $"{Platform.Name} Socket Initialization.");

            if (Platform.Socket != null)
                Platform.Socket.Close();

            Platform.Socket = new WebSocket(url);
            Platform.Socket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            Platform.Socket.SslConfiguration.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            Platform.Socket.OnOpen += OnOpen;
            Platform.Socket.OnMessage += OnMessage;
            Platform.Socket.OnClose += OnClose;
            Platform.Socket.OnError += OnError;

            Platform.Socket.Connect();
        }
        void OnOpen(object sender, EventArgs e) => Processor.OnOpen(Platform);
        void OnMessage(object sender, MessageEventArgs e) => SocketMessages.Enqueue(JsonUtility.FromJson<SocketMessage>(e.Data));
        void OnClose(object sender, CloseEventArgs e) => Log.Warning(this, $"{e.Reason} {e.Code}");
        void OnError(object sender, ErrorEventArgs e) => Log.Error(this, $"{e.Message}");

        bool VerifyToken()
        {
            if (string.IsNullOrEmpty(Platform.Token))
            {
                Log.Warning(this, $"Platform {Processor.name} doesn't have a User Token!");

                return false;
            }

            return true;
        }
    }

    #region PLATFORM
    [Serializable]
    public class Platform : IStorage.Data
    {
        [NonSerialized] public string ChannelID;
        [NonSerialized] public string SessionID;
        [NonSerialized] public WebSocket Socket;

        public bool Enabled;
        public string Token;
        public string Channel;
    }
    #endregion
}