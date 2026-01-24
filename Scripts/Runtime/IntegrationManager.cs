using System;

using UnityEngine;
using UnityEngine.Events;

namespace Integration
{
    public class IntegrationManager : MonoBehaviour
    {
        [SerializeField] float RefreshPeriod = 2f;

        float T;

        [Space]
        [SerializeField] UnityEvent<SocketEvent> OnEvent;

        [Space]
        [SerializeField] PlatformAdapter TwitchAdapter;
        [SerializeField] PlatformAdapter VKAdapter;
    }

    [Serializable]
    public class SocketEvent
    {
        public string Nick;
        public string Text;
    }
}