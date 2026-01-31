using System;
using System.Threading.Tasks;

using Core;

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
        public void StartAuth()
        {
            //var scope = "scope=";
            //if (scopes != null)
            //    for (int s = 0; s < Scopes.Length; s++)
            //        scope += $"{Scopes[s]}" + (s == scopes.Length - 1 ? "" : "%20");

            //Application.OpenURL($"{AuthPath}?response_type=token&client_id={AppID}&redirect_uri={RedirectPath}&{scope}");
        }

        [SerializeField] bool LogMessageTypes = true;
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
        public async void SubscribeToEvents(Platform platform)
        {
            for (int e = 0; e < Events.Length; e++)
            {
                SubscribeToEvent(Events[e], platform);

                await Task.Delay(30);
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
            public uint ID;
            public string Name;
        }
    }
}