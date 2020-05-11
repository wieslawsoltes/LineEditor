using System;
using System.Linq;
using System.Reactive.Linq;

namespace LineEditor
{
    public class LineTool : IDisposable
    {
        private enum State { None, Start, End }
        private State _state = State.None;
        private CanvasShape _drawingCanvas;
        private CanvasShape _boundsCanvas;
        private LineShape _lineShape;
        private IDisposable _downs;
        private IDisposable _drags;

        public bool IsEnabled { get; set; }

        public LineTool(CanvasShape drawingCanvas, CanvasShape boundsCanvas)
        {
            _drawingCanvas = drawingCanvas;
            _boundsCanvas = boundsCanvas;

            var moves = _drawingCanvas.Moves.Where(_ => _drawingCanvas.IsCaptured?.Invoke() == true);
            var drags = Observable.Merge(_drawingCanvas.Downs, _drawingCanvas.Ups, moves);

            _downs = _drawingCanvas.Downs.Where(_ => IsEnabled).Subscribe(p =>
            {
                if (_drawingCanvas.IsCaptured?.Invoke() == true)
                {
                    _lineShape.Bounds.Hide();
                    _drawingCanvas.InvalidateShape?.Invoke();
                    _boundsCanvas.InvalidateShape?.Invoke();
                    _state = State.None;
                    _drawingCanvas.ReleaseCapture?.Invoke();
                }
                else
                {
                    _lineShape = new LineShape();
                    _lineShape.Point1.X = p.X;
                    _lineShape.Point1.Y = p.Y;
                    _lineShape.Point2.X = p.X;
                    _lineShape.Point2.Y = p.Y;

                    _drawingCanvas.Children.Add(_lineShape);
                    _lineShape.Bounds = new LineBounds(_boundsCanvas, _lineShape);
                    _lineShape.Bounds.Update();
                    _lineShape.Bounds.Show();
                    _drawingCanvas.Capture?.Invoke();
                    _drawingCanvas.InvalidateShape?.Invoke();
                    _boundsCanvas.InvalidateShape?.Invoke();
                    _state = State.End;
                }
            });

            _drags = drags.Where(_ => IsEnabled).Subscribe(p =>
            {
                if (_state == State.End)
                {
                    _lineShape.Point2.X = p.X;
                    _lineShape.Point2.Y = p.Y;
                    _lineShape.Bounds.Update();
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
