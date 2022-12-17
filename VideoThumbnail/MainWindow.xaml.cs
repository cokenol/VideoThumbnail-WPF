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
        List<file> files = new List<file>();

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
            SearchFolder();
        }
        private async void SearchFolder()
        {
            Loading.Text = "Getting files";
            await Task.Delay(5);
            List<file> files = DirSearch(FolderLocation.Text);
            //files.Shuffle();
            if (files != null)
            {
                VideoFiles.ItemsSource = files;
                VideoFiles.DisplayMemberPath = "FileName";
                VideoFiles.SelectedValuePath = "Path";
            }
            Loading.Text = "";
        }
        
        private async void btnFolderCreate(object sender, RoutedEventArgs e)
        {
            progressBar_overall.Maximum = 100;
            int total = VideoFiles.Items.Count;
            Loading.Text = string.Format("Batch {0} clips 0%", total);
            var progress_overall = new Progress<int>(v =>
            {
                // This lambda is executed in context of UI thread,
                // so it can safely update form controls
                progressBar_overall.Value = v;
                Loading.Text = string.Format("Batch {0} clips {1}%", total, v);
            });

            
            System.Collections.IEnumerable files = VideoFiles.ItemsSource;
            //System.Windows.Controls.ProgressBar progressBar_tb = progressBar;
            
            await Task.Run(() => CreateThumbnailAll(progress_overall, total, files));
            Loading.Text = "";
            progressBar_overall.Value = 0;
        }
        public void CreateThumbnailAll(IProgress<int> progress_overall, int total, System.Collections.IEnumerable files)
        {
            // This method is executed in the context of
            // another thread (different than the main UI thread),
            // so use only thread-safe code
            //progressBar_tb.Maximum = 100;
            
            var progress = new Progress<int>(v =>
            {
                // This lambda is executed in context of UI thread,
                // so it can safely update form controls
                //progressBar_tb.Value = v;
            });
            int i = 0;
            foreach (file file in files)
            {
                fileNameChanged(progress, file.Path);

                // Use progress to notify UI thread that progress has
                // changed
                if (progress_overall != null)
                {
                    progress_overall.Report((i + 1) * 100 / total);
                }
                i++;
            }
            System.Windows.MessageBox.Show("All " + total.ToString() + " videos thumbnail clips created");
        }
        public List<file> DirSearch(string sDir)
        {
            List<string> mediaExtensions = new List<string> { ".mp4", ".mov", ".mkv"};
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
                            Console.WriteLine("Path = {0}, Filename = {1}", f, System.IO.Path.GetFileName(f));
                        }
                    }
                }
                DirSearch(d);
            }
            //Console.WriteLine(filesFound.Count);
            return filesFound;
        }
        public List<file> DirSearch(string sDir, string searchTerm)
        {
            List<string> mediaExtensions = new List<string> { ".mp4", ".mov", ".mkv" };
            List<file> filesFound = new List<file>();

            foreach (string d in Directory.GetDirectories(sDir, "*", SearchOption.AllDirectories))
            {
                foreach (string f in Directory.GetFiles(d, "*.*"))
                {
                    //Console.WriteLine(f);
                    //Console.WriteLine(System.IO.Path.GetExtension(f).ToLower());
                    //Console.WriteLine(mediaExtensions.Contains(System.IO.Path.GetExtension(f).ToLower()));
                    //Console.WriteLine(System.IO.Path.GetFileName(f));
                    if (System.IO.Path.GetFileNameWithoutExtension(f).ToLower().Contains(searchTerm.ToLower()))
                    {
                        Regex re = new Regex(@"thumbnail");
                        if (mediaExtensions.Contains(System.IO.Path.GetExtension(f).ToLower()))
                        {
                            if (!re.IsMatch(System.IO.Path.GetFileName(f)))
                            {
                                filesFound.Add(new file() { Path = f, FileName = System.IO.Path.GetFileName(f) });
                                Console.WriteLine("Path = {0}, Filename = {1}", f, System.IO.Path.GetFileName(f));
                            }
                        }
                    }
                }
                DirSearch(d, searchTerm);
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
        //private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        //{
        //    System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        //    if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        //        FileLocation.Text = openFileDialog.FileName;
        //}
        //private async void btnExecute(object sender, RoutedEventArgs e)
        //{
        //    ExecuteFileButton.Content = "Doing...";
        //    await Task.Delay(1);
        //    btnExecute_Do();
        //    ExecuteFileButton.Content = "Execute";
        //}
        private void btnExecute_Do()
        {
            //method 1
            //ShellFile shellFile = ShellFile.FromFilePath(FileLocation.Text);
            //Bitmap bm = shellFile.Thumbnail.Bitmap;
            //images.ItemsSource = GetListOfImages(bm, 20);

            //method2            
            //images.ItemsSource = GetListOfImages2(FileLocation.Text, 16);

        }
        private async void VideoFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                //Loading thumbnails shows up
                Loading.Text = "Loading Thumbnail...";
                await Task.Delay(5);
                progressBar.Maximum = 100;
                var progress = new Progress<int>(v =>
                {
                    // This lambda is executed in context of UI thread,
                    // so it can safely update form controls
                    progressBar.Value = v;
                });

                if (VideoFiles.SelectedValue != null)
                {
                    string file = VideoFiles.SelectedValue.ToString();
                    var val = await Task.Run(() => fileNameChanged(progress, file));
                    await Task.Delay(5);
                    progressBar.Value = 100;                    
                    images.ItemsSource = val;
                    RenameName.Text = file;
                    //System.Windows.MessageBox.Show(System.IO.Path.GetFileName(file));
                }
                //Loading thumbnails hides
                Loading.Text = "";

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
            await Task.Delay(5);
            progressBar.Value = 0;

        }
        private List<thumbnail> fileNameChanged(IProgress<int> progress, string file)
        {
            List<thumbnail> thumbnails = new List<thumbnail>();
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
                    thumbnails = GetListOfImages2(file, 16, progress);
                    
                }
                thumbnailGenerated[file] = thumbnails;
            }
            else
            {
                thumbnails = thumbnailGenerated[file];
            }
            //images.ItemsSource = thumbnails;
            return thumbnails;
        }
        private void fileNameChanged_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }
        public static List<thumbnail> GetListOfImages2(string video, int times, IProgress<int> progress)
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

                // Use progress to notify UI thread that progress has
                // changed
                if (progress != null)
                    progress.Report((int)i * 100 / times);

                //saving thumbnail clip to file
                ConvertSettings cs = new ConvertSettings();
                cs.Seek = (int)position;
                cs.MaxDuration = 5;
                cs.SetVideoFrameSize(displayWidth,displayHeight);
                string filename = System.IO.Path.GetFileNameWithoutExtension(video);
                string fileExt = System.IO.Path.GetExtension(video).Replace(".","");
                Console.WriteLine(fileExt);
                string outputFile = System.IO.Path.GetDirectoryName(video) + @"\" + filename + @"_clip\" + string.Format("thumbnail_{0}.mp4",i);
                Console.WriteLine(outputFile);
                try
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(video) + @"\" + filename + @"_clip\");
                    ffMpeg.ConvertMedia(video, null, outputFile, Format.mp4, cs);
                }
                catch (Exception)
                {
                    //System.Windows.MessageBox.Show(ex.ToString());
                    cs.VideoFrameSize = "qvga";
                    ffMpeg.ConvertMedia(video, null, outputFile, Format.mp4, cs);
                }
                


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

        private void btnRename(object sender, RoutedEventArgs e)
        {
            RenameFile();
        }
        private void RenameFile()
        {
            //clip
            string clipFolderPath = System.IO.Path.GetDirectoryName(VideoFiles.SelectedValue.ToString());
            string clipFolderNameOri = System.IO.Path.GetFileNameWithoutExtension(VideoFiles.SelectedValue.ToString()) + "_clip";
            string clipFolderNameNew = System.IO.Path.GetFileNameWithoutExtension(RenameName.Text) + "_clip";
            System.Windows.MessageBox.Show(clipFolderNameOri + "\n" + clipFolderNameNew);
            string oldClip = clipFolderPath + "\\" + clipFolderNameOri;
            string newClip = clipFolderPath + "\\" + clipFolderNameNew;
            System.Windows.MessageBox.Show(oldClip + "\n" + newClip);

            //file
            string oldFile = VideoFiles.SelectedValue.ToString();
            string newFile = RenameName.Text;

            images.ItemsSource = null;
            try
            {
                CopyDirectory(oldClip, newClip, true);
                //Directory.Move(oldClip, newClip);
                Directory.Delete(oldClip, true);
                System.Windows.MessageBox.Show("Rename Success");
                Directory.Move(oldFile, newFile);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Rename Failed" + "\n" + ex);
            }
            RepopulateFiles();
        }
        private async void RepopulateFiles()
        {
            Loading.Text = "Getting files";
            await Task.Delay(5);
            List<file> files = DirSearch(FolderLocation.Text);
            //files.Shuffle();
            if (files != null)
            {
                VideoFiles.ItemsSource = files;
                VideoFiles.DisplayMemberPath = "FileName";
                VideoFiles.SelectedValuePath = "Path";
            }
            Loading.Text = "";
        }
        //method to copy folder
        static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = System.IO.Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = System.IO.Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        private void btnDeleteVideo(object sender, RoutedEventArgs e)
        {
            //clip folder
            string clipFolderPath = System.IO.Path.GetDirectoryName(VideoFiles.SelectedValue.ToString());
            string clipFolderNameOri = System.IO.Path.GetFileNameWithoutExtension(VideoFiles.SelectedValue.ToString()) + "_clip";
            string oldClip = clipFolderPath + "\\" + clipFolderNameOri;
            System.Windows.MessageBox.Show(oldClip);

            //file
            string oldFile = VideoFiles.SelectedValue.ToString();
            System.Windows.MessageBox.Show(oldFile);

            images.ItemsSource = null;
            try
            {
                Directory.Delete(oldClip, true);
                File.Delete(oldFile);
                System.Windows.MessageBox.Show("Delete Success");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Delete Failed" + "\n" + ex);
            }
            RepopulateFiles();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchWithSearchTerm();
        }
        private async void SearchWithSearchTerm()
        {
            Loading.Text = "Getting files";
            await Task.Delay(5);
            List<file> files = DirSearch(FolderLocation.Text, SearchTerm.Text);
            //files.Shuffle();
            if (files != null)
            {
                VideoFiles.ItemsSource = files;
                VideoFiles.DisplayMemberPath = "FileName";
                VideoFiles.SelectedValuePath = "Path";
            }
            Loading.Text = "";
        }

        private void SearchTermOnKeyDownHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SearchWithSearchTerm();
            }
        }

        private void RenameKeyDownHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                RenameFile();
            }
        }

        private void FolderKeyDownHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SearchFolder();
            }
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
    static class ExtensionsClass
    {
        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
