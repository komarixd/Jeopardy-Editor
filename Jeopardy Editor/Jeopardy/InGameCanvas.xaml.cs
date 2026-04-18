using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Jeopardy
{
    public partial class InGameCanvas : Page
    {
        string folder;
        TextBlock targetBlock;

        public InGameCanvas(string folder, TextBlock tgtblk)
        {
            InitializeComponent();

            this.folder = folder;
            this.targetBlock = tgtblk;

            this.Loaded += (_, __) =>
            {
                LoadGame();
            };
        }

        // =======================
        // Create Text
        // =======================

        void CreateText(Point pos, string loadedText = "Enter text here", double w = 200, double h = 40)
        {
            TextBox tb = new TextBox
            {
                Width = w,
                Height = h,
                Text = loadedText,
                FontSize = 24,
                FontFamily = new FontFamily("Arial"),
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                TextWrapping = TextWrapping.Wrap,
                CaretBrush = Brushes.White,
                IsReadOnly = true
            };

            MainCanvas.Children.Add(tb);
            Canvas.SetLeft(tb, pos.X);
            Canvas.SetTop(tb, pos.Y);
        }

        // =======================
        // Create Image
        // =======================

        void CreateImage(Point pos, string filePath = null, double w = double.NaN, double h = double.NaN)
        {
            if (filePath == null)
            {
                OpenFileDialog dlg = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif" };
                if (dlg.ShowDialog() != true) return;
                filePath = dlg.FileName;
            }

            BitmapImage bmp = new BitmapImage(new Uri(filePath));

            if (double.IsNaN(w))
            {
                float scale = 600f / Math.Max(bmp.PixelWidth, bmp.PixelHeight);
                w = bmp.PixelWidth * scale;
                h = bmp.PixelHeight * scale;
            }

            Image img = new Image
            {
                Source = bmp,
                Width = w,
                Height = h,
                Stretch = Stretch.Fill,
                Tag = filePath
            };

            MainCanvas.Children.Add(img);
            Canvas.SetLeft(img, pos.X);
            Canvas.SetTop(img, pos.Y);
        }

        // =======================
        // Create Audio
        // =======================

        void CreateAudio(Point pos, string filePath = null)
        {
            if (filePath == null)
            {
                OpenFileDialog dlg = new OpenFileDialog { Filter = "Audio Files|*.mp3;*.wav;*.wma;*.aac" };
                if (dlg.ShowDialog() != true) return;
                filePath = dlg.FileName;
            }

            bool isPlaying = false;
            bool isDraggingSlider = false; // NEW: Track if user is sliding

            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };

            // ===== ROOT =====
            Border root = new Border
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(4),
                Tag = filePath,
                Child = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center }
            };

            Border panelBorder = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                Background = Brushes.Transparent
            };

            StackPanel panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            panelBorder.Child = panel;
            root.Child = panelBorder;

            // ===== MEDIA ELEMENT =====
            MediaElement media = new MediaElement
            {
                LoadedBehavior = MediaState.Manual,
                UnloadedBehavior = MediaState.Manual,
                Volume = 0.8,
                Width = 0,
                Height = 0,
                IsHitTestVisible = false
            };

            // ===== PLAY BUTTON =====
            Button playButton = new Button
            {
                Content = "▶",
                Width = 40,
                Height = 40,
                FontSize = 18
            };

            // ===== TIMELINE (SEEKABLE) =====
            Slider timeline = new Slider
            {
                Width = 200,
                Minimum = 0,
                Margin = new Thickness(10, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            // ===== VOLUME SLIDER =====
            Slider volume = new Slider
            {
                Width = 80,
                Minimum = 0,
                Maximum = 1,
                Value = 0.8,
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(media);
            panel.Children.Add(playButton);
            panel.Children.Add(timeline);
            panel.Children.Add(volume);

            MainCanvas.Children.Add(root);
            Canvas.SetLeft(root, pos.X);
            Canvas.SetTop(root, pos.Y);

            // ===== MEDIA EVENTS =====
            media.MediaOpened += (_, __) =>
            {
                if (media.NaturalDuration.HasTimeSpan)
                    timeline.Maximum = media.NaturalDuration.TimeSpan.TotalSeconds;
            };

            media.MediaEnded += (_, __) =>
            {
                media.Position = TimeSpan.Zero;
                timeline.Value = 0;
                playButton.Content = "▶";
                isPlaying = false;
                timer.Stop();
            };

            media.MediaFailed += (_, e) =>
            {
                MessageBox.Show(
                    e.ErrorException?.Message ?? "Unknown audio error",
                    "Audio Error");
            };

            // ===== TIMER UPDATE (FIXED) =====
            timer.Tick += (_, __) =>
            {
                if (!isDraggingSlider && media.NaturalDuration.HasTimeSpan)
                {
                    timeline.Value = media.Position.TotalSeconds;
                }
            };

            // ===== PLAY / PAUSE =====
            playButton.Click += (_, __) =>
            {
                if (isPlaying)
                {
                    media.Pause();
                    playButton.Content = "▶";
                    timer.Stop();
                    isPlaying = false;
                }
                else
                {
                    media.Play();
                    playButton.Content = "❚❚";
                    timer.Start();
                    isPlaying = true;
                }
            };

            // ===== SEEK =====
            timeline.Loaded += (_, __) =>
            {
                timeline.ApplyTemplate();

                if (timeline.Template != null)
                {
                    var track = timeline.Template.FindName("PART_Track", timeline) as Track;

                    if (track != null)
                    {
                        track.DecreaseRepeatButton.Background = Brushes.LimeGreen;
                        track.IncreaseRepeatButton.Background = Brushes.DarkGray;
                    }
                }
            };

            timeline.PreviewMouseUp += (_, __) =>
            {
                media.Position = TimeSpan.FromSeconds(timeline.Value);
            };

            // ===== VOLUME =====
            volume.ValueChanged += (_, __) =>
            {
                media.Volume = volume.Value;
            };

            // ===== SET SOURCE =====
            media.Source = new Uri(filePath, UriKind.RelativeOrAbsolute);

            // ===== DRAG AND DROP LOGIC (FIXED) =====
            Point offset = new Point();

            root.MouseLeftButtonDown += (s, e) =>
            {
                if (e.OriginalSource is Button ||
                    e.OriginalSource is Slider ||
                    e.OriginalSource is Thumb)
                {
                    if (e.OriginalSource is Thumb || e.OriginalSource is Slider)
                        isDraggingSlider = true;

                    return;
                }

                offset = e.GetPosition(root);
                root.CaptureMouse();
            };

            root.MouseMove += (s, e) =>
            {
                if (root.IsMouseCaptured)
                {
                    Point currentPosition = e.GetPosition(MainCanvas);
                    Canvas.SetLeft(root, currentPosition.X - offset.X);
                    Canvas.SetTop(root, currentPosition.Y - offset.Y);
                }
            };

            root.MouseLeftButtonUp += (s, e) =>
            {
                isDraggingSlider = false;
                if (root.IsMouseCaptured)
                {
                    root.ReleaseMouseCapture();
                }
            };
        }

        void LoadGame()
        {
            string jsonPath = System.IO.Path.Combine(this.folder, "data.json");

            // Check if file exists to prevent crashing if folder is empty!
            if (!File.Exists(jsonPath)) return;

            string json = File.ReadAllText(jsonPath);
            List<CanvasItemModel> loadedItems = JsonSerializer.Deserialize<List<CanvasItemModel>>(json);

            for (int i = MainCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (MainCanvas.Children[i] is FrameworkElement fe && fe.Name != "MovablePanel")
                {
                    MainCanvas.Children.RemoveAt(i);
                }
            }

            foreach (var item in loadedItems)
            {
                Point pos = new Point(item.X, item.Y);

                if (item.ElementType == "Text")
                {
                    CreateText(pos, item.Content, item.Width, item.Height);
                }
                else if (item.ElementType == "Image")
                {
                    if (File.Exists(item.Content))
                        CreateImage(pos, item.Content, item.Width, item.Height);
                }
                else if (item.ElementType == "Audio")
                {
                    if (File.Exists(item.Content))
                        CreateAudio(pos, item.Content);
                }
            }
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            this.NavigationService.GoBack();
        }

        private void Done(object sender, RoutedEventArgs e)
        {
            // Changes the text block to gray on the main board!
            if (this.targetBlock != null)
            {
                this.targetBlock.Foreground = Brushes.Gray;
            }
            this.NavigationService.GoBack();
        }
    }
}