using System.Threading.Tasks;

using Core;

using Integration.JSON;

using UnityEngine;
using UnityEngine.Networking;

namespace Integration2
{
    [CreateAssetMenu(fileName = "Twitch", menuName = "Integration/Twitch Processor")]
    public class Twitch : Processor
    {
        protected static string EventSubURL = "https://api.twitch.tv/helix/eventsub/subscriptions";
        protected static string GetUsersURL = "https://api.twitch.tv/helix/users";

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
            message.type = message.metadata.message_type;
        }

        protected async override void SubscribeToEvent(string type, Platform platform)
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
    }
}