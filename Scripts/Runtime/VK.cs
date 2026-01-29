using System.Collections.Generic;
using System.Threading.Tasks;

using Core;

using Input;

using Integration.JSON;

using Unity.Entities;

using UnityEngine;
using UnityEngine.Networking;

namespace Integration
{
    [CreateAssetMenu(fileName = "VK", menuName = "Integration/VK Processor")]
    public class VK : Processor
    {
        protected static string EntryPath = "https://apidev.live.vkvideo.ru";
        protected static string[] Colors = new string[]
        {
            "d66e34", "b8aaff", "1d90ff", "9961f9", "59a840", "e73629", "de6489", "20bba1",
            "f8b301", "0099bb", "7bbeff", "e542ff", "a36c59", "8ba259", "00a9ff", "a20bff"
        };

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
                message.type = "notification";

            base.DetermineType(ref message);
        }
        public override void Invoke(SocketMessage message, EntityManager manager)
        {
            var data = message.push.pub.data;
            switch (data.type)
            {
                case "channel_chat_message_send":
                var m = data.data.chat_message;
                if (m.author.nick == "ChatBot")
                    return;

                Sys.Add_M(new OuterInput
                {
                    Platform = "vk",

                    Title = "Message",
                    ID = m.id.ToString(),

                    Nick = m.author.nick,
                    NickColor = $"#{Colors[m.author.nick_color]}",

                    Badges = ExtractBadges(m.author),
                    UserInput = ExtractChatMessage(data.data.chat_message),
                },
                manager);
                break;
                case "channel_chat_message_delete":
                Sys.Add_M(new OuterInput
                {
                    Platform = "vk",

                    Title = "Delete Message",
                    ID = data.data.chat_message.id.ToString()
                },
                manager);
                break;
            }
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
                        id = type.ToUint(),
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
        protected virtual List<OuterInput.Part> ExtractChatMessage(Message message)
        {
            var list = new List<OuterInput.Part>();
            var tasks = new List<Task>();

            for (int p = 0; p < message.parts.Count; p++)
            {
                var part = message.parts[p];
                var ep = new OuterInput.Part { };

                if (part.text != null)
                    ep.Message = new OuterInput.Part.Text
                    {
                        Content = part.text.content
                    };
                if (part.smile != null && !string.IsNullOrEmpty(part.smile.medium_url))
                    ep.Emote = new OuterInput.Part.Smile
                    {
                        Hash = part.smile.id.GetHashCode(),
                        URL = part.smile.medium_url
                    };
                if (part.mention != null)
                    ep.Reply = new OuterInput.Part.Mention
                    {
                        Nick = part.mention.nick
                    };

                list.Add(ep);
            }

            return list;
        }
        protected virtual List<OuterInput.Badge> ExtractBadges(Author author)
        {
            var list = new List<OuterInput.Badge>()
            {
                new OuterInput.Badge
                {
                    Hash = 0
                }
            };

            for (int r = 0; r < author.roles.Count; r++)
            {
                var role = author.roles[r];

                list.Add(new OuterInput.Badge
                {
                    Hash = role.id.GetHashCode(),
                    URL = role.medium_url,
                });
            }

            for (int b = 0; b < author.badges.Count; b++)
            {
                var badge = author.badges[b];

                list.Add(new OuterInput.Badge
                {
                    Hash = badge.id.GetHashCode(),
                    URL = badge.medium_url,
                });
            }

            return list;
        }
    }
}