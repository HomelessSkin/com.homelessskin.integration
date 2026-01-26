using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Core;

using Integration.JSON;

using UI;

using UnityEngine;

using WebSocketSharp;

namespace Integration2
{
    [Serializable]
    public class Adapter : Storage
    {
        [Space]
        [SerializeField] Processor Processor;

        Platform Platform;

        protected Queue<SocketMessage> SocketMessages = new Queue<SocketMessage>();

        #region old
        //protected async void EnqueueMessage(SocketMessage message)
        //{
        //    await Task.Delay(0);
        //    //if (message.Parts != null)
        //    //    for (int p = 0; p < message.Parts.Count; p++)
        //    //    {
        //    //        var part = message.Parts[p];

        //    //        if (!string.IsNullOrEmpty(part.Emote.URL))
        //    //        {
        //    //            var smile = await Web.DownloadSpriteTexture(part.Emote.URL);
        //    //            if (smile)
        //    //                Manager.DrawSmile(smile, part.Emote.Hash);
        //    //            else
        //    //                part.Emote.Hash = 0;
        //    //        }

        //    //        message.Parts[p] = part;
        //    //    }

        //    //if (message.Badges != null)
        //    //    for (int b = 0; b < message.Badges.Count; b++)
        //    //    {
        //    //        var part = message.Badges[b];

        //    //        if (!string.IsNullOrEmpty(part.URL))
        //    //        {
        //    //            var badge = await Web.DownloadSpriteTexture(part.URL);
        //    //            if (badge)
        //    //                Manager.DrawBadge(badge, part.Hash);
        //    //        }
        //    //    }

        //    //SocketMessages.Enqueue(message);
        //}
        #endregion

        public void Load()
        {
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
        public void OnPing() => Processor.OnPing(Platform);
        public void SubscribeToEvents(Session session)
        {
            if (session != null)
                Platform.SessionID = session.id;

            Processor.SubscribeToEvents(Platform);
        }
        public void DetermineType(ref SocketMessage message) => Processor.DetermineType(ref message);
        public bool TryGetMessage(out SocketMessage message) => SocketMessages.TryDequeue(out message);

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