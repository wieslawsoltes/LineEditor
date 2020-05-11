using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace LineEditor.Avalonia
{
    public class EnumBindingSourceExtension : MarkupExtension
    {
        private Type _enumType;

        public Type EnumType
        {
            get { return _enumType; }
            set
            {
                if (value != _enumType)
                {
                    if (null != value)
                    {
                        Type enumType = Nullable.GetUnderlyingType(value) ?? value;
                        if (!enumType.IsEnum)
                        {
                            throw new ArgumentException("Type must be for an Enum.");
                        }
                    }
                    _enumType = value;
                }
            }
        }

        public EnumBindingSourceExtension()
        {
        }

        public EnumBindingSourceExtension(Type enumType)
        {
            EnumType = enumType;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (null == _enumType)
            {
                throw new InvalidOperationException("The EnumType must be specified.");
            }

            var actualEnumType = Nullable.GetUnderlyingType(_enumType) ?? _enumType;
            var enumValues = Enum.GetValues(actualEnumType);
            if (actualEnumType == _enumType)
            {
                return enumValues;
            }

            var tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
            enumValues.CopyTo(tempArray, 1);
            return tempArray;
        }
    }

    public class PanAndZoomBorder : Border
    {
        private bool _initialize = true;
        private IControl _child = null;
        private Point _origin;
        private Point _start;
        private bool _isCaptured = false;

        private TranslateTransform GetTranslateTransform(IControl element)
        {
            return (TranslateTransform)((TransformGroup)element.RenderTransform).Children.First(tr => tr is TranslateTransform);
        }

        private ScaleTransform GetScaleTransform(IControl element)
        {
            return (ScaleTransform)((TransformGroup)element.RenderTransform).Children.First(tr => tr is ScaleTransform);
        }

        public PanAndZoomBorder()
        {
            this.GetObservable(ChildProperty).Subscribe((value) =>
            {
                if (value != null && value != Child)
                {
                    _child = value;
                    if (_initialize)
                    {
                        var group = new TransformGroup();
                        var st = new ScaleTransform();
                        group.Children.Add(st);
                        var tt = new TranslateTransform();
                        group.Children.Add(tt);
                        _child.RenderTransform = group;
                        _child.RenderTransformOrigin = new RelativePoint(new Point(0.0, 0.0), RelativeUnit.Absolute);
                        PointerWheelChanged += PanAndZoomBorder_PointerWheelChanged;
                        PointerPressed += PanAndZoomBorder_PointerPressed;
                        PointerReleased += PanAndZoomBorder_PointerReleased;
                        PointerMoved += PanAndZoomBorder_PointerMoved;
                        _initialize = false;
                    }
                }
            });
        }

        public void Reset()
        {
            if (_initialize == false && _child != null)
            {
                var st = GetScaleTransform(_child);
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;
                var tt = GetTranslateTransform(_child);
                tt.X = 0.0;
                tt.Y = 0.0;
            }
        }

        private void PanAndZoomBorder_PointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            if (_initialize == false && _child != null)
            {
                var st = GetScaleTransform(_child);
                var tt = GetTranslateTransform(_child);
                double zoom = e.Delta.X > 0 ? .2 : -.2;
                if (!(e.Delta.X > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
                    return;
                Point relative = e.GetPosition(_child);
                double abosuluteX = relative.X * st.ScaleX + tt.X;
                double abosuluteY = relative.Y * st.ScaleY + tt.Y;
                st.ScaleX += zoom;
                st.ScaleY += zoom;
                tt.X = abosuluteX - relative.X * st.ScaleX;
                tt.Y = abosuluteY - relative.Y * st.ScaleY;
            }
        }

        private void PanAndZoomBorder_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                if (_initialize == false && _child != null)
                {
                    var tt = GetTranslateTransform(_child);
                    _start = e.GetPosition(this);
                    _origin = new Point(tt.X, tt.Y);
                    Cursor = new Cursor(StandardCursorType.Hand);
                    _isCaptured = true;
                }
            }

            if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed /*&& e.ClickCount == 2*/ && _initialize == false && _child != null)
            {
                Reset();
            }
        }

        private void PanAndZoomBorder_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                if (_initialize == false && _child != null)
                {
                    _isCaptured = false;
                    Cursor = new Cursor(StandardCursorType.Arrow);
                }
            }
        }

        private void PanAndZoomBorder_PointerMoved(object sender, PointerEventArgs e)
        {
            if (_initialize == false && _child != null && _isCaptured)
            {
                var tt = GetTranslateTransform(_child);
                Vector v = _start - e.GetPosition(this);
                tt.X = _origin.X - v.X;
                tt.Y = _origin.Y - v.Y;
            }
        }
    }

    public class DrawingContextRenderer
    {
        private PenLineCap ToPenLineCap(LineCap lineCap)
        {
            switch (lineCap)
            {
                default:
                case LineCap.Flat:
                    return PenLineCap.Flat;
                case LineCap.Square:
                    return PenLineCap.Square;
                case LineCap.Round:
                    return PenLineCap.Round;
            }
        }

        private Color ToColor(ArgbColor color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public void DrawLine(DrawingContext dc, LineShape lineShape)
        {
            if (lineShape.Stroke == null)
            {
                return;
            }

            var brush = new SolidColorBrush(ToColor(lineShape.Stroke));

            var pen = new Pen(brush, lineShape.StrokeThickness)
            {
                Brush = brush,
                Thickness = lineShape.StrokeThickness,
                LineCap = ToPenLineCap(lineShape.StartLineCap)
            };

            var point0 = new Point(lineShape.Point1.X, lineShape.Point1.Y);
            var point1 = new Point(lineShape.Point2.X, lineShape.Point2.Y);

            dc.DrawLine(pen, point0, point1);
        }

        public void DrawRectangle(DrawingContext dc, RectangleShape rectangleShape)
        {
            if (rectangleShape.Stroke == null)
            {
                return;
            }

            var brush = new SolidColorBrush(ToColor(rectangleShape.Stroke));

            var pen = new Pen(brush, rectangleShape.StrokeThickness)
            {
                Brush = brush,
                Thickness = rectangleShape.StrokeThickness,
                LineCap = PenLineCap.Round
            };

            var point1 = new Point(rectangleShape.TopLeft.X, rectangleShape.TopLeft.Y);
            var point2 = new Point(rectangleShape.BottomRight.X, rectangleShape.BottomRight.Y);

            var rectangle = new Rect(point1, point2);

            dc.DrawRectangle(null, pen, rectangle);
        }

        public void DrawPolygon(DrawingContext dc, PolygonShape polygonShape)
        {
            if (polygonShape.Stroke == null)
            {
                return;
            }

            var brush = new SolidColorBrush(ToColor(polygonShape.Stroke));

            var pen = new Pen(brush, polygonShape.StrokeThickness)
            {
                Brush = brush,
                Thickness = polygonShape.StrokeThickness,
                LineCap = ToPenLineCap(polygonShape.StartLineCap)
            };

            var points = polygonShape.Points;
            var length = polygonShape.Points.Length;

            for (int i = 0; i < length; i++)
            {
                var index0 = i;
                var index1 = (i == length - 1) ? 0 : i + 1;
                var point0 = new Point(points[index0].X, points[index0].Y);
                var point1 = new Point(points[index1].X, points[index1].Y);

                dc.DrawLine(pen, point0, point1);
            }
        }

        public void DrawBackground(DrawingContext dc, CanvasShape canvasShape)
        {
            if (canvasShape.Background == null)
            {
                return;
            }

            var brush = new SolidColorBrush(ToColor(canvasShape.Background));

            var rectangle = new Rect(0, 0, canvasShape.Width, canvasShape.Height);

            dc.DrawRectangle(brush, null, rectangle);
        }

        public void DrawCanvasShape(DrawingContext dc, CanvasShape canvasShape)
        {
            foreach (var shape in canvasShape.Children)
            {
                if (shape is LineShape lineShape)
                {
                    DrawLine(dc, lineShape);
                }
                else if (shape is RectangleShape rectangleShape)
                {
                    DrawRectangle(dc, rectangleShape);
                }
                else if (shape is PolygonShape polygonShape)
                {
                    DrawPolygon(dc, polygonShape);
                }
            }
        }
    }

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
