using System.Windows;
using System.Windows.Media;

namespace LineEditor.Wpf
{
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
            brush.Freeze();

            var pen = new Pen(brush, lineShape.StrokeThickness)
            {
                Brush = brush,
                Thickness = lineShape.StrokeThickness,
                StartLineCap = ToPenLineCap(lineShape.LineCap),
                EndLineCap = ToPenLineCap(lineShape.LineCap)
            };
            pen.Freeze();

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
            brush.Freeze();

            var pen = new Pen(brush, rectangleShape.StrokeThickness)
            {
                Brush = brush,
                Thickness = rectangleShape.StrokeThickness,
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
            };
            pen.Freeze();

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
            brush.Freeze();

            var pen = new Pen(brush, polygonShape.StrokeThickness)
            {
                Brush = brush,
                Thickness = polygonShape.StrokeThickness,
                StartLineCap = ToPenLineCap(polygonShape.LineCap),
                EndLineCap = ToPenLineCap(polygonShape.LineCap)
            };
            pen.Freeze();

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
            brush.Freeze();

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
}
