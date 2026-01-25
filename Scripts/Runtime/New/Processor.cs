using System.Threading.Tasks;

using Core;

using UnityEngine;

namespace Integration2
{
    public abstract class Processor : KeyScriptable
    {
        public string AppID;
        public string SocketURL = "wss://pubsub-dev.live.vkvideo.ru/connection/websocket?format=json&cf_protocol_version=v2";
        public string RedirectURL = "https://oauth.vk.com/blank.html";

        [Space]
        public string[] Scopes;

        [Space]
        public string PersistentPath = "Unknown";

        public abstract Task<string> Connect(Platform data);
        public abstract void OnOpen(Platform data);
    }
}