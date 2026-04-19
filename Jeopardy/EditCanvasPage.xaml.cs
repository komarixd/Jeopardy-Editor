using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Text.Json;
using System.IO;

namespace Jeopardy
{
    // =======================
    // DATA MODELS
    // =======================
    public class CanvasItemModel
    {
        public string ElementType { get; set; } // "Text", "Image", or "Audio"
        public string Content { get; set; }     // The actual text, or the file path
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    public class HintPageModel
    {
        public string HintTitle { get; set; }
        public List<CanvasItemModel> Items { get; set; } = new List<CanvasItemModel>();
    }

    // =======================
    // MAIN PAGE CLASS
    // =======================
    public partial class EditCanvasPage : Page
    {
        string folder;
        bool isDraggingPanel = false;
        Point panelOffset;
        Random rnd;

        private enum ToolType { Cursor, Text, Image, Audio }
        private ToolType currentTool = ToolType.Cursor;

        // Global selection
        FrameworkElement selectedElement = null;
        ResizeAdorner selectedAdorner = null;

        // Page/Hint selection
        Border selectedHintThumbnail = null;

        static Cursor CCursor = new Cursor(Application.GetResourceStream(
            new Uri("/Resources/Cursors/cursor_white.cur", UriKind.Relative)).Stream);
        static Cursor CText = new Cursor(Application.GetResourceStream(
            new Uri("/Resources/Cursors/text_white.cur", UriKind.Relative)).Stream);
        static Cursor CImage = new Cursor(Application.GetResourceStream(
            new Uri("/Resources/Cursors/image_white.cur", UriKind.Relative)).Stream);
        static Cursor CAudio = new Cursor(Application.GetResourceStream(
            new Uri("/Resources/Cursors/audio_white.cur", UriKind.Relative)).Stream);

        public EditCanvasPage(string folder)
        {
            InitializeComponent();
            Cursor_Click(null, null);

            rnd = new Random();
            this.folder = folder;

            this.Loaded += (s, e) =>
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.PreviewKeyDown += Page_PreviewKeyDown;
                }
                LoadGame();
            };
        }

        // =======================
        // SELECTION HELPERS
        // =======================
        void SelectElement(FrameworkElement element, ResizeAdorner adorner)
        {
            DeselectElement();
            selectedElement = element;
            selectedAdorner = adorner;
            adorner.Visibility = Visibility.Visible;
        }

        void DeselectElement()
        {
            if (selectedAdorner != null)
                selectedAdorner.Visibility = Visibility.Hidden;

            selectedElement = null;
            selectedAdorner = null;
        }

