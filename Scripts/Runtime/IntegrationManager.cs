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
        [SerializeField] UnityEvent<Message> OnMessage;
        [SerializeField] UnityEvent<Event> OnEvent;
    }

    [Serializable]
    public class Message
    {

    }

    [Serializable]
    public class Event
    {

    }
}