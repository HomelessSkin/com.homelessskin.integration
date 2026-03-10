using System.Collections.Generic;

using Core;

using Input;

using UnityEngine;
using UnityEngine.UI;

namespace Integration
{
    public class Messenger : MonoBehaviour
    {
        [SerializeField] int MaxChatCount = 100;
        [SerializeField] RectTransform Content;
        [SerializeField] GameObject MessagePrefab;

        List<ChatMessage> Pool = new List<ChatMessage>();
        List<ChatMessage> Messages = new List<ChatMessage>();

        public void ClearChat()
        {
            for (int m = 0; m < Messages.Count; m++)
                ToPool(Messages[m]);

            Messages.Clear();
        }
        public void OnMessage(OuterInput input)
        {
            var m = FromPool();
            m.Init(input);

            Messages.Add(m);
        }
        public void OnDeleteMessage(OuterInput input)
        {
            for (int m = 0; m < Messages.Count; m++)
            {
                var message = Messages[m];
                var m_Input = message.GetInput();
                if (m_Input.Platform == input.Platform &&
                      m_Input.ID == input.ID)
                {
                    ToPool(message);

                    Messages.RemoveAt(m);

                    break;
                }
            }
        }
        public void OnBan(OuterInput input)
        {
            var toRemove = new List<int>();
            for (int m = 0; m < Messages.Count; m++)
            {
                var message = Messages[m];
                var m_Input = message.GetInput();
                if (m_Input.Platform == input.Platform &&
                      m_Input.Nick == input.Nick)
                {
                    ToPool(message);

                    toRemove.Add(m);
                }
            }

            for (int t = toRemove.Count - 1; t >= 0; t--)
                Messages.RemoveAt(toRemove[t]);
        }

        protected virtual void ToPool(ChatMessage message)
        {
            StreamingSprites.RemoveRange(message.GetSmiles());

            message.transform.SetParent(null);
            message.gameObject.SetActive(false);

            Pool.Add(message);
        }
        protected virtual ChatMessage FromPool()
        {
            ChatMessage message;
            if (Messages.Count >= MaxChatCount)
            {
                message = Messages[0];

                message.transform.SetParent(null);
                message.transform.SetParent(Content);

                Messages.RemoveAt(0);
            }
            else if (Pool.Count > 0)
            {
                message = Pool[0];
                message.transform.SetParent(Content);
                message.gameObject.SetActive(true);

                Pool.RemoveAt(0);
            }
            else
                message = Instantiate(MessagePrefab, Content, false).GetComponent<ChatMessage>();

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);

            return message;
        }
    }
}