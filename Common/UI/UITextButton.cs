using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace VampireSummonRedux.Common.UI
{
    public class UITextButton : UIPanel
    {
        private readonly UIText _text;

        public bool Enabled { get; private set; } = true;

        // Optional: set by UIState so hovering updates the description line
        public System.Action<string> OnHoverDescription;
        public string HoverDescription;

        // Track base/hover colors so disabled can override them
        private readonly Color _baseColor = new Color(63, 82, 151) * 0.7f;
        private readonly Color _hoverColor = new Color(73, 94, 171);
        private readonly Color _disabledColor = new Color(40, 40, 40) * 0.7f;

        public UITextButton(string text, float scale = 0.50f)
        {
            SetPadding(8);

            BackgroundColor = _baseColor;

            _text = new UIText(text, scale, false);
            Append(_text);
        }

        public void SetText(string text) => _text.SetText(text);

        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
            BackgroundColor = Enabled ? _baseColor : _disabledColor;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            if (!Enabled)
                return;

            base.LeftClick(evt);
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);

            if (!Enabled)
                return;

            BackgroundColor = _hoverColor;

            if (OnHoverDescription != null && !string.IsNullOrEmpty(HoverDescription))
                OnHoverDescription(HoverDescription);
        }

        public override void MouseOut(UIMouseEvent evt)
        {
            base.MouseOut(evt);

            BackgroundColor = Enabled ? _baseColor : _disabledColor;
        }
    }
}
