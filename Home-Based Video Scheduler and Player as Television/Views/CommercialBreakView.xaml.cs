using Home_Based_Video_Scheduler_and_Player_as_Television.Models;
using Home_Based_Video_Scheduler_and_Player_as_Television.ViewModels;
using System;
using System.Collections.Specialized;
using Microsoft.Win32;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

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

                // Subscribe to collection changes for live updates
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

            // Fill the track to show full show length
            TrackFill.Width = trackW;

            // Draw tick marks every 10% of duration
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

                // Time label
                var secs = totalSec * i / 10.0;
                var ts   = TimeSpan.FromSeconds(secs);
                var lbl  = new TextBlock
                {
                    Text       = ts.ToString(@"h\:mm"),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize   = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x55))
                };
                Canvas.SetLeft(lbl, x - 14);
                Canvas.SetTop(lbl, 0);
                TimeLabelCanvas.Children.Add(lbl);
            }

            // Draw a marker for each commercial break
            if (_vm.BreaksForShow == null) return;
            foreach (var b in _vm.BreaksForShow)
            {
                double ratio = b.Offset.TotalSeconds / totalSec;
                double x     = trackW * ratio;

                // Vertical line
                var line = new Line
                {
                    X1 = x, Y1 = 4, X2 = x, Y2 = 48,
                    Stroke = new SolidColorBrush(Color.FromRgb(0xE8, 0x31, 0x2A)),
                    StrokeThickness = 2
                };
                TimelineCanvas.Children.Add(line);

                // Diamond marker at mid-track
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
                // Tooltip with commercial name and offset
                diamond.ToolTip = $"[Ad] {b.CommercialTitle}\n@ {b.OffsetDisplay}";
                diamond.Cursor  = Cursors.Hand;
                diamond.Tag     = b;
                diamond.MouseLeftButtonDown += (s, e) => _vm.SelectedBreak = (CommercialBreak)((Polygon)s).Tag;
                TimelineCanvas.Children.Add(diamond);

                // Offset label above the line
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

            // START / END labels
            var start = new TextBlock
            {
                Text = "START", FontFamily = new FontFamily("Consolas"),
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x44))
            };
            Canvas.SetLeft(start, 0); Canvas.SetTop(start, 0);
            TimeLabelCanvas.Children.Add(start);

            var totalTs = TimeSpan.FromSeconds(totalSec);
            var end = new TextBlock
            {
                Text = totalTs.ToString(@"h\:mm"),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x44))
            };
            Canvas.SetRight(end, 0); Canvas.SetTop(end, 0);
            TimeLabelCanvas.Children.Add(end);
        }
    }
}
