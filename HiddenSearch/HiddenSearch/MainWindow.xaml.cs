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

//using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.ComponentModel;
namespace HiddenSearch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    ///
    public partial class MainWindow : Window
    {
        #region Variables

        //SETUP VARIABLES//
        private static string defaultSenderIP = "169.254.50.139"; //169.254.41.115 A, 169.254.50.139 B
        string compID = "B";
        int initialImg = 4; //1 for cats (img1), 2 for caterpillars (img2), 3 for mice (img3), 4 for (img4)

        // bool together = true; //Start together!

        EyeXHost eyeXHost;
        private bool SenderOn = true;
        private bool ReceiverOn = true;
        private static int ReceiverPort = 11000, SenderPort = 11000;//ReceiverPort is the port used by Receiver, SenderPort is the port used by Sender
        private bool communication_started_Receiver = false;//indicates whether the Receiver is ready to receive message(coordinates). Used for thread control
        private bool communication_started_Sender = false;//Indicate whether the program is sending its coordinates to others. Used for thread control
        private System.Threading.Thread communicateThread_Receiver; //Thread for receiver
        private System.Threading.Thread communicateThread_Sender;   //Thread for sender
        private static string SenderIP = "", ReceiverIP = ""; //The IP's for sender and receiver.
        private static string IPpat = @"(\d+)(\.)(\d+)(\.)(\d+)(\.)(\d+)\s+"; // regular expression used for matching ip address
        private Regex r = new Regex(IPpat, RegexOptions.IgnoreCase);//regular expression variable
        private static string NumPat = @"(\d+)\s+";
        private Regex regex_num = new Regex(NumPat, RegexOptions.IgnoreCase);
        private System.Windows.Threading.DispatcherTimer dispatcherTimer;
        private static String sending;
        private static String received;

        int ind_1, ind_2, ind_3, ind_4;
        int stage = 0;
        double t0, t1, t2, t3, t4, t5, t6;
        //int time1, time2, time3;

        TimeSpan timerStart;

        //Fixation vis
        Point fixationTrack = new Point(0, 0);
        Point fastTrack = new Point(0, 0);
        Point otherFixationTrack = new Point(0, 0);
        Point otherFastTrack = new Point(0, 0);
        double fixationStart = 0;
        bool fixStart = true;
        bool fixShift = false;
        double fadeTimer = 0;

        //Double vis
        int shareTime = 0;
        int awayTime = 0;
        bool shareStart = true;
        double shareX, shareY;

        //heatmap
        SolidColorBrush brush = new SolidColorBrush();
        Color orange = Color.FromArgb(255, 255, 128, 0);
        Color yellow = Color.FromArgb(255, 255, 255, 0);
        Color red = Color.FromArgb(255, 255, 0, 0);

        int num_ellipses = 700;
        int ellipse_count = 0;
        Ellipse[] ellipses;
        double ellipse_size = 80;

        //settings
        bool gazeSetting = false;
        bool fixSetting = false;
        bool heatmapSetting = false;

        int wrongClicks = 0;

        //log
        string pathfolder = "C:/Users/master/Documents/gamelog/";
        string path;
        string time;
        string datapoint;
        int timediff;
        #endregion

        public MainWindow()
        {
            if (initialImg == 2)
            {
                Window1 window1 = new Window1(path, compID, defaultSenderIP);
                window1.Show();
                this.Close();
            }
            else if (initialImg == 3)
            {
                Window2 window2 = new Window2(path, compID, defaultSenderIP);
                window2.Show();
                this.Close();
            }
            else if (initialImg == 4)
            {
                Window3 window3 = new Window3(path, compID, defaultSenderIP);
                window3.Show();
                this.Close();
            }
            else
            {
                setupMainWindow();
            }
        }
        private void setupMainWindow()
        {
            DataContext = this;
            InitializeComponent();
            eyeXHost = new EyeXHost();
            eyeXHost.Start();

            //var gazeData = eyeXHost.CreateGazePointDataStream(GazePointDataMode.LightlyFiltered);
            var fixationData = eyeXHost.CreateFixationDataStream(FixationDataMode.Sensitive);
            var gazeData = eyeXHost.CreateGazePointDataStream(GazePointDataMode.LightlyFiltered);
            fixationData.Next += fixTrack;
            gazeData.Next += trackDot;
            initializeHeatmap();

            if (ReceiverOn)
            {
                IPHostEntry ipHostInfo = Dns.GetHostByName(Dns.GetHostName());
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                Receive_Status_Text.Text = "Receiving Data at\nIP:" + ipAddress.ToString();
                Receive_Status_Text.Visibility = Visibility.Visible;
            }
            if (SenderOn)
            {
                SenderIP = defaultSenderIP;
                Share_Status_Text.Text = "Sharing Data to\nIP:" + SenderIP.ToString();
                Share_Status_Text.Visibility = Visibility.Visible;
                communication_started_Sender = false;
            }

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Render);
            dispatcherTimer.Tick += new EventHandler(update);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 20);
            dispatcherTimer.Start();

            setup();
        }
        private void setup()
        {
            Rectangle test = new Rectangle();
            foreach (UIElement child in myCanvas.Children)
            {
                if (Equals(child.GetType(), test.GetType()))
                {
                    child.Visibility = Visibility.Hidden;
                }
            }
            bg.Visibility = Visibility.Visible;
            key.Visibility = Visibility.Visible;
            nextHighlight(System.Windows.Media.Colors.Purple, "mouse");
            initLog();
            t0 = DateTime.Now.TimeOfDay.TotalSeconds;
            timerStart = DateTime.Now.TimeOfDay;
        }

        private void nextHighlight(System.Windows.Media.Color color, String name)
        {
            Rectangle hitem = FindName(name) as Rectangle;
            Rectangle hkey = FindName("s" + name) as Rectangle;
            hkey.Visibility = Visibility.Visible;
            hitem.Visibility = Visibility.Visible;
            hkey.Fill = new SolidColorBrush(color);
            hkey.Opacity = .5;
        }
        private void initLog()
        {
            path = pathfolder + compID + "_" + DateTime.Now.ToString("MM-dd_hh-mm") + ".txt";
            time = DateTime.Now.ToString("hh:mm:ss.ff");
            datapoint = "Starting @ " + time + "\n";
            System.IO.StreamWriter file = new System.IO.StreamWriter(path, true);
            file.WriteLine(datapoint);
            file.Close();
        }
        private void logTime(double currTime, double prevTime)
        {
            timerStart = DateTime.Now.TimeOfDay;
            time = DateTime.Now.ToString("hh:mm:ss.ff");
            timediff = (int)(currTime - prevTime);
            datapoint = "Img1: " + compID + " @ " + time + " - " + timediff.ToString() + "sec\n";
            System.IO.StreamWriter file = new System.IO.StreamWriter(path, true);
            file.WriteLine(datapoint);
            file.Close();
        }

        #region buttons
        private void gazeButton(object sender, RoutedEventArgs e)
        {
            gazeSetting = !gazeSetting;
            if (gazeSetting)
            {
                GazeButton.Content = "Turn off Gazepath";
                otrack0.Visibility = Visibility.Visible;
                otrack1.Visibility = Visibility.Visible;
                otrackLine.Visibility = Visibility.Visible;
            }
            else
            {
                GazeButton.Content = "Turn on Gazepath";
                otrack0.Visibility = Visibility.Hidden;
                otrack1.Visibility = Visibility.Hidden;
                otrackLine.Visibility = Visibility.Hidden;
            }
        }
        private void fixButton(object sender, RoutedEventArgs e)
        {
            fixSetting = !fixSetting;
            if (fixSetting)
            {
                FixButton.Content = "Turn off Fixation";
                doubleHighlight.Visibility = Visibility.Visible;
            }
            else
            {
                FixButton.Content = "Turn on Fixation";
                doubleHighlight.Visibility = Visibility.Hidden;
            }
        }
        private void heatmapButton(object sender, RoutedEventArgs e)
        {
            heatmapSetting = !heatmapSetting;
            if (heatmapSetting)
            {
                HeatmapButton.Content = "Turn off Heatmap";
            }
            else
            {
                HeatmapButton.Content = "Turn on Heatmap";
                for (int i = 0; i < num_ellipses; i++)
                {
                    ellipses[i].Visibility = Visibility.Hidden;
                }
            }
        }
        private void nextImageButton(object sender, RoutedEventArgs e)
        {
            Window1 window1 = new Window1(path, compID, defaultSenderIP);
            window1.Show();
            this.Close();
        }
        #endregion

        private void fixTrack(object s, EyeXFramework.FixationEventArgs e)
        {
            if (e.EventType == FixationDataEventType.Begin)
            {
                fixationStart = e.Timestamp;
            }
            double fixationtime = e.Timestamp - fixationStart;
            if (fixationtime > 150 & fixStart)
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
            string timetemp = DateTime.Now.TimeOfDay.Subtract(timerStart).ToString();
            Timer.Text = timetemp.Substring(0, timetemp.Length - 8);

            sending = ((int)fastTrack.X).ToString() + "|" + ((int)fastTrack.Y).ToString() + ":" + ((int)fixationTrack.X).ToString() + "!" + ((int)fixationTrack.Y).ToString() + "(" + ((int)(100 * track0.Opacity)).ToString();
            //If user pressed Receiver or Cursor button but communication haven't started yet or has terminated, start a thread on tryCommunicateReceiver()
            if (ReceiverOn && communication_started_Receiver == false)
            {
                communication_started_Receiver = true;
                communicateThread_Receiver = new System.Threading.Thread(new ThreadStart(() => tryCommunicateReceiver(sending)));
                communicateThread_Receiver.Start();
            }

            //If user pressed Sender button but communication haven't started yet or has terminated, start a thread on tryCommunicateSender()
            if (SenderOn && communication_started_Sender == false)
            {
                communication_started_Sender = true;
                communicateThread_Sender = new System.Threading.Thread(new ThreadStart(() => tryCommunicateSender(sending)));
                communicateThread_Sender.Start();
            }
            if (received != null)
            {
                test.Text = received.ToString();
                ind_1 = received.IndexOf("|");
                ind_2 = received.IndexOf(":");
                ind_3 = received.IndexOf("!");
                ind_4 = received.IndexOf("(");

                otherFastTrack.X = Convert.ToInt32(received.Substring(0, ind_1));
                otherFastTrack.Y = Convert.ToInt32(received.Substring(ind_1 + 1, ind_2 - ind_1 - 1));
                otherFixationTrack.X = Convert.ToInt32(received.Substring(ind_2 + 1, ind_3 - ind_2 - 1));
                otherFixationTrack.Y = Convert.ToInt32(received.Substring(ind_3 + 1, ind_4 - ind_3 - 1));
                track0.Opacity = Convert.ToInt32(received.Substring(ind_4 + 1, received.Length - ind_4 - 1)) / 100;

                otherFixationTrack = PointFromScreen(otherFixationTrack);
                Canvas.SetLeft(otrack0, otherFixationTrack.X);
                Canvas.SetTop(otrack0, otherFixationTrack.Y);
                otrackLine.X1 = otherFixationTrack.X + 5;
                otrackLine.Y1 = otherFixationTrack.Y + 5;

                otherFastTrack = PointFromScreen(otherFastTrack);
                Canvas.SetLeft(otrack1, otherFastTrack.X);
                Canvas.SetTop(otrack1, otherFastTrack.Y);
                otrackLine.X2 = Canvas.GetLeft(otrack1) + 5;
                otrackLine.Y2 = Canvas.GetTop(otrack1) + 5;
                if (heatmapSetting)
                {
                    addColor(orange, otherFastTrack.X, otherFastTrack.Y, ellipse_size); //heatmap
                }
            }

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

            doubleTrack();
        }

        #region heatmap
        private void addPermColor(Color color, double leftCoord, double topCoord, double size)
        {
            Ellipse fixEllipse = new Ellipse();
            fixEllipse.Height = size;
            fixEllipse.Width = size;
            fixEllipse.Opacity = 0.01;
            brush.Color = color;
            fixEllipse.Fill = brush;
            Canvas.SetLeft(fixEllipse, leftCoord - size / 2);
            Canvas.SetTop(fixEllipse, topCoord - size / 2);
            myCanvas.Children.Add(fixEllipse);
        }
        private void addColor(Color color, double leftCoord, double topCoord, double size)
        {
            if (ellipse_count >= num_ellipses) { ellipse_count = 0; }
            brush.Color = color;
            ellipses[ellipse_count].Visibility = Visibility.Visible;
            Canvas.SetLeft(ellipses[ellipse_count], leftCoord - size / 2);
            Canvas.SetTop(ellipses[ellipse_count], topCoord - size / 2);
            ellipses[ellipse_count].Fill = brush;
            ellipse_count++;
        }
        private void initializeHeatmap()
        {
            //after num_ellipses are set, it'll start replacing the oldest ellipses
            ellipses = new Ellipse[num_ellipses];
            for (int i = 0; i < num_ellipses; i++)
            {
                ellipses[i] = new Ellipse();
                ellipses[i].Width = ellipse_size;
                ellipses[i].Height = ellipse_size;
                ellipses[i].Opacity = 0.01;
                Panel.SetZIndex(ellipses[i], i);
                ellipses[i].Visibility = Visibility.Hidden;
                myCanvas.Children.Add(ellipses[i]);
            }
        }
        #endregion

        private void doubleTrack()
        {
            double distance = Math.Sqrt(Math.Pow(fastTrack.X - otherFastTrack.X, 2) + Math.Pow(fastTrack.Y - otherFastTrack.Y, 2));
            if (distance < 125)
            {
                shareX = (.7 * shareX + .3 * ((fastTrack.X + otherFastTrack.X) / 2));
                shareY = (.7 * shareY + .3 * ((fastTrack.Y + otherFastTrack.Y) / 2));
                shareTime++;
                awayTime = 0;
                doubleHighlight.Width += 25 / shareTime;
                doubleHighlight.Height += 25 / shareTime;
                Canvas.SetLeft(doubleHighlight, shareX - doubleHighlight.Width / 2);
                Canvas.SetTop(doubleHighlight, shareY - doubleHighlight.Height / 2);
                if (shareStart)
                {
                    shareX = (fastTrack.X + otherFastTrack.X) / 2;
                    shareY = (fastTrack.Y + otherFastTrack.Y) / 2;
                    Canvas.SetLeft(doubleHighlight, shareX - doubleHighlight.Width / 2);
                    Canvas.SetTop(doubleHighlight, shareY - doubleHighlight.Height / 2);
                    shareStart = false;
                }
            }
            else
            {
                awayTime++;
                if (awayTime > 10)
                {
                    doubleHighlight.Width = 0;
                    doubleHighlight.Height = 0;
                    shareTime = 0;
                }
                shareStart = true;
            }
        }

        private void itemClicked(object sender, MouseButtonEventArgs e)
        {
            Rectangle box = sender as Rectangle;
            Rectangle key = FindName("s" + box.Name) as Rectangle;
            key.Fill = new SolidColorBrush(System.Windows.Media.Colors.Black);
            box.Opacity = .5;
            key.Opacity = .8;

            if (stage == 0 && box.Name.CompareTo("mouse") == 0)
            {
                nextHighlight(System.Windows.Media.Colors.Purple, "pear");
                t1 = DateTime.Now.TimeOfDay.TotalSeconds;
                logTime(t1, t0);
            }
            else if (stage == 1 && box.Name.CompareTo("pear") == 0)
            {
                nextHighlight(System.Windows.Media.Colors.Purple, "cone");
                t2 = DateTime.Now.TimeOfDay.TotalSeconds;
                logTime(t2, t1);
            }
            else if (stage == 2 && box.Name.CompareTo("cone") == 0)
            {
                t3 = DateTime.Now.TimeOfDay.TotalSeconds;
                logTime(t3, t2);
                if (compID.CompareTo("A") == 0)
                {
                    nextHighlight(System.Windows.Media.Colors.Red, "carrot");
                }
                else
                {
                    nextHighlight(System.Windows.Media.Colors.Blue, "candycane");
                }
            }
            else if (stage == 3)
            {
                if (box.Name.CompareTo("carrot") == 0)
                {
                    nextHighlight(System.Windows.Media.Colors.Red, "fish");
                    t4 = DateTime.Now.TimeOfDay.TotalSeconds;
                    logTime(t4, t3);
                }
                else if (box.Name.CompareTo("candycane") == 0)
                {
                    nextHighlight(System.Windows.Media.Colors.Blue, "shoe");
                    t4 = DateTime.Now.TimeOfDay.TotalSeconds;
                    logTime(t4, t3);
                }
            }
            else if (stage == 4)
            {
                if (box.Name.CompareTo("fish") == 0)
                {
                    nextHighlight(System.Windows.Media.Colors.Red, "pencil");
                    t5 = DateTime.Now.TimeOfDay.TotalSeconds;
                    logTime(t5, t4);
                }
                else if (box.Name.CompareTo("shoe") == 0)
                {
                    nextHighlight(System.Windows.Media.Colors.Blue, "mushroom");
                    t5 = DateTime.Now.TimeOfDay.TotalSeconds;
                    logTime(t5, t4);
                }
            }
            else if (stage == 5)
            {
                if (box.Name.CompareTo("pencil") == 0)
                {
                    t6 = DateTime.Now.TimeOfDay.TotalSeconds;
                    logTime(t6, t5);
                }
                else if (box.Name.CompareTo("mushroom") == 0)
                {
                    t6 = DateTime.Now.TimeOfDay.TotalSeconds;
                    logTime(t6, t5);
                }
            }
            stage++;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            //CleanUp();
            //SenderOn = false;
            //ReceiverOn = false;
            communication_started_Receiver = false;
            communication_started_Sender = false;
            //dispatcherTimer.Stop();
            //eyeXHost.Dispose();
            try
            {
                communicateThread_Receiver.Abort();
                communicateThread_Sender.Abort();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            base.OnClosing(e);

        }

        private void bg_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            wrongClicks++;
            WrongClicks.Text = string.Format("Wrong Clicks: {0:0}", wrongClicks);
        }

        #region Sender/Receiver Methods
        public void tryCommunicateReceiver(String x)
        {
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            ReceiverIP = ipHostInfo.AddressList[0].ToString();

            while (ReceiverIP == "")
            {
                System.Threading.Thread.Sleep(1000);
            }
            AsynchronousSocketListener.StartListening();
        }
        public void tryCommunicateSender(String x)
        {
            while (SenderIP == "")
            {
                System.Threading.Thread.Sleep(1000);
            }
            SynchronousClient.StartClient(x); //start sending info
            communication_started_Sender = false;

            //AsynchronousSocketListener.StartListening();
        }
        public class StateObject
        {
            // Client  socket.
            public Socket workSocket = null;
            // Size of receive buffer.
            public const int BufferSize = 1024;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
            // Received data string.
            public StringBuilder sb = new StringBuilder();
        }
        //THis is the Receiver function (Asyncronous)
        // Citation: https://msdn.microsoft.com/en-us/library/fx6588te%28v=vs.110%29.aspx
        public class AsynchronousSocketListener
        {
            // Thread signal.
            public static ManualResetEvent allDone = new ManualResetEvent(false);
            public AsynchronousSocketListener()
            {
            }
            public static void StartListening()
            {
                if (ReceiverIP != "")
                {
                    // Data buffer for incoming data.
                    byte[] bytes = new Byte[1024];

                    // Establish the local endpoint for the socket.
                    // The DNS name of the computer
                    IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
                    IPAddress ipAddress = IPAddress.Parse(ReceiverIP);
                    IPEndPoint localEndPoint = new IPEndPoint(ipAddress, ReceiverPort);

                    // Create a TCP/IP socket.
                    Socket listener = new Socket(AddressFamily.InterNetwork,
                        SocketType.Stream, ProtocolType.Tcp);

                    // Bind the socket to the local endpoint and listen for incoming connections.
                    try
                    {
                        listener.Bind(localEndPoint);
                        listener.Listen(100);
                        //ommunication_received==false
                        while (true)
                        {
                            // Set the event to nonsignaled state.
                            allDone.Reset();

                            // Start an asynchronous socket to listen for connections.
                            //Console.WriteLine("Waiting for a connection...");
                            listener.BeginAccept(
                                new AsyncCallback(AcceptCallback),
                                listener);

                            allDone.WaitOne();

                            // Wait until a connection is made before continuing.
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                    //Console.WriteLine("\nPress ENTER to continue...");
                    //Console.Read();
                }
            }
            public static void AcceptCallback(IAsyncResult ar)
            {
                // Signal the main thread to continue.
                allDone.Set();

                // Get the socket that handles the client request.
                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);

                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = handler;
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }
            public static void ReadCallback(IAsyncResult ar)
            {
                String content = String.Empty;

                // Retrieve the state object and the handler socket
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;

                // Read data from the client socket. 
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There  might be more data, so store the data received so far.
                    state.sb.Append(Encoding.ASCII.GetString(
                        state.buffer, 0, bytesRead));

                    // Check for end-of-file tag. If it is not there, read more data.
                    content = state.sb.ToString();
                    if (content.IndexOf("<EOF>") > -1)
                    {
                        // All the data has been read from the client. Display it on the console.
                        int x_start_ind = content.IndexOf("x: "), x_end_ind = content.IndexOf("xend ");
                        // int x_start_ind = content.IndexOf("x: "), x_end_ind = content.IndexOf("xend ");
                        // int y_start_ind = content.IndexOf("y: "), y_end_ind = content.IndexOf("yend ");

                        if (x_start_ind > -1 && x_end_ind > -1)
                        {
                            try
                            {
                                //convert the received string into x and y                                
                                // x_received = Convert.ToInt32(content.Substring(x_start_ind + 3, x_end_ind - (x_start_ind + 3)));
                                // y_received = Convert.ToInt32(content.Substring(y_start_ind + 3, y_end_ind - (y_start_ind + 3)));
                                string s = content.Substring(x_start_ind + 3, x_end_ind - (x_start_ind + 3));
                                //received_cards_arr = s.Split(',').Select(str => int.Parse(str)).ToArray(); ;
                                // received = Convert.ToInt32(content.Substring(x_start_ind + 3, x_end_ind - (x_start_ind + 3)));
                                received = s;
                            }
                            catch (FormatException)
                            {
                                Console.WriteLine("Input string is not a sequence of digits.");
                            }
                            catch (OverflowException)
                            {
                                Console.WriteLine("The number cannot fit in an Int32.");
                            }
                        }
                        // Show the data on the console.
                        //Console.WriteLine("x : {0}  y: {1}", x_received, y_received);

                        // Echo the data back to the client.
                        Send(handler, content);
                    }
                    else
                    {
                        // Not all data received. Get more.
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);
                    }
                }
            }

            private static void Send(Socket handler, String data)
            {
                // Convert the string data to byte data using ASCII encoding.
                byte[] byteData = Encoding.ASCII.GetBytes(data);

                // Begin sending the data to the remote device.
                handler.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), handler);
            }

            private static void SendCallback(IAsyncResult ar)
            {
                try
                {
                    // Retrieve the socket from the state object.
                    Socket handler = (Socket)ar.AsyncState;

                    // Complete sending the data to the remote device.
                    int bytesSent = handler.EndSend(ar);
                    //Console.WriteLine("Sent {0} bytes to client.", bytesSent);x

                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
        //This is the sending function (Syncronous)
        public class SynchronousClient
        {

            public static void StartClient(String x)
            {
                // Data buffer for incoming data.
                byte[] bytes = new byte[1024];

                // Connect to a remote device.
                try
                {
                    // Establish the remote endpoint for the socket.
                    // This example uses port 11000 on the local computer.
                    IPHostEntry ipHostInfo = Dns.GetHostByName(Dns.GetHostName());
                    IPAddress ipAddress = IPAddress.Parse(SenderIP);
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, SenderPort);

                    // Create a TCP/IP  socket.
                    Socket sender = new Socket(AddressFamily.InterNetwork,
                        SocketType.Stream, ProtocolType.Tcp);

                    // Connect the socket to the remote endpoint. Catch any errors.
                    try
                    {
                        sender.Connect(remoteEP);

                        Console.WriteLine("Socket connected to {0}",
                            sender.RemoteEndPoint.ToString());
                        //
                        string array_to_string = string.Join(",", x);
                        string message_being_sent = "x: " + x + "xend <EOF>";
                        //string message_being_sent = "x: " + x + "xend y: " + y + "yend cursorx: " +
                        //    System.Windows.Forms.Cursor.Position.X + "cursorxend cursory: " +
                        //    System.Windows.Forms.Cursor.Position.Y + "cursoryend <EOF>";
                        // Encode the data string into a byte array.
                        byte[] msg = Encoding.ASCII.GetBytes(message_being_sent);

                        // Send the data through the socket.
                        int bytesSent = sender.Send(msg);

                        // Receive the response from the remote device.
                        int bytesRec = sender.Receive(bytes);
                        Console.WriteLine("Echoed test = {0}",
                            Encoding.ASCII.GetString(bytes, 0, bytesRec));

                        // Release the socket.
                        sender.Shutdown(SocketShutdown.Both);
                        sender.Close();

                    }
                    catch (ArgumentNullException ane)
                    {
                        Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                    }
                    catch (SocketException se)
                    {
                        Console.WriteLine("SocketException : {0}", se.ToString());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unexpected exception : {0}", e.ToString());
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            public static string data = null;


        }
        #endregion
    }
}
