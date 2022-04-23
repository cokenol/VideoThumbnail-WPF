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
using System.Windows.Forms;
using System.Threading;
using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace VideoThumbnail
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IDictionary<string, List<thumbnail>> thumbnailGenerated = new Dictionary<string, List<thumbnail>>();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                FolderLocation.Text = fbd.SelectedPath;
        }
        private void btnFolderExecute(object sender, RoutedEventArgs e)
        {
            List<file> files = DirSearch(FolderLocation.Text);
            if (files != null)
            {
                VideoFiles.ItemsSource = files;
                VideoFiles.DisplayMemberPath = "FileName";
                VideoFiles.SelectedValuePath = "Path";
            }
        }
        public List<file> DirSearch(string sDir)
        {
            List<string> mediaExtensions = new List<string> { ".mp4", ".mov" };
            List<file> filesFound = new List<file>();

            foreach (string d in Directory.GetDirectories(sDir, "*", SearchOption.AllDirectories))
            {
                foreach (string f in Directory.GetFiles(d, "*.*"))
                {
                    //Console.WriteLine(f);
                    //Console.WriteLine(System.IO.Path.GetExtension(f).ToLower());
                    //Console.WriteLine(mediaExtensions.Contains(System.IO.Path.GetExtension(f).ToLower()));
                    //Console.WriteLine(System.IO.Path.GetFileName(f));
                    Regex re = new Regex(@"thumbnail");
                    if (mediaExtensions.Contains(System.IO.Path.GetExtension(f).ToLower()))
                    {
                        if(!re.IsMatch(System.IO.Path.GetFileName(f)))
                        {
                            filesFound.Add(new file() { Path = f, FileName = System.IO.Path.GetFileName(f) });
                        }
                    }
                }
                DirSearch(d);
            }
            //Console.WriteLine(filesFound.Count);
            return filesFound;
        }
        public class file
        {
            public string FileName { get; set; }
            public string Path { get; set; }
        }
        public class thumbnail
        {
            public BitmapImage image { get; set; }
            public string path { get; set; }
            public string folderpath { get; set; }
            public string thumbnailClipPath { get; set; }
            public int position { get; set; }
            public TimeSpan positionTime { get; set; }
            public int tWidth { get; set; }
            public int tHeight { get; set; }

        }
        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
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
        private async void VideoFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                //Loading thumbnails shows up
                Loading.Foreground = System.Windows.Media.Brushes.Black;
                await Task.Delay(5);
                if(VideoFiles.SelectedValue != null)
                {
                    fileNameChanged_Do(VideoFiles.SelectedValue.ToString());
                }
                //Loading thumbnails hides
                Loading.Foreground = System.Windows.Media.Brushes.White;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }

        }
        private void fileNameChanged_Do(string file)
        {
            List<thumbnail> thumbnails = new List<thumbnail>();
            //method2
            //if (!thumbnailGenerated.ContainsKey(file))
            //{
            //    thumbnails = GetListOfImages2(file, 16);
            //    thumbnailGenerated[file] = thumbnails;
            //}
            //else
            //{
            //    thumbnails = thumbnailGenerated[file];
            //}
            //images.ItemsSource = thumbnails;


            //for thumbnail clips
            string fileNameNoExt = System.IO.Path.GetFileNameWithoutExtension(file);
            if (!thumbnailGenerated.ContainsKey(file))
            {
                if (Directory.Exists(System.IO.Path.GetDirectoryName(file) + @"\" + fileNameNoExt + @"_clip\"))
                {
                    thumbnails = GetListOfImages2FromFile(file, 16);
                }
                else
                {
                    thumbnails = GetListOfImages2(file, 16);
                }
                thumbnailGenerated[file] = thumbnails;
            }
            else
            {
                thumbnails = thumbnailGenerated[file];
            }
            //if (Directory.Exists(System.IO.Path.GetDirectoryName(file) + @"\" + fileNameNoExt + @"_clip\"))
            //{
            //    thumbnails = GetListOfImages2FromFile(file, 16);
            //}
            //else
            //{
            //    thumbnails = GetListOfImages2(file, 16);             
            //}
            images.ItemsSource = thumbnails;
        }
        public static List<thumbnail> GetListOfImages2(string video, int times)
        {
            List<thumbnail> imagesL = new List<thumbnail>();
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
                // saving thumbnail to image
                //var thumbJpegStream = new MemoryStream();
                float position = (float)duration * i / times;
                //ffMpeg.GetVideoThumbnail(video, thumbJpegStream, position);
                //var img = StreamToImage(thumbJpegStream, displayHeight, displayWidth);

                //saving thumbnail clip to file
                ConvertSettings cs = new ConvertSettings();
                cs.Seek = (int)position;
                cs.MaxDuration = 5;
                cs.SetVideoFrameSize(displayWidth, displayHeight);
                string filename = System.IO.Path.GetFileNameWithoutExtension(video);
                var outputFile = System.IO.Path.GetDirectoryName(video) + @"\" + filename + @"_clip\" + string.Format("thumbnail_{0}.mp4",i);
                try
                {
                    ffMpeg.ConvertMedia(video, "mp4", outputFile, Format.mp4, cs);
                }
                catch (Exception)
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(video) + @"\" + filename + @"_clip\");
                    ffMpeg.ConvertMedia(video, "mp4", outputFile, Format.mp4, cs);                
                }
                Console.WriteLine(outputFile);


                //try thumbnail class
                thumbnail tb = new thumbnail();
                //tb.image = img;
                tb.path = video;
                tb.folderpath = System.IO.Path.GetDirectoryName(video);
                tb.thumbnailClipPath = outputFile;
                tb.position = (int)position*1000;
                tb.positionTime = TimeSpan.FromSeconds(position);
                tb.tWidth = displayWidth;
                tb.tHeight = displayHeight;
                imagesL.Add(tb);

                //imagesL.Add(img);
            }
            
            return imagesL;
        }

        public static List<thumbnail> GetListOfImages2FromFile(string video, int times)
        {
            List<thumbnail> imagesL = new List<thumbnail>();
            FFMpegConverter ffMpeg = new FFMpegConverter();
            var ffProbe = new NReco.VideoInfo.FFProbe();
            var videoInfo = ffProbe.GetMediaInfo(video);
            double duration = videoInfo.Duration.TotalSeconds;
            int height = videoInfo.Streams[0].Height;
            int width = videoInfo.Streams[0].Width;
            //Console.WriteLine("W= {0} x H ={1}", width, height);
            ImageChecker wh = new ImageChecker { ImageWidth = 200, ImageHeight = 150 };
            //wh.CheckedWidthHeight(height, width);
            int displayWidth = wh.ImageWidth;
            int displayHeight = wh.ImageHeight;

            for (float i = 0; i < times; i++)
            {
                //Console.WriteLine("doing {0}", i);
                // saving thumbnail to image
                //var thumbJpegStream = new MemoryStream();
                float position = (float)duration * i / times;
                //ffMpeg.GetVideoThumbnail(video, thumbJpegStream, position);
                //var img = StreamToImage(thumbJpegStream, displayHeight, displayWidth);

                //saving thumbnail clip to file
                //ConvertSettings cs = new ConvertSettings();
                //cs.Seek = (int)position;
                //cs.MaxDuration = 5;
                //cs.SetVideoFrameSize(displayWidth, displayHeight);
                string filename = System.IO.Path.GetFileNameWithoutExtension(video);
                var outputFile = System.IO.Path.GetDirectoryName(video) + @"\" + filename + @"_clip\" + string.Format("thumbnail_{0}.mp4", i);
                //try
                //{
                //    ffMpeg.ConvertMedia(video, "mp4", outputFile, Format.mp4, cs);
                //}
                //catch (Exception)
                //{
                //    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(video) + @"\" + filename + @"_clip\");
                //    ffMpeg.ConvertMedia(video, "mp4", outputFile, Format.mp4, cs);
                //}
                //Console.WriteLine(outputFile);


                //try thumbnail class
                thumbnail tb = new thumbnail();
                //tb.image = img;
                tb.path = video;
                tb.folderpath = System.IO.Path.GetDirectoryName(video);
                tb.thumbnailClipPath = outputFile;
                tb.position = (int)position * 1000;
                tb.positionTime = TimeSpan.FromSeconds(position);
                tb.tWidth = displayWidth;
                tb.tHeight = displayHeight;
                imagesL.Add(tb);

                //imagesL.Add(img);
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

        private void btnPlayVideo(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(VideoFiles.SelectedValue.ToString());
        }

        private void PlayVideo(object sender, MouseButtonEventArgs e)
        {
            var img = (System.Windows.Controls.Image)sender;
            ProcessStartInfo startInfo = new ProcessStartInfo(@"C:\Program Files\MPC-HC\mpc-hc64.exe");
            startInfo.WindowStyle = ProcessWindowStyle.Normal;

            startInfo.Arguments = VideoFiles.SelectedValue.ToString() + " /start " + img.Tag.ToString();
            Process.Start(startInfo);
            //System.Windows.MessageBox.Show(VideoFiles.SelectedValue.ToString() + " /start " + img.Tag.ToString());
        }
        private void PlayVideoFromStack(object sender, MouseButtonEventArgs e)
        {
            var img = (System.Windows.Controls.StackPanel)sender;
            ProcessStartInfo startInfo = new ProcessStartInfo(@"C:\Program Files\MPC-HC\mpc-hc64.exe");
            startInfo.WindowStyle = ProcessWindowStyle.Normal;

            startInfo.Arguments = VideoFiles.SelectedValue.ToString() + " /start " + img.Tag.ToString();
            Process.Start(startInfo);
            //System.Windows.MessageBox.Show(VideoFiles.SelectedValue.ToString() + " /start " + img.Tag.ToString());
        }
        public void GetVideoPlayer(object sender, RoutedEventArgs e)
        {
            const string extPathTemplate = @"HKEY_CLASSES_ROOT\{0}";
            const string cmdPathTemplate = @"HKEY_CLASSES_ROOT\{0}\shell\open\command";

            // 1. Find out document type name for .jpeg files
            const string ext = ".mp4";

            var extPath = string.Format(extPathTemplate, ext);

            var docName = Registry.GetValue(extPath, string.Empty, string.Empty) as string;
            if (!string.IsNullOrEmpty(docName))
            {
                // 2. Find out which command is associated with our extension
                var associatedCmdPath = string.Format(cmdPathTemplate, docName);
                var associatedCmd =
                    Registry.GetValue(associatedCmdPath, string.Empty, string.Empty) as string;

                if (!string.IsNullOrEmpty(associatedCmd))
                {
                    Console.WriteLine("\"{0}\" command is associated with {1} extension", associatedCmd, ext);
                }
                //return associatedCmd;
            }
            //return "";
        }
        private void mediaElement1_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MediaElement me = (MediaElement)sender;
            //me.Position = (TimeSpan)me.Tag;
            me.Play();
        }

        private void mediaElement1_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MediaElement me = (MediaElement)sender;
            me.Stop();
            //me.Position = (TimeSpan)me.Tag;
        }

        private void mediaElement1_Loaded(object sender, RoutedEventArgs e)
        {
            MediaElement me = (MediaElement)sender;
            me.Pause();
            //me.Position = (TimeSpan) me.Tag;
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
        private int _columnNums = 4;
        public int ColumnNums
        {
            get => _columnNums;
        }
        public void CheckedWidthHeight(int height, int width)
        {
            int factor = 5;
            if (height > width)
            {
                //Console.WriteLine("Height {0} > Width {1} ",height, width);
                _columnNums = 8;
                _imageHeight = height / factor;
                _imageWidth = width / factor;
                //Console.WriteLine("{0} {1}",ImageHeight.ToString(), ImageWidth.ToString());
            }
            else
            {
                //Console.WriteLine("Height {0} < Width {1} ", height, width);
                _columnNums = 4;
                _imageHeight = height / factor;
                _imageWidth = width / factor;
                //Console.WriteLine("{0} {1}", ImageHeight.ToString(), ImageWidth.ToString());
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
