using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Core;

using Integration.JSON;

using UnityEngine;

using WebSocketSharp;

namespace Integration2
{
    public abstract class Adapter : IStorage
    {
        public int _MaxSaveFiles => 1;
        public string _DataFile => "*.json";
        public string _ResourcesPath => "";
        public string _PersistentPath => Processor.PersistentPath;

        Processor Processor;
        Platform Platform;

        protected Queue<SocketMessage> SocketMessages = new Queue<SocketMessage>();

        public Adapter(Processor processor, string name, string channel, string token)
        {
            Processor = processor;
            Platform = new Platform
            {
                Name = name,

                Enabled = true,
                ProcessorID = processor.ID,
                Token = token,
                Channel = channel,
            };
        }
        public Adapter(Processor processor, Platform platform)
        {
            Processor = processor;
            Platform = platform;
        }

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
        //protected bool VerifyToken()
        //{
        //    //if (string.IsNullOrEmpty(Token))
        //    //{
        //    //    Log.Warning(this.GetType().FullName, $"Platform {Data.Name} doesn't have User Token!");

        //    //    return false;
        //    //}

        //    return true;
        //}

        public async Task Connect()
        {
            Platform.ChannelID = await Processor.Connect(Platform);
            if (!string.IsNullOrEmpty(Platform.ChannelID))
                InitializeSocket(Processor.SocketURL);
        }
        public void Disconnect()
        {
            if (Platform.Socket != null &&
                 Platform.Socket.IsAlive)
                Platform.Socket.Close();
        }
        protected abstract Task<bool> SubscribeToEvent(string type);
        public bool GetMessage(out SocketMessage message) => SocketMessages.TryDequeue(out message);

        void InitializeSocket(string url)
        {
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
        void OnOpen(object sender, EventArgs e)
        {
            Processor.OnOpen(Platform);
        }
        void OnMessage(object sender, MessageEventArgs e)
        {
            var message = JsonUtility.FromJson<SocketMessage>(e.Data);
            message.platform = Processor.ID;

            SocketMessages.Enqueue(message);
        }
        void OnClose(object sender, CloseEventArgs e) => Log.Error(this.GetType().FullName, $"{e.Reason} {e.Code}");
        void OnError(object sender, ErrorEventArgs e) => Log.Error(this.GetType().FullName, $"{e.Message}");
    }

    #region PLATFORM
    [Serializable]
    public class Platform : IStorage.Data
    {
        [NonSerialized] public string ChannelID;
        [NonSerialized] public string SessionID;
        [NonSerialized] public WebSocket Socket;

        public int ProcessorID;
        public bool Enabled;
        public string Token;
        public string Channel;
    }
    #endregion
}