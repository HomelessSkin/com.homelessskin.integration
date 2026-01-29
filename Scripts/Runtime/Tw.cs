using System.Collections.Generic;
using System.Threading.Tasks;

using Core;

using Input;

using Integration.JSON;

using UnityEngine;
using UnityEngine.Networking;

namespace Integration
{
    internal class Tw
    {
        protected static string AppID = "6ss2l29z27gl1rmz061rajdhd9mgr6";
        protected static string AuthPath = "https://id.twitch.tv/oauth2/authorize";
        protected static string BadgesURL = $"https://api.twitch.tv/helix/chat/badges";
        protected static string Token;
        protected static string ChannelID;

        async Task RefreshGlobalSet()
        {
            using (var request = UnityWebRequest.Get(BadgesURL + "/global"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {Token}");
                request.SetRequestHeader("Client-ID", AppID);

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var list = new List<Task<Texture2D>>();
                    var response = JsonUtility.FromJson<BadgesResponse>(request.downloadHandler.text);

                    //for (int p = 0; p < parts.Count; p++)
                    //{
                    //    var badge = parts[p];
                    //    if (badge.SetID != "subscriber")
                    //    {
                    //        SetBadgeURL(response, ref badge);

                    //        parts[p] = badge;
                    //    }
                    //}
                }
                else
                    Log.Error(this, request.error);
            }
        }
        async Task RefreshSubSet()
        {
            using (var request = UnityWebRequest.Get(BadgesURL + $"?broadcaster_id={ChannelID}"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {Token}");
                request.SetRequestHeader("Client-ID", AppID);

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var list = new List<Task<Texture2D>>();
                    var response = JsonUtility.FromJson<BadgesResponse>(request.downloadHandler.text);

                    //for (int p = 0; p < parts.Count; p++)
                    //{
                    //    var badge = parts[p];
                    //    if (badge.SetID == "subscriber")
                    //    {
                    //        SetBadgeURL(response, ref badge);

                    //        parts[p] = badge;
                    //    }
                    //}
                }
                else
                    Log.Error(this, request.error);
            }
        }

        void SetBadgeURL(BadgesResponse response, ref OuterInput.Badge badge)
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