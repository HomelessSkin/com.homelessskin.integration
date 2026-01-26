using System.Collections.Generic;
using System.Threading.Tasks;

using Core;

using Integration.JSON;

using UnityEngine;
using UnityEngine.Networking;

namespace Integration
{
    internal class Twitch : Platform
    {
        internal static void StartAuth(string[] scopes)
        {
            var scope = "scope=";
            if (scopes != null)
                for (int s = 0; s < scopes.Length; s++)
                    scope += $"{scopes[s]}" + (s == scopes.Length - 1 ? "" : "%20");

            Application.OpenURL($"{AuthPath}?response_type=token&client_id={AppID}&redirect_uri={RedirectPath}&{scope}");
        }

        protected static string AppID = "6ss2l29z27gl1rmz061rajdhd9mgr6";
        protected static string AuthPath = "https://id.twitch.tv/oauth2/authorize";
        protected static string EmoteURL = "https://static-cdn.jtvnw.net/emoticons/v2";

        protected Queue<SocketMessage> Responses = new Queue<SocketMessage>();

        protected virtual async Task Notification(SocketMessage message)
        {
            switch (message.metadata.subscription_type)
            {
                case "channel.chat.message":
                {
                    EnqueueMessage(new MC_Message
                    {
                        Platform = 1,
                        ID = message.payload.@event.message_id,

                        Badges = await GetBadges(message.payload.@event.badges),
                        NickColor = message.payload.@event.color,
                        Nick = message.payload.@event.chatter_user_name,
                        IsSlashMe = message.payload.@event.message.text.StartsWith("\u0001ACTION"),
                        Parts = GetParts(message.payload.@event.message.fragments),
                    });
                }
                break;
                case "channel.chat.message_delete":
                {
                    Manager.DeleteMessage(1, message.payload.@event.message_id);
                }
                break;
            }
        }

        protected List<MC_Message.Part> GetParts(Fragment[] fragments)
        {
            var parts = new List<MC_Message.Part>();
            for (int f = 0; f < fragments.Length; f++)
            {
                var fragment = fragments[f];
                var m = new MC_Message.Part();
                if (!string.IsNullOrEmpty(fragment.emote.id))
                {
                    var hash = fragment.emote.id.GetHashCode();
                    m.Emote = new MC_Message.Part.Smile { Hash = hash, Draw = true };

                    if (!Manager.HasSmile(hash, true))
                        m.Emote.URL = EmoteURL + $"/{fragment.emote.id}/static/light/2.0";
                }
                else if (fragment.text != null)
                    m.Message = new MC_Message.Part.Text { Content = fragments[f].text };

                parts.Add(m);
            }

            return parts;
        }
        protected async Task<List<MC_Message.Badge>> GetBadges(Badge[] badges)
        {
            var parts = new List<MC_Message.Badge>() { new MC_Message.Badge { Hash = 1 } };

            var refreshGlobal = false;
            var refreshSub = false;
            for (int f = 0; f < badges.Length; f++)
            {
                var badge = badges[f];
                var hash = (badge.set_id + badge.id).GetHashCode();

                var needed = !Manager.HasBadge(hash, true);
                if (badge.set_id == "subscriber")
                    refreshSub |= needed;
                else
                    refreshGlobal |= needed;

                var b = new MC_Message.Badge
                {
                    IsNeeded = needed,
                    Hash = hash,
                    SetID = badge.set_id,
                    ID = badge.id,
                };

                parts.Add(b);
            }

            if (refreshGlobal)
                await RefreshGlobalSet();
            if (refreshSub)
                await RefreshSubSet();

            return parts;

            async Task RefreshGlobalSet()
            {
                using (var request = UnityWebRequest.Get($"https://api.twitch.tv/helix/chat/badges" + "/global"))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {Token}");
                    request.SetRequestHeader("Client-ID", AppID);

                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var list = new List<Task<Texture2D>>();
                        var response = JsonUtility.FromJson<BadgesResponse>(request.downloadHandler.text);

                        for (int p = 0; p < parts.Count; p++)
                        {
                            var badge = parts[p];
                            if (badge.IsNeeded && badge.SetID != "subscriber")
                            {
                                SetBadgeURL(response, ref badge);

                                parts[p] = badge;
                            }
                        }
                    }
                    else
                        Log.Error(this, request.error);
                }
            }
            async Task RefreshSubSet()
            {
                using (var request = UnityWebRequest.Get($"https://api.twitch.tv/helix/chat/badges" +
                    $"?broadcaster_id={Data.ChannelID}"))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {Token}");
                    request.SetRequestHeader("Client-ID", AppID);

                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var list = new List<Task<Texture2D>>();
                        var response = JsonUtility.FromJson<BadgesResponse>(request.downloadHandler.text);

                        for (int p = 0; p < parts.Count; p++)
                        {
                            var badge = parts[p];
                            if (badge.IsNeeded && badge.SetID == "subscriber")
                            {
                                SetBadgeURL(response, ref badge);

                                parts[p] = badge;
                            }
                        }
                    }
                    else
                        Log.Error(this, request.error);
                }
            }

            void SetBadgeURL(BadgesResponse response, ref MC_Message.Badge badge)
            {
                for (int r = 0; r < response.data.Count; r++)
                    if (response.data[r].set_id == badge.SetID)
                    {
                        var stop = false;
                        var versions = response.data[r].versions;
                        for (int v = 0; v < versions.Count; v++)
                            if (versions[v].id == badge.ID)
                            {
                                stop = true;

                                badge.URL = versions[v].image_url_2x;

                                break;
                            }

                        if (stop)
                            break;
                    }
            }
        }
    }
}