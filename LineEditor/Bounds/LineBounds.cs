#define DRAW_HITTEST
#define DRAW_BOUNDS
using System;

namespace LineEditor
{
    public class LineBounds : BoundsBase
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

        public override void Update()
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

        public override bool IsVisible()
        {
            return _isVisible;
        }

        public override void Show()
        {
            if (!_isVisible)
            {
#if DRAW_HITTEST
                _canvasShape.Children.Add(_linePolygon);
                _canvasShape.Children.Add(_point1Polygon);
                _canvasShape.Children.Add(_point2Polygon);
#endif
#if DRAW_BOUNDS
                _canvasShape.Children.Add(_rectangleShape); 
#endif
                _isVisible = true;
            }
        }

        public override void Hide()
        {
            if (_isVisible)
            {
#if DRAW_HITTEST
                _canvasShape.Children.Remove(_linePolygon);
                _canvasShape.Children.Remove(_point1Polygon);
                _canvasShape.Children.Remove(_point2Polygon);
#endif
#if DRAW_BOUNDS
                _canvasShape.Children.Remove(_rectangleShape); 
#endif
                _isVisible = false;
            }
        }

        public override bool Contains(double x, double y)
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

        public override void Move(double dx, double dy)
        {
            switch (_hitResult)
            {
                case HitResult.Point1:
                    {
                        double x1 = _lineShape.Point1.X - dx;
                        double y1 = _lineShape.Point1.Y - dy;
                        _lineShape.Point1.X = _canvasShape.EnableSnap ? _canvasShape.Snap(x1, _canvasShape.SnapX) : x1;
                        _lineShape.Point1.Y = _canvasShape.EnableSnap ? _canvasShape.Snap(y1, _canvasShape.SnapY) : y1;
                        _lineShape.Point1 = _lineShape.Point1;
                    }
                    break;
                case HitResult.Point2:
                    {
                        double x2 = _lineShape.Point2.X - dx;
                        double y2 = _lineShape.Point2.Y - dy;
                        _lineShape.Point2.X = _canvasShape.EnableSnap ? _canvasShape.Snap(x2, _canvasShape.SnapX) : x2;
                        _lineShape.Point2.Y = _canvasShape.EnableSnap ? _canvasShape.Snap(y2, _canvasShape.SnapY) : y2;
                        _lineShape.Point2 = _lineShape.Point2;
                    }
                    break;
                case HitResult.Line:
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
                    break;
            }
        }
    }
}
