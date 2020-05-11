using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace LineEditor.Avalonia
{
    public class MainWindow : Window
    {
        private Grid layout;
        private CanvasViewModel _canvasViewModel;

        public MainWindow()
        {
            InitializeComponent();

            VisualRoot.Renderer.DrawDirtyRects = true;

            layout = this.FindControl<Grid>("layout");

            InitializeViewModel();

            KeyDown += MainWindow_PreviewKeyDown;
  
            DataContext = _canvasViewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeViewModel()
        {
            _canvasViewModel = new CanvasViewModel
            {
                BackgroundCanvas = new CanvasShape(),
                DrawingCanvas = new CanvasShape(),
                BoundsCanvas = new CanvasShape()
            };

            var backgroundRenderCanvas = new RenderCanvas(_canvasViewModel.BackgroundCanvas);
            var drawingRenderCanvas = new RenderCanvas(_canvasViewModel.DrawingCanvas);
            var boundsRenderCanvas = new RenderCanvas(_canvasViewModel.BoundsCanvas);

            layout.Children.Add(backgroundRenderCanvas);
            layout.Children.Add(drawingRenderCanvas);
            layout.Children.Add(boundsRenderCanvas);

            CreateGrid(
                _canvasViewModel.BackgroundCanvas,
                _canvasViewModel.BackgroundCanvas.Width,
                _canvasViewModel.BackgroundCanvas.Height,
                30,
                0, 0);

            layout.Width = _canvasViewModel.BackgroundCanvas.Width;
            layout.Height = _canvasViewModel.BackgroundCanvas.Height;

            ObserveInput(_canvasViewModel.DrawingCanvas, layout/*boundsRenderCanvas*/);

            _canvasViewModel.SelectionTool = new SelectionTool(_canvasViewModel.DrawingCanvas, _canvasViewModel.BoundsCanvas)
            {
                IsEnabled = false
            };

            _canvasViewModel.LineTool = new LineTool(_canvasViewModel.DrawingCanvas, _canvasViewModel.BoundsCanvas)
            {
                IsEnabled = true
            };

            _canvasViewModel.RectangleTool = new RectangleTool(_canvasViewModel.DrawingCanvas, _canvasViewModel.BoundsCanvas)
            {
                IsEnabled = false
            };
        }

        private void ObserveInput(CanvasShape canvasShape, IControl target)
        {
            canvasShape.Downs = Observable.FromEventPattern<PointerPressedEventArgs>(
                 target,
                 "PointerPressed").Select(e =>
                 {
                     var p = e.EventArgs.GetPosition(target);
                     return new Point2(
                         canvasShape.EnableSnap ? canvasShape.Snap(p.X, canvasShape.SnapX) : p.X,
                         canvasShape.EnableSnap ? canvasShape.Snap(p.Y, canvasShape.SnapY) : p.Y);
                 });

            canvasShape.Ups = Observable.FromEventPattern<PointerReleasedEventArgs>(
                target,
                "PointerReleased").Select(e =>
                {
                    var p = e.EventArgs.GetPosition(target);
                    return new Point2(
                        canvasShape.EnableSnap ? canvasShape.Snap(p.X, canvasShape.SnapX) : p.X,
                        canvasShape.EnableSnap ? canvasShape.Snap(p.Y, canvasShape.SnapY) : p.Y);
                });

            canvasShape.Moves = Observable.FromEventPattern<PointerEventArgs>(
                target,
                "PointerMoved").Select(e =>
                {
                    var p = e.EventArgs.GetPosition(target);
                    return new Point2(
                        canvasShape.EnableSnap ? canvasShape.Snap(p.X, canvasShape.SnapX) : p.X,
                        canvasShape.EnableSnap ? canvasShape.Snap(p.Y, canvasShape.SnapY) : p.Y);
                });

            bool captured = false;

            canvasShape.IsCaptured = () => captured;

            canvasShape.Capture = () => captured = true;

            canvasShape.ReleaseCapture = () => captured = false;
        }

        private void CreateGrid(CanvasShape canvasShape, double width, double height, double size, double originX, double originY)
        {
            var thickness = 2.0;
            var stroke = new ArgbColor(0xFF, 0xE8, 0xE8, 0xE8);

            for (double y = size; y < height; y += size)
            {
                var lineShape = new LineShape();
                lineShape.Point1.X = originX;
                lineShape.Point1.Y = y;
                lineShape.Point2.X = width;
                lineShape.Point2.Y = y;
                lineShape.Stroke = stroke;
                lineShape.StrokeThickness = thickness;
                canvasShape.Children.Add(lineShape);
            }

            for (double x = size; x < width; x += size)
            {
                var lineShape = new LineShape();
                lineShape.Point1.X = x;
                lineShape.Point1.Y = originY;
                lineShape.Point2.X = x;
                lineShape.Point2.Y = height;
                lineShape.Stroke = stroke;
                lineShape.StrokeThickness = thickness;
                canvasShape.Children.Add(lineShape);
            }
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.S:
                    _canvasViewModel.LineTool.IsEnabled = false;
                    _canvasViewModel.RectangleTool.IsEnabled = false;
                    _canvasViewModel.SelectionTool.IsEnabled = true;
                    break;
                case Key.L:
                    _canvasViewModel.LineTool.IsEnabled = true;
                    _canvasViewModel.RectangleTool.IsEnabled = false;
                    _canvasViewModel.SelectionTool.IsEnabled = false;
                    break;
                case Key.R:
                    _canvasViewModel.LineTool.IsEnabled = false;
                    _canvasViewModel.RectangleTool.IsEnabled = true;
                    _canvasViewModel.SelectionTool.IsEnabled = false;
                    break;
                case Key.G:
                    _canvasViewModel.ToggleSnap();
                    break;
                case Key.Delete:
                    _canvasViewModel.Delete();
                    break;
            }
        }
    }
}
