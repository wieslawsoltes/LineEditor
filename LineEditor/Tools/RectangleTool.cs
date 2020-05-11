using System;
using System.Linq;
using System.Reactive.Linq;

namespace LineEditor
{
    public class RectangleTool : IDisposable
    {
        private enum State { None, TopLeft, BottomRight }
        private State _state = State.None;
        private CanvasShape _drawingCanvas;
        private CanvasShape _boundsCanvas;
        private RectangleShape _rectangleShape;
        private IDisposable _downs;
        private IDisposable _drags;

        public bool IsEnabled { get; set; }

        public RectangleTool(CanvasShape drawingCanvas, CanvasShape boundsCanvas)
        {
            _drawingCanvas = drawingCanvas;
            _boundsCanvas = boundsCanvas;

            var moves = _drawingCanvas.Moves.Where(_ => _drawingCanvas.IsCaptured?.Invoke() == true);
            var drags = Observable.Merge(_drawingCanvas.Downs, _drawingCanvas.Ups, moves);

            _downs = _drawingCanvas.Downs.Where(_ => IsEnabled).Subscribe(p =>
            {
                if (_drawingCanvas.IsCaptured?.Invoke() == true)
                {
                    //_rectangleShape.Bounds.Hide();
                    _drawingCanvas.InvalidateShape?.Invoke();
                    _boundsCanvas.InvalidateShape?.Invoke();
                    _state = State.None;
                    _drawingCanvas.ReleaseCapture?.Invoke();
                }
                else
                {
                    _rectangleShape = new RectangleShape();
                    _rectangleShape.TopLeft.X = p.X;
                    _rectangleShape.TopLeft.Y = p.Y;
                    _rectangleShape.BottomRight.X = p.X;
                    _rectangleShape.BottomRight.Y = p.Y;

                    _drawingCanvas.Children.Add(_rectangleShape);
                    //_rectangleShape.Bounds = new RectangleBounds(_boundsCanvas, _rectangleShape);
                    //_rectangleShape.Bounds.Update();
                    //_rectangleShape.Bounds.Show();
                    _drawingCanvas.Capture?.Invoke();
                    _drawingCanvas.InvalidateShape?.Invoke();
                    _boundsCanvas.InvalidateShape?.Invoke();
                    _state = State.BottomRight;
                }
            });

            _drags = drags.Where(_ => IsEnabled).Subscribe(p =>
            {
                if (_state == State.BottomRight)
                {
                    _rectangleShape.BottomRight.X = p.X;
                    _rectangleShape.BottomRight.Y = p.Y;
                    //_rectangleShape.Bounds.Update();
                    _drawingCanvas.InvalidateShape?.Invoke();
                    _boundsCanvas.InvalidateShape?.Invoke();
                }
            });
        }

        public void Dispose()
        {
            _downs.Dispose();
            _drags.Dispose();
        }
    }
}
