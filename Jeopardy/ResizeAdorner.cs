using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Jeopardy
{
    public class ResizeAdorner : Adorner
    {
        VisualCollection AdornerVisual;
        Thumb topLeft, topRight, bottomLeft, bottomRight;
        Rectangle Rec;
        Button closeButton; // 1. Declare the close button

        public ResizeAdorner(UIElement adornedElement) : base(adornedElement)
        {
            AdornerVisual = new VisualCollection(this);

            topLeft = new Thumb() { Background = Brushes.White, Height = 12, Width = 12 };
            topRight = new Thumb() { Background = Brushes.White, Height = 12, Width = 12 };
            bottomLeft = new Thumb() { Background = Brushes.White, Height = 12, Width = 12 };
            bottomRight = new Thumb() { Background = Brushes.White, Height = 12, Width = 12 };

            Rec = new Rectangle()
            {
                Stroke = Brushes.LightGray,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection() { 3, 2 },
                IsHitTestVisible = false
            };

            // 2. Initialize the close button with some basic styling
            closeButton = new Button()
            {
                Content = "✖",
                Background = Brushes.Red,
                Foreground = Brushes.White,
                Width = 20,
                Height = 20,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            // 3. Attach the click event
            closeButton.Click += CloseButton_Click;

            topLeft.DragDelta += TopLeft_DragDelta;
            topRight.DragDelta += TopRight_DragDelta;
            bottomLeft.DragDelta += BottomLeft_DragDelta;
            bottomRight.DragDelta += BottomRight_DragDelta;

            AdornerVisual.Add(Rec);
            AdornerVisual.Add(topLeft);
            AdornerVisual.Add(topRight);
            AdornerVisual.Add(bottomLeft);
            AdornerVisual.Add(bottomRight);
            AdornerVisual.Add(closeButton); // Add button to the visual tree
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement adornedElement = this.AdornedElement as FrameworkElement;
            if (adornedElement != null)
            {
                // First, remove this adorner from the AdornerLayer to clean it up
                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
                if (adornerLayer != null)
                {
                    adornerLayer.Remove(this);
                }

                // Next, remove the adorned element from its parent container
                Panel parentPanel = adornedElement.Parent as Panel;
                if (parentPanel != null)
                {
                    parentPanel.Children.Remove(adornedElement);
                }
            }
        }

        private void BottomRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            FrameworkElement fe = this.AdornedElement as FrameworkElement;
            fe.Width = Math.Max(fe.Width + e.HorizontalChange, 10);
            fe.Height = Math.Max(fe.Height + e.VerticalChange, 10);
        }

        private void BottomLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            FrameworkElement fe = this.AdornedElement as FrameworkElement;
            double newWidth = Math.Max(fe.Width - e.HorizontalChange, 10);
            double left = Canvas.GetLeft(fe);

            Canvas.SetLeft(fe, left + (fe.Width - newWidth));
            fe.Width = newWidth;
            fe.Height = Math.Max(fe.Height + e.VerticalChange, 10);
        }

        private void TopRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            FrameworkElement fe = this.AdornedElement as FrameworkElement;
            double newHeight = Math.Max(fe.Height - e.VerticalChange, 10);
            double top = Canvas.GetTop(fe);

            Canvas.SetTop(fe, top + (fe.Height - newHeight));
            fe.Height = newHeight;
            fe.Width = Math.Max(fe.Width + e.HorizontalChange, 10);
        }

        private void TopLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            FrameworkElement fe = this.AdornedElement as FrameworkElement;
            double newWidth = Math.Max(fe.Width - e.HorizontalChange, 10);
            double newHeight = Math.Max(fe.Height - e.VerticalChange, 10);

            double left = Canvas.GetLeft(fe);
            double top = Canvas.GetTop(fe);

            Canvas.SetLeft(fe, left + (fe.Width - newWidth));
            Canvas.SetTop(fe, top + (fe.Height - newHeight));

            fe.Width = newWidth;
            fe.Height = newHeight;
        }

        protected override Visual GetVisualChild(int index)
        {
            return AdornerVisual[index];
        }

        protected override int VisualChildrenCount => AdornerVisual.Count;

        protected override Size ArrangeOverride(Size finalSize)
        {
            FrameworkElement fe = this.AdornedElement as FrameworkElement;

            double w = fe.RenderSize.Width;
            double h = fe.RenderSize.Height;

            Rec.Arrange(new Rect(-5, -5, w + 10, h + 10));

            topLeft.Arrange(new Rect(-5 - topLeft.Width / 2, -5 - topLeft.Height / 2, topLeft.Width, topLeft.Height));
            topRight.Arrange(new Rect(w - topRight.Width / 2 + 5, -5 - topRight.Height / 2, topRight.Width, topRight.Height));
            bottomLeft.Arrange(new Rect(-5 - bottomLeft.Width / 2, h - bottomLeft.Height / 2 + 5, bottomLeft.Width, bottomLeft.Height));
            bottomRight.Arrange(new Rect(w - bottomRight.Width / 2 + 5, h - bottomRight.Height / 2 + 5, bottomRight.Width, bottomRight.Height));

            // 4. Position the close button just outside the top-right resize thumb
            closeButton.Arrange(new Rect(w + 10, -20, closeButton.Width, closeButton.Height));

            return finalSize;
        }
    }
}