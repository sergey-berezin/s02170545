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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Task2 {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public DataView dataView { get; set; }

        public MainWindow() {
            dataView = new DataView();
            DataContext = this;

            InitializeComponent();

            classListBox.SelectionChanged += ClassSelected;
        }

        private void ChooseFileCiick(object sender, RoutedEventArgs e) {
            using (var fbd = new FolderBrowserDialog()) {
                DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath)) {
                    dataView.DirName = fbd.SelectedPath;
                }
            }
        }

        private void ClassSelected(object sender, RoutedEventArgs e) {
            dataView.SelectedClassId = classListBox.SelectedIndex;
            // dataView.classAtList(classListBox.SelectedIndex);
        }

        private void MatchClicked(object sender, RoutedEventArgs e) {
            dataView.StartMatching();
            Console.WriteLine("AfterClicked");
        }

        private void StopClicked(object sender, RoutedEventArgs e) {
            dataView.StopMatching();
        }
    }
}
