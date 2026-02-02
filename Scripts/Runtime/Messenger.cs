using System.Collections.Generic;

using Core;

using Input;

using UnityEngine;

namespace Integration
{
    public class Messenger : MonoBehaviour
    {
        [SerializeField] int MaxChatCount = 100;
        [SerializeField] Transform Content;
        [SerializeField] GameObject MessagePrefab;

        List<Message> Pool = new List<Message>();
        List<Message> Messages = new List<Message>();

        public void ClearChat()
        {
            for (int m = 0; m < Messages.Count; m++)
                RemoveMessage(Messages[m]);

            Messages.Clear();
        }
        public void OnMessage(OuterInput message, Command command)
        {
            var m = FromPool();
            m.Init(message);

            Messages.Add(m);
        }
        public void OnDeleteMessage(OuterInput message, Command command)
        {
            var index = -1;
            for (int m = 0; m < Messages.Count; m++)
            {
                var me = Messages[m];
                if (me.GetPlatform() == message.Platform &&
                      me.GetID() == message.ID)
                {
                    index = m;

                    RemoveMessage(me);

                    break;
                }
            }

            if (index >= 0)
                Messages.RemoveAt(index);
        }

        protected virtual void ToPool(Message message)
        {
            message.transform.SetParent(null);
            message.gameObject.SetActive(false);

            Pool.Add(message);
        }
        protected virtual Message FromPool()
        {
            Message message;
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
                message = Instantiate(MessagePrefab, Content, false).GetComponent<Message>();

            return message;
        }

        void RemoveMessage(Message message)
        {
            StreamingSprites.RemoveRange(message.GetSmiles());

            ToPool(message);
        }
    }
}