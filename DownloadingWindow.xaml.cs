using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Net;
using System.Threading;
using System.Windows.Media.Effects;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for DownloadingWindow.xaml
    /// </summary>
    public partial class DownloadingWindow : Window
    {
        public int percentage;
        public DownloadingWindow(string title)
        {
            Title = title;
            InitializeComponent();
        }
        public void OnProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            percentage = e.ProgressPercentage;
            Progress.Value = percentage;
            if (percentage >= Progress.Maximum)
            {
                Thread.Sleep(1000);
                Close();

            }
        }
    }
}
