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
        string PluginDirectory
        {
            get
            {
                var directoryInfo = new System.IO.DirectoryInfo(PluginAutoUpdateDirectory);
                var targetDirectory = directoryInfo.Parent;
                return targetDirectory.FullName;
            }
        }
        public string RevitVersion
        {
            get
            {
                var directoryInfo = new System.IO.DirectoryInfo(PluginAutoUpdateDirectory);

                // Поднимаемся на 3 уровня вверх
                var targetDirectory = directoryInfo.Parent.Parent;

                // Получаем название папки (в вашем случае это версия Revit)
                string revitVersion = targetDirectory.Name;
                return revitVersion;
            }
        }
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
            byte[] assemblyBytes = File.ReadAllBytes(revitRibbonPanelCustom.AssemlyPath);

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
        public List<string> FindDllsWithApplicationStart()
        {
            List<string> result = new List<string>();

            // Рекурсивно ищем все файлы с расширением .dll
            var dllFiles = Directory.GetFiles(PluginDirectory, "*.dll", SearchOption.AllDirectories);

            foreach (var dll in dllFiles)
            {
                using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(dll))
                {
                    try
                    {
                        TypeDefinition typeStart = null;
                        foreach (var type in assemblyDefinition.MainModule.Types)
                        {
                            // Проверяем, реализует ли тип интерфейс IPluginUpdater
                            if (type.Interfaces.Any(i => i.InterfaceType.Name == "IPluginUpdater"))
                            {
                                typeStart = type;
                                break;
                            }
                        }

                        if (typeStart != null)
                        {
                            result.Add(dll);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Логируем ошибки, если нужно
                        // Console.WriteLine($"Ошибка при обработке {dll}: {ex.Message}");
                    }
                }

            }

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