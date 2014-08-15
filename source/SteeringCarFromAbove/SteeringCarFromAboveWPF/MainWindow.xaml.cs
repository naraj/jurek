﻿using System;
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
using SteeringCarFromAbove;
using AForge.Video.DirectShow;
using AForge.Video;
using System.Runtime.InteropServices;

namespace SteeringCarFromAboveWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int POSITION_STEP = 30;
        private TrackPlanner planner_ = null;
        GlyphRecognitionStudio.MainForm glyphRecogniser;
        IVideoSource videoSource = null;
        bool waitingForNextBaseImage = false;
        System.Drawing.Bitmap baseImage = null;

        public MainWindow()
        {
            glyphRecogniser = new GlyphRecognitionStudio.MainForm();
            glyphRecogniser.frameProcessed += glyphRecogniser_frameProcessed;
            glyphRecogniser.Show();

            InitializeComponent();

            planner_ = new TrackPlanner(
                locationTolerance: POSITION_STEP - 1, angleTolerance: 9.0d,
                positionStep: (int)POSITION_STEP, angleStep: 10.0d,
                mapSizeX: 1000.0d, mapSizeY: 1000.0d);

            planner_.NewSuccessorFound += planner_NewSuccessorFound;

            Map map = new Map(1000, 1000);

            map.car = new PositionAndOrientation(_x: 500.0, _y: 100.0d, _angle: 90.0d);
            map.parking = new PositionAndOrientation(_x: 1500.0, _y: 900, _angle: 90.0d);
            map.obstacles.Add(new System.Drawing.Rectangle(350, 570, 300, 50));
            map.obstacles.Add(new System.Drawing.Rectangle(350, 700, 300, 50));
            map.obstacles.Add(new System.Drawing.Rectangle(150, 150, 50, 300));
            map.obstacles.Add(new System.Drawing.Rectangle(150, 550, 50, 300));

            planner_.PrepareTracks(map);
            Canvas_trackPlanner.UpdateLayout();

            DrawMap(map);
        }

        // http://stackoverflow.com/questions/1118496/using-image-control-in-wpf-to-display-system-drawing-bitmap
        [DllImport("gdi32")]
        static extern int DeleteObject(IntPtr o);

        public static BitmapSource loadBitmap(System.Drawing.Bitmap source)
        {
            IntPtr ip = source.GetHbitmap();
            BitmapSource bs = null;
            try
            {
                bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
                   IntPtr.Zero, Int32Rect.Empty,
                   System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(ip);
            }

            return bs;
        }

        void glyphRecogniser_frameProcessed(object sender, GlyphRecognitionStudio.MainForm.FrameData e)
        {
            if (waitingForNextBaseImage)
            {
                baseImage = e.getImage();
                this.Dispatcher.Invoke(new Action(() => image_baseImagePicker.Source = loadBitmap(baseImage)));

                //image_baseImagePicker.Source = loadBitmap(baseImage);
                waitingForNextBaseImage = false;
            }
            Console.WriteLine("Frame processed");
        }

        List<Line> lastTrack = new List<Line>();
        private void DrawTrack(List<PositionAndOrientation> track)
        {
            foreach (var item in lastTrack)
	        {
                Canvas_trackPlanner.Children.Remove(item);
	        }
            
            lastTrack.Clear();
            foreach (PositionAndOrientation item in track)
            {
                Line l = new Line();

                const double LENGTH = POSITION_STEP;
                l.Stroke = Brushes.OrangeRed;
                l.StrokeThickness = 1;
                l.X1 = item.x;
                l.X2 = item.x - Math.Cos(item.angle / 180.0d * Math.PI) * LENGTH;
                l.Y1 = item.y;
                l.Y2 = item.y - Math.Sin(item.angle / 180.0d * Math.PI) * LENGTH;

                Canvas_trackPlanner.Children.Add(l);

                lastTrack.Add(l);
            }
        }

        private void DrawMap(Map map)
        {
            DrawBorder(map);
            DrawCar(map);
            DrawParking(map);
            DrawObstacles(map);
        }

        private void DrawObstacles(Map map)
        {
            foreach (var obstacle in map.obstacles)
            {
                System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();
                rect.Stroke = new SolidColorBrush(Colors.Red);
                rect.Width = obstacle.Width;
                rect.Height = obstacle.Height;
                Canvas.SetLeft(rect, obstacle.X);
                Canvas.SetTop(rect, obstacle.Y);
                rect.StrokeThickness = 3;

                Canvas_trackPlanner.Children.Add(rect);
            }
        }

        private void DrawParking(Map map)
        {
            const double parkingSizeX = 38;
            const double parkingSizeY = 70;

            System.Windows.Shapes.Rectangle parking = new System.Windows.Shapes.Rectangle();
            parking.Stroke = new SolidColorBrush(Colors.Red);
            parking.Width = parkingSizeX;
            parking.Height = parkingSizeY;
            Canvas.SetLeft(parking, map.parking.x - parkingSizeX / 2);
            Canvas.SetTop(parking, map.parking.y - parkingSizeY / 2);
            parking.StrokeThickness = 7;

            Canvas_trackPlanner.Children.Add(parking);
        }

        private void DrawCar(Map map)
        {
            const double carSizeX = 25;
            const double carSizeY = 55;

            System.Windows.Shapes.Rectangle car = new System.Windows.Shapes.Rectangle();
            car.Stroke = new SolidColorBrush(Colors.Red);
            car.Width = carSizeX;
            car.Height = carSizeY;
            Canvas.SetLeft(car, map.car.x - carSizeX / 2);
            Canvas.SetTop(car, map.car.y - carSizeY / 2);
            car.StrokeThickness = 7;

            Canvas_trackPlanner.Children.Add(car);
        }

        private void DrawBorder(Map map)
        {
            System.Windows.Shapes.Rectangle border = new System.Windows.Shapes.Rectangle();
            border.Stroke = new SolidColorBrush(Colors.Black);
            border.Width = map.mapSizeX;
            border.Height = map.mapSizeY;
            Canvas.SetLeft(border, 0);
            Canvas.SetTop(border, 0);
            border.StrokeThickness = 5;

            Canvas_trackPlanner.Children.Add(border);
        }

        //http://stackoverflow.com/questions/1335426/is-there-a-built-in-c-net-system-api-for-hsv-to-rgb
        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            byte v = Convert.ToByte(value);
            byte p = Convert.ToByte(value * (1 - saturation));
            byte q = Convert.ToByte(value * (1 - f * saturation));
            byte t = Convert.ToByte(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }

        private int counter = 0;
        void planner_NewSuccessorFound(object sender, SteeringCarFromAbove.TrackPlanner.BFSNode e)
        {
            if (counter++ % 3 == 0)
            {
                int predecessorsCount = 0;
                SteeringCarFromAbove.TrackPlanner.BFSNode curr = e;
                while ((curr = curr.predecessor) != null) predecessorsCount++;

                Line l = new Line();

                const double LENGTH = POSITION_STEP;
                l.Stroke = new SolidColorBrush(ColorFromHSV((25.0d * predecessorsCount) % 360.0d, 0.3d, 1.0d)); //Brushes.LightSteelBlue;
                l.StrokeThickness = 1;
                l.X1 = e.position.x;
                l.X2 = e.position.x - Math.Cos(e.position.angle / 180.0d * Math.PI) * LENGTH;
                l.Y1 = e.position.y;
                l.Y2 = e.position.y - Math.Sin(e.position.angle / 180.0d * Math.PI) * LENGTH;

                Canvas_trackPlanner.Children.Add(l);
            }
        }


        private Point lastMouseDown_;
        private void Canvas_trackPlanner_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lastMouseDown_ = e.GetPosition(Canvas_trackPlanner);
            Console.WriteLine(String.Format("Click down: {0}, {1}", e.GetPosition(Canvas_trackPlanner).X, e.GetPosition(Canvas_trackPlanner).Y));
        }

        private void Canvas_trackPlanner_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Point mouseUp = e.GetPosition(Canvas_trackPlanner);
            Console.WriteLine(String.Format("Click up: {0}, {1}", e.GetPosition(Canvas_trackPlanner).X, e.GetPosition(Canvas_trackPlanner).Y));

            double deltaY = mouseUp.Y - lastMouseDown_.Y;
            double deltaX = mouseUp.X - lastMouseDown_.X;

            double angleInDegrees = Math.Atan2(deltaY, deltaX) * 180 / Math.PI;

            List<PositionAndOrientation> track = planner_.GetTrackFromPreparedPlanner(
                new PositionAndOrientation(lastMouseDown_.X, lastMouseDown_.Y, angleInDegrees));

            DrawTrack(track);
        }

        private void button_ChangeVideoSource_Click(object sender, RoutedEventArgs e)
        {
            VideoCaptureDeviceForm form = new VideoCaptureDeviceForm();
            if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                videoSource = form.VideoDevice;
                glyphRecogniser.InjectVideoSource(videoSource);
            }
            else
            {
                Console.WriteLine("Couldnt open video source");
            }

        }

        private void button_GetNextImage_Click(object sender, RoutedEventArgs e)
        {
            waitingForNextBaseImage = true;
        }

    }
}
