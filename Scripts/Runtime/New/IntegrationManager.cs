using Core;

using Integration.JSON;

using UnityEngine;
using UnityEngine.Events;

namespace Integration2
{
    public class IntegrationManager : MonoBehaviour
    {
        [Space]
        [SerializeField] Adapter VKAdapter;

        [Space]
        [SerializeField] Adapter TwitchAdapter;

        [Space]
        [SerializeField] UnityEvent<SocketMessage>[] Events;

        void Start()
        {
            StartAdapter(VKAdapter);
            StartAdapter(TwitchAdapter);
        }
        void Update()
        {
            UpdateAdapter(VKAdapter);
            UpdateAdapter(TwitchAdapter);
        }

        async void StartAdapter(Adapter adapter)
        {
            adapter.Load();
            await adapter.Connect();
        }
        void UpdateAdapter(Adapter adapter)
        {
            while (adapter.TryGetMessage(out var message))
            {
                adapter.DetermineType(ref message);

                Log.Info(adapter, $"{message.type} message received.");

                switch (message.type)
                {
                    case "session_keepalive":
                    adapter.OnPing();
                    break;
                    case "session_welcome":
                    adapter.SubscribeToEvents(message.payload.session);
                    break;
                    default:
                    break;
                }
            }
        }
    }
}