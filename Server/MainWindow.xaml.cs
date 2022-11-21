using Core.Methods.Linear;
using Core.Methods.Linear.Factories;
using DataAccess.Managers;
using Microsoft.Win32;
using Server.Loggers;
using Server.Solvers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            PathAParallelTextBox.Text = "E:\\Study\\Current\\ПСП\\Курсовая работа\\TestData\\test1.A";
            PathBParallelTextBox.Text = "E:\\Study\\Current\\ПСП\\Курсовая работа\\TestData\\test1.B";
            PathResParallelTextBox.Text = "E:\\Study\\Current\\ПСП\\Курсовая работа\\TestData\\test.des";

            PathALinearTextBox.Text = "E:\\Study\\Current\\ПСП\\Курсовая работа\\TestData\\test1.A";
            PathBLinearTextBox.Text = "E:\\Study\\Current\\ПСП\\Курсовая работа\\TestData\\test1.B";
            PathResLinearTextBox.Text = "E:\\Study\\Current\\ПСП\\Курсовая работа\\TestData\\test.des";

            _ofDialog.Filter = "Matrix A(*.A)|*.A|Vector B(*.B)|*.B|Results(*.des)|*.des";
            _sfDialog.Filter = "Result(*.des)|*.des";
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

            OpenResults(PathResLinearTextBox.Text);
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

        private void PathVerSearchParallelButton_Click(object sender, RoutedEventArgs e)
        {
            _ofDialog.ShowDialog();
            PathVerParallelTextBox.Text = _ofDialog.FileName;
        }

        private void PathResSearchParallelButton_Click(object sender, RoutedEventArgs e)
        {
            _sfDialog?.ShowDialog();
            PathResParallelTextBox.Text = _sfDialog.FileName;
        }

        private void StartServerButton_Click(object sender, RoutedEventArgs e)
        {
            _server = new ParallelSolver(new FileManagerTxt(), int.Parse(WaitClientsCountTextBox.Text), new TimeLogger());
            _server.Notify += UpdateConnections;
            _server.StartServer(int.Parse(ServerPortTextBox.Text), ServerIpTextBox.Text);

            ResultsLabelTab2.Content += $"Server started in {ServerIpTextBox.Text}:{ServerPortTextBox.Text}\n";
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

        private void CalculateParallelButton_Click(object sender, RoutedEventArgs e)
        {
            var results = _server.Solve(PathAParallelTextBox.Text, PathBParallelTextBox.Text, PathResParallelTextBox.Text);

            var fileManager = new FileManagerTxt();
            fileManager.SaveResults(PathResParallelTextBox.Text, results);

            ResultsLabelTab2.Content += "\n\n" + _server.GetTimeLog() + "\n";

            if (VerifyCheckBox.IsChecked == true)
            {
                var expectedResults = fileManager.ReadVector(PathVerParallelTextBox.Text);

                if (VerifyResults(expectedResults, results))
                {
                    ResultsLabelTab2.Content += "\nПроверка результатов успешно завершена\n";
                }
                else
                {
                    ResultsLabelTab2.Content += "\nПроверка результатов не пройдена!\n";
                }
            }

            OpenResults(PathResParallelTextBox.Text);

        }

        private void OpenResults(string resultsPath)
        {
            var p = new Process();

            p.StartInfo = new ProcessStartInfo(resultsPath)
            {
                UseShellExecute = true
            };

            p.Start();
        }
        private bool VerifyResults(double[] expected, double[] actual)
        {
            if (expected.Length == actual.Length)
            {
                for (var i = 0; i < expected.Length; i++)
                {
                    if (Math.Abs(expected[i] - actual[i]) > Math.Pow(1, -5))
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        private void VerifyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (VerifyCheckBox.IsChecked == true)
            {
                PathVerParallelTextBox.IsReadOnly = false;
                PathVerSearchParallelButton.IsEnabled = true;
            }
            else
            {
                PathVerParallelTextBox.IsReadOnly = true;
                PathVerSearchParallelButton.IsEnabled = false;
            }
        }
    }
}
