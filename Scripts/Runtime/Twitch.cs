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
    [CreateAssetMenu(fileName = "Twitch", menuName = "Integration/Twitch Processor")]
    public class Twitch : Processor
    {
        protected static string AuthPath = "https://id.twitch.tv/oauth2/authorize";
        protected static string EventSubURL = "https://api.twitch.tv/helix/eventsub/subscriptions";
        protected static string GetUsersURL = "https://api.twitch.tv/helix/users";
        protected static string EmoteURL = "https://static-cdn.jtvnw.net/emoticons/v2";
        protected static string BadgesURL = $"https://api.twitch.tv/helix/chat/badges";

        public async override Task<string> Connect(Platform data)
        {
            using (var request = UnityWebRequest.Get(GetUsersURL + $"?login={data.Channel}"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {data.Token}");
                request.SetRequestHeader("Client-ID", AppID);

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                    return JsonUtility.FromJson<UserResponse>(request.downloadHandler.text).data[0].id;
                else
                    Log.Error(this, $"{request.error}");
            }

            return "";
        }
        public override void OnOpen(Platform data)
        {

        }
        public override void OnPing(Platform platform) { }
        public override void DetermineType(ref SocketMessage message)
        {
            switch (message.metadata.message_type)
            {
                case "session_welcome":
                case "session_keepalive":
                message.type = message.metadata.message_type;
                break;
                default:
                message.type = message.metadata.subscription_type;
                break;
            }

            base.DetermineType(ref message);
        }
        public override void Invoke(SocketMessage message, EntityManager manager)
        {
            switch (message.metadata.subscription_type)
            {
                case "channel.chat.message":
                Sys.Add_M(new OuterInput
                {
                    Title = "Message",
                    Platform = "twitch",
                    ID = message.payload.@event.message_id,
                    Nick = message.payload.@event.chatter_user_name,
                    NickColor = message.payload.@event.color,
                    UserInput = ExtractChatMessage(message.payload.@event.message.fragments),
                    Badges = ExtractBadges(message.payload.@event.badges),
                },
                manager);
                break;
                case "channel.chat.message_delete":
                Sys.Add_M(new OuterInput
                {
                    Title = "Delete Message",
                    Platform = "twitch",
                    ID = message.payload.@event.message_id,
                },
                manager);
                break;
            }
        }

        protected async override Task SubscribeToEvent(string type, Platform platform)
        {
            var data = JsonUtility.ToJson(new EventSubRequest
            {
                type = type,
                version = "1",
                condition = new Condition
                {
                    broadcaster_user_id = platform.ChannelID,
                    user_id = platform.ChannelID,
                },
                transport = new Transport
                {
                    method = "websocket",
                    session_id = $"{platform.SessionID}",
                },
            });

            using (var request = UnityWebRequest.Post(EventSubURL, "", "application/json"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {platform.Token}");
                request.SetRequestHeader("Client-Id", AppID);

                var bodyRaw = System.Text.Encoding.UTF8.GetBytes(data);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                    Log.Info(this, $"Subscribed successfully to: {type} event");
                else
                    Log.Error(this, $"Failed to create subscription to: {type} event. With error: {request.error}");
            }
        }
        protected virtual List<OuterInput.Part> ExtractChatMessage(Fragment[] fragments)
        {
            var list = new List<OuterInput.Part>();

            for (int f = 0; f < fragments.Length; f++)
            {
                var fragment = fragments[f];
                var ep = new OuterInput.Part();

                if (!string.IsNullOrEmpty(fragment.emote.id))
                    ep.Emote = new OuterInput.Part.Smile
                    {
                        Hash = fragment.emote.id.GetHashCode(),
                        URL = EmoteURL + $"/{fragment.emote.id}/static/light/2.0"
                    };
                else if (fragment.text != null)
                    ep.Message = new OuterInput.Part.Text
                    {
                        Content = fragments[f].text
                    };

                list.Add(ep);
            }

            return list;
        }
        protected virtual List<OuterInput.Badge> ExtractBadges(Badge[] badges)
        {
            var list = new List<OuterInput.Badge>()
            {
                new OuterInput.Badge
                {
                    Hash = 1
                }
            };

            for (int f = 0; f < badges.Length; f++)
            {
                var badge = badges[f];
                list.Add(new OuterInput.Badge
                {
                    Hash = (badge.set_id + badge.id).GetHashCode(),
                    SetID = badge.set_id,
                    ID = badge.id,
                });
            }

            return list;
        }
    }
}