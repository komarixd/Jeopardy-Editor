using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace Jeopardy
{
    /// <summary>
    /// Interaction logic for Page1.xaml
    /// </summary>
    public partial class InGameJeopardy : Page
    {
        string folder;
        Random rnd;

        public InGameJeopardy(string folder)
        {
            InitializeComponent();
            this.KeepAlive = true;
            rnd = new Random();
            this.folder = folder;

            MapTitleText.Text = File.ReadAllLines(System.IO.Path.Combine(folder, "mapinfo"))[0];

            LoadGame();
        }

        private void AddColumn()
        {
            StackPanel columnStack = new StackPanel
            {
                Width = 250,
                Margin = new Thickness(5)
            };

            Grid headerGrid = new Grid();

            TextBlock titleBox = new TextBlock
            {
                Text = "CATEGORY",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(10, 50, 120)),
                Foreground = Brushes.White,
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 10),
            };

            StackPanel rowContainer = new StackPanel();

            headerGrid.Children.Add(titleBox);

            columnStack.Children.Add(headerGrid);
            columnStack.Children.Add(rowContainer);

            BoardContainer.Children.Add(columnStack);
        }

        private void AddCell(StackPanel container)
        {
            Border cellBorder = new Border
            {
                Height = 120,
                Margin = new Thickness(0, 5, 0, 5),
                Background = new SolidColorBrush(Color.FromRgb(30, 80, 180)),
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(5),
                Cursor = Cursors.Hand // ADDED: Makes it obvious the tile is clickable during gameplay
            };

            Grid cellGrid = new Grid();

            TextBlock valueInput = new TextBlock
            {
                FontSize = 36,
                FontWeight = FontWeights.ExtraBold,
                Foreground = Brushes.Gold,
                Background = Brushes.Transparent,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            cellBorder.MouseLeftButtonDown += (s, e) =>
            {
                NavigationService.Navigate(new InGameCanvas(valueInput.Tag.ToString(), valueInput));
            };

            cellGrid.Children.Add(valueInput);
            cellBorder.Child = cellGrid;

            container.Children.Add(cellBorder);
        }

        void LoadGame()
        {
            string filePath = System.IO.Path.Combine(this.folder, "game.json");
            if (File.Exists(filePath))
            {
                string jsonData = File.ReadAllText(filePath);
                List<JeopardyColumn> columns = JsonSerializer.Deserialize<List<JeopardyColumn>>(jsonData);
                BoardContainer.Children.Clear();
                foreach (var col in columns)
                {
                    AddColumn();
                    StackPanel columnStack = BoardContainer.Children[BoardContainer.Children.Count - 1] as StackPanel;
                    TextBlock titleBox = (columnStack.Children[0] as Grid).Children[0] as TextBlock;
                    titleBox.Text = col.Title;
                    StackPanel rowContainer = columnStack.Children[1] as StackPanel;
                    rowContainer.Children.Clear();
                    foreach (var cell in col.Cells)
                    {
                        AddCell(rowContainer);
                        Border cellBorder = rowContainer.Children[rowContainer.Children.Count - 1] as Border;
                        TextBlock valueBtn = (cellBorder.Child as Grid).Children[0] as TextBlock;
                        valueBtn.Text = cell.Value;
                        valueBtn.Tag = cell.Path;
                    }
                }
            }
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            this.NavigationService.GoBack();
        }
    }
}