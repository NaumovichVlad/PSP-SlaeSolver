using Core.Methods.Linear;
using Core.Methods.Linear.Factories;
using DataAccess.Managers;
using Microsoft.Win32;
using Server.Loggers;
using Server.Solvers;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly OpenFileDialog _ofDialog;
        private readonly SaveFileDialog _sfDialog;
        public MainWindow()
        {
            InitializeComponent();

            _ofDialog = new OpenFileDialog();
            _sfDialog = new SaveFileDialog();

            InitializeFileDialogs();
            InitializeLinearMethodsCombobox();
        }

        private void InitializeFileDialogs()
        {
            _ofDialog.Filter = "Matrix A(*.A)|*.A|Vector B(*.B)|*.B";
            _sfDialog.Filter = "Result(*.res)|*.res";
        }

        private void InitializeLinearMethodsCombobox()
        {
            LinearMethodsCombobox.ItemsSource = Enum.GetNames(typeof(SlaeSolverMethodsLinear));
            LinearMethodsCombobox.SelectedIndex = 0;
        }

        private void PathASearchLinearButton_Click(object sender, RoutedEventArgs e)
        {
            _ofDialog.ShowDialog();
            PathALinearTextBox.Text = _ofDialog.FileName;
        }

        private void PathBSearchLinearButton_Click(object sender, RoutedEventArgs e)
        {
            _ofDialog.ShowDialog();
            PathBLinearTextBox.Text = _ofDialog.FileName;
        }

        private void PathResSearchLinearButton_Click(object sender, RoutedEventArgs e)
        {
            _sfDialog?.ShowDialog();
            PathResLinearTextBox.Text = _sfDialog.FileName;
        }

        private void CalculateLinearButton_Click(object sender, RoutedEventArgs e)
        {
            var method = Enum.Parse<SlaeSolverMethodsLinear>(LinearMethodsCombobox.SelectedItem.ToString());

            var calculater = new LinearSolver(LinearSolverFactory.GetSolver(method), new FileManagerTxt(), new TimeLogger());

            calculater.Solve(PathALinearTextBox.Text, PathBLinearTextBox.Text, PathResLinearTextBox.Text);

            ResultsLabelTab1.Content = calculater.GetTimeLog();
        }
    }
}
