
namespace LineEditor
{
    public class LineShape : BaseShape
    {
        public PointShape Point1 { get; set; }

        public PointShape Point2 { get; set; }

        public ArgbColor Stroke { get; set; }

        public double StrokeThickness { get; set; }

        public LineCap LineCap { get; set; }

        public LineShape()
        {
            Point1 = new PointShape(0.0, 0.0);
            Point2 = new PointShape(0.0, 0.0);
            Stroke = new ArgbColor(0xFF, 0x00, 0x00, 0x00);
            StrokeThickness = 30.0;
            LineCap = LineCap.Square;
        }
    }
}
