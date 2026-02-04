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

        void StartAdapter(Adapter adapter)
        {
            adapter.Load();
            adapter.Connect();
        }
    }
}