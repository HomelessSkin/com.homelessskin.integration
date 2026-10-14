using Input;

using UI;

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

            var del = input.Clone();
            del.Title = "Delete Message Button";
            DeleteButton.RemoveAllInputs();
            DeleteButton.AddInput(del);

            var to = input.Clone();
            to.Title = "Timeout Button";
            TimeoutButton.RemoveAllInputs();
            TimeoutButton.AddInput(to);

            var ban = input.Clone();
            ban.Title = "Ban Button";
            BanButton.RemoveAllInputs();
            BanButton.AddInput(ban);
        }
    }
}