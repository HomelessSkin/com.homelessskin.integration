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
            const string TwitchMessagesURL = "https://api.twitch.tv/helix/chat/messages";
            const string TwitchCategoriesURL = "https://api.twitch.tv/helix/search/categories";
            const string TwitchChannelsURL = "https://api.twitch.tv/helix/channels";
            const string TwitchModerationURL = "https://api.twitch.tv/helix/moderation/chat";
            const string TwitchBanURL = "https://api.twitch.tv/helix/moderation/bans";
            const string TwitchClipsURL = "https://api.twitch.tv/helix/clips";

            const string SetCategoryTitle = "Set Category";

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

            [Space]
            public int ClipDelay = 120;
            public int AgitDelay = 120;
            public int LastMessageDelay = 10;
            public string AgitMessage;

            float ClipT = 0f;
            public float AgitT = 0f;
            public float LastMessage;

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
                    button.AddInput(new OuterInput(SetCategoryTitle, $"{result.data[d].id}"));

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
            public async void SetCategory(OuterInput input)
            {
                if (string.IsNullOrEmpty(input.Message))
                    Log.Warning(this, $"No Category ID!");
                else
                {
                    var platform = TwitchAdapter.GetPlatform();
                    await TwitchAdapter.Patch($"{TwitchChannelsURL}?broadcaster_id={platform.ChannelID}", new CategorySet { game_id = input.Message });
                }

                for (int b = 0; b < Buttons.Length; b++)
                    Destroy(Buttons[b].gameObject);

                Buttons = null;

                Categories.gameObject.SetActive(false);
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
            public async Task<string> TimeOut(string source, string id)
            {
                switch (source)
                {
                    case "vk":
                    return null;

                    case "twitch":
                    var platform = TwitchAdapter.GetPlatform();
                    return await TwitchAdapter.Post($"{TwitchBanURL}?broadcaster_id={platform.ChannelID}&moderator_id={platform.ChannelID}",
                           new TwitchTimeout
                           {
                               data = new TwitchTimeoutData
                               {
                                   user_id = id,
                                   duration = 600
                               }
                           });
                }

                return null;
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
            public async void Clip(OuterInput input)
            {
                if (ClipT > 0f)
                {
                    SendMessage($"Клип не создан! Оставшееся время до следующего клипа: {Mathf.RoundToInt(ClipT)} секунд!");

                    return;
                }

                ClipT += ClipDelay;

                var platform = TwitchAdapter.GetPlatform();
                var response = await TwitchAdapter.Post($"{TwitchClipsURL}?broadcaster_id={platform.ChannelID}&has_delay={false}");
                if (string.IsNullOrEmpty(response))
                {
                    SendMessage($"Клип не создан! Twitch залагал!");

                    return;
                }

                var clip = JsonUtility.FromJson<TwitchClipResponse>(response);
                if (clip.data != null && clip.data.Length > 0 && !string.IsNullOrEmpty(clip.data[0].edit_url))
                    SendMessage($"Клип создан! Ссылка для редактирования: {clip.data[0].edit_url}");
                else
                    SendMessage($"Клип создан, но Twitch не дал на него ссылку!");
            }

            public void Update(float dt)
            {
                if (ClipT > 0f)
                    ClipT -= dt;

                if (AgitT > 0f)
                {
                    AgitT -= dt;

                    return;
                }

                if (Time.realtimeSinceStartup - LastMessage > LastMessageDelay)
                    return;

                AgitT += AgitDelay;

                if (!string.IsNullOrEmpty(AgitMessage))
                    SendMessage(AgitMessage);
            }
            public void ResetLastMessage() => LastMessage = Time.realtimeSinceStartup;
        }

        public void SendPlatformMessage(string message) => _Chat.SendMessage(message);
        public void SetClipDelay(string value) => int.TryParse(value, out _Chat.ClipDelay);
        public void SetAgitDelay(string value) => int.TryParse(value, out _Chat.AgitDelay);
        public void SetLastMessageDelay(string value) => int.TryParse(value, out _Chat.LastMessageDelay);
        public void SetAgitMessage(string value) => _Chat.AgitMessage = value;

        #region OUTER INPUT
        public void OnMessage(OuterInput input)
        {
            switch (Commander.Process(ref input))
            {
                case Command.Response.StopInteractionNow:
                DeleteMessage(input);

                Log.Warning(this, $"Message of {input.Agent} was not posted!");
                return;

                case Command.Response.SoundAlert:
                input.Point = 1;
                break;
            }

            _Chat.MainMessenger.OnMessage(input);
            _Chat.OBSMessenger.OnMessage(input);

            _Chat.ResetLastMessage();
        }
        public void SendPlatformMessage(OuterInput input) => _Chat.SendMessage(input);
        public void FindCategory(OuterInput input) => _Chat.FindCategory(input);
        public void SetCategory(OuterInput input) => _Chat.SetCategory(input);
        public void DeleteMessage(OuterInput input) => _Chat.DeleteMessage(input);
        public async void TimeOut(OuterInput input) => await _Chat.TimeOut(input.Source, input.UserID);
        public void Ban(OuterInput input) => _Chat.Ban(input);
        public void Clip(OuterInput input) => _Chat.Clip(input);
        #endregion
        #endregion

        protected override void Awake()
        {
            base.Awake();

            StreamingSprites.Prepare(_Chat.Smiles);
        }
        protected override void Update()
        {
            base.Update();

            var dt = Time.deltaTime;

            _Chat.Update(dt);

            StreamingSprites.UpdateAnimations(dt);
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
        class TwitchClipResponse
        {
            public ClipData[] data;
        }
        [Serializable]
        class ClipData
        {
            public string edit_url;
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