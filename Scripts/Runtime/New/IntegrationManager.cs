using UnityEngine;

namespace Integration2
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

        async void StartAdapter(Adapter adapter)
        {
            adapter.Load();
            await adapter.Connect();
        }
    }
}