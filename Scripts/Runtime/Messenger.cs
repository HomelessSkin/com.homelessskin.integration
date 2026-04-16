using System.Collections.Generic;

using Core;

using Input;

using UI;

using UnityEngine;

namespace Integration
{
    public class Messenger : MonoBehaviour
    {
        [SerializeField] Scroll Scroll;
        [SerializeField] GameObject MessagePrefab;

        void Start()
        {
            Scroll.Init(MessagePrefab);
        }

        public void ClearChat()
        {
            var view = Scroll.GetView();
            for (int m = 0; m < view.Count; m++)
                Scroll.ToPool(m);
        }

        protected virtual bool FromPool(out RectTransform t) => Scroll.TryGetFromPool(out t);

        #region OUTER INPUT
        public void OnMessage(OuterInput input)
        {
            if (FromPool(out var m))
            {
                m.GetComponent<ChatMessage>().Init(input);

                Scroll.ToView(m);
            }
        }
        public void OnDeleteMessage(OuterInput input)
        {
            var view = Scroll.GetView();
            for (int v = 0; v < view.Count; v++)
            {
                var transform = view[v];
                var message = transform.GetComponent<ChatMessage>();
                var m_Input = message.GetInput();
                if (m_Input.Platform == input.Platform &&
                      m_Input.ID == input.ID)
                {
                    StreamingSprites.RemoveRange(message.GetSmiles());

                    Scroll.ToPool(v);

                    break;
                }
            }
        }
        public void OnBan(OuterInput input)
        {
            var toRemove = new List<int>();
            var view = Scroll.GetView();
            for (int m = 0; m < view.Count; m++)
            {
                var transform = view[m];
                var message = transform.GetComponent<ChatMessage>();
                var m_Input = message.GetInput();
                if (m_Input.Platform == input.Platform &&
                      m_Input.Nick == input.Nick)
                {
                    StreamingSprites.RemoveRange(message.GetSmiles());

                    toRemove.Add(m);
                }
            }

            for (int t = view.Count - 1; t >= 0; t--)
                Scroll.ToPool(toRemove[t]);
        }
        #endregion
    }
}