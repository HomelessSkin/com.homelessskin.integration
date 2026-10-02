using Input;

using UnityEngine;
using UnityEngine.UI;

namespace Integration
{
    public class OBS_Message : ChatMessage
    {
        [Space]
        [SerializeField] Image Icon;

        public override void Init(OuterInput input)
        {
            base.Init(input);

            Icon.gameObject.SetActive(input.Point == 1);
        }
    }
}