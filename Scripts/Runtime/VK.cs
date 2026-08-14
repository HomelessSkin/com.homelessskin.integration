using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Core;

using Input;

using Unity.Entities;

using UnityEngine;

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
            var jwt = await Get<JWT>($"{EntryPath}/v1/websocket/token", platform.Token);
            if (jwt != null)
                return jwt.data.token;

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
        public override void DetermineType(ref SocketMessage message, ref Platform platform)
        {
            var mvk = message as SocketMessage_VK;

            if (message.id != 0u)
                message.type = IDToType(message.id);
            else if (mvk.push == null || mvk.push.pub == null)
                message.type = "session_keepalive";
            else
                message.type = "notification";
        }
        public override void Invoke(SocketMessage message, EntityManager manager)
        {
            var mvk = message as SocketMessage_VK;

            var data = mvk.push.pub.data;
            switch (data.type)
            {
                case "channel_chat_message_send":
                var m = data.data.chat_message;
                if (m.author.nick == "ChatBot")
                    return;

                Sys.Add_M(new OuterInput
                {
                    Source = "vk",

                    Title = "Message",
                    ID = m.id.ToString(),

                    Agent = $"<color=#{Colors[m.author.nick_color]}>{m.author.nick}</color>",

                    Message = ExtractChatMessage(data.data.chat_message),
                    Badges = ExtractBadges(m.author),
                },
                manager);
                break;
                case "channel_chat_message_delete":
                Sys.Add_M(new OuterInput
                {
                    Source = "vk",

                    Title = "Delete Message",
                    ID = data.data.chat_message.id.ToString()
                },
                manager);
                break;
            }
        }
        public override SocketMessage MessageFromJson(string data) => JsonUtility.FromJson<SocketMessage_VK>(data);

        protected override async Task SubscribeToEvent(string type, Platform platform)
        {
            var response = await Get<ChannelsResponse>($"{EntryPath}/v1/channel?channel_url={platform.Channel.ToLower()}", platform.Token);
            if (response == null)
                return;

            var message = new SubMessage { id = type.ToUint() };

            switch (type)
            {
                case "sub_chat":
                message.subscribe = new Sub { channel = $"{response.data.channel.web_socket_channels.chat}" };
                break;
            }

            Log.Info(this, $"Sending subscription to channel: {message.subscribe.channel}");
            platform.Socket.Send(JsonUtility.ToJson(message));
        }
        protected virtual string ExtractChatMessage(Message message)
        {
            var text = "";

            for (int f = 0; f < message.parts.Count; f++)
            {
                var part = message.parts[f];

                if (part.text != null)
                    text += part.text.content;
                if (part.smile != null && !string.IsNullOrEmpty(part.smile.medium_url))
                {
                    var hash = part.smile.id.GetHashCode();
                    var index = StreamingSprites.GetSpriteIndex(hash, part.smile.medium_url);

                    text += $"<sprite name=\"{StreamingSprites.Asset}_{index}\">";
                }
            }

            return text;
        }
        protected virtual List<int> ExtractBadges(Author author)
        {
            var list = new List<int>() { 1 };

            for (int r = 0; r < author.roles.Count; r++)
            {
                var role = author.roles[r];
                var hash = role.id.GetHashCode();

                list.Add(StreamingSprites.GetSpriteIndex(hash, role.medium_url));
            }

            for (int b = 0; b < author.badges.Count; b++)
            {
                var badge = author.badges[b];
                var hash = badge.id.GetHashCode();

                list.Add(StreamingSprites.GetSpriteIndex(hash, badge.medium_url));
            }

            return list;
        }
    }

    #region JSON
    [Serializable]
    public class SocketMessage_VK : SocketMessage
    {
        public Push push;
    }
    #endregion
}