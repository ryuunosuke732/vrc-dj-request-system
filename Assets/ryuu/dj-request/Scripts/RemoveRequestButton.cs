
using UdonSharp;
using UnityEngine;


namespace ryuu
{
    public class RemoveRequestButton : UdonSharpBehaviour
    {
        public int index;
        public RequestController requestController;

        float _animation = 0f;
        RectTransform _parentrect;
        float _width;
        float _height;

        private void Start()
        {
            _parentrect = transform.parent.GetComponent<RectTransform>();
            _width = _parentrect.sizeDelta.x;
            _height = _parentrect.sizeDelta.y;
        }
        public void onClick()
        {
            if (requestController == null)
            {
                Debug.LogError("[RyuuRequest] Request controller was not set!");
                return;
            }
            if (!requestController.animateDelete)
            {
                requestController.removeRequest(index);
            }
            else
            {
                updateAnimation();
            }
        }

        float EaseOutPower(float x, float pow)
        {
            return 1 - Mathf.Pow(1 - x, pow);
        }

        float EaseInOutPower(float x, float pow)
        {
            return Mathf.Lerp(EaseInPower(x, pow), EaseOutPower(x, pow), x);
        }

        float EaseInPower(float x, float pow)
        {
            return Mathf.Pow(x, pow);
        }

        public void updateAnimation()
        {
            float duration = 0.275f;
            
            float width_multiplier = EaseInOutPower(Mathf.Clamp(_animation * 2, 0, 1f), 3);
            float height_multiplier = EaseInOutPower(Mathf.Clamp((_animation - .5f) * 2, 0, 1f), 3);

            if (_animation < 1f)
            {
                _animation += 1f / (60f * duration);
                _parentrect.sizeDelta = new Vector2(Mathf.Lerp(_width, 0, width_multiplier), Mathf.Lerp(_height, -38, height_multiplier));
                SendCustomEventDelayedSeconds(nameof(updateAnimation), 1f / 60f);
                return;
            }
            else
            {
                _animation = 0f;
                requestController.removeRequest(index);
            }
        }
    }
}