using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace LineEditor
{
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
}
