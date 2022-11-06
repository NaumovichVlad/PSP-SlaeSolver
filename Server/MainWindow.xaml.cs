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
        private ParallelSolver _server;
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

        private void PathASearchParallelButton_Click(object sender, RoutedEventArgs e)
        {
            _ofDialog.ShowDialog();
            PathAParallelTextBox.Text = _ofDialog.FileName;
        }

        private void PathBSearchParallelButton_Click(object sender, RoutedEventArgs e)
        {
            _ofDialog.ShowDialog();
            PathBParallelTextBox.Text = _ofDialog.FileName;
        }

        private void PathResSearchParallelButton_Click(object sender, RoutedEventArgs e)
        {
            _sfDialog?.ShowDialog();
            PathResParallelTextBox.Text = _sfDialog.FileName;
        }

        private void StartServerButton_Click(object sender, RoutedEventArgs e)
        {
            _server = new ParallelSolver(int.Parse(ServerPortTextBox.Text), ServerIpTextBox.Text);
            _server.Notify += UpdateConnections;
            _server.StartServer();

            ResultsLabelTab2.Content += $"Server started in {ServerIpTextBox.Text}:{ServerPortTextBox.Text}";
        }

        private void UpdateConnections(int count, string address)
        {
            if (ClientsCountTextBox.CheckAccess())
            {
                ClientsCountTextBox.Text = count.ToString();
            }
            else
            {
                Dispatcher.BeginInvoke((Action<string>)SetClientsCountTextBox, count.ToString());
            }

            var message = $"\nNew client connected: {address}";

            if (ResultsLabelTab2.CheckAccess())
            {
                ResultsLabelTab2.Content += message;
            }
            else
            {
                Dispatcher.BeginInvoke((Action<string>)SetResultsLabelTab2, message);
            }
        }

        private void SetClientsCountTextBox(string text)
        {
            Action action = new Action(() => { ClientsCountTextBox.Text = text; });

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(action);
            }
            else
            {
                action();
            }
        }

        private void SetResultsLabelTab2(string text)
        {
            Action action = new Action(() => { ResultsLabelTab2.Content += text; });

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(action);
            }
            else
            {
                action();
            }
        }
    }
}