        // =======================
        // MOVABLE PANEL
        // =======================
        private void MovablePanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDraggingPanel = true;
            panelOffset = e.GetPosition(MovablePanel);
            MovablePanel.CaptureMouse();
        }

        private void MovablePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDraggingPanel) return;

            Point p = e.GetPosition(MainCanvas);
            Canvas.SetLeft(MovablePanel, p.X - panelOffset.X);
            Canvas.SetTop(MovablePanel, p.Y - panelOffset.Y);
        }

        private void MovablePanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDraggingPanel = false;
            MovablePanel.ReleaseMouseCapture();
        }

        // =======================
        // TOOL BUTTONS
        // =======================
        private void Cursor_Click(object sender, RoutedEventArgs e)
        {
            currentTool = ToolType.Cursor;
            Mouse.OverrideCursor = CCursor;
        }

        private void Text_Click(object sender, RoutedEventArgs e)
        {
            currentTool = ToolType.Text;
            Mouse.OverrideCursor = CText;
        }

        private void Image_Click(object sender, RoutedEventArgs e)
        {
            currentTool = ToolType.Image;
            Mouse.OverrideCursor = CImage;
        }

        private void Audio_Click(object sender, RoutedEventArgs e)
        {
            currentTool = ToolType.Audio;
            Mouse.OverrideCursor = CAudio;
        }

        // =======================
        // CANVAS CLICK
        // =======================
        private void Page_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(MainCanvas);
            DependencyObject hit = (DependencyObject)MainCanvas.InputHitTest(pos);

            // Empty space → deselect
            if (hit == MainCanvas || hit == null)
            {
                DeselectElement();
                MainCanvas.Focus();
                Keyboard.Focus(this);
            }

            if (currentTool == ToolType.Cursor) return;

            // Prevent placing over movable panel
            if (hit is FrameworkElement fe &&
                (fe.Name == "MovablePanel" || fe.IsDescendantOf(MovablePanel)))
                return;

            switch (currentTool)
            {
                case ToolType.Text:
                    CreateText(pos);
                    break;
                case ToolType.Image:
                    CreateImage(pos);
                    break;
                case ToolType.Audio:
                    CreateAudio(pos);
                    break;
            }

            Cursor_Click(null, null);
        }

        // =======================
        // CREATE ELEMENTS
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

            ResizeAdorner adorner = new ResizeAdorner(tb);
            AdornerLayer.GetAdornerLayer(MainCanvas).Add(adorner);
            adorner.Visibility = Visibility.Hidden;

            Point offset = new Point();
            bool dragging = false;

            tb.MouseEnter += (_, __) =>
            {
                if (selectedElement != tb) adorner.Visibility = Visibility.Visible;
            };

            tb.MouseLeave += (_, __) =>
            {
                if (selectedElement != tb) adorner.Visibility = Visibility.Hidden;
            };

            tb.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (currentTool != ToolType.Cursor) return;

                SelectElement(tb, adorner);

                if (e.ClickCount == 1)
                {
                    offset = e.GetPosition(tb);
                    dragging = true;
                    tb.CaptureMouse();
                }
                else if (e.ClickCount == 2)
                {
                    tb.IsReadOnly = false;
                    tb.Focus();
                    tb.CaretIndex = tb.Text.Length;
                }
                e.Handled = true;
            };

            tb.MouseMove += (_, e) =>
            {
                if (!dragging || e.LeftButton != MouseButtonState.Pressed) return;
                Point p = e.GetPosition(MainCanvas);
                Canvas.SetLeft(tb, p.X - offset.X);
                Canvas.SetTop(tb, p.Y - offset.Y);
            };

            tb.MouseLeftButtonUp += (_, __) =>
            {
                dragging = false;
                tb.ReleaseMouseCapture();
            };

            tb.LostFocus += (_, __) => { tb.IsReadOnly = true; };
        }

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

            ResizeAdorner adorner = new ResizeAdorner(img);
            AdornerLayer.GetAdornerLayer(MainCanvas).Add(adorner);
            adorner.Visibility = Visibility.Hidden;

            Point offset = new Point();
            bool dragging = false;

            img.MouseEnter += (_, __) =>
            {
                if (selectedElement != img) adorner.Visibility = Visibility.Visible;
            };

            img.MouseLeave += (_, __) =>
            {
                if (selectedElement != img) adorner.Visibility = Visibility.Hidden;
            };

            img.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (currentTool != ToolType.Cursor) return;
                SelectElement(img, adorner);
                offset = e.GetPosition(img);
                dragging = true;
                img.CaptureMouse();
                e.Handled = true;
            };

            img.MouseMove += (_, e) =>
            {
                if (!dragging || e.LeftButton != MouseButtonState.Pressed) return;
                Point p = e.GetPosition(MainCanvas);
                Canvas.SetLeft(img, p.X - offset.X);
                Canvas.SetTop(img, p.Y - offset.Y);
            };

            img.MouseLeftButtonUp += (_, __) =>
            {
                dragging = false;
                img.ReleaseMouseCapture();
            };
        }

        void CreateAudio(Point pos, string filePath = null)
        {
            if (filePath == null)
            {
                OpenFileDialog dlg = new OpenFileDialog { Filter = "Audio Files|*.mp3;*.wav;*.wma;*.aac" };
                if (dlg.ShowDialog() != true) return;
                filePath = dlg.FileName;
            }

            bool isPlaying = false;
            bool isDraggingTimeline = false;

            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };

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

            StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            panelBorder.Child = panel;
            root.Child = panelBorder;

            MediaElement media = new MediaElement
            {
                LoadedBehavior = MediaState.Manual,
                UnloadedBehavior = MediaState.Manual,
                Volume = 0.8,
                Width = 0,
                Height = 0,
                IsHitTestVisible = false
            };

            Button playButton = new Button { Content = "▶", Width = 40, Height = 40, FontSize = 18 };
            Slider timeline = new Slider { Width = 200, Minimum = 0, Margin = new Thickness(10, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
            Slider volume = new Slider { Width = 80, Minimum = 0, Maximum = 1, Value = 0.8, VerticalAlignment = VerticalAlignment.Center };

            Button closeButton = new Button
            {
                Content = "✖",
                Background = Brushes.Red,
                Foreground = Brushes.White,
                Width = 20,
                Height = 20,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            closeButton.Click += (_, __) =>
            {
                media.Stop();
                timer.Stop();
                MainCanvas.Children.Remove(root);
            };

            media.Unloaded += (_, __) => { media.Stop(); timer.Stop(); };

            panel.Children.Add(media);
            panel.Children.Add(playButton);
            panel.Children.Add(timeline);
            panel.Children.Add(volume);
            panel.Children.Add(closeButton);

            MainCanvas.Children.Add(root);
            Canvas.SetLeft(root, pos.X);
            Canvas.SetTop(root, pos.Y);

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
                MessageBox.Show(e.ErrorException?.Message ?? "Unknown audio error", "Audio Error");
            };

            timer.Tick += (_, __) =>
            {
                if (!isDraggingTimeline) timeline.Value = media.Position.TotalSeconds;
            };

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

            timeline.PreviewMouseDown += (_, __) => { isDraggingTimeline = true; };
            timeline.PreviewMouseUp += (_, __) =>
            {
                media.Position = TimeSpan.FromSeconds(timeline.Value);
                isDraggingTimeline = false;
            };

            volume.ValueChanged += (_, __) => { media.Volume = volume.Value; };
            media.Source = new Uri(filePath);

            bool dragging = false;
            Point offset = new Point();

            root.MouseLeftButtonDown += (s, e) =>
            {
                if (e.OriginalSource is Button || e.OriginalSource is Slider || e.OriginalSource is Thumb) return;
                dragging = true;
                offset = e.GetPosition(root);
                root.CaptureMouse();
            };

            root.MouseMove += (_, e) =>
            {
                if (!dragging || e.LeftButton != MouseButtonState.Pressed) return;
                Point p = e.GetPosition(MainCanvas);
                Canvas.SetLeft(root, p.X - offset.X);
                Canvas.SetTop(root, p.Y - offset.Y);
            };

            root.MouseLeftButtonUp += (_, __) =>
            {
                dragging = false;
                root.ReleaseMouseCapture();
            };
        }

        private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && selectedElement != null)
            {
                MainCanvas.Children.Remove(selectedElement);
                DeselectElement();
            }

            if (e.Key == Key.Escape)
            {
                Cursor_Click(null, null);
                DeselectElement();

                if (currentTool != ToolType.Cursor) return;

                if (SettingPanel.Visibility == Visibility.Visible)
                    SettingPanel.Visibility = Visibility.Collapsed;
                else
                    SettingPanel.Visibility = Visibility.Visible;
            }
        }

        // ==========================
        // HINT PAGE MULTI-PAGE LOGIC
        // ==========================
        private void AddHintPage(object sender, RoutedEventArgs e)
        {
            CreateThumbnail(new HintPageModel { HintTitle = "Hint" });
        }

        private void CreateThumbnail(HintPageModel pageData)
        {
            Border hintPage = new Border
            {
                Width = 144,
                Height = 81,
                Margin = new Thickness(5),
                BorderThickness = new Thickness(3),
                BorderBrush = Brushes.Black,
                Background = Brushes.DimGray,
                Tag = pageData // Store the data model here!
            };

            Grid cellGrid = new Grid();

            TextBox text = new TextBox
            {
                Text = pageData.HintTitle,
                FontSize = 30,
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            text.TextChanged += (s, ev) => pageData.HintTitle = text.Text;

            Button delBtn = new Button
            {
                Content = "X",
                FontSize = 10,
                Width = 20,
                Height = 20,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
            };

            delBtn.Click += (_, __) =>
            {
                HintPageContainer.Children.Remove(hintPage);
                if (selectedHintThumbnail == hintPage)
                {
                    selectedHintThumbnail = null;
                    LoadPageToCanvas(new List<CanvasItemModel>()); // Clear screen
                }
            };

            hintPage.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (e.OriginalSource is Button) return;

                if (selectedHintThumbnail != null)
                {
                    ((HintPageModel)selectedHintThumbnail.Tag).Items = CaptureCurrentCanvas();
                    selectedHintThumbnail.BorderBrush = Brushes.Black;
                }

                LoadPageToCanvas(pageData.Items);

                selectedHintThumbnail = hintPage;
                hintPage.BorderBrush = Brushes.DeepSkyBlue;
            };

            cellGrid.Children.Add(text);
            cellGrid.Children.Add(delBtn);
            hintPage.Child = cellGrid;

            HintPageContainer.Children.Add(hintPage);

            // Automatically select if it's the very first page
            if (HintPageContainer.Children.Count == 1)
            {
                var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent };
                hintPage.RaiseEvent(args);
            }
        }

        // =======================
        // SAVE & LOAD
        // =======================
        private List<CanvasItemModel> CaptureCurrentCanvas()
        {
            List<CanvasItemModel> items = new List<CanvasItemModel>();
            foreach (UIElement child in MainCanvas.Children)
            {
                if (child is FrameworkElement fe)
                {
                    if (fe.Name == "MovablePanel" || fe.Name == "HintPanel") continue;

                    CanvasItemModel model = new CanvasItemModel
                    {
                        X = Canvas.GetLeft(fe),
                        Y = Canvas.GetTop(fe),
                        Width = double.IsNaN(fe.Width) ? fe.ActualWidth : fe.Width,
                        Height = double.IsNaN(fe.Height) ? fe.ActualHeight : fe.Height
                    };

                    if (fe is TextBox tb)
                    {
                        model.ElementType = "Text";
                        model.Content = tb.Text;
                        items.Add(model);
                    }
                    else if (fe is Image img && fe.Tag is string imgPath)
                    {
                        model.ElementType = "Image";
                        model.Content = imgPath;
                        items.Add(model);
                    }
                    else if (fe is Border border && fe.Tag is string audioPath)
                    {
                        model.ElementType = "Audio";
                        model.Content = audioPath;
                        items.Add(model);
                    }
                }
            }
            return items;
        }

        private void LoadPageToCanvas(List<CanvasItemModel> items)
        {
            DeselectElement();

            // Clear current canvas (but keep panels)
            for (int i = MainCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (MainCanvas.Children[i] is FrameworkElement fe && fe.Name != "MovablePanel" && fe.Name != "HintPanel")
                {
                    MainCanvas.Children.RemoveAt(i);
                }
            }

            foreach (var item in items)
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Sync the active screen to the active thumbnail before saving
            if (selectedHintThumbnail != null)
                ((HintPageModel)selectedHintThumbnail.Tag).Items = CaptureCurrentCanvas();

            List<HintPageModel> pagesToSave = new List<HintPageModel>();

            foreach (var child in HintPageContainer.Children)
            {
                if (child is Border b && b.Tag is HintPageModel model)
                {
                    pagesToSave.Add(model);
                }
            }

            string json = JsonSerializer.Serialize(pagesToSave, new JsonSerializerOptions { WriteIndented = true });
            // Note: Changed file name to hints.json to fit the new multi-page structure
            File.WriteAllText(System.IO.Path.Combine(this.folder, "hints.json"), json);
            NavigationService.GoBack();
        }

        private void LoadGame()
        {
            string path = System.IO.Path.Combine(this.folder, "hints.json");

            if (!File.Exists(path)) return;

            string json = File.ReadAllText(path);
            List<HintPageModel> loadedPages = JsonSerializer.Deserialize<List<HintPageModel>>(json);

            HintPageContainer.Children.Clear();
            selectedHintThumbnail = null;
            LoadPageToCanvas(new List<CanvasItemModel>()); 

            foreach (var page in loadedPages)
            {
                CreateThumbnail(page);
            }
        }

        private void HideSettingPanel(object sender, RoutedEventArgs e)
        {
            SettingPanel.Visibility = Visibility.Hidden;
        }
    }
}