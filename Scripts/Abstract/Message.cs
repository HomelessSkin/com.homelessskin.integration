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

        OuterInput Input;

        List<int> Icons = new List<int>();

        internal void Init(OuterInput message)
        {
            Icons.Clear();

            Input = message;

            var color = "#808080";
            if (!string.IsNullOrEmpty(message.NickColor))
                color = message.NickColor;

            var text = "";

            if (message.Badges != null)
                for (int b = 0; b < message.Badges.Count; b++)
                {
                    var badge = message.Badges[b];
                    text += $"<sprite name=\"{StreamingSprites.Asset}_{badge.Index}\">";

                    if (!Icons.Contains(badge.Index))
                        Icons.Add(badge.Index);
                }

            text += $"<color={color}>{message.Nick}</color>: ";

            if (message.UserInput != null)
            {
                if (message.IsSlashMe)
                    text += $"<color={message.NickColor}><i>";

                for (int pt = 0; pt < message.UserInput.Count; pt++)
                {
                    var part = message.UserInput[pt];
                    if (part.Message != null &&
                        !string.IsNullOrEmpty(part.Message.Content))
                        text += part.Message.Content;

                    if (part.Emote != null)
                    {
                        text += $"<sprite name=\"{StreamingSprites.Asset}_{part.Emote.Index}\">";

                        if (!Icons.Contains(part.Emote.Index))
                            Icons.Add(part.Emote.Index);
                    }
                }

                if (message.IsSlashMe)
                    text += $"</color></i>";
            }

            Content.text = text;
        }
        internal string GetPlatform() => Input.Platform;
        internal string GetID() => Input.ID;
        internal List<int> GetSmiles() => Icons;
    }
}