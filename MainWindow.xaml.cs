using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TrainerTextsWPF;

namespace BlindTyping
{
    public partial class MainWindow : Window
    {
        public static DataContent data = new DataContent();
        private GraphLevel graphLevel = new GraphLevel();
        private FileManager fileManager;
        private int ErrorCount;
        private bool _isTrainingStart = false;
        Stopwatch stopwatch = new Stopwatch();
        public MainWindow()
        {
            InitializeComponent();
            ProgressBarValue = graphLevel.UpdateGraph(ProgressBarValue, ref Namelevel);
            fileManager = new FileManager();
            fileManager.GetFilesInPath();
            comboBoxFiles = fileManager.SetValueComboBox(comboBoxFiles);

            data = fileManager.Deserialize(fileManager.GetFirstFile());
            ContentText.Text = data.Text;
        }

        private void СreateFile(object sender, RoutedEventArgs e)
        {
            Window window = new Window()
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dad7cd")),
                Height = 200,
                Width = 200
            };
            StackPanel panel = new StackPanel()
            {
                Orientation = Orientation.Vertical,
            };
            Label label = new Label()
            {
                Content = "Название файла"
            };
            TextBox textBox = new TextBox();
            panel.Children.Add(label);
            panel.Children.Add(textBox);
            window.Content = panel;
            window.ShowDialog();
            if (textBox.Text == "" || File.Exists(System.IO.Path.Combine(fileManager.GetPathFile(), $"{textBox.Text}.json")))
            {
                MessageBox.Show("Файл либо существует, либо поле пустое.");
                return;
            }
            else
            {
                DataContent dataContent = new DataContent();
                dataContent.NameFile = textBox.Text;
                dataContent.Text = "";

                using (File.Create(System.IO.Path.Combine(fileManager.GetPathFile(), $"{textBox.Text}.json"))) { };
                fileManager.Serialize(dataContent, dataContent.NameFile);
                fileManager.AddFile(dataContent.NameFile);

                comboBoxFiles = fileManager.SetValueComboBox(comboBoxFiles);

            }

        }

