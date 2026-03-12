using System;
using System.Threading.Tasks;

using Core;

using Unity.Entities;


#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Networking;

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
        [SerializeField]
        protected MessageType[] MessageTypes = new MessageType[]
        {
            new MessageType
            {
                Name = "session_welcome"
            },
            new MessageType
            {
                Name = "session_keepalive"
            },
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

        public async Task<T> Get<T>(string uri, string bearer) where T : class
        {
            using (var request = UnityWebRequest.Get(uri))
            {
                request.SetRequestHeader("Authorization", $"Bearer {bearer}");
                request.SetRequestHeader("Client-ID", AppID);

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Log.Info(this, $"{uri} Object loaded successfully.");

                    return JsonUtility.FromJson<T>(request.downloadHandler.text);
                }
                else
                    Log.Error(this, request.error);
            }

            return null;
        }
        public async Task<bool> Delete(string uri, string bearer)
        {
            using (var request = UnityWebRequest.Delete(uri))
            {
                request.SetRequestHeader("Authorization", $"Bearer {bearer}");
                request.SetRequestHeader("Client-ID", AppID);

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Log.Info(this, $"Delete {uri} success!");

                    return true;
                }
                else
                    Log.Error(this, request.error);
            }

            return false;
        }
        public async Task<string> Post(string uri, string bearer, object obj)
        {
            using (var request = UnityWebRequest.Post(uri, "", "application/json"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {bearer}");
                request.SetRequestHeader("Client-ID", AppID);

                var data = JsonUtility.ToJson(obj);
                var bodyRaw = System.Text.Encoding.UTF8.GetBytes(data);

                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Log.Info(this, $"{obj.GetType().FullName} has been posted.");

                    return request.downloadHandler.text;
                }
                else
                    Log.Error(this, request.error);
            }

            return "";
        }
        public async Task<string> Patch<T>(string uri, string bearer, T obj) where T : class
        {
            using (var request = new UnityWebRequest(uri, "PATCH"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {bearer}");
                request.SetRequestHeader("Client-ID", AppID);
                request.SetRequestHeader("Content-Type", "application/json");

                var data = JsonUtility.ToJson(obj);
                var bodyRaw = System.Text.Encoding.UTF8.GetBytes(data);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Log.Info(this, $"{obj.GetType().FullName} has been patched.");

                    return request.downloadHandler.text;
                }
                else
                    Log.Error(this, request.error);
            }

            return "";
        }

        public abstract void OnOpen(Platform platform);
        public abstract void OnPing(Platform platform);
        public abstract void DetermineType(ref SocketMessage message, ref Platform platform);
        public abstract void Invoke(SocketMessage message, EntityManager manager);
        public abstract SocketMessage MessageFromJson(string data);

        public abstract Task<string> Connect(Platform platform);

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

        #region MESSAGE TYPE
        [Serializable]
        public class MessageType
        {
            public uint ID;
            public string Name;
        }
        #endregion
    }

    [Serializable]
    public class SocketMessage
    {
        public string type;
        public uint id;
    }
}