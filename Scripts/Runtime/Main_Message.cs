using Core;

using UI;

using Unity.Entities;

using UnityEngine;

namespace Integration
{
    public class Main_Message : Message
    {
        [SerializeField] MenuButton DeleteButton;
        [SerializeField] MenuButton TimeoutButton;
        [SerializeField] MenuButton BanButton;

        protected virtual void Start()
        {
            DeleteButton?.AddListener(OnDelete);
            TimeoutButton?.AddListener(OnTimeout);
            BanButton?.AddListener(OnBan);
        }
        protected virtual void OnDestroy()
        {
            DeleteButton?.RemoveAllListeners();
            TimeoutButton?.RemoveAllListeners();
            BanButton?.RemoveAllListeners();
        }

        protected virtual void OnDelete()
        {
            Input.Title = "Delete Message Button";
            Sys.Add_M(Input, World.DefaultGameObjectInjectionWorld.EntityManager);
        }
        protected virtual void OnTimeout()
        {
            Input.Title = "Timeout Button";
            Sys.Add_M(Input, World.DefaultGameObjectInjectionWorld.EntityManager);
        }
        protected virtual void OnBan()
        {
            Input.Title = "Ban Button";
            Sys.Add_M(Input, World.DefaultGameObjectInjectionWorld.EntityManager);
        }
    }
}