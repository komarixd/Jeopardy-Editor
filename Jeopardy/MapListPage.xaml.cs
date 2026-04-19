using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using System.IO;
using System.Reflection;

namespace Jeopardy
{
    /// <summary>
    /// Interaction logic for MapListPage.xaml
    /// </summary>
    
    public class Map {
        public int id;
        public string name;
        public string description;
        public string author;
        public float difficulty;
        public string path;

        public Map(int id, string name, string author, string description, float difficulty, string path)
        {
            this.id = id;
            this.name = name;
            this.author = author;
            this.description = description;
            this.difficulty = difficulty;
            this.path = path;
        }
    }   

    public partial class MapListPage : Page
    {
        string mode;
        string MapFolder = "Maps";

        string targetFolder = "";

        List<Map> maps = new List<Map>();

        public MapListPage(string mode)
        {
            InitializeComponent();

            this.Focusable = true;
            this.Focus();

            this.mode = mode;
            Mode.Content = this.mode;

            this.KeyDown += MapListPage_KeyDown;

            GetMaps();
            ShowMaps(maps);


        }

        private void GetMaps()
        {
            string[] folderlist = Directory.GetDirectories(MapFolder);
            foreach (string folder in folderlist) { 
                string mapinfo = System.IO.Path.Combine(folder, "mapinfo");
                if (File.Exists(mapinfo))
                {
                    string[] lines = File.ReadAllLines(mapinfo);
                    if (lines.Length >= 4)
                    {
                        string name = lines[0];                        
                        string author = lines[1];
                        string description = lines[2];
                        float difficulty = 0.0f;
                        float.TryParse(lines[3], out difficulty);
                        string path = folder;
                        maps.Add(new Map(maps.Count + 1, name, author, description, difficulty, path));
                    }
                }
            }
        }

        private void ShowMaps(List<Map> mapsToShow)
        {
            MapGrid.Children.Clear();

            foreach (var map in mapsToShow)
            {
                Grid cardContainer = new Grid();

                Border border = new Border
                {
                    BorderThickness = new Thickness(2),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    Margin = new Thickness(8),
                    Padding = new Thickness(15),
                    CornerRadius = new CornerRadius(10),
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    Height = 180, 
                    Cursor = Cursors.Hand
                };

                StackPanel panel = new StackPanel();

                TextBlock nameText = new TextBlock
                {
                    Text = map.name,
                    FontWeight = FontWeights.Bold,
                    FontSize = 18,
                    Foreground = Brushes.White,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 10),
                    TextWrapping = TextWrapping.NoWrap,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };

                TextBlock detailsText = new TextBlock
                {
                    Text = $"By: {map.author}\nDiff: {map.difficulty}",
                    Foreground = Brushes.LightGray,
                    FontSize = 12,
                    LineHeight = 18,
                    TextAlignment = TextAlignment.Center
                };

                TextBlock descText = new TextBlock
                {
                    Text = map.description,
                    Foreground = Brushes.DarkGray,
                    FontSize = 11,
                    FontStyle = FontStyles.Italic,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 10, 0, 0),
                    TextAlignment = TextAlignment.Center,
                    MaxHeight = 60
                };

                panel.Children.Add(nameText);
                panel.Children.Add(detailsText);
                panel.Children.Add(descText);

                border.Child = panel;

          

                border.MouseEnter += (s, e) => {
                    border.BorderBrush = Brushes.Cyan;
                    border.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                };
                border.MouseLeave += (s, e) => {
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));
                    border.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                };

                if (this.mode == "EDIT")
                {
                    border.MouseLeftButtonDown += (sender, e) =>
                    {
                        EditMapPanel.Visibility = Visibility.Visible;
                        EditMapName.Text = map.name;
                        EditMapAuthor.Text = map.author;
                        EditMapDesc.Text = map.description;
                        EditMapDiff.Text = map.difficulty.ToString();
                        targetFolder = map.path;
                    };
                }
                else
                {
                    border.MouseLeftButtonDown += (sender, e) =>
                    {
                        NavigationService.Navigate(new InGameJeopardy(map.path));
                    };
                }

                cardContainer.Children.Add(border);

                if (this.mode == "EDIT")
                {
                    Button deleteBtn = new Button
                    {
                        Content = "×",
                        Width = 24,
                        Height = 24,
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.Gray,
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 12, 12, 0),
                        Cursor = Cursors.Hand
                    };

                    deleteBtn.MouseEnter += (s, e) => deleteBtn.Foreground = Brushes.Red;
                    deleteBtn.MouseLeave += (s, e) => deleteBtn.Foreground = Brushes.Gray;

                    deleteBtn.Click += (s, e) =>
                    {
                        e.Handled = true;

                        var result = MessageBox.Show($"Are you sure you want to delete '{map.name}'?",
                                                     "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            try
                            {
                                if (Directory.Exists(map.path))
                                {
                                    Directory.Delete(map.path, true);
                                }

                                maps.Remove(map);
                                Refresh(null, null);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error deleting folder: {ex.Message}");
                            }
                        }
                    };
                    cardContainer.Children.Add(deleteBtn);
                }  

                MapGrid.Children.Add(cardContainer);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchBox.Text.ToLower().Trim();

            if (string.IsNullOrWhiteSpace(query))
            {
                ShowMaps(maps);
            }
            else
            {
                var filteredMaps = maps.Where(m =>
                    m.name.ToLower().Contains(query) ||
                    m.author.ToLower().Contains(query)
                ).ToList();

                ShowMaps(filteredMaps);
            }
        }

        private void EditJeopardy(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new EditJeopardy(targetFolder));
        }

        private void HideEditMapPanel(object sender, RoutedEventArgs e)
        {
            EditMapPanel.Visibility = Visibility.Collapsed;
        }

        private void SaveMapEdit(object sender, RoutedEventArgs e)
        {
            File.WriteAllLines(System.IO.Path.Combine(targetFolder, "mapinfo"), new string[] {
                EditMapName.Text,
                EditMapAuthor.Text,
                EditMapDesc.Text,
                EditMapDiff.Text
            });

            Refresh(null, null);

            EditMapPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowNewMapPanel(object sender, RoutedEventArgs e)
        {
            NewMapPanel.Visibility = Visibility.Visible;
        }

        private void HideNewMapPanel(object sender, RoutedEventArgs e)
        {
            NewMapPanel.Visibility = Visibility.Collapsed;
        }

        private void CreateNewMap(object sender, RoutedEventArgs e)
        {
            Random rnd = new Random();
            int r = rnd.Next(10000, 99999);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");

            string NewFolderName = System.IO.Path.Combine(MapFolder, $"{timestamp}_{r}");

            Directory.CreateDirectory(NewFolderName);

            File.WriteAllLines(System.IO.Path.Combine(NewFolderName, "mapinfo"), new string[] {
                NewMapName.Text,
                NewMapAuthor.Text,
                NewMapDesc.Text,
                NewMapDiff.Text
            });

            File.WriteAllText(System.IO.Path.Combine(NewFolderName, "game.json"), "[]");

            NewMapPanel.Visibility = Visibility.Collapsed;
            Refresh(null, null);
        }

        private void Refresh(object sender, RoutedEventArgs e)
        {
            maps.Clear();
            GetMaps();
            SearchBox_TextChanged(null, null);
        }

        private void GoMenu(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void MapListPage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (this.NavigationService != null)
                {
                    this.NavigationService.Navigate(new MainMenuPage());
                }
            }
        }


    }
}
