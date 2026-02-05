using Input;

using UnityEngine;

namespace Integration
{
    public class IntegrationManager : MonoBehaviour
    {
        [Space]
        [SerializeField] Adapter VKAdapter;

        [Space]
        [SerializeField] Adapter TwitchAdapter;

        void Start()
        {
            StartAdapter(VKAdapter);
            StartAdapter(TwitchAdapter);
        }
        void Update()
        {
            VKAdapter.Invoke();
            TwitchAdapter.Invoke();
        }
        void OnDestroy()
        {
            VKAdapter.Disconnect();
            TwitchAdapter.Disconnect();
        }

        public void StartAuth(int type)
        {
            switch (type)
            {
                case 0:
                VKAdapter.StartAuth();
                break;
                case 1:
                TwitchAdapter.StartAuth();
                break;
            }
        }
        public void Submit(int type)
        {
            switch (type)
            {
                case 0:
                VKAdapter.Reset();
                break;
                case 1:
                TwitchAdapter.Reset();
                break;
            }
        }
        public void Connect(int type)
        {
            switch (type)
            {
                case 0:
                VKAdapter.Connect();
                break;
                case 1:
                TwitchAdapter.Connect();
                break;
            }
        }

        public void DeleteMessageButton(OuterInput input, Command command = null)
        {
            switch (input.Platform)
            {
                case "vk":
                VKAdapter.RequestDeleteMessage(input);
                break;
                case "twitch":
                TwitchAdapter.RequestDeleteMessage(input);
                break;
            }
        }
        public void TimeoutButton(OuterInput input, Command command = null)
        {
            switch (input.Platform)
            {
                case "vk":
                VKAdapter.RequestTimeout(input);
                break;
                case "twitch":
                TwitchAdapter.RequestTimeout(input);
                break;
            }
        }
        public void BanButton(OuterInput input, Command command = null)
        {
            switch (input.Platform)
            {
                case "vk":
                VKAdapter.RequestBan(input);
                break;
                case "twitch":
                TwitchAdapter.RequestBan(input);
                break;
            }
        }

        void StartAdapter(Adapter adapter)
        {
            adapter.Load();
            adapter.Connect();
        }
    }
}