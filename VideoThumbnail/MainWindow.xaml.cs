﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Drawing;
using Microsoft.WindowsAPICodePack.Shell;
using System.Diagnostics;
using NReco.VideoConverter;
using System.Drawing.Imaging;
using System.ComponentModel;

namespace VideoThumbnail
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window 
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                FileLocation.Text = openFileDialog.FileName;
            //FileLocation.Text = File.ReadAllText(openFileDialog.FileName);
        }
        private async void btnExecute(object sender, RoutedEventArgs e)
        {
            ExecuteFileButton.Content = "Doing...";
            await Task.Delay(1);
            btnExecute_Do();
            ExecuteFileButton.Content = "Execute";
        }
        private void btnExecute_Do()
        {
            //method 1
            //ShellFile shellFile = ShellFile.FromFilePath(FileLocation.Text);
            //Bitmap bm = shellFile.Thumbnail.Bitmap;
            //images.ItemsSource = GetListOfImages(bm, 20);

            //method2            
            images.ItemsSource = GetListOfImages2(FileLocation.Text, 16);
            
        }
        public static List<BitmapImage> GetListOfImages2(string video, int times)
        {
            List<BitmapImage> imagesL = new List<BitmapImage>();
            FFMpegConverter ffMpeg = new FFMpegConverter();
            var ffProbe = new NReco.VideoInfo.FFProbe();
            var videoInfo = ffProbe.GetMediaInfo(video);
            double duration = videoInfo.Duration.TotalSeconds;
            int height = videoInfo.Streams[0].Height;
            int width = videoInfo.Streams[0].Width;
            //Console.WriteLine("W= {0} x H ={1}", width, height);
            ImageChecker wh = new ImageChecker { ImageWidth = 200, ImageHeight = 150 };
            wh.CheckedWidthHeight(height, width);
            int displayWidth = wh.ImageWidth;
            int displayHeight = wh.ImageHeight;

            for (float i = 0; i < times; i++)
            {
                //Console.WriteLine("doing {0}", i);
                var thumbJpegStream = new MemoryStream();
                float position = (float)duration * i / times;
                //Console.WriteLine(position);
                ffMpeg.GetVideoThumbnail(video, thumbJpegStream, position);
                //Console.WriteLine(StreamToImage(thumbJpegStream));
                var img = StreamToImage(thumbJpegStream, displayHeight, displayWidth);
                //wh.SetWidthHeight(img);
                imagesL.Add(img);
            }
            return imagesL;
        }
        private static BitmapImage StreamToImage(MemoryStream stream,int height,int width)
        {
            // assumes that the streams position is at the beginning
            // for example if you use a memory stream you might need to point it to 0 first
            var image = new BitmapImage();

            image.BeginInit();
            stream.Seek(0, SeekOrigin.Begin);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.DecodePixelHeight = height;
            image.DecodePixelWidth = width;
            image.EndInit();

            image.Freeze();
            return image;
        }
        public List<BitmapImage> GetListOfImages(Bitmap bitmap, int times)
        {
            List<BitmapImage> images = new List<BitmapImage>();
            for (long i = 0; i < times; i++)
            {
                Console.WriteLine("doing {0}", i);
                images.Add(ConvertBitmapToBitmapImage(i, times, bitmap));
                Console.WriteLine(images.Count);
            }
            return images;
        }

        public static BitmapImage ConvertBitmapToBitmapImage(long times, long capacity, Bitmap bitmap)
        {
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);

            BitmapImage image = new BitmapImage();
            image.BeginInit();
            long position = ms.Length * times / capacity;
            ms.Seek(position, SeekOrigin.End);
            Console.WriteLine(
                "Capacity = {0}, Length = {1}, Position = {2}\n",
                ms.Capacity.ToString(),
                ms.Length.ToString(),
                ms.Position.ToString());
            image.StreamSource = ms;
            image.EndInit();

            return image;
        }
        
        static Bitmap LoadImage(string path)
        {
            var ms = new MemoryStream(File.ReadAllBytes(path));
            return (Bitmap)System.Drawing.Image.FromStream(ms);
        }        
    }

    public class ImageChecker : INotifyPropertyChanged //Hold control and hit period to add the using for this
    {
        private int _imageWidth;
        public int ImageWidth
        {
            get { return _imageWidth; }
            set { _imageWidth = value; OnPropertyChanged("ImageWidth"); }
        }

        private int _imageHeight;
        public int ImageHeight
        {
            get { return _imageHeight; }
            set { _imageHeight = value; OnPropertyChanged("ImageHeight"); }
        }
        private int _columnNums = 8;
        public int ColumnNums
        {
            get { return _columnNums; }
            set { _columnNums = value; OnPropertyChanged("ColumnNums"); }
        }
        public void CheckedWidthHeight(int height, int width)
        {
            if (height > width)
            {
                Console.WriteLine("Height {0} > Width {1} ",height, width);
                _columnNums = 5;
                _imageHeight = height / 10;
                _imageWidth = width / 10;
                Console.WriteLine("{0} {1}",ImageHeight.ToString(), ImageWidth.ToString());
            }
            else
            {
                Console.WriteLine("Height {0} < Width {1} ", height, width);
                _columnNums = 4;
                _imageHeight = height / 10;
                _imageWidth = width / 10;
                Console.WriteLine("{0} {1}", ImageHeight.ToString(), ImageWidth.ToString());
            }
        }
        public void SetWidthHeight(BitmapImage img)
        {
            img.DecodePixelHeight = ImageHeight;
            img.DecodePixelWidth = ImageWidth;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged(String prop)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
    }

}
