using System.Linq;
using System.Reactive.Linq;

namespace LineEditor
{
    public class CanvasViewModel
    {
        public CanvasShape BackgroundCanvas { get; set; }

        public CanvasShape DrawingCanvas { get; set; }

        public CanvasShape BoundsCanvas { get; set; }

        public SelectionTool SelectionTool { get; set; }

        public LineTool LineTool { get; set; }

        public RectangleTool RectangleTool { get; set; }

        public void ToggleSnap()
        {
            DrawingCanvas.EnableSnap = !DrawingCanvas.EnableSnap;
        }

        public void Clear()
        {
            DrawingCanvas.Children.Clear();
            DrawingCanvas.InvalidateShape?.Invoke();
            BoundsCanvas.InvalidateShape?.Invoke();
        }

        public void Render()
        {
            BackgroundCanvas.InvalidateShape?.Invoke();
            DrawingCanvas.InvalidateShape?.Invoke();
            BoundsCanvas.InvalidateShape?.Invoke();
        }

        public void Delete()
        {
            var selectedShapes = DrawingCanvas.Children.Where(c => c.Bounds != null && c.Bounds.IsVisible()).ToList();

            foreach (var child in selectedShapes)
            {
                child.Bounds.Hide();
            }

            foreach (var child in selectedShapes)
            {
                DrawingCanvas.Children.Remove(child);
            }

            DrawingCanvas.InvalidateShape?.Invoke();
            BoundsCanvas.InvalidateShape?.Invoke();
        }
    }
}
