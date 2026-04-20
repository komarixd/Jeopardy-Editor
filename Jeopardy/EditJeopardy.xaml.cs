using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Jeopardy
{
    public class JeopardyColumn
    {
        public string Title { get; set; }
        public List<JeopardyCell> Cells { get; set; }
    }

    public class JeopardyCell
    {
        public string Value { get; set; }
        public string Path { get; set; }
    }

    public partial class EditJeopardy : Page
    {
        string folder;
        private Point _dragStartPoint;
        Random rnd;
        Button targetBtn;

        // NEW: Flag to prevent autosaving while the board is actively loading
        private bool _isLoading = false;

        public EditJeopardy(string folder)
        {
            InitializeComponent();
            rnd = new Random();
            this.folder = folder;
            MapTitleText.Text = File.ReadAllLines(System.IO.Path.Combine(folder, "mapinfo"))[0];
            LoadGame();
        }

        // NEW: Helper method to trigger save only if we aren't loading
        private void AutoSave()
        {
            if (_isLoading) return;
            SaveGame(null, null);
        }

        private void AddColumnBtn_Click(object sender, RoutedEventArgs e)
        {
            StackPanel columnStack = new StackPanel
            {
                Width = 250,
                Margin = new Thickness(5)
            };

            Grid headerGrid = new Grid();

            TextBox titleBox = new TextBox
            {
                Text = "CATEGORY",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(10, 50, 120)),
                Foreground = Brushes.White,
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 10),
                BorderThickness = new Thickness(2),
                BorderBrush = Brushes.White
            };

            // NEW: Save whenever the category title loses focus or is changed
            titleBox.LostFocus += (s, ev) => AutoSave();

            Button deleteColumnBtn = new Button
            {
                Content = "X",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = Brushes.Red,
                Width = 20,
                Height = 20,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 5, 5, 0),
                Cursor = Cursors.Hand
            };

            StackPanel rowContainer = new StackPanel { AllowDrop = true }; // ENABLE DRAG DROP
            rowContainer.Drop += RowContainer_Drop;

            deleteColumnBtn.Click += (s, ev) =>
            {
                // NEW: Delete all cell folders associated with this column before removing it
                foreach (Border cell in rowContainer.Children)
                {
                    if (cell.Child is Grid cGrid && cGrid.Children[0] is Button vBtn)
                    {
                        string cellFolder = vBtn.Tag?.ToString();
                        if (!string.IsNullOrEmpty(cellFolder) && Directory.Exists(cellFolder))
                        {
                            try { Directory.Delete(cellFolder, true); } catch { }
                        }
                    }
                }

                BoardContainer.Children.Remove(columnStack);
                AutoSave(); // AUTOSAVE AFTER REMOVING COLUMN
            };

            headerGrid.Children.Add(titleBox);
            headerGrid.Children.Add(deleteColumnBtn);

            Button addRowBtn = new Button
            {
                Content = "+ Add Row",
                Height = 40,
                Margin = new Thickness(0, 5, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                Foreground = Brushes.LightGray,
                FontSize = 14
            };

            addRowBtn.Click += (s, ev) =>
            {
                AddCell(rowContainer);
                AutoSave(); // AUTOSAVE AFTER ADDING A CELL
            };

            columnStack.Children.Add(headerGrid);
            columnStack.Children.Add(rowContainer);
            columnStack.Children.Add(addRowBtn);

            if (!_isLoading) for (int i = 0; i < 3; i++) { AddCell(rowContainer); }

            BoardContainer.Children.Add(columnStack);
            AutoSave(); // AUTOSAVE AFTER ADDING A NEW COLUMN
        }

        private void AddCell(StackPanel container, bool isNew = true)
        {
            int value = 200;

            int r = rnd.Next(1, 9999999);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            string NewFolderName = System.IO.Path.Combine(this.folder, $"{timestamp}_{r}");

            if (isNew)
            {
                Directory.CreateDirectory(NewFolderName);
                File.WriteAllText(System.IO.Path.Combine(NewFolderName, "hints.json"), "[{\"HintTitle\":\"Hint\",\"Items\":[]}]");
            }

            Border cellBorder = new Border
            {
                Height = 120,
                Margin = new Thickness(0, 5, 0, 5),
                Background = new SolidColorBrush(Color.FromRgb(30, 80, 180)),
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(5)
            };

            Grid cellGrid = new Grid();

            Button valueInput = new Button
            {
                Content = $"{value}",
                FontSize = 36,
                FontWeight = FontWeights.ExtraBold,
                Foreground = Brushes.Gold,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Tag = NewFolderName
            };

            Button deleteCellBtn = new Button
            {
                Content = "×",
                FontSize = 14,
                Width = 20,
                Height = 20,
                Background = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0)),
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(5),
                BorderThickness = new Thickness(0),
                Visibility = Visibility.Collapsed
            };

            cellBorder.MouseEnter += (s, e) => deleteCellBtn.Visibility = Visibility.Visible;
            cellBorder.MouseLeave += (s, e) => deleteCellBtn.Visibility = Visibility.Collapsed;

            deleteCellBtn.Click += (s, e) =>
            {
                Panel currentParent = cellBorder.Parent as Panel;
                if (currentParent != null)
                {
                    currentParent.Children.Remove(cellBorder);
                }

                // NEW: Delete the cell's folder
                string cellFolder = valueInput.Tag?.ToString();
                if (!string.IsNullOrEmpty(cellFolder) && Directory.Exists(cellFolder))
                {
                    try { Directory.Delete(cellFolder, true); } catch { /* Ignore locked file errors */ }
                }

                AutoSave(); // AUTOSAVE AFTER REMOVING A CELL
            };

            valueInput.Click += (s, e) =>
            {
                targetBtn = valueInput;
                NewScore.Text = targetBtn.Content.ToString();
                EditPanel.Visibility = Visibility.Visible;
            };

            cellGrid.Children.Add(valueInput);
            cellGrid.Children.Add(deleteCellBtn);

            cellBorder.Child = cellGrid;

            cellBorder.PreviewMouseLeftButtonDown += (s, e) =>
            {
                _dragStartPoint = e.GetPosition(null);
            };

            cellBorder.MouseMove += Cell_MouseMove;

            container.Children.Add(cellBorder);
        }

        private void Cell_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _dragStartPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    Border dragSource = sender as Border;
                    if (dragSource != null)
                    {
                        DragDrop.DoDragDrop(dragSource, dragSource, DragDropEffects.Move);
                    }
                }
            }
        }

        private void RowContainer_Drop(object sender, DragEventArgs e)
        {
            StackPanel targetContainer = sender as StackPanel;
            Border droppedCell = e.Data.GetData(typeof(Border)) as Border;

            if (targetContainer != null && droppedCell != null)
            {
                StackPanel oldParent = droppedCell.Parent as StackPanel;
                if (oldParent != null)
                {
                    oldParent.Children.Remove(droppedCell);
                }

                int index = 0;
                foreach (UIElement child in targetContainer.Children)
                {
                    Point p = e.GetPosition(child);
                    if (p.Y < (child as FrameworkElement).ActualHeight / 2) break;
                    index++;
                }

                targetContainer.Children.Insert(index, droppedCell);

                AutoSave(); // AUTOSAVE AFTER REORDERING/DRAG DROP
            }
        }

        private void SaveData(object sender, RoutedEventArgs e)
        {
            targetBtn.Content = NewScore.Text;
            EditPanel.Visibility = Visibility.Collapsed;

            AutoSave(); // AUTOSAVE AFTER EDITING A CELL SCORE
        }

        private void EditGame(object sender, RoutedEventArgs e)
        {
            EditPanel.Visibility = Visibility.Collapsed;
            NavigationService.Navigate(new EditCanvasPage(targetBtn.Tag.ToString()));
        }

        private void HideEditPanel(object sender, RoutedEventArgs e)
        {
            EditPanel.Visibility = Visibility.Collapsed;
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void SaveGame(object sender, RoutedEventArgs e)
        {
            List<JeopardyColumn> columns = new List<JeopardyColumn>();
            foreach (StackPanel column in BoardContainer.Children)
            {
                TextBox titleBox = (column.Children[0] as Grid).Children[0] as TextBox;
                StackPanel rowContainer = column.Children[1] as StackPanel;
                JeopardyColumn colData = new JeopardyColumn
                {
                    Title = titleBox.Text,
                    Cells = new List<JeopardyCell>()
                };
                foreach (Border cell in rowContainer.Children)
                {
                    Button valueBtn = (cell.Child as Grid).Children[0] as Button;
                    colData.Cells.Add(new JeopardyCell
                    {
                        Value = valueBtn.Content.ToString(),
                        Path = valueBtn.Tag.ToString()
                    });
                }
                columns.Add(colData);
            }
            string jsonData = JsonSerializer.Serialize(columns, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(System.IO.Path.Combine(this.folder, "game.json"), jsonData);
        }

        private void LoadGame()
        {
            _isLoading = true; // Suspend AutoSave while populating the board

            string filePath = System.IO.Path.Combine(this.folder, "game.json");
            if (File.Exists(filePath))
            {
                string jsonData = File.ReadAllText(filePath);
                List<JeopardyColumn> columns = JsonSerializer.Deserialize<List<JeopardyColumn>>(jsonData);
                BoardContainer.Children.Clear();
                foreach (var col in columns)
                {
                    AddColumnBtn_Click(null, null);
                    StackPanel columnStack = BoardContainer.Children[BoardContainer.Children.Count - 1] as StackPanel;
                    TextBox titleBox = (columnStack.Children[0] as Grid).Children[0] as TextBox;
                    titleBox.Text = col.Title;
                    StackPanel rowContainer = columnStack.Children[1] as StackPanel;
                    rowContainer.Children.Clear();
                    foreach (var cell in col.Cells)
                    {
                        AddCell(rowContainer, false);
                        Border cellBorder = rowContainer.Children[rowContainer.Children.Count - 1] as Border;
                        Button valueBtn = (cellBorder.Child as Grid).Children[0] as Button;
                        valueBtn.Content = cell.Value;
                        valueBtn.Tag = cell.Path;
                    }
                }
            }

            _isLoading = false; // Resume AutoSave functionality
        }

        private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && EditPanel.Visibility == Visibility.Visible)
            {
                EditPanel.Visibility = Visibility.Collapsed;
                return;
            }
            AutoSave();
            NavigationService.GoBack();
        }
    }
}