using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Core;

using UnityEngine;

using WebSocketSharp;

namespace Integration
{
    public abstract class Platform
    {
        protected static string RedirectPath = "https://oauth.vk.com/blank.html";

        public PlatformData Data;

        internal bool Enabled
        {
            get => Data.Enabled;
            set
            {
                Data.Enabled = value;
            }
        }
        internal bool IsWorking;
        internal string Type { get => Data.Type; }
        internal string Channel { get => Data.Channel; }

        protected string Token = "";

        protected WebSocket Socket;
        protected MultiChatManager Manager;

        protected Queue<MC_Message> MC_Messages = new Queue<MC_Message>();

        protected abstract Task<bool> SubscribeToEvent(string type);
        protected abstract Task ProcessSocketMessages();

        internal Task Refresh() => ProcessSocketMessages();
        internal bool GetMessage(out MC_Message message) => MC_Messages.TryDequeue(out message);

        protected async void EnqueueMessage(MC_Message message)
        {
            if (message.Parts != null)
                for (int p = 0; p < message.Parts.Count; p++)
                {
                    var part = message.Parts[p];

                    if (!string.IsNullOrEmpty(part.Emote.URL))
                    {
                        var smile = await Web.DownloadSpriteTexture(part.Emote.URL);
                        if (smile)
                            Manager.DrawSmile(smile, part.Emote.Hash);
                        else
                            part.Emote.Hash = 0;
                    }

                    message.Parts[p] = part;
                }

            if (message.Badges != null)
                for (int b = 0; b < message.Badges.Count; b++)
                {
                    var part = message.Badges[b];

                    if (!string.IsNullOrEmpty(part.URL))
                    {
                        var badge = await Web.DownloadSpriteTexture(part.URL);
                        if (badge)
                            Manager.DrawBadge(badge, part.Hash);
                    }
                }

            MC_Messages.Enqueue(message);
        }
        protected bool VerifyToken()
        {
            if (string.IsNullOrEmpty(Token))
            {
                Log.Warning(this.GetType().FullName, $"Platform {Data.Name} doesn't have User Token!");

                return false;
            }

            return true;
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

    #region MESSAGE
    public struct MC_Message
    {
        public byte Platform;
        public string ID;

        public List<Badge> Badges;
        public string NickColor;
        public string Nick;
        public bool IsSlashMe;
        public List<Part> Parts;

        public struct Badge
        {
            public bool IsNeeded;
            public int Hash;
            public string SetID;
            public string ID;
            public string URL;
        }

        public struct Part
        {
            public Mention Reply;
            public Smile Emote;
            public Text Message;

            public struct Mention
            {
                public string Nick;
            }

            public struct Smile
            {
                public bool Draw;
                public int Hash;
                public string URL;
            }

            public struct Text
            {
                public string Content;
            }
        }
    }
    #endregion
}