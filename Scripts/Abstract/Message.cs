using System.Collections.Generic;

using Core;

using Input;

using TMPro;

using UnityEngine;

namespace Integration
{
    public abstract class ChatMessage : MonoBehaviour
    {
        [SerializeField] TMP_Text Nick;
        [SerializeField] TMP_Text Badges;
        [SerializeField] TMP_Text Content;

        protected OuterInput Input;

        protected List<int> Icons = new List<int>();

        public virtual void Init(OuterInput input)
        {
            Input = input;

            Icons.Clear();

            if (input.Icons != null)
                Icons.AddRange(input.Icons);
            if (input.Badges != null)
            {
                Icons.AddRange(input.Badges);

                var badges = "";
                for (int b = 0; b < input.Badges.Count; b++)
                    badges += $"<sprite name=\"{StreamingSprites.Asset}_{input.Badges[b]}\">";

                Badges.text = badges;
            }

            Nick.text = $"{input.Agent}";
            Content.text = $"{input.Message}";
        }

        public OuterInput GetInput() => Input;
        public List<int> GetSmiles() => Icons;
    }
}