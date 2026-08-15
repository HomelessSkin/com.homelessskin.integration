using System;
using System.Threading.Tasks;

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
            string TwitchCategoriesURL = $"https://api.twitch.tv/helix/search/categories";
            string TwitchChannelsURL = $"https://api.twitch.tv/helix/channels";
            string TwitchModerationURL = $"https://api.twitch.tv/helix/moderation/chat";
            string TwitchBanURL = $"https://api.twitch.tv/helix/moderation/bans";
            string TwitchClipsURL = $"https://api.twitch.tv/helix/clips";

            [Space]
            public MonoAdapter VKAdapter;
            public MonoAdapter TwitchAdapter;

            [Space]
            public Messenger MainMessenger;
            public Messenger OBSMessenger;

            [Space]
            public RectTransform Categories;
            public RectTransform Content;
            public MenuButton Category;

            MenuButton[] Buttons;

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
                if (string.IsNullOrEmpty(input.Message))
                    return;

                var platform = TwitchAdapter.GetPlatform();
                await TwitchAdapter.Post($"{TwitchMessagesURL}", new TwitchMessage
                {
                    broadcaster_id = platform.ChannelID,
                    sender_id = platform.ChannelID,
                    message = $"{input.Message}"
                });
            }
            public async void FindCategory(OuterInput input)
            {
                if (string.IsNullOrEmpty(input.Message))
                    return;

                var result = await TwitchAdapter.Get<TwitchCategoriesResponse>($"{TwitchCategoriesURL}?query={input.Message}");
                if (result == null ||
                     result.data == null ||
                     result.data.Length == 0)
                {
                    Log.Warning(this, $"Can not find Category {input.Message}!");

                    return;
                }

                Categories.gameObject.SetActive(true);

                Buttons = new MenuButton[result.data.Length];
                for (int d = 0; d < result.data.Length; d++)
                {
                    var button = Instantiate(Category, Content);

                    button.SetLabel(result.data[d].name);
                    var key = $"{result.data[d].id}";
                    button.AddListener(() => SetCategory(key));

                    Buttons[d] = button;
                }

                for (int d = 0; d < result.data.Length; d++)
                {
                    var c = 0;
                    Texture2D text = null;
                    while (!text && c < 1000)
                    {
                        text = await DataUtil.LoadTexture(result.data[d].box_art_url);

                        await Task.Delay(128);
                    }

                    if (text && Buttons != null && d < Buttons.Length)
                        Buttons[d]?.SetImage(text);
                    else
                        break;
                }
            }
            public async void DeleteMessage(OuterInput input)
            {
                switch (input.Source)
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
                switch (input.Source)
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
                switch (input.Source)
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
            public async void Clip()
            {
                var platform = TwitchAdapter.GetPlatform();
                await TwitchAdapter.Post($"{TwitchClipsURL}?broadcaster_id={platform.ChannelID}&has_delay={false}");
            }

            async void SetCategory(string id)
            {
                var platform = TwitchAdapter.GetPlatform();
                await TwitchAdapter.Patch($"{TwitchChannelsURL}?broadcaster_id={platform.ChannelID}", new CategorySet { game_id = id });

                for (int b = 0; b < Buttons.Length; b++)
                    Destroy(Buttons[b].gameObject);

                Buttons = null;

                Categories.gameObject.SetActive(false);
            }
        }

        public void SendPlatformMessage(string message) => _Chat.SendMessage(message);
        public void FindCategory(OuterInput input) => _Chat.FindCategory(input);
        public void SendPlatformMessage(OuterInput input) => _Chat.SendMessage(input);
        public void DeleteMessage(OuterInput input) => _Chat.DeleteMessage(input);
        public void TimeOut(OuterInput input) => _Chat.TimeOut(input);
        public void Ban(OuterInput input) => _Chat.Ban(input);
        public void Clip() => _Chat.Clip();
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
        class TwitchCategoriesResponse
        {
            public Category[] data;
        }
        [Serializable]
        class Category
        {
            public string name;
            public string id;
            public string box_art_url;
        }
        [Serializable]
        class CategorySet
        {
            public string game_id;
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