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
                    Log.Error(this.GetType().FullName, $"{request.error}");
            }

            return "";
        }
        public override void OnOpen(Platform data)
        {

        }
    }
}