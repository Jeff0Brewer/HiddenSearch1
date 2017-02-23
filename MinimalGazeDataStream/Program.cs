//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

namespace MinimalGazeDataStream
//for recording data
{
    using EyeXFramework;
    using System;
    using Tobii.EyeX.Framework;

    public static class Program
    {
        public static void Main(string[] args)
        {
            // SET THESE FOR THIS COMPUTER //
            string compID = "A";
            string path = "C:/Users/Master/Documents/GitHub/HiddenSearch1/gazelog/" + compID + "_" + DateTime.Now.ToString("MM-dd_hh-mm") + ".txt";
            //
            string datapoint;
            string time;
            int recordEveryXTime = 15; //don't need to record every point...
            int recordEveryXTimeCounter = 0;
            using (var eyeXHost = new EyeXHost())
            {
                // Create a data stream: lightly filtered gaze point data.
                using (var lightlyFilteredGazeDataStream = eyeXHost.CreateGazePointDataStream(GazePointDataMode.LightlyFiltered))
                {
                    eyeXHost.Start();

                    lightlyFilteredGazeDataStream.Next += (s, e) =>
                    {
                        if ((recordEveryXTimeCounter % recordEveryXTime == 0))
                        {
                            // Convert ms to readable time format
                            time = DateTime.Now.ToString("hh:mm:ss.ff");
                            Console.WriteLine("Gaze point at ({0:0.00}, {1:0.00}) @ {2:0}", e.X, e.Y, time);
                            datapoint = string.Format("Gaze point at ({0:0.00}, {1:0.00}) @ {2:0} on ", e.X, e.Y, time) + compID + "\n";
                            System.IO.StreamWriter file = new System.IO.StreamWriter(path, true);
                            file.WriteLine(datapoint);
                            file.Close();
                        }
                        recordEveryXTimeCounter++;
                    };
                    Console.WriteLine("Listening for gaze data, press any key to exit...");
                    Console.In.Read();
                }
            }
        }
    }
}