        private void EditText(object sender, RoutedEventArgs e)
        {
            TextBox textBox1 = new TextBox()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                FontSize = 15,
                Text = ContentText.Text,
                TextWrapping = TextWrapping.Wrap,
                BorderThickness = new Thickness(0)
            };
            Window window = new Window()
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dad7cd"))
            };

            window.Content = textBox1;
            window.ShowDialog();
            data.Text = textBox1.Text;
            fileManager.Serialize(data, data.NameFile);

            ContentText.Text = textBox1.Text;
            TextBoxInput.Text = "";

            stopwatch.Stop();
            _isTrainingStart = false;
        }

        private void DeleteText(object sender, RoutedEventArgs e)
        {
            TextBoxInput.Text = "";
            stopwatch.Reset();
            _isTrainingStart = false;
        }

        private void isErrorIngored_Click(object sender, RoutedEventArgs e)
        {
            if (isErrorIngored.IsChecked ?? false)
            {
                if (TextBoxInput.Text.Length == 0)
                    return;
                if (MessageBox.Show("введенный вами текст - будет удален. Точно применить?", "Внимание", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    TextBoxInput.Text = "";
                }

            }
        }

        private void isCommasIngored_Click(object sender, RoutedEventArgs e)
        {
            if (isCommasIngored.IsChecked ?? false)
            {
                string tempText = ContentText.Text;
                tempText = Regex.Replace(tempText, @"\W+", " ");
                ContentText.Text = tempText;
                stopwatch.Reset();
                TypingTime.Text = "";
                _isTrainingStart = false;
            }
        }

        void OnComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            int selectedIndex = comboBox.Items.IndexOf(comboBox.SelectedItem);

            data = fileManager.Deserialize(fileManager.GetIndexFile(selectedIndex));
            ContentText.Text = data.Text;
        }
        private async void ShowTime()
        {
            while (true)
            {
                await Task.Delay(500);
                Dispatcher.Invoke(() =>
                {
                    if (stopwatch.IsRunning)
                        TypingTime.Text = stopwatch.Elapsed.TotalSeconds.ToString("0.000");
                    else
                        return;
                });
            }
        }
        private void TextBoxInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (ContentText.Text.Length == 0)
                {
                    string tempText = TextBoxInput.Text;
                    string substring = tempText.Substring(0, tempText.Length - 1);
                    TextBoxInput.Text = substring;
                    ErrorCount = 0;
                    return;
                }

                if (!_isTrainingStart && TextBoxInput.Text.Length != 0)
                {
                    stopwatch.Restart();
                    _isTrainingStart = true;
                    ShowTime();
                }
                if (TextBoxInput.Text.Length == ContentText.Text.Length)
                {
                    stopwatch.Stop();
                    ResultGeneral(stopwatch.Elapsed.TotalSeconds, ErrorCount, ContentText.Text.Length);
                    ProgressBarValue = graphLevel.UpdateGraph(ProgressBarValue, ref Namelevel, ContentText.Text.Length);
                    ErrorCount = 0;
                    _isTrainingStart = false;
                    TextBoxInput.Text = "";
                }
                else
                {
                    string textFromTxt = ContentText.Text;
                    string textInput = TextBoxInput.Text;

                    for (int i = 0; i < textInput.Length; i++)
                    {
                        if (textInput[i] != textFromTxt[i])
                        {
                            if (isErrorIngored.IsChecked ?? false)
                            {
                                string tempText = TextBoxInput.Text;
                                string substring = tempText.Substring(0, tempText.Length - 1);
                                TextBoxInput.Text = substring;
                                TextBoxInput.CaretIndex = substring.Length;
                            }
                            else
                            {
                                ErrorCount++;
                                break;
                            }
                        }
                    }
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                MessageBox.Show($"{"Добавьте текст для печати."}");
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex}");
                return;
            }
        }
        
        private void ResultGeneral(double time, int errors, int length)
        {

            double resultRPM = length / time;
            TypingSpeed.Text = $"Длина текста: {length}";
            CountErrors.Text = $"Кол-во ошибок: {errors}";
            LengthText.Text = $"Скорость печати: {resultRPM:F3} RPM";
        }

    }
    public class FileManager
    {
        private List<string> files = new List<string>();
        private static string PathDirectory = $"{System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TrainerTexts")}";
        private string PathFile = $"{System.IO.Path.Combine(PathDirectory, "Texts")}";
        public string GetIndexFile(int index) =>  files[index];
        public string GetPathFile() => PathFile;
        public void AddFile(string nameFile) => files.Add(nameFile);
        public string GetFirstFile() => files[0];

        public FileManager()
        {
            CheakExtension();
        }
        public ComboBox SetValueComboBox(ComboBox comboBox)
        {
            comboBox.ItemsSource = null;
            comboBox.ItemsSource = files;
            return comboBox;
        }
        public void GetFilesInPath()
        {
            try
            {
                DirectoryInfo? dir = new DirectoryInfo(PathFile);
                FileInfo[] fileList = dir.GetFiles();

                foreach (var file in fileList)
                {
                    files.Add(System.IO.Path.GetFileNameWithoutExtension(file.FullName));
                }
            }
            catch (Exception e)
            {
                throw new Exception($"{e}");
            }
        }
        public void Serialize(DataContent dataContent, string nameFile)
        {

            string text = JsonSerializer.Serialize(dataContent);

            using StreamWriter writer = new StreamWriter(System.IO.Path.Combine(PathFile, $"{nameFile}.json"));
            writer.Write(text);
            writer.Close();
        }
        public DataContent Deserialize(string fileName)
        {
            string pathFile = System.IO.Path.Combine(PathFile, $"{fileName}.json");
            using StreamReader reader = new StreamReader(pathFile);
            string json = reader.ReadToEnd();

            DataContent? dataContent = JsonSerializer.Deserialize<DataContent>(json);

            if (dataContent is not null)
                return dataContent;
            else
                throw new ArgumentNullException($"Invalid deserialize {pathFile}");
        }
        public void CheakExtension()
        {

            if (!Directory.Exists(PathDirectory))
                Directory.CreateDirectory(PathDirectory);

            if (!Directory.Exists(PathFile))
            {
                Directory.CreateDirectory(PathFile);
                DataContent dataContent = new DataContent()
                {
                    NameFile = "Русский набор",
                    Text = "Cтолица России - Москва"
                };
                using (File.Create(System.IO.Path.Combine(PathFile, $"{dataContent.NameFile}.json"))) { };
                Serialize(dataContent, dataContent.NameFile);
                MainWindow.data = dataContent; // передаю как первый файл  
            }
        }
    }
    [Serializable]
    public class DataContent
    {
        public string? Text { get; set; }
        public string? NameFile { get; set; }
    }

}
