using System;
using System.Collections.Generic;
using System.IO;
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

namespace Jeopardy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // BG COLOR #242e9c
        static Cursor CCursor = new Cursor(Application.GetResourceStream(new Uri("/Resources/Cursors/cursor_white.cur", UriKind.Relative)).Stream);
        public MainWindow()
        {
            InitializeComponent();
            Mouse.OverrideCursor = CCursor;
            string exeFolder = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Directory.CreateDirectory(exeFolder + "\\Maps");
            MainFrame.Navigate(new MainMenuPage());
            //MainFrame.Navigate(new EditCanvasPage());
        }
    }
}
