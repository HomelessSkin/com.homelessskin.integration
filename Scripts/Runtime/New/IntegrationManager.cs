using System;

using Integration.JSON;

using UnityEngine;
using UnityEngine.Events;

namespace Integration2
{
    public class IntegrationManager : MonoBehaviour
    {
        [SerializeField] float RefreshPeriod = 2f;

        float T;

        [Space]
        [SerializeField] UnityEvent<SocketMessage>[] Events;

        [Space]
        [SerializeField] Processor[] Processors;

        Adapter[] Adapters;
    }
}