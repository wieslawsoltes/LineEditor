
namespace LineEditor
{
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
}
