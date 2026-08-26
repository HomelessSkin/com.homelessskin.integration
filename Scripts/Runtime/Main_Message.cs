using Core;

using Input;

using UI;

using Unity.Entities;

using UnityEngine;

namespace Integration
{
    public class Main_Message : ChatMessage
    {
        [Space]
        [SerializeField] MenuButton DeleteButton;
        [SerializeField] MenuButton TimeoutButton;
        [SerializeField] MenuButton BanButton;

        public override void Init(OuterInput input)
        {
            base.Init(input);

            var del = new OuterInput(input);
            del.Title = "Delete Message Button";
            DeleteButton.RemoveAllInputs();
            DeleteButton.AddInput(del);

            var to = new OuterInput(input);
            to.Title = "Timeout Button";
            TimeoutButton.RemoveAllInputs();
            TimeoutButton.AddInput(to);

            var ban = new OuterInput(input);
            ban.Title = "Ban Button";
            BanButton.RemoveAllInputs();
            BanButton.AddInput(ban);
        }
    }
}