using System.Threading.Tasks;

using Core;

using Integration.JSON;

using UnityEngine;
using UnityEngine.Networking;

namespace Integration2
{
    [CreateAssetMenu(fileName = "VK", menuName = "Integration/VK Processor")]
    public class VK : Processor
    {
        protected static string EntryPath = "https://apidev.live.vkvideo.ru";

        public override async Task<string> Connect(Platform platform)
        {
            using (var request = UnityWebRequest.Get(EntryPath + "/v1/websocket/token"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {platform.Token}");

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                    return JsonUtility.FromJson<JWT>(request.downloadHandler.text).data.token;
                else
                    Log.Error(this, $"{request.error}");
            }

            return "";
        }
        public override void OnOpen(Platform platform)
        {
            platform.Socket.Send(JsonUtility.ToJson(new ConnectMessage
            {
                id = 792420933u,
                connect = new Connect
                {
                    token = platform.ChannelID
                }
            }));
        }
        public override void OnPing(Platform platform) => platform.Socket.Send("{}");
        public override void DetermineType(ref SocketMessage message)
        {
            if (message.id != 0u)
                message.type = IDToType(message.id);
            else if (message.push == null || message.push.pub == null)
                message.type = "session_keepalive";
            else
                message.type = message.push.pub.data.type;
        }

        protected override async void SubscribeToEvent(string type, Platform platform)
        {
            using (var request = UnityWebRequest.Get(EntryPath +
                $"/v1/channel" +
                $"?channel_url={platform.Channel.ToLower()}"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {platform.Token}");
                request.downloadHandler = new DownloadHandlerBuffer();

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var channel = JsonUtility.FromJson<ChannelsResponse>(request.downloadHandler.text).data.channel;

                    var message = new SubMessage
                    {
                        id = "sub".ToUint(),
                    };

                    switch (type)
                    {
                        case "chat":
                        message.subscribe = new Sub { channel = $"{channel.web_socket_channels.chat}" };
                        break;
                    }

                    Log.Info(this, $"Sending subscription to channel: {message.subscribe.channel}");
                    platform.Socket.Send(JsonUtility.ToJson(message));
                }
                else
                    Log.Error(this, $"{request.error}");
            }
        }
    }
}