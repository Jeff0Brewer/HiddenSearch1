using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EyeXFramework.Wpf;
using Tobii.EyeX.Framework;
using EyeXFramework;

namespace HiddenSearch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    ///

    public partial class MainWindow : Window
    {
        private System.Windows.Threading.DispatcherTimer dispatcherTimer;

        Point fixationTrack = new Point(0, 0);
        Point fastTrack = new Point(0, 0);
        double fixationStart = 0;
        bool fixStart = true;
        bool fixShift = false;
        double fadeTimer = 0;

        EyeXHost eyeXHost = new EyeXHost();

        public MainWindow()
        {
            InitializeComponent();

            eyeXHost.Start();
            //var gazeData = eyeXHost.CreateGazePointDataStream(GazePointDataMode.LightlyFiltered);
            var fixationData = eyeXHost.CreateFixationDataStream(FixationDataMode.Sensitive);
            var gazeData = eyeXHost.CreateGazePointDataStream(GazePointDataMode.LightlyFiltered);
            fixationData.Next += fixTrack;
            gazeData.Next += trackDot;

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Render);
            dispatcherTimer.Tick += new EventHandler(update);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 20);
            dispatcherTimer.Start();
        }


        private void fixTrack(object s, EyeXFramework.FixationEventArgs e)
        {
            if (e.EventType == FixationDataEventType.Begin)
            {
                fixationStart = e.Timestamp;
            }
            double fixationtime = e.Timestamp - fixationStart;
            if (fixationtime > 700 & fixStart)
            {
                fixationTrack = new Point(e.X, e.Y);
                fixShift = true;
                fixStart = false;
            }

            if (e.EventType == FixationDataEventType.End)
            {
                fixStart = true;
            }
        }

        private void trackDot(object s, EyeXFramework.GazePointEventArgs e)
        {
            fastTrack = new Point(e.X, e.Y);
        }

        void update(object sender, EventArgs e)
        {
            
            

            if (fixShift & fixationTrack.X != double.NaN & fixationTrack.Y != double.NaN)
            {
                fixationTrack = PointFromScreen(fixationTrack);
                //Canvas.SetLeft(track1, Canvas.GetLeft(track0));
                //Canvas.SetTop(track1, Canvas.GetTop(track0));
                Canvas.SetLeft(track0, fixationTrack.X);
                Canvas.SetTop(track0, fixationTrack.Y);
                trackLine.X1 = fixationTrack.X + 5;
                trackLine.Y1 = fixationTrack.Y + 5;
                fadeTimer = 150;
                //trackLine.X2 = Canvas.GetLeft(track1) + 10;
                //trackLine.Y2 = Canvas.GetTop(track1) + 10;
                fixShift = false;

            }
            fastTrack = PointFromScreen(fastTrack);
            track0.Opacity = fadeTimer / 150;
            trackLine.Opacity = fadeTimer / 150;
            fadeTimer--;
            double left = Canvas.GetLeft(track1);
            double top = Canvas.GetTop(track1);
            Canvas.SetLeft(track1, (fastTrack.X - left) / 1.3 + left);
            Canvas.SetTop(track1, (fastTrack.Y - top) / 1.3 + top);
            trackLine.X2 = Canvas.GetLeft(track1) + 5;
            trackLine.Y2 = Canvas.GetTop(track1) + 5;
        }

        private void itemClicked(object sender, MouseButtonEventArgs e)
        {
            Rectangle box = sender as Rectangle;
            Rectangle key = FindName("s" + box.Name) as Rectangle;
            box.Opacity = .5;
            key.Opacity = .8;
        }
    }
}
