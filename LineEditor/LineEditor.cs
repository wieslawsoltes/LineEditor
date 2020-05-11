using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace LineEditor
{
    public struct Point2
    {
        public double X { get; private set; }

        public double Y { get; private set; }

        public Point2(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public class ArgbColor
    {
        public byte A { get; set; }

        public byte R { get; set; }

        public byte G { get; set; }

        public byte B { get; set; }

        public ArgbColor(byte a, byte r, byte g, byte b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }
    }

    public enum LineCap
    {
        Flat = 0,
        Square = 1,
        Round = 2,
        Triangle = 3
    }

    public interface IBounds
    {
        void Update();
        bool IsVisible();
        void Show();
        void Hide();
        bool Contains(double x, double y);
        void Move(double dx, double dy);
    }

    public abstract class BaseShape
    {
        public IBounds Bounds { get; set; }
    }

    public class PointShape : BaseShape
    {
        public double X { get; set; }

        public double Y { get; set; }

        public PointShape(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static bool operator <(PointShape p1, PointShape p2)
        {
            return p1.X < p2.X || (p1.X == p2.X && p1.Y < p2.Y);
        }

        public static bool operator >(PointShape p1, PointShape p2)
        {
            return p1.X > p2.X || (p1.X == p2.X && p1.Y > p2.Y);
        }

        public int CompareTo(PointShape other)
        {
            return (this > other) ? -1 : ((this < other) ? 1 : 0);
        }
    }

    public class LineShape : BaseShape
    {
        public PointShape Point1 { get; set; }

        public PointShape Point2 { get; set; }

        public ArgbColor Stroke { get; set; }

        public double StrokeThickness { get; set; }

        public LineCap StartLineCap { get; set; }

        public LineCap EndLineCap { get; set; }

        public LineShape()
        {
            Point1 = new PointShape(0.0, 0.0);
            Point2 = new PointShape(0.0, 0.0);
            Stroke = new ArgbColor(0xFF, 0x00, 0x00, 0x00);
            StrokeThickness = 30.0;
            StartLineCap = LineCap.Square;
            EndLineCap = LineCap.Square;
        }
    }

    public class RectangleShape : BaseShape
    {
        public PointShape TopLeft { get; set; }

        public PointShape BottomRight { get; set; }

        public ArgbColor Stroke { get; set; }

        public double StrokeThickness { get; set; }

        public RectangleShape()
        {
            TopLeft = new PointShape(0.0, 0.0);
            BottomRight = new PointShape(0.0, 0.0);
            Stroke = new ArgbColor(0xFF, 0xBF, 0x00, 0xFF);
            StrokeThickness = 2.0;
        }
    }

    public class PolygonShape : BaseShape
    {
        public PointShape[] Points { get; set; }

        public ArgbColor Stroke { get; set; }

        public double StrokeThickness { get; set; }

        public LineCap StartLineCap { get; set; }

        public LineCap EndLineCap { get; set; }

        public PolygonShape()
        {
            Stroke = new ArgbColor(0xFF, 0x00, 0xBF, 0xFF);
            StrokeThickness = 2.0;
            StartLineCap = LineCap.Round;
            EndLineCap = LineCap.Round;
        }

        public bool Contains(double x, double y)
        {
            bool contains = false;
            for (int i = 0, j = Points.Length - 1; i < Points.Length; j = i++)
            {
                if (((Points[i].Y > y) != (Points[j].Y > y))
                    && (x < (Points[j].X - Points[i].X) * (y - Points[i].Y) / (Points[j].Y - Points[i].Y) + Points[i].X))
                {
                    contains = !contains;
                }
            }
            return contains;
        }
    }

    public class CanvasShape : BaseShape
    {
        public double Width { get; set; }

        public double Height { get; set; }

        public ArgbColor Background { get; set; }

        public IList<BaseShape> Children { get; set; }

        public bool EnableSnap { get; set; }

        public double SnapX { get; set; }

        public double SnapY { get; set; }

        public Func<bool> IsCaptured { get; set; }

        public Action Capture { get; set; }

        public Action ReleaseCapture { get; set; }

        public Action InvalidateShape { get; set; }

        public IObservable<Point2> Downs { get; set; }

        public IObservable<Point2> Ups { get; set; }

        public IObservable<Point2> Moves { get; set; }

        public CanvasShape()
        {
            Width = 600.0;
            Height = 600.0;
            //Background = new ArgbColor(0x00, 0xFF, 0xFF, 0xFF);
            Background = null;
            Children = new ObservableCollection<BaseShape>();
            SnapX = 15.0;
            SnapY = 15.0;
            EnableSnap = true;
        }

        public double Snap(double val, double snap)
        {
            double r = val % snap;
            return r >= snap / 2.0 ? val + snap - r : val - r;
        }
    }

    public class LineBounds : IBounds
    {
        private enum HitResult { None, Point1, Point2, Line };
        private HitResult _hitResult;
        private LineShape _lineShape;
        private CanvasShape _canvasShape;
        private PolygonShape _linePolygon;
        private PolygonShape _point1Polygon;
        private PolygonShape _point2Polygon;
        private RectangleShape _rectangleShape;
        private bool _isVisible;

        public LineBounds(CanvasShape canvasShape, LineShape lineShape)
        {
            _lineShape = lineShape;
            _canvasShape = canvasShape;
            _hitResult = HitResult.None;
            _linePolygon = CreatePolygon(4);
            _point1Polygon = CreatePolygon(4);
            _point2Polygon = CreatePolygon(4);
            _rectangleShape = new RectangleShape();
        }

        private PolygonShape CreatePolygon(int count)
        {
            var points = new PointShape[count];

            for (int i = 0; i < count; i++)
            {
                points[i] = new PointShape(0, 0);
            }

            var polygon = new PolygonShape
            {
                Points = points
            };

            return polygon;
        }

        private void ExpandPoint(PointShape point, PolygonShape polygonShape, double thickness)
        {
            var x = point.X - (thickness / 2.0);
            var y = point.Y - (thickness / 2.0);
            polygonShape.Points[0].X = x;
            polygonShape.Points[0].Y = y;
            polygonShape.Points[1].X = x + thickness;
            polygonShape.Points[1].Y = y;
            polygonShape.Points[2].X = x + thickness;
            polygonShape.Points[2].Y = y + thickness;
            polygonShape.Points[3].X = x;
            polygonShape.Points[3].Y = y + thickness;
        }

        private double Angle(PointShape point0, PointShape point1)
        {
            return Math.Atan2(point0.Y - point1.Y, point0.X - point1.X);
        }

        private void Rotate(PointShape point, double radians, PointShape center)
        {
            var x = (point.X - center.X) * Math.Cos(radians) - (point.Y - center.Y) * Math.Sin(radians) + center.X;
            var y = (point.X - center.X) * Math.Sin(radians) + (point.Y - center.Y) * Math.Cos(radians) + center.Y;
            point.X = x;
            point.Y = y;
        }

        public void Update()
        {
            ExpandPoint(_lineShape.Point1, _point1Polygon, _lineShape.StrokeThickness);
            ExpandPoint(_lineShape.Point2, _point2Polygon, _lineShape.StrokeThickness);

            var radians = Angle(_lineShape.Point1, _lineShape.Point2);

            Rotate(_point1Polygon.Points[0], radians, _lineShape.Point1);
            Rotate(_point1Polygon.Points[1], radians, _lineShape.Point1);
            Rotate(_point1Polygon.Points[2], radians, _lineShape.Point1);
            Rotate(_point1Polygon.Points[3], radians, _lineShape.Point1);

            Rotate(_point2Polygon.Points[0], radians, _lineShape.Point2);
            Rotate(_point2Polygon.Points[1], radians, _lineShape.Point2);
            Rotate(_point2Polygon.Points[2], radians, _lineShape.Point2);
            Rotate(_point2Polygon.Points[3], radians, _lineShape.Point2);

            _linePolygon.Points[0].X = _point1Polygon.Points[1].X;
            _linePolygon.Points[0].Y = _point1Polygon.Points[1].Y;
            _linePolygon.Points[1].X = _point1Polygon.Points[2].X;
            _linePolygon.Points[1].Y = _point1Polygon.Points[2].Y;
            _linePolygon.Points[2].X = _point2Polygon.Points[3].X;
            _linePolygon.Points[2].Y = _point2Polygon.Points[3].Y;
            _linePolygon.Points[3].X = _point2Polygon.Points[0].X;
            _linePolygon.Points[3].Y = _point2Polygon.Points[0].Y;

            double tlx = double.MaxValue;
            double tly = double.MaxValue;
            double brx = double.MinValue;
            double bry = double.MinValue;

            foreach (var point in _linePolygon.Points)
            {
                tlx = Math.Min(tlx, point.X);
                tly = Math.Min(tly, point.Y);
                brx = Math.Max(brx, point.X);
                bry = Math.Max(bry, point.Y);
            }

            _rectangleShape.TopLeft.X = tlx;
            _rectangleShape.TopLeft.Y = tly;
            _rectangleShape.BottomRight.X = brx;
            _rectangleShape.BottomRight.Y = bry;
        }

        public bool IsVisible()
        {
            return _isVisible;
        }

        public void Show()
        {
            if (!_isVisible)
            {
#if true
                _canvasShape.Children.Add(_linePolygon);
                _canvasShape.Children.Add(_point1Polygon);
                _canvasShape.Children.Add(_point2Polygon);
#endif
                _canvasShape.Children.Add(_rectangleShape);
                _isVisible = true;
            }
        }

        public void Hide()
        {
            if (_isVisible)
            {
#if true
                _canvasShape.Children.Remove(_linePolygon);
                _canvasShape.Children.Remove(_point1Polygon);
                _canvasShape.Children.Remove(_point2Polygon);
#endif
                _canvasShape.Children.Remove(_rectangleShape);
                _isVisible = false;
            }
        }

        public bool Contains(double x, double y)
        {
            if (_point1Polygon.Contains(x, y))
            {
                _hitResult = HitResult.Point1;
                return true;
            }
            else if (_point2Polygon.Contains(x, y))
            {
                _hitResult = HitResult.Point2;
                return true;
            }
            else if (_linePolygon.Contains(x, y))
            {
                _hitResult = HitResult.Line;
                return true;
            }
            _hitResult = HitResult.None;
            return false;
        }

        public void Move(double dx, double dy)
        {
            switch (_hitResult)
            {
                case HitResult.Point1:
                    MovePoint1(dx, dy);
                    break;
                case HitResult.Point2:
                    MovePoint2(dx, dy);
                    break;
                case HitResult.Line:
                    MoveLine(dx, dy);
                    break;
            }
        }

        private void MovePoint1(double dx, double dy)
        {
            double x1 = _lineShape.Point1.X - dx;
            double y1 = _lineShape.Point1.Y - dy;
            _lineShape.Point1.X = _canvasShape.EnableSnap ? _canvasShape.Snap(x1, _canvasShape.SnapX) : x1;
            _lineShape.Point1.Y = _canvasShape.EnableSnap ? _canvasShape.Snap(y1, _canvasShape.SnapY) : y1;
            _lineShape.Point1 = _lineShape.Point1;
        }

        private void MovePoint2(double dx, double dy)
        {
            double x2 = _lineShape.Point2.X - dx;
            double y2 = _lineShape.Point2.Y - dy;
            _lineShape.Point2.X = _canvasShape.EnableSnap ? _canvasShape.Snap(x2, _canvasShape.SnapX) : x2;
            _lineShape.Point2.Y = _canvasShape.EnableSnap ? _canvasShape.Snap(y2, _canvasShape.SnapY) : y2;
            _lineShape.Point2 = _lineShape.Point2;
        }

        private void MoveLine(double dx, double dy)
        {
            double x1 = _lineShape.Point1.X - dx;
            double y1 = _lineShape.Point1.Y - dy;
            double x2 = _lineShape.Point2.X - dx;
            double y2 = _lineShape.Point2.Y - dy;
            _lineShape.Point1.X = _canvasShape.EnableSnap ? _canvasShape.Snap(x1, _canvasShape.SnapX) : x1;
            _lineShape.Point1.Y = _canvasShape.EnableSnap ? _canvasShape.Snap(y1, _canvasShape.SnapY) : y1;
            _lineShape.Point2.X = _canvasShape.EnableSnap ? _canvasShape.Snap(x2, _canvasShape.SnapX) : x2;
            _lineShape.Point2.Y = _canvasShape.EnableSnap ? _canvasShape.Snap(y2, _canvasShape.SnapY) : y2;
            _lineShape.Point1 = _lineShape.Point1;
            _lineShape.Point2 = _lineShape.Point2;
        }
    }

    public class SelectionTool : IDisposable
    {
        [Flags]
        private enum State
        {
            None = 0,
            Hover = 1,
            Selected = 2,
            Move = 4,
            HoverSelected = Hover | Selected,
            HoverMove = Hover | Move,
            SelectedMove = Selected | Move
        }
        private State _state = State.None;
        private CanvasShape _drawingCanvas;
        private CanvasShape _boundsCanvas;
        private Point2 _original;
        private Point2 _start;
        private BaseShape _selected;
        private BaseShape _hover;
        private IDisposable _downs;
        private IDisposable _ups;
        private IDisposable _drag;
        private bool _isEnabled;

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (_isEnabled)
                {
                    Reset();
                }
                _isEnabled = value;
            }
        }

        public SelectionTool(CanvasShape drawingCanvas, CanvasShape boundsCanvas)
        {
            _drawingCanvas = drawingCanvas;
            _boundsCanvas = boundsCanvas;

            var drags = Observable.Merge(_drawingCanvas.Downs, _drawingCanvas.Ups, _drawingCanvas.Moves);

            _downs = _drawingCanvas.Downs.Where(_ => IsEnabled).Subscribe(p => Down(p));
            _ups = _drawingCanvas.Ups.Where(_ => IsEnabled).Subscribe(p => Up(p));
            _drag = drags.Where(_ => IsEnabled).Subscribe(p => Drag(p));
        }

        private BaseShape HitTest(IList<BaseShape> children, double x, double y)
        {
            return children.Where(c => c.Bounds != null && c.Bounds.Contains(x, y)).FirstOrDefault();
        }

        private bool IsState(State state)
        {
            return (_state & state) == state;
        }

        private void Down(Point2 p)
        {
            bool render = false;

            if (IsState(State.Selected))
            {
                HideSelected();
                render = true;
            }

            if (IsState(State.Hover))
            {
                HideHover();
                render = true;
            }

            _selected = HitTest(_drawingCanvas.Children, p.X, p.Y);
            if (_selected != null)
            {
                ShowSelected();
                InitMove(p);
                _drawingCanvas.Capture?.Invoke();
                render = true;
            }

            if (render)
            {
                _drawingCanvas.InvalidateShape?.Invoke();
                _boundsCanvas.InvalidateShape?.Invoke();
            }
        }

        private void Up(Point2 p)
        {
            if (_drawingCanvas.IsCaptured?.Invoke() == true)
            {
                if (IsState(State.Move))
                {
                    FinishMove(p);
                    _drawingCanvas.ReleaseCapture?.Invoke();
                }
            }
        }

        private void Drag(Point2 p)
        {
            if (_drawingCanvas.IsCaptured?.Invoke() == true)
            {
                if (IsState(State.Move))
                {
                    Move(p);
                }
            }
            else
            {
                bool render = false;
                var result = HitTest(_drawingCanvas.Children, p.X, p.Y);

                if (IsState(State.Hover))
                {
                    if (IsState(State.Selected))
                    {
                        if (_hover != _selected && _hover != result)
                        {
                            HideHover();
                            render = true;
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (result != _hover)
                        {
                            HideHover();
                            render = true;
                        }
                        else
                        {
                            return;
                        }
                    }
                }

                if (result != null)
                {
                    if (IsState(State.Selected))
                    {
                        if (result != _selected)
                        {
                            _hover = result;
                            ShowHover();
                            render = true;
                        }
                    }
                    else
                    {
                        _hover = result;
                        ShowHover();
                        render = true;
                    }
                }

                if (render)
                {
                    _drawingCanvas.InvalidateShape?.Invoke();
                    _boundsCanvas.InvalidateShape?.Invoke();
                }
            }
        }

        private void ShowHover()
        {
            _hover.Bounds.Show();
            _state |= State.Hover;
        }

        private void HideHover()
        {
            _hover.Bounds.Hide();
            _hover = null;
            _state &= ~State.Hover;
        }

        private void ShowSelected()
        {
            _selected.Bounds.Show();
            _state |= State.Selected;
        }

        private void HideSelected()
        {
            _selected.Bounds.Hide();
            _selected = null;
            _state &= ~State.Selected;
        }

        private void InitMove(Point2 p)
        {
            _original = p;
            _start = p;
            _state |= State.Move;
        }

        private void FinishMove(Point2 p)
        {
            _state &= ~State.Move;
        }

        private void Move(Point2 p)
        {
            if (_selected != null)
            {
                double dx = _start.X - p.X;
                double dy = _start.Y - p.Y;
                _start = p;
                _selected.Bounds.Move(dx, dy);
                _selected.Bounds.Update();
                _drawingCanvas.InvalidateShape?.Invoke();
                _boundsCanvas.InvalidateShape?.Invoke();
            }
        }

        private void Reset()
        {
            bool render = false;

            if (_hover != null)
            {
                _hover.Bounds.Hide();
                _hover = null;
                render = true;
            }

            if (_selected != null)
            {
                _selected.Bounds.Hide();
                _selected = null;
                render = true;
            }

            _state = State.None;

            if (render)
            {
                _drawingCanvas.InvalidateShape?.Invoke();
                _boundsCanvas.InvalidateShape?.Invoke();
            }
        }

        public void Dispose()
        {
            _downs.Dispose();
            _ups.Dispose();
            _drag.Dispose();
        }
    }

    public class LineTool : IDisposable
    {
        private enum State { None, Start, End }
        private State _state = State.None;
        private CanvasShape _drawingCanvas;
        private CanvasShape _boundsCanvas;
        private LineShape _lineShape;
        private IDisposable _downs;
        private IDisposable _drags;

        public bool IsEnabled { get; set; }

        public LineTool(CanvasShape drawingCanvas, CanvasShape boundsCanvas)
        {
            _drawingCanvas = drawingCanvas;
            _boundsCanvas = boundsCanvas;

            var moves = _drawingCanvas.Moves.Where(_ => _drawingCanvas.IsCaptured?.Invoke() == true);
            var drags = Observable.Merge(_drawingCanvas.Downs, _drawingCanvas.Ups, moves);

            _downs = _drawingCanvas.Downs.Where(_ => IsEnabled).Subscribe(p =>
            {
                if (_drawingCanvas.IsCaptured?.Invoke() == true)
                {
                    _lineShape.Bounds.Hide();
                    _drawingCanvas.InvalidateShape?.Invoke();
                    _boundsCanvas.InvalidateShape?.Invoke();
                    _state = State.None;
                    _drawingCanvas.ReleaseCapture?.Invoke();
                }
                else
                {
                    _lineShape = new LineShape();
                    _lineShape.Point1.X = p.X;
                    _lineShape.Point1.Y = p.Y;
                    _lineShape.Point2.X = p.X;
                    _lineShape.Point2.Y = p.Y;

                    _drawingCanvas.Children.Add(_lineShape);
                    _lineShape.Bounds = new LineBounds(_boundsCanvas, _lineShape);
                    _lineShape.Bounds.Update();
                    _lineShape.Bounds.Show();
                    _drawingCanvas.Capture?.Invoke();
                    _drawingCanvas.InvalidateShape?.Invoke();
                    _boundsCanvas.InvalidateShape?.Invoke();
                    _state = State.End;
                }
            });

            _drags = drags.Where(_ => IsEnabled).Subscribe(p =>
            {
                if (_state == State.End)
                {
                    _lineShape.Point2.X = p.X;
                    _lineShape.Point2.Y = p.Y;
                    _lineShape.Bounds.Update();
                    _drawingCanvas.InvalidateShape?.Invoke();
                    _boundsCanvas.InvalidateShape?.Invoke();
                }
            });
        }

        public void Dispose()
        {
            _downs.Dispose();
            _drags.Dispose();
        }
    }

    public class RectangleTool : IDisposable
    {
        private enum State { None, TopLeft, BottomRight }
        private State _state = State.None;
        private CanvasShape _drawingCanvas;
        private CanvasShape _boundsCanvas;
        private RectangleShape _rectangleShape;
        private IDisposable _downs;
        private IDisposable _drags;

        public bool IsEnabled { get; set; }

        public RectangleTool(CanvasShape drawingCanvas, CanvasShape boundsCanvas)
        {
            _drawingCanvas = drawingCanvas;
            _boundsCanvas = boundsCanvas;

            var moves = _drawingCanvas.Moves.Where(_ => _drawingCanvas.IsCaptured?.Invoke() == true);
            var drags = Observable.Merge(_drawingCanvas.Downs, _drawingCanvas.Ups, moves);

            _downs = _drawingCanvas.Downs.Where(_ => IsEnabled).Subscribe(p =>
            {
                if (_drawingCanvas.IsCaptured?.Invoke() == true)
                {
                    //_rectangleShape.Bounds.Hide();
                    _drawingCanvas.InvalidateShape?.Invoke();
                    _boundsCanvas.InvalidateShape?.Invoke();
                    _state = State.None;
                    _drawingCanvas.ReleaseCapture?.Invoke();
                }
                else
                {
                    _rectangleShape = new RectangleShape();
                    _rectangleShape.TopLeft.X = p.X;
                    _rectangleShape.TopLeft.Y = p.Y;
                    _rectangleShape.BottomRight.X = p.X;
                    _rectangleShape.BottomRight.Y = p.Y;

                    _drawingCanvas.Children.Add(_rectangleShape);
                    //_rectangleShape.Bounds = new RectangleBounds(_boundsCanvas, _rectangleShape);
                    //_rectangleShape.Bounds.Update();
                    //_rectangleShape.Bounds.Show();
                    _drawingCanvas.Capture?.Invoke();
                    _drawingCanvas.InvalidateShape?.Invoke();
                    _boundsCanvas.InvalidateShape?.Invoke();
                    _state = State.BottomRight;
                }
            });

            _drags = drags.Where(_ => IsEnabled).Subscribe(p =>
            {
                if (_state == State.BottomRight)
                {
                    _rectangleShape.BottomRight.X = p.X;
                    _rectangleShape.BottomRight.Y = p.Y;
                    //_rectangleShape.Bounds.Update();
                    _drawingCanvas.InvalidateShape?.Invoke();
                    _boundsCanvas.InvalidateShape?.Invoke();
                }
            });
        }

        public void Dispose()
        {
            _downs.Dispose();
            _drags.Dispose();
        }
    }

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
