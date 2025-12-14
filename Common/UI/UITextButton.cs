using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace VampireSummonRedux.Common.UI
{
    public class UITextButton : UIPanel
    {
        private UIText _text;

        public UITextButton(string text)
        {
            SetPadding(8);
            _text = new UIText(text);
            Append(_text);
        }

        public void SetText(string text) => _text.SetText(text);

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            BackgroundColor = new Color(73, 94, 171);
        }

        public override void MouseOut(UIMouseEvent evt)
        {
            base.MouseOut(evt);
            BackgroundColor = new Color(63, 82, 151) * 0.7f;
        }
    }
}
