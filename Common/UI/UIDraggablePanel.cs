using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace VampireSummonRedux.Common.UI
{
    public class UIDraggablePanel : UIPanel
    {
        private Vector2 _dragOffset;
        private bool _dragging;

        public override void MouseDown(UIMouseEvent evt)
        {
            base.MouseDown(evt);
            _dragging = true;
            _dragOffset = new Vector2(evt.MousePosition.X - Left.Pixels, evt.MousePosition.Y - Top.Pixels);
        }

        public override void MouseUp(UIMouseEvent evt)
        {
            base.MouseUp(evt);
            _dragging = false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (_dragging)
            {
                Left.Set(Main.mouseX - _dragOffset.X, 0f);
                Top.Set(Main.mouseY - _dragOffset.Y, 0f);
                Recalculate();
            }
        }
    }
}
