# Hidden Search

Hidden image search task for collaborative pairs using gaze visualization support for Tobii Eye trackers. 
Meant to be used with two locally networked computers.

## Task
- each puzzle has hidden images with an image bank on the right side
- task for each puzzle includes three images to find collaboratively with a partner, three individual
- look for the image highlighted in yellow (for collaborative part) and blue/red (for individual part), one by one
- click the image in the puzzle when it's found to move onto next one
- wrong clicks and total time are recorded

## Gaze visualizations
- three visualization options for viewing your partner's gaze information: 
1. *Gaze* (black dot for partner's gaze position, red dot for last point they fixated on)
2. *Fixation* (blue circle that appears when two partners are fixating on same location)
3. *Heatmap* (yellow to orange heatmap, darkens when partner fixates somewhere, fades when they look away)
- buttons can turn on/off any of the three

## Notes
- Window1, Window2, Window3, Window4 correspond to four different puzzles
- when running, screen will display IP address computer is receiving gaze data at, and IP address it is sending gaze data to
###### In *MainWindow.xaml.cs*:
- *defaultSenderIP* should be set to IP address the other computer is receiving data at 
- *compID* set to either "A" or "B" for each of two computers
- *initialImg* can be set to index from 1 to 4, to begin at one of 4 different hidden image puzzles
- change *pathfolder* to file path of folder where logs of data from game and gaze visualizations should be output to
