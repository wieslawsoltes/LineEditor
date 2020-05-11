using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LineEditor.Wpf
{
    public class PanAndZoomBorder : Border
    {
        private bool _isCaptured = false;
        private bool _initialize = true;
        private UIElement _child = null;
        private Point _origin;
        private Point _start;

        private TranslateTransform GetTranslateTransform(UIElement element)
        {
            return (TranslateTransform)((TransformGroup)element.RenderTransform).Children.First(tr => tr is TranslateTransform);
        }

        private ScaleTransform GetScaleTransform(UIElement element)
        {
            return (ScaleTransform)((TransformGroup)element.RenderTransform).Children.First(tr => tr is ScaleTransform);
        }

        public override UIElement Child
        {
            get { return base.Child; }
            set
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
                        _child.RenderTransformOrigin = new Point(0.0, 0.0);
                        MouseWheel += Border_MouseWheel;
                        MouseRightButtonDown += Border_MouseRightButtonDown;
                        MouseRightButtonUp += Border_MouseRightButtonUp;
                        MouseMove += Border_MouseMove;
                        PreviewMouseDown += Border_PreviewMouseDown;
                        _initialize = false;
                    }
                }
                base.Child = value;
            }
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

        private void Border_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_initialize == false && _child != null)
            {
                var st = GetScaleTransform(_child);
                var tt = GetTranslateTransform(_child);
                double zoom = e.Delta > 0 ? .2 : -.2;
                if (!(e.Delta > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
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

        private void Border_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_initialize == false && _child != null)
            {
                var tt = GetTranslateTransform(_child);
                _start = e.GetPosition(this);
                _origin = new Point(tt.X, tt.Y);
                Cursor = Cursors.Hand;
                _child.CaptureMouse();
                _isCaptured = true;
            }
        }

        private void Border_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_initialize == false && _child != null)
            {
                _child.ReleaseMouseCapture();
                _isCaptured = false;
                Cursor = Cursors.Arrow;
            }
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            if (_initialize == false && _child != null && _isCaptured /*_child.IsMouseCaptured*/)
            {
                var tt = GetTranslateTransform(_child);
                Vector v = _start - e.GetPosition(this);
                tt.X = _origin.X - v.X;
                tt.Y = _origin.Y - v.Y;
            }
        }

        private void Border_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle && e.ClickCount == 2 && _initialize == false && _child != null)
            {
                Reset();
            }
        }
    }
}
