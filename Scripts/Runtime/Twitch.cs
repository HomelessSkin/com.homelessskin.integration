using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Core;

using Input;

using Unity.Entities;

using UnityEngine;
using UnityEngine.Networking;

namespace Integration
{
    [CreateAssetMenu(fileName = "Twitch", menuName = "Integration/Twitch Processor")]
    public class Twitch : Processor
    {
        protected static string EventSubURL = "https://api.twitch.tv/helix/eventsub/subscriptions";
        protected static string GetUsersURL = "https://api.twitch.tv/helix/users";
        protected static string EmoteURL = "https://static-cdn.jtvnw.net/emoticons/v2";
        protected static string BadgesURL = $"https://api.twitch.tv/helix/chat/badges";

        BadgesResponse GlobalBadges;
        BadgesResponse UserBadges;

        public override void OnPing(Platform platform) { }
        public override void DetermineType(ref SocketMessage message, ref Platform platform)
        {
            var mtw = message as SocketMessage_TW;
            switch (mtw.metadata.message_type)
            {
                case "session_keepalive":
                message.type = mtw.metadata.message_type;
                break;
                case "session_welcome":
                platform.SessionID = mtw.payload.session.id;
                goto case "session_keepalive";
                default:
                message.type = mtw.metadata.message_type;
                break;
            }
        }
        public override void Invoke(SocketMessage message, EntityManager manager)
        {
            var mtw = message as SocketMessage_TW;
            switch (mtw.metadata.subscription_type)
            {
                case "channel.ban":
                Sys.Add_M(new OuterInput
                {
                    Title = "Ban",
                    Platform = "twitch",
                    Nick = mtw.payload.@event.user_name,
                },
                manager);
                break;
                case "channel.chat.message":
                Sys.Add_M(new OuterInput
                {
                    Title = "Message",
                    Platform = "twitch",
                    ID = mtw.payload.@event.message_id,
                    UserID = mtw.payload.@event.chatter_user_id,
                    Nick = mtw.payload.@event.chatter_user_name,
                    NickColor = mtw.payload.@event.color,
                    UserInput = ExtractChatMessage(mtw.payload.@event.message.fragments),
                    Badges = ExtractBadges(mtw.payload.@event.badges),
                },
                manager);
                break;
                case "channel.chat.message_delete":
                Sys.Add_M(new OuterInput
                {
                    Title = "Delete Message",
                    Platform = "twitch",
                    ID = mtw.payload.@event.message_id,
                },
                manager);
                break;
            }
        }
        public override SocketMessage MessageFromJson(string data) => JsonUtility.FromJson<SocketMessage_TW>(data);

        public override async void OnOpen(Platform platform)
        {
            GlobalBadges = await Get<BadgesResponse>($"{BadgesURL}/global", platform.Token);
            UserBadges = await Get<BadgesResponse>($"{BadgesURL}?broadcaster_id={platform.ChannelID}", platform.Token);
        }
        public override async Task<string> Connect(Platform platform)
        {
            var response = await Get<UserResponse>($"{GetUsersURL}?login={platform.Channel}", platform.Token);
            if (response != null)
                return response.data[0].id;

            return "";
        }

        protected override async Task SubscribeToEvent(string type, Platform platform)
        {
            await Post(EventSubURL, platform.Token, new EventSubRequest
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
        }

        protected virtual List<OuterInput.Part> ExtractChatMessage(Fragment[] fragments)
        {
            var list = new List<OuterInput.Part>();

            for (int f = 0; f < fragments.Length; f++)
            {
                var fragment = fragments[f];
                var ep = new OuterInput.Part();

                if (!string.IsNullOrEmpty(fragment.emote.id))
                {
                    var hash = fragment.emote.id.GetHashCode();
                    ep.Emote = new OuterInput.Icon
                    {
                        Hash = hash,
                        Index = StreamingSprites.GetSpriteIndex(hash, EmoteURL + $"/{fragment.emote.id}/static/light/2.0")
                    };
                }
                else if (fragment.text != null)
                    ep.Message = new OuterInput.Part.Text
                    {
                        Content = fragments[f].text
                    };

                list.Add(ep);
            }

            return list;
        }
        protected virtual List<OuterInput.Icon> ExtractBadges(Badge[] badges)
        {
            var list = new List<OuterInput.Icon>()
            {
                new OuterInput.Icon
                {
                    Index = 1
                }
            };

            for (int f = 0; f < badges.Length; f++)
            {
                var badge = badges[f];
                var hash = (badge.set_id + badge.id).GetHashCode();
                list.Add(new OuterInput.Icon
                {
                    Hash = hash,
                    Index = StreamingSprites.GetSpriteIndex(hash, GetBadgeURL(badge.set_id, badge.id)),
                });
            }

            return list;
        }

        string GetBadgeURL(string set_id, string id)
        {
            if (set_id == "subscriber")
            {
                for (int r = 0; r < UserBadges.data.Count; r++)
                    if (UserBadges.data[r].set_id == set_id)
                    {
                        var versions = UserBadges.data[r].versions;
                        for (int v = 0; v < versions.Count; v++)
                            if (versions[v].id == id)
                                return versions[v].image_url_2x;
                    }
            }
            else
            {
                for (int r = 0; r < GlobalBadges.data.Count; r++)
                    if (GlobalBadges.data[r].set_id == set_id)
                    {
                        var versions = GlobalBadges.data[r].versions;
                        for (int v = 0; v < versions.Count; v++)
                            if (versions[v].id == id)
                                return versions[v].image_url_2x;
                    }
            }

            return "";
        }
    }

    #region JSON
    public class SocketMessage_TW : SocketMessage
    {
        public Metadata metadata;
        public Payload payload;
    }

    [Serializable]
    public class Metadata
    {
        public string message_type;
        public string subscription_type;
    }

    [Serializable]
    public class Payload
    {
        public Session session;
        public SocketEvent @event;
    }

    [Serializable]
    public class SocketEvent
    {
        public string id;
        public string message_id;
        public string color;
        public string chatter_user_id;
        public string chatter_user_name;
        public string user_id;
        public string user_name;
        public string user_input;
        public Message message;
        public Cheer cheer;
        public Badge[] badges;
    }
    #endregion
}