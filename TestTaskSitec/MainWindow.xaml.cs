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
        private string pathRKK = "";
        private string pathAppeals = "";

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
                TextBlock_PathRKK.Text = "Файл РКК: " + openFileDialog.SafeFileName ;
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
                    if(type == "RKK")
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

            DataGrid_ResultTable.Columns[0].Header = "Ответственный исполнитель";
            DataGrid_ResultTable.Columns[1].Header = "Количество неисполненных входящих документов";
            DataGrid_ResultTable.Columns[2].Header = "Количество неисполненных письменных обращений граждан";
            DataGrid_ResultTable.Columns[3].Header = "Общее количество документов и обращений";

        }
        private void Button_Click_LoadTable(object sender, RoutedEventArgs e)
        {
            if (pathRKK == "" || pathAppeals == "")
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

            stopWatch.Stop();

            runTime.Text = $"Время выполнения: {stopWatch.ElapsedMilliseconds} мс";
            total.Text = $"Не исполнено в срок {executorsTable.Sum(p => p.Value.counterRKK + p.Value.counterAppeals)} документов, из них:";
            totalRKK.Text = $"- количество неисполненных входящих документов: {executorsTable.Sum(p => p.Value.counterRKK)};";
            totalAppeals.Text = $"- количество неисполненных письменных обращений граждан: {executorsTable.Sum(p => p.Value.counterAppeals)}.";
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
            if(executors == null)
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
                    writer.WriteLine("{0,4} |{1,22} |{2,12} |{3,18} |{4,14} |", "№", "Исполнитель", "Кол-во РКК", "Кол-во обращений", "Общее кол-во");
                    for (int i = 0; i < executors.Length; i++)
                    {
                        writer.WriteLine("{0,4} |{1,22} |{2,12} |{3,18} |{4,14} |", i, executors[i].executor, executors[i].countRKK, executors[i].countAppeals, executors[i].countRKK + executors[i].countAppeals);

                    }
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
