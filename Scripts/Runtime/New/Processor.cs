using System;
using System.Threading.Tasks;

using Core;

using Integration.JSON;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace Integration2
{
    public abstract class Processor : ScriptableObject
    {
        [SerializeField] protected string AppID;
        [SerializeField] protected string SocketURL;
        [SerializeField] protected string RedirectURL = "https://oauth.vk.com/blank.html";

        [Space]
        [SerializeField] protected string[] Scopes;

        [Space]
        [SerializeField] protected string[] Events;

        [Space]
        [SerializeField]
        protected MessageType[] MessageTypes = new MessageType[]
        {
            new MessageType { Name = "session_welcome" },
            new MessageType { Name = "session_keepalive" },
        };

        public string GetSocketURL() => SocketURL;
        public void SubscribeToEvents(Platform platform)
        {
            for (int e = 0; e < Events.Length; e++)
                SubscribeToEvent(Events[e], platform);
        }

        public abstract Task<string> Connect(Platform platform);
        public abstract void OnOpen(Platform platform);
        public abstract void OnPing(Platform platform);
        public abstract void DetermineType(ref SocketMessage message);

        protected abstract void SubscribeToEvent(string type, Platform platform);

        protected string IDToType(uint id)
        {
            for (int t = 0; t < MessageTypes.Length; t++)
                if (MessageTypes[t].ID == id)
                    return MessageTypes[t].Name;

            return "";
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (MessageTypes != null)
                for (int t = 0; t < MessageTypes.Length; t++)
                    MessageTypes[t].ID = MessageTypes[t].Name.ToUint();

            EditorUtility.SetDirty(this);
        }
#endif

        [Serializable]
        public class MessageType
        {
            public string Name;
            public uint ID;
        }
    }
}