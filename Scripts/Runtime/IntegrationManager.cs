using System;

using Core;

using Input;

using UI;

using UnityEngine;

namespace Integration
{
    public class ChatManager : UIManagerBase
    {
        #region CANVASSER
        void RenderMainCanvas() => _Canvasser.QueueRender();
        void RenderOBSCanvas() => _Canvasser.QueueRender(1);
        #endregion

        #region DRAWER
        //protected override void RedrawTheme(IStorage.Data storage)
        //{
        //    base.RedrawTheme(storage);

        //    for (int m = 0; m < _Chat.Messages.Count; m++)
        //    {
        //        var message = _Chat.Messages[m];
        //        var drawables = message.GetComponentsInChildren<Drawable>();
        //        for (int d = 0; d < drawables.Length; d++)
        //        {
        //            var drawable = drawables[d];
        //            if (!(drawable as IRedrawable).IsRedrawable())
        //                continue;
        //            if (TryGetDrawerData(drawable.GetKey(), out var sprite))
        //                drawable.SetData(sprite);
        //        }
        //    }

        //    for (int m = 0; m < _Chat.Pool.Count; m++)
        //    {
        //        var message = _Chat.Pool[m];
        //        var drawables = message.GetComponentsInChildren<Drawable>();
        //        for (int d = 0; d < drawables.Length; d++)
        //        {
        //            var drawable = drawables[d];
        //            if (!(drawable as IRedrawable).IsRedrawable())
        //                continue;
        //            if (TryGetDrawerData(drawable.GetKey(), out var sprite))
        //                drawable.SetData(sprite);
        //        }
        //    }
        //}
        #endregion

        [Space]
        [SerializeField] protected Chat _Chat;
        #region CHAT
        [Serializable]
        protected class Chat
        {
            protected string TwitchModerationURL = $"https://api.twitch.tv/helix/moderation/chat";
            protected string TwitchBanURL = $"https://api.twitch.tv/helix/moderation/bans";

            [Space]
            public MonoAdapter VKAdapter;
            public MonoAdapter TwitchAdapter;

            [Space]
            public Messenger MainMessenger;
            public Messenger OBSMessenger;

            [Space]
            public StreamingSpritesData Smiles;

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

        public void DeleteMessage(OuterInput input) => _Chat.DeleteMessage(input);
        public void TimeOut(OuterInput input) => _Chat.TimeOut(input);
        public void Ban(OuterInput input) => _Chat.Ban(input);
        #endregion

        protected override void Awake()
        {
            base.Awake();

            StreamingSprites.Prepare(_Chat.Smiles);
            Log.AddReadListener(RenderMainCanvas);
        }

        #region TWITCH TIMEOUT
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