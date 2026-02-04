using System.Collections.Generic;

using Core;

using Input;

using TMPro;

using UnityEngine;

namespace Integration
{
    public abstract class Message : MonoBehaviour
    {
        [SerializeField] TMP_Text Content;

        protected OuterInput Input;

        protected List<int> Icons = new List<int>();

        public void Init(OuterInput input)
        {
            Icons.Clear();

            Input = input;

            var color = "#808080";
            if (!string.IsNullOrEmpty(input.NickColor))
                color = input.NickColor;

            var text = "";

            if (input.Badges != null)
                for (int b = 0; b < input.Badges.Count; b++)
                {
                    var badge = input.Badges[b];
                    text += $"<sprite name=\"{StreamingSprites.Asset}_{badge.Index}\">";

                    if (!Icons.Contains(badge.Hash))
                        Icons.Add(badge.Hash);
                }

            text += $"<color={color}>{input.Nick}</color>: ";

            if (input.UserInput != null)
            {
                if (input.IsSlashMe)
                    text += $"<color={input.NickColor}><i>";

                for (int pt = 0; pt < input.UserInput.Count; pt++)
                {
                    var part = input.UserInput[pt];
                    if (part.Message != null &&
                        !string.IsNullOrEmpty(part.Message.Content))
                        text += part.Message.Content;

                    if (part.Emote != null)
                    {
                        text += $"<sprite name=\"{StreamingSprites.Asset}_{part.Emote.Index}\">";

                        if (!Icons.Contains(part.Emote.Hash))
                            Icons.Add(part.Emote.Hash);
                    }
                }

                if (input.IsSlashMe)
                    text += $"</color></i>";
            }

            Content.text = text;
        }
        public string GetPlatform() => Input.Platform;
        public string GetID() => Input.ID;
        public List<int> GetSmiles() => Icons;
    }
}