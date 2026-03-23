using Home_Based_Video_Scheduler_and_Player_as_Television.Models;
using Home_Based_Video_Scheduler_and_Player_as_Television.ViewModels;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Color = System.Windows.Media.Color;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Views
{
    public partial class CommercialBreakView : Window
    {
        private CommercialBreakViewModel _vm;

        public CommercialBreakView()
        {
            InitializeComponent();
            _vm = new CommercialBreakViewModel();
            DataContext = _vm;
            _vm.PropertyChanged += OnVmPropertyChanged;
            Loaded += (s, e) => RedrawTimeline();
            SizeChanged += (s, e) => RedrawTimeline();
        }

        private void OnVmPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CommercialBreakViewModel.BreaksForShow) ||
                e.PropertyName == nameof(CommercialBreakViewModel.SelectedShow) ||
                e.PropertyName == nameof(CommercialBreakViewModel.ShowDurationSeconds))
            {
                RedrawTimeline();

                if (_vm.BreaksForShow != null)
                    _vm.BreaksForShow.CollectionChanged += (s2, e2) => RedrawTimeline();
            }
        }

        private void RedrawTimeline()
        {
            TimelineCanvas.Children.Clear();
            TimeLabelCanvas.Children.Clear();

            double trackW = TimelineCanvas.ActualWidth;
            if (trackW < 10) return;

            double totalSec = _vm.ShowDurationSeconds;
            if (totalSec <= 0) return;

            TrackFill.Width = trackW;

            for (int i = 1; i < 10; i++)
            {
                double x = trackW * i / 10.0;
                var tick = new Line
                {
                    X1 = x, Y1 = 18, X2 = x, Y2 = 34,
                    Stroke = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x44)),
                    StrokeThickness = 1
                };
                TimelineCanvas.Children.Add(tick);

                var secs = totalSec * i / 10.0;
                var ts   = TimeSpan.FromSeconds(secs);
                var lbl  = new TextBlock
                {
                    Text       = ts.ToString(@"h\:mm"),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize   = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0xAA))
                };
                Canvas.SetLeft(lbl, x - 14);
                Canvas.SetTop(lbl, 0);
                TimeLabelCanvas.Children.Add(lbl);
            }

            if (_vm.BreaksForShow == null) return;
            foreach (var bd in _vm.BreaksForShow)
            {
                var b = bd.Break;
                double ratio = b.Offset.TotalSeconds / totalSec;
                double x     = trackW * ratio;

                var line = new Line
                {
                    X1 = x, Y1 = 4, X2 = x, Y2 = 48,
                    Stroke = new SolidColorBrush(Color.FromRgb(0xE8, 0x31, 0x2A)),
                    StrokeThickness = 2
                };
                TimelineCanvas.Children.Add(line);

                var diamond = new Polygon
                {
                    Fill = new SolidColorBrush(Color.FromRgb(0xE8, 0x31, 0x2A)),
                    Points = new PointCollection
                    {
                        new Point(x,     22),
                        new Point(x + 6, 28),
                        new Point(x,     34),
                        new Point(x - 6, 28)
                    }
                };
                diamond.ToolTip = $"[Ad] {b.CommercialTitle}\n@ {b.OffsetDisplay}";
                diamond.Cursor  = Cursors.Hand;
                diamond.Tag     = bd;
                diamond.MouseLeftButtonDown += (s, e) => _vm.SelectedBreak = (CommercialBreakDisplay)((Polygon)s).Tag;
                TimelineCanvas.Children.Add(diamond);

                var label = new TextBlock
                {
                    Text       = b.Offset.ToString(@"h\:mm"),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize   = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xA5, 0x00))
                };
                Canvas.SetLeft(label, x - 10);
                Canvas.SetTop(label, 0);
                TimelineCanvas.Children.Add(label);
            }

            var start = new TextBlock
            {
                Text = "START", FontFamily = new FontFamily("Consolas"),
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0xAA))
            };
            Canvas.SetLeft(start, 0); Canvas.SetTop(start, 0);
            TimeLabelCanvas.Children.Add(start);

            var totalTs = TimeSpan.FromSeconds(totalSec);
            var end = new TextBlock
            {
                Text = totalTs.ToString(@"h\:mm"),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0xAA))
            };
            Canvas.SetRight(end, 0); Canvas.SetTop(end, 0);
            TimeLabelCanvas.Children.Add(end);
        }
    }
}
