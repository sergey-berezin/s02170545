using System.Windows;
using System.Windows.Forms;

namespace WPFClient {
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
        }

        private void MatchClicked(object sender, RoutedEventArgs e) {
            dataView.StartMatching();
        }

        private void StopClicked(object sender, RoutedEventArgs e) {
            dataView.StopMatching();
        }
    }
}
