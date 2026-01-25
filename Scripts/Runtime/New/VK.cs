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

        public override async Task<string> Connect(Platform data)
        {
            using (var request = UnityWebRequest.Get(EntryPath + "/v1/websocket/token"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {data.Token}");

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                    return JsonUtility.FromJson<JWT>(request.downloadHandler.text).data.token;
                else
                    Log.Error(this.GetType().FullName, $"{request.error}");
            }

            return "";
        }
        public override void OnOpen(Platform data)
        {
            data.Socket.Send(JsonUtility.ToJson(new ConnectMessage
            {
                id = (uint)MessageType.Connection,
                connect = new Connect
                {
                    token = data.ChannelID
                }
            }));
        }

        protected enum MessageType : uint
        {
            Connection = 1u,
            ChatSub = 2u,
        }
    }
}