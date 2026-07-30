using System;

using Core;

using Input;

using UI;

using UnityEngine;

namespace Integration
{
    public class ChatManager : UIManagerBase
    {
        [Space]
        [SerializeField] protected Chat _Chat;
        #region CHAT
        [Serializable]
        protected class Chat
        {
            string TwitchMessagesURL = $"https://api.twitch.tv/helix/chat/messages";
            string TwitchModerationURL = $"https://api.twitch.tv/helix/moderation/chat";
            string TwitchBanURL = $"https://api.twitch.tv/helix/moderation/bans";

            [Space]
            public MonoAdapter VKAdapter;
            public MonoAdapter TwitchAdapter;

            [Space]
            public Messenger MainMessenger;
            public Messenger OBSMessenger;

            [Space]
            public StreamingSpritesData Smiles;

            public async void SendMessage(string message)
            {
                var platform = TwitchAdapter.GetPlatform();
                await TwitchAdapter.Post($"{TwitchMessagesURL}", new TwitchMessage
                {
                    broadcaster_id = platform.ChannelID,
                    sender_id = platform.ChannelID,
                    message = message
                });
            }
            public async void SendMessage(OuterInput input)
            {
                if (input.UserInput == null || input.UserInput.Count == 0)
                    return;

                var platform = TwitchAdapter.GetPlatform();
                await TwitchAdapter.Post($"{TwitchMessagesURL}", new TwitchMessage
                {
                    broadcaster_id = platform.ChannelID,
                    sender_id = platform.ChannelID,
                    message = $"{input.UserInput[0].Message?.Content}"
                });
            }
            public async void DeleteMessage(OuterInput input)
            {
                switch (input.Platform)
                {
                    case "vk":
                    {
                        MainMessenger.OnDeleteMessage(input);
                        OBSMessenger.OnDeleteMessage(input);
                    }
                    break;
                    case "twitch":
                    {
                        var platform = TwitchAdapter.GetPlatform();
                        await TwitchAdapter.Delete($"{TwitchModerationURL}?broadcaster_id={platform.ChannelID}&moderator_id={platform.ChannelID}&message_id={input.ID}");
                    }
                    break;
                }
            }
            public async void TimeOut(OuterInput input)
            {
                switch (input.Platform)
                {
                    case "vk":
                    {

                    }
                    break;
                    case "twitch":
                    {
                        var platform = TwitchAdapter.GetPlatform();
                        await TwitchAdapter.Post($"{TwitchBanURL}?broadcaster_id={platform.ChannelID}&moderator_id={platform.ChannelID}",
                                    new TwitchTimeout
                                    {
                                        data = new TwitchTimeoutData
                                        {
                                            user_id = input.UserID,
                                            duration = 600
                                        }
                                    });
                    }
                    break;
                }
            }
            public async void Ban(OuterInput input)
            {
                switch (input.Platform)
                {
                    case "vk":
                    {

                    }
                    break;
                    case "twitch":
                    {
                        var platform = TwitchAdapter.GetPlatform();
                        await TwitchAdapter.Post($"{TwitchBanURL}?broadcaster_id={platform.ChannelID}&moderator_id={platform.ChannelID}",
                            new TwitchBan
                            {
                                data = new TwitchBanData
                                {
                                    user_id = input.UserID
                                }
                            });
                    }
                    break;
                }
            }
        }

        public void SendPlatformMessage(string message) => _Chat.SendMessage(message);
        public void SendPlatformMessage(OuterInput input) => _Chat.SendMessage(input);
        public void DeleteMessage(OuterInput input) => _Chat.DeleteMessage(input);
        public void TimeOut(OuterInput input) => _Chat.TimeOut(input);
        public void Ban(OuterInput input) => _Chat.Ban(input);
        #endregion

        protected override void Awake()
        {
            base.Awake();

            StreamingSprites.Prepare(_Chat.Smiles);
        }

        #region TWITCH
        [Serializable]
        class TwitchMessage
        {
            public string broadcaster_id;
            public string sender_id;
            public string message;
        }
        [Serializable]
        class TwitchTimeout
        {
            public TwitchTimeoutData data;

        }
        [Serializable]
        class TwitchTimeoutData
        {
            public string user_id;
            public int duration;
        }
        [Serializable]
        class TwitchBan
        {
            public TwitchBanData data;
        }
        [Serializable]
        class TwitchBanData
        {
            public string user_id;
        }
        #endregion
    }
}