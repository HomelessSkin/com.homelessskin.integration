using Core;

using UI;

using Unity.Entities;

using UnityEngine;

namespace Integration
{
    public class Main_Message : Message
    {
        [SerializeField] MenuButton DeleteButton;

        protected virtual void Start()
        {
            DeleteButton?.AddListener(OnDelete);
        }
        protected virtual void OnDestroy()
        {
            DeleteButton?.RemoveAllListeners();
        }

        protected virtual void OnDelete()
        {
            Input.Title = "Delete Message Button";
            Sys.Add_M(Input, World.DefaultGameObjectInjectionWorld.EntityManager);
        }
    }
}