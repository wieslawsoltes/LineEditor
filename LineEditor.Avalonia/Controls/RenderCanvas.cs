using Avalonia.Controls;
using Avalonia.Media;

namespace LineEditor.Avalonia
{
    public class RenderCanvas : Control
    {
        private readonly CanvasShape _canvasShape;
        private readonly DrawingContextRenderer _drawingContextRenderer;

        public RenderCanvas(CanvasShape canvasShape)
        {
            _canvasShape = canvasShape;
            _canvasShape.InvalidateShape = () => this.InvalidateVisual();

            _drawingContextRenderer = new DrawingContextRenderer();

            //Background = Brushes.Transparent;
            Width = _canvasShape.Width;
            Height = _canvasShape.Height;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            _drawingContextRenderer.DrawBackground(context, _canvasShape);
            _drawingContextRenderer.DrawCanvasShape(context, _canvasShape);
        }
    }
}
