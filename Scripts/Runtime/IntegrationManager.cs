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

        async void StartAdapter(Adapter adapter)
        {
            adapter.Load();
            await adapter.Connect();
        }
    }
}