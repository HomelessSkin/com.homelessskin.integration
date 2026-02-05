using System;
using System.Threading.Tasks;

using Core;

using Input;

using Integration.JSON;

using Unity.Entities;


#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace Integration
{
    public abstract class Processor : ScriptableObject
    {
        [Header("Authorization")]
        [Space]
        [SerializeField] protected string AppID;
        [SerializeField] protected string AuthURL;
        [SerializeField] protected string SocketURL;
        [SerializeField] protected string RedirectURL = "https://oauth.vk.com/blank.html";
        [SerializeField] protected string[] Scopes;

        [Header("Events")]
        [Space]
        [SerializeField] int SubscriptionDelay = 60;
        [SerializeField] protected string[] Events;

        [Header("Socket Messages")]
        [Space]
        [SerializeField] bool LogMessageTypes = true;
        [SerializeField]
        protected MessageType[] MessageTypes = new MessageType[]
        {
            new MessageType { Name = "session_welcome" },
            new MessageType { Name = "session_keepalive" },
        };

        public void StartAuth()
        {
            var scope = "scope=";
            if (Scopes != null)
                for (int s = 0; s < Scopes.Length; s++)
                    scope += $"{Scopes[s]}" + (s == Scopes.Length - 1 ? "" : "%20");

            Application.OpenURL($"{AuthURL}?response_type=token&client_id={AppID}&redirect_uri={RedirectURL}&{scope}");
        }
        public string GetSocketURL() => SocketURL;
        public async void SubscribeToEvents(Platform platform)
        {
            for (int e = 0; e < Events.Length; e++)
            {
                await SubscribeToEvent(Events[e], platform);
                await Task.Delay(SubscriptionDelay);
            }
        }
        public virtual void DetermineType(ref SocketMessage message)
        {
            if (LogMessageTypes)
                Log.Info(this, $"{message.type} message received.");
        }

        public abstract Task<string> Connect(Platform platform);
        public abstract void OnOpen(Platform platform);
        public abstract void OnPing(Platform platform);
        public abstract void Invoke(SocketMessage message, EntityManager manager);
        public abstract void RequestDeleteMessage(OuterInput input, Platform platform);
        public abstract void RequestTimeout(OuterInput input, Platform platform);
        public abstract void RequestBan(OuterInput input, Platform platform);

        protected abstract Task SubscribeToEvent(string type, Platform platform);

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
            public uint ID;
            public string Name;
        }
    }
}