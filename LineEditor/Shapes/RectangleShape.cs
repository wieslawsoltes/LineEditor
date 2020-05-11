
namespace LineEditor
{
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
}
