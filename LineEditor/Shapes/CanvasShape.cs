using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LineEditor
{
    public class CanvasShape : BaseShape
    {
        public double Width { get; set; }

        public double Height { get; set; }

        public ArgbColor Background { get; set; }

        public IList<BaseShape> Children { get; set; }

        public bool EnableSnap { get; set; }

        public double SnapX { get; set; }

        public double SnapY { get; set; }

        public Func<bool> IsCaptured { get; set; }

        public Action Capture { get; set; }

        public Action ReleaseCapture { get; set; }

        public Action InvalidateShape { get; set; }

        public IObservable<Point2> Downs { get; set; }

        public IObservable<Point2> Ups { get; set; }

        public IObservable<Point2> Moves { get; set; }

        public CanvasShape()
        {
            Width = 600.0;
            Height = 600.0;
            Background = null; //  = new ArgbColor(0x00, 0xFF, 0xFF, 0xFF);
            Children = new ObservableCollection<BaseShape>();
            SnapX = 15.0;
            SnapY = 15.0;
            EnableSnap = true;
        }

        public double Snap(double val, double snap)
        {
            double r = val % snap;
            return r >= snap / 2.0 ? val + snap - r : val - r;
        }
    }
}
