using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using Microsoft.Win32;

namespace TestTaskSitec
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, CounterRKKAndAppeals> executorsTable = null;
        private string[] RKK = null;
        private string[] Appeals = null;
        private string pathRKK = null;
        private string pathAppeals = null;
        private Executors[] executors = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        struct CounterRKKAndAppeals
        {
            public int counterRKK { get; set; }
            public int counterAppeals { get; set; }
        }

        struct Executors
        {
            public string executor { get; set; }
            public int countRKK { get; set; }
            public int countAppeals { get; set; }
            public int sum { get; set; }
        }

        private void Button_Click_OpenRKK(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                pathRKK = openFileDialog.FileName;
                TextBlock_PathRKK.Text = "Файл РКК: " + openFileDialog.SafeFileName;
            }
        }

        private void Button_Click_OpenAppeals(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                pathAppeals = openFileDialog.FileName;
                TextBlock_PathAppeals.Text = "Файл Обращений: " + openFileDialog.SafeFileName;
            }
        }

        private void ProcessFile(Dictionary<string, CounterRKKAndAppeals> executorsTable, string[] file, string type)
        {
            for (int i = 0; i < file.Length; i++)
            {
                var partsString = file[i].Split('\t');
                string executor = partsString[0];
                if (partsString[0] == "Климов Сергей Александрович")
                {
                    string[] subordinates = partsString[1].Split(';');
                    executor = subordinates[0].Replace("(Отв.)", "").Trim();
                }
                else
                {
                    string[] fio = executor.Split(' ');
                    executor = fio[0] + ' ' + fio[1].Substring(0, 1) + '.' + fio[2].Substring(0, 1) + '.';
                }

                if (executorsTable.ContainsKey(executor))
                {
                    CounterRKKAndAppeals value = executorsTable[executor];
                    if (type == "RKK")
                        value.counterRKK++;
                    else
                        value.counterAppeals++;
                    executorsTable[executor] = value;
                }
                else
                {
                    if (type == "RKK")
                        executorsTable.Add(executor, new CounterRKKAndAppeals() { counterRKK = 1 });
                    else
                        executorsTable.Add(executor, new CounterRKKAndAppeals() { counterAppeals = 1 });
                }
            }
        }

        private void UpdateTable(string sortType = "default")
        {
            if (executors == null)
                return;

            switch (sortType)
            {
                case "SortByRKK":
                    executors = executors.OrderByDescending(x => x.countRKK).ThenByDescending(x => x.countAppeals).ToArray();
                    sortedType.Text = "Сортировка: по количеству РКК.";

                    break;
                case "SortByAppeals":
                    executors = executors.OrderByDescending(x => x.countAppeals).ThenByDescending(x => x.countRKK).ToArray();
                    sortedType.Text = "Сортировка: по количеству обращений.";
                    break;
                case "SortByRKKAndAppeals":
                    executors = executors.OrderByDescending(x => x.sum).ThenByDescending(x => x.countRKK).ToArray();
                    sortedType.Text = "Сортировка: по общему количеству документов.";
                    break;
                default:
                    executors = executors.OrderByDescending(x => x.executor).ToArray();
                    sortedType.Text = "Сортировка: по фамилии ответственного исполнителя.";
                    break;
            }

            DataGrid_ResultTable.ItemsSource = executors;

            DataGrid_ResultTable.Columns[0].Header = "Ответственный \nисполнитель";
            DataGrid_ResultTable.Columns[1].Header = "Количество неисполненных \nвходящих документов";
            DataGrid_ResultTable.Columns[2].Header = "Количество неисполненных \nписьменных обращений \nграждан";
            DataGrid_ResultTable.Columns[3].Header = "Общее количество \nдокументов и обращений";
        }
        private void Button_Click_LoadTable(object sender, RoutedEventArgs e)
        {
            if (pathRKK == null || pathAppeals == null)
            {
                MessageBox.Show("Не все файлы выбраны!");
                return;
            }
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            RKK = File.ReadAllLines(pathRKK);
            Appeals = File.ReadAllLines(pathAppeals);
            executorsTable = new Dictionary<string, CounterRKKAndAppeals>();
            ProcessFile(executorsTable, RKK, "RKK");
            ProcessFile(executorsTable, Appeals, "Appeals");

            int fullSumRKK = executorsTable.Sum(p => p.Value.counterRKK);
            int fullSumAppeals = executorsTable.Sum(p => p.Value.counterAppeals);
            int fullSumRKKAndAppeals = fullSumRKK + fullSumAppeals;

            stopWatch.Stop();

            runTime.Text = $"Время выполнения: {stopWatch.ElapsedMilliseconds} мс";
            total.Text = $"Не исполнено в срок {fullSumRKKAndAppeals} документов, из них:";
            totalRKK.Text = $"- количество неисполненных входящих документов: {fullSumRKK};";
            totalAppeals.Text = $"- количество неисполненных письменных обращений граждан: {fullSumAppeals}.";
            dateOfCompilation.Text = $"Дата составления справки: {DateTime.Now.ToShortDateString()}";

            executors = executorsTable.Select(
                p => new Executors
                {
                    executor = p.Key,
                    countRKK = p.Value.counterRKK,
                    countAppeals = p.Value.counterAppeals,
                    sum = p.Value.counterRKK + p.Value.counterAppeals
                }).ToArray();
            executorsTable = null;

            UpdateTable();
        }

        private void DataGrid_ResultTable_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = e.Row.GetIndex() + 1;
        }

        private void Button_Click_Save(object sender, RoutedEventArgs e)
        {
            if (executors == null)
            {
                MessageBox.Show("Таблица пустая!");
                return;
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text files (*.txt)|*.txt";
            if (saveFileDialog.ShowDialog() == true)
            {
                using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName, false))
                {
                    DataGrid_ResultTable.Columns[0].Header = "Ответственный \nисполнитель";
                    DataGrid_ResultTable.Columns[1].Header = "Количество неисполненных \nвходящих документов";
                    DataGrid_ResultTable.Columns[2].Header = "Количество неисполненных \nписьменных обращений \nграждан";
                    DataGrid_ResultTable.Columns[3].Header = "Общее количество \nдокументов и обращений";

                    writer.WriteLine("Справка о неисполненных документах и обращениях граждан\n");
                    writer.WriteLine(total.Text);
                    writer.WriteLine(totalRKK.Text);
                    writer.WriteLine(totalAppeals.Text);
                    writer.WriteLine(sortedType.Text);
                    writer.WriteLine();
                    writer.WriteLine("{0,4} |{1,22} |{2,28} |{3,24} |{4,23} |", "№", "Ответственный", "Количество неисполненных", "Количество неисполненных", "Общее количество");
                    writer.WriteLine("{0,4} |{1,22} |{2,28} |{3,24} |{4,23} |", "", "исполнитель", "входящих документов граждан", "письменных обращений", "документов и обращений");
                    writer.WriteLine("----------------------------------------------------------------------------------------------------------------");
                    for (int i = 0; i < executors.Length; i++)
                    {
                        writer.WriteLine("{0,4} |{1,22} |{2,28} |{3,24} |{4,23} |", i, executors[i].executor, executors[i].countRKK, executors[i].countAppeals, executors[i].countRKK + executors[i].countAppeals);

                    }
                    writer.WriteLine(dateOfCompilation.Text);
                }
            }
        }

        private void SortingList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            TextBlock selectedItem = (TextBlock)comboBox.SelectedItem;
            string sortType = selectedItem.Name;

            UpdateTable(sortType);
        }
    }
}
