using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace VampireSummonRedux.Common.UI
{
    public class UIDraggablePanel : UIPanel
    {
        private bool dragging;
        private Vector2 offset;

        public override void OnInitialize()
        {
            // Use UIElement events (compatible)
            OnLeftMouseDown += DragStart;
            OnLeftMouseUp += DragEnd;
        }

        private void DragStart(UIMouseEvent evt, UIElement listeningElement)
        {
            dragging = true;
            offset = evt.MousePosition - new Vector2(Left.Pixels, Top.Pixels);
        }

        private void DragEnd(UIMouseEvent evt, UIElement listeningElement)
        {
            dragging = false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (dragging)
            {
                Left.Set(Main.mouseX - offset.X, 0f);
                Top.Set(Main.mouseY - offset.Y, 0f);
                Recalculate();
            }
        }
    }
}
