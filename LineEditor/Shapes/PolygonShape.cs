
namespace LineEditor
{
    public class PolygonShape : BaseShape
    {
        public PointShape[] Points { get; set; }

        public ArgbColor Stroke { get; set; }

        public double StrokeThickness { get; set; }

        public LineCap LineCap { get; set; }

        public PolygonShape()
        {
            Stroke = new ArgbColor(0xFF, 0x00, 0xBF, 0xFF);
            StrokeThickness = 2.0;
            LineCap = LineCap.Round;
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
}
