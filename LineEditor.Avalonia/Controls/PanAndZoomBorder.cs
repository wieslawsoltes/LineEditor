using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace LineEditor.Avalonia
{
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
                if (value != null && value != _child)
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
                double zoom = e.Delta.Y > 0 ? .2 : -.2;
                if (!(e.Delta.Y > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
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
}
