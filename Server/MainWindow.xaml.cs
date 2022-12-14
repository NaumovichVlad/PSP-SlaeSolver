using Core.Methods.Linear;
using Core.Methods.Linear.Factories;
using DataAccess.Managers;
using Microsoft.Win32;
using Server.Loggers;
using Server.Solvers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;

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
            PathNodesParallelTextBox.Text = "E:\\Study\\Current\\ПСП\\Курсовая работа\\TestData\\nodes.txt";
            PathLoadTestParallelTextBox.Text = "E:\\Study\\Current\\ПСП\\Курсовая работа\\TestData\\loadTests.txt";

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
            _server = new ParallelSolver(int.Parse(ServerPortTextBox.Text), ServerIpTextBox.Text, new FileManagerTxt(), new TimeLogger());
            _server.Notify += UpdateConnections;
            ResultsLabelTab2.Content += $"Server started in {ServerIpTextBox.Text}:{ServerPortTextBox.Text}\n";
            _server.StartServer(PathNodesParallelTextBox.Text);
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
                LoadTesting(results, fileManager);
            }

            OpenResults(PathResParallelTextBox.Text);

        }

        private void LoadTesting(double[] results, IFileManager fileManager)
        {
            var expectedResults = fileManager.ReadVector(PathVerParallelTextBox.Text);

            var loadTestREsults = VerifyResults(expectedResults, results);

            fileManager.SaveLoadTestingResults(loadTestREsults, PathLoadTestParallelTextBox.Text);

            ResultsLabelTab2.Content += "\nПроверка результатов успешно завершена\n";

            OpenResults(PathLoadTestParallelTextBox.Text);
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
        private double[] VerifyResults(double[] expected, double[] actual)
        {
            var results = new double[5];
            var errorVector = new double[actual.Length];

            for (var i = 0; i < expected.Length; i++)
            {
                errorVector[i] = actual[i] - expected[i];
            }

            results[0] = expected.Length;
            results[1] = int.Parse(ClientsCountTextBox.Text);
            results[3] = errorVector.Min(el => Math.Abs(el));
            results[2] = errorVector.Max(el => Math.Abs(el));
            results[4] = Math.Sqrt(errorVector.Sum(el => Math.Pow(el, 2)));

            return results;
        }

        private void VerifyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (VerifyCheckBox.IsChecked == true)
            {
                PathVerParallelTextBox.IsReadOnly = false;
                PathVerSearchParallelButton.IsEnabled = true;
                PathLoadTestSearchParallelButton.IsEnabled = true;
            }
            else
            {
                PathVerParallelTextBox.IsReadOnly = true;
                PathVerSearchParallelButton.IsEnabled = false;
                PathLoadTestSearchParallelButton.IsEnabled = false;
            }
        }

        private void PathNodesSearchParallelButton_Click(object sender, RoutedEventArgs e)
        {
            var ofDialog = new OpenFileDialog();

            ofDialog.Filter = "Addresses (*.txt)|*.txt";

            ofDialog.ShowDialog();
            PathNodesParallelTextBox.Text = _ofDialog.FileName;
        }

        private void PathLoadTestSearchParallelButton_Click(object sender, RoutedEventArgs e)
        {
            var ofDialog = new OpenFileDialog();

            ofDialog.Filter = "Loadtest (*.txt)|*.txt";

            ofDialog.ShowDialog();

            PathLoadTestParallelTextBox.Text = _ofDialog.FileName;
        }
    }
}
