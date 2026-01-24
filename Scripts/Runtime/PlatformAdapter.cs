using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Core;

using Integration.JSON;

using UnityEngine;

using WebSocketSharp;

namespace Integration
{
    public abstract class PlatformAdapter : MonoBehaviour, IStorage
    {
        [Space]
        [SerializeField] string AppID;
        [SerializeField] string RedirectURL;
        [SerializeField] string[] Scopes;

        [Space]
        [SerializeField] string PersistentPath;

        public int _MaxSaveFiles => 1;
        public string _DataFile => "*.json";
        public string _ResourcesPath => "";
        public string _PersistentPath => PersistentPath;

        PlatformData Data;
        WebSocket Socket;

        protected Queue<SocketMessage> SocketMessages = new Queue<SocketMessage>();

        internal abstract Task Refresh();

        internal bool GetMessage(out SocketMessage message) => SocketMessages.TryDequeue(out message);
        internal void Disconnect()
        {
            if (Socket != null && Socket.IsAlive)
                Socket.Close();
        }

        protected abstract void Connect();
        protected abstract void OnOpen(object sender, EventArgs e);
        protected abstract void OnMessage(object sender, MessageEventArgs e);
        protected abstract Task<bool> SubscribeToEvent(string type);

        protected void InitializeSocket(string url)
        {
            if (Socket != null)
                Socket.Close();

            Socket = new WebSocket(url);
            Socket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            Socket.SslConfiguration.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            Socket.OnOpen += OnOpen;
            Socket.OnMessage += OnMessage;
            Socket.OnClose += OnClose;
            Socket.OnError += OnError;

            Socket.Connect();
        }
        protected void OnClose(object sender, CloseEventArgs e) => Log.Error(this.GetType().FullName, $"{e.Reason} {e.Code}");
        protected void OnError(object sender, ErrorEventArgs e) => Log.Error(this.GetType().FullName, $"{e.Message}");
        protected bool VerifyToken()
        {
            //if (string.IsNullOrEmpty(Token))
            //{
            //    Log.Warning(this.GetType().FullName, $"Platform {Data.Name} doesn't have User Token!");

            //    return false;
            //}

            return true;
        }

        protected async void EnqueueMessage(SocketMessage message)
        {
            await Task.Delay(0);
            //if (message.Parts != null)
            //    for (int p = 0; p < message.Parts.Count; p++)
            //    {
            //        var part = message.Parts[p];

            //        if (!string.IsNullOrEmpty(part.Emote.URL))
            //        {
            //            var smile = await Web.DownloadSpriteTexture(part.Emote.URL);
            //            if (smile)
            //                Manager.DrawSmile(smile, part.Emote.Hash);
            //            else
            //                part.Emote.Hash = 0;
            //        }

            //        message.Parts[p] = part;
            //    }

            //if (message.Badges != null)
            //    for (int b = 0; b < message.Badges.Count; b++)
            //    {
            //        var part = message.Badges[b];

            //        if (!string.IsNullOrEmpty(part.URL))
            //        {
            //            var badge = await Web.DownloadSpriteTexture(part.URL);
            //            if (badge)
            //                Manager.DrawBadge(badge, part.Hash);
            //        }
            //    }

            //SocketMessages.Enqueue(message);
        }

        #region DATA
        [Serializable]
        public class PlatformData : IStorage.Data
        {
            public bool Enabled;

            public string ChannelID;
            public string Channel;

            public string SessionID;
        }
        #endregion
    }
}