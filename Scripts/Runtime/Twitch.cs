using System;
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
        protected static string EventSubURL = "https://api.twitch.tv/helix/eventsub/subscriptions";
        protected static string GetUsersURL = "https://api.twitch.tv/helix/users";
        protected static string EmoteURL = "https://static-cdn.jtvnw.net/emoticons/v2";
        protected static string BadgesURL = $"https://api.twitch.tv/helix/chat/badges";
        protected static string ModerationURL = $"https://api.twitch.tv/helix/moderation/chat";
        protected static string BanURL = $"https://api.twitch.tv/helix/moderation/bans";

        BadgesResponse GlobalBadges;
        BadgesResponse UserBadges;

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
        public override async void OnOpen(Platform platform)
        {
            GlobalBadges = await RefreshBadgeSet("/global", platform.Token);
            UserBadges = await RefreshBadgeSet($"?broadcaster_id={platform.ChannelID}", platform.Token);
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
                message.type = message.metadata.message_type;
                break;
            }

            base.DetermineType(ref message);
        }
        public override void Invoke(SocketMessage message, EntityManager manager)
        {
            switch (message.metadata.subscription_type)
            {
                case "channel.ban":
                Sys.Add_M(new OuterInput
                {
                    Title = "Ban",
                    Platform = "twitch",
                    Nick = message.payload.@event.user_name,
                },
                manager);
                break;
                case "channel.chat.message":
                Sys.Add_M(new OuterInput
                {
                    Title = "Message",
                    Platform = "twitch",
                    ID = message.payload.@event.message_id,
                    UserID = message.payload.@event.chatter_user_id,
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
        public override async void RequestDeleteMessage(OuterInput input, Platform platform)
        {
            using (var request = UnityWebRequest.Delete($"{ModerationURL}" +
                $"?broadcaster_id={platform.ChannelID}" +
                $"&moderator_id={platform.ChannelID}" +
                $"&message_id={input.ID}"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {platform.Token}");
                request.SetRequestHeader("Client-ID", AppID);

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                    Log.Info(this, $"Delete message {input.ID} success!");
                else
                    Log.Error(this, request.error);
            }
        }
        public override async void RequestTimeout(OuterInput input, Platform platform)
        {
            using (var request = UnityWebRequest.Post($"{BanURL}" +
                $"?broadcaster_id={platform.ChannelID}" +
                $"&moderator_id={platform.ChannelID}",
                "",
                "application/json"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {platform.Token}");
                request.SetRequestHeader("Client-ID", AppID);

                var data = JsonUtility.ToJson(new Timeout
                {
                    data = new TimeoutData
                    {
                        user_id = input.UserID,
                        duration = 600
                    }
                });
                var bodyRaw = System.Text.Encoding.UTF8.GetBytes(data);

                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                    Log.Info(this, $"Timeout User {input.UserID} success!");
                else
                    Log.Error(this, request.error);
            }
        }
        public override async void RequestBan(OuterInput input, Platform platform)
        {
            using (var request = UnityWebRequest.Post($"{BanURL}" +
                $"?broadcaster_id={platform.ChannelID}" +
                $"&moderator_id={platform.ChannelID}",
                "",
                "application/json"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {platform.Token}");
                request.SetRequestHeader("Client-ID", AppID);

                var data = JsonUtility.ToJson(new Ban
                {
                    data = new BanData
                    {
                        user_id = input.UserID,
                    }
                });
                var bodyRaw = System.Text.Encoding.UTF8.GetBytes(data);

                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                    Log.Info(this, $"Ban User {input.UserID} success!");
                else
                    Log.Error(this, request.error);
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
        protected virtual List<OuterInput.Icon> ExtractBadges(JSON.Badge[] badges)
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
                    Index = StreamingSprites.GetSpriteIndex(hash, GetBadgeURI(badge.set_id, badge.id)),
                });
            }

            return list;
        }

        string GetBadgeURI(string set_id, string id)
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
        async Task<BadgesResponse> RefreshBadgeSet(string url, string token)
        {
            using (var request = UnityWebRequest.Get($"{BadgesURL}{url}"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {token}");
                request.SetRequestHeader("Client-ID", AppID);

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Log.Info(this, $"{url} Badge Set loaded successfully.");

                    return JsonUtility.FromJson<BadgesResponse>(request.downloadHandler.text);
                }
                else
                    Log.Error(this, request.error);
            }

            return null;
        }

        #region TIMEOUT
        [Serializable]
        class Timeout
        {
            public TimeoutData data;

        }
        [Serializable]
        class TimeoutData
        {
            public string user_id;
            public int duration;
        }
        [Serializable]
        class Ban
        {
            public BanData data;
        }
        [Serializable]
        class BanData
        {
            public string user_id;
        }
        #endregion
    }
}