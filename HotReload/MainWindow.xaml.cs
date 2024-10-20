using AnlaxPackage;
using Mono.Cecil;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AnlaxRevitUpdate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string PluginAutoUpdateDirectory { get; set; }
        public bool GoodDownload {  get; set; }
        public bool IsDebug
        {
            get
            {
                if (PluginAutoUpdateDirectory.Contains("AnlaxDev"))
                {
                    return true;
                }
                return false;
            }
        } 

        public MainWindow(List<RevitRibbonPanelCustom> listReload)
        {
            PluginAutoUpdateDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            GoodDownload = true;
            InitializeComponent();
                try
                {
                    TextBlockMessage.Text = "Не закрывайте окно. Идет проверка обновления плагина Anlax\n";
                    Show();
                    ProgressBarDownload.Maximum = listReload.Count;
                    int progress = 0;
                    Task.Run(() =>
                    {
                        foreach (RevitRibbonPanelCustom revitPanel in listReload)
                        {
                            string message = HotReload(revitPanel);
                            string assemblyPath = revitPanel.AssemlyPath;
                            string plugName = GetPluginName(assemblyPath);
                            progress++;
                            Dispatcher.Invoke(() =>
                            {
                                ProgressBarDownload.Value = progress;
                                TextBlockMessage.Text += $"Загрузка {plugName}. {message}\n";
                                TextBlockDownload.Text = $"{progress}/{listReload.Count} загружено";
                            });
                            if (message != "Загрузка прошла успешно" && message != "Загружена актуальная версия плагина Anlax")
                            {
                                GoodDownload = false;
                            }
                        }
                        if (GoodDownload)
                        {
                            Timer timer = new Timer(CloseWindowCallback, null, 2000, Timeout.Infinite);
                        }

                    });

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error", $"Ошибка в автообновлении: {ex.StackTrace}");
                }

        }
        private void CloseWindowCallback(object state)
        {
            // Этот метод будет вызван после истечения 5 секунд
            Dispatcher.Invoke(() =>
            {
                // Закрыть окно
                Close();
            });
        }
        private string HotReload(RevitRibbonPanelCustom revitRibbonPanelCustom)
        {
            Assembly assembly = revitRibbonPanelCustom.AssemblyLoad;
            // Ищем класс "ApplicationStart"
            Type typeStart = assembly.GetTypes()
.Where(t => t.GetInterfaces().Any(i => i == typeof(IPluginUpdater)))
.FirstOrDefault();

            if (typeStart != null)
            {
                object instance = Activator.CreateInstance(typeStart);
                MethodInfo onStartupMethod = typeStart.GetMethod("DownloadPluginUpdate");
                var allMethods = typeStart.GetMethods();
                if (onStartupMethod != null)
                {
                    string message = (string)onStartupMethod.Invoke(instance, new object[] { revitRibbonPanelCustom.AssemlyPath, IsDebug });
                    return message;
                }
            }
            return "Ошибка обновления";
        }
        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonAnik_Click(object sender, RoutedEventArgs e)
        {
            string anik = "Идет Бог по Раю\n\n" +
              "Видит, сады горят\n" +
              "На грушевый пофигу\n" +
              "А яблочный\n" +
              "Спас";
            MessageBox.Show(anik);
        }


        private void Button_Click_Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private string GetPluginName(string filePath)
        {
            // Получаем путь до файла без последней части
            string directory = System.IO.Path.GetDirectoryName(filePath);

            // Разбиваем путь на папки
            string[] pathParts = directory.Split(System.IO.Path.DirectorySeparatorChar);

            // Получаем имя файла без расширения
            string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(filePath);

            // Берем две последние папки и имя файла
            string result = System.IO.Path.Combine(pathParts[pathParts.Length - 1], fileNameWithoutExtension);

            return result;
        }
        private void ButtonRestart_Click(object sender, RoutedEventArgs e)
        {
            // Получаем текущий путь к исполняемому файлу
            string appPath = Process.GetCurrentProcess().MainModule.FileName;

            // Запускаем новый процесс
            Process.Start(appPath);

            // Завершаем текущее приложение
            Application.Current.Shutdown();
        }
    }
}