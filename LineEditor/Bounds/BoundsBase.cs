
namespace LineEditor
{
    public abstract class BoundsBase
    {
        public abstract void Update();
        public abstract bool IsVisible();
        public abstract void Show();
        public abstract void Hide();
        public abstract bool Contains(double x, double y);
        public abstract void Move(double dx, double dy);
    }
}
