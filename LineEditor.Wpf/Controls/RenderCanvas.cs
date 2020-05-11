using System.Windows.Controls;
using System.Windows.Media;

namespace LineEditor.Wpf
{
    public class RenderCanvas : Canvas
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

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            _drawingContextRenderer.DrawBackground(dc, _canvasShape);
            _drawingContextRenderer.DrawCanvasShape(dc, _canvasShape);
        }
    }
}
