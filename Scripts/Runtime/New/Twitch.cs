using System.Threading.Tasks;

using Core;

using Input;

using Integration.JSON;

using Unity.Entities;

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
        public override async void InvokeAsync(SocketMessage message, EntityManager manager)
        {
            switch (message.metadata.subscription_type)
            {
                case "channel.chat.message":
                Sys.Add_M(new IInteractable.Event
                {
                    Title = "Message",
                    Platform = "twitch",
                    ID = message.payload.@event.message_id,
                    Nick = message.payload.@event.chatter_user_name,
                    NickColor = message.payload.@event.color,
                    UserInput = await ExtractText(message)
                },
                manager);
                break;
                case "channel.chat.message_delete":
                Sys.Add_M(new IInteractable.Event
                {
                    Title = "DeleteMessage",
                    Platform = "twitch",
                    ID = message.payload.@event.message_id,
                },
                manager);
                break;
            }
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
        protected virtual async Task<string> ExtractText(SocketMessage message)
        {
            await Task.Delay(0);
            return "";
        }
    }
}