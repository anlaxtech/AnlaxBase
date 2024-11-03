using AnlaxBase;
using AnlaxBase.HotReload;
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
using System.Windows.Threading;

namespace AnlaxRevitUpdate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string PluginAutoUpdateDirectory { get; set; }
        public bool GoodDownload { get; set; }
        // Добавим событие для завершения
        public event EventHandler UpdateCompleted;

        public bool IsDebug
        {
            get
            {
                return PluginAutoUpdateDirectory.Contains("AnlaxDev");
            }
        }
        public string AssemblyFileVersion => FileVersionInfo
.GetVersionInfo(Assembly.GetExecutingAssembly().Location)
.FileVersion;
        public MainWindow(List<RevitRibbonPanelCustom> listReload)
        {
            PluginAutoUpdateDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            GoodDownload = true;
            InitializeComponent();
            VersionText.Text = AssemblyFileVersion;
            AutoUpdateBox.IsChecked = AuthSettings.Initialize().UpdateStart;
            // Настраиваем UI
            TextBlockMessage.Text = "Не закрывайте окно. Идет проверка обновления плагина Anlax\n";
            ProgressBarDownload.Maximum = listReload.Count + 1;
        }

        // Основная логика обновления
        public void StartUpdate(List<RevitRibbonPanelCustom> listReload)
        {
            int progress = 0;
            Task updateTask = Task.Run(() =>
            {
                foreach (RevitRibbonPanelCustom revitPanel in listReload)
                {
                    string message = HotReload(revitPanel);
                    string assemblyPath = revitPanel.AssemlyPath;
                    string plugName = GetPluginName(assemblyPath);
                    progress++;
                    // Обновляем UI через Dispatcher.InvokeAsync
                    Dispatcher.Invoke(() =>
                    {
                        ProgressBarDownload.Value = progress;
                        TextBlockMessage.Text += $"Загрузка {plugName}. {message}\n";
                        TextBlockDownload.Text = $"{progress}/{listReload.Count + 1} загружено";
                    });



                    if (message != "Загрузка прошла успешно" && message != "Загружена актуальная версия плагина")
                    {
                        GoodDownload = false;
                    }
                }
                string messageMain = ReloadMainPlug();

                // После завершения загрузки
                Dispatcher.Invoke(() =>
                {
                    ProgressBarDownload.Value = ProgressBarDownload.Maximum;
                    TextBlockDownload.Text = "Обновление завершено!";
                    TextBlockMessage.Text += $"Загрузка AnlaxBaseUpdater. {messageMain}\n";
                    TextBlockMessage.Text += "Все обновления завершены!\n";
                });
                // Закрываем окно через 2 секунды, если обновления прошли успешно
                if (GoodDownload)
                {
                    Dispatcher.Invoke(async () =>
                    {
                        await Task.Delay(5000);
                        Close();
                    });
                }
                // Поднимаем событие завершения
                UpdateCompleted?.Invoke(this, EventArgs.Empty);
            });
            }

        public void StartUpdateBehind(List<RevitRibbonPanelCustom> listReload)
        {
            int progress = 0;

            Dispatcher.Invoke(() => this.Hide());
            Task updateTask = Task.Run(() =>
            {
                foreach (RevitRibbonPanelCustom revitPanel in listReload)
                {
                    string message = HotReload(revitPanel);
                    string assemblyPath = revitPanel.AssemlyPath;
                    string plugName = GetPluginName(assemblyPath);
                    progress++;
                    // Обновляем UI через Dispatcher.InvokeAsync
                    Dispatcher.Invoke(() =>
                    {
                        ProgressBarDownload.Value = progress;
                        TextBlockMessage.Text += $"Загрузка {plugName}. {message}\n";
                        TextBlockDownload.Text = $"{progress}/{listReload.Count + 1} загружено";
                    });



                    if (message != "Загрузка прошла успешно" && message != "Загружена актуальная версия плагина")
                    {
                        GoodDownload = false;
                    }
                }
                string messageMain = ReloadMainPlug();

                // После завершения загрузки
                Dispatcher.Invoke(() =>
                {
                    ProgressBarDownload.Value = ProgressBarDownload.Maximum;
                    TextBlockDownload.Text = "Обновление завершено!";
                    TextBlockMessage.Text += $"Загрузка AnlaxBaseUpdater. {messageMain}\n";
                    TextBlockMessage.Text += "Все обновления завершены!\n";
                });

                // Закрываем окно через 2 секунды, если обновления прошли успешно
                // Проверяем результат и управляем отображением окна через диспетчер
                Dispatcher.Invoke(async () =>
                {
                    if (GoodDownload)
                    {
                        await Task.Delay(2000);
                        Close();
                    }
                    else
                    {
                        this.Show(); // Показ окна, если обновление не удалось
                    }
                });
                // Поднимаем событие завершения
                UpdateCompleted?.Invoke(this, EventArgs.Empty);
            });
        }
        private string ReloadMainPlug()
        {
            string pathToBaseDll = System.IO.Path.Combine(PluginAutoUpdateDirectory, "AutoUpdate\\AnlaxRevitUpdate.dll");
            string userName = "anlaxtech";
            string repposotoryName = "AnlaxRevitUpdate";
            GitHubBaseDownload gitHubBaseDownload = new GitHubBaseDownload(pathToBaseDll, userName, repposotoryName, "AutoUpdate");
            return gitHubBaseDownload.HotReloadPlugin(true);
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
            AuthSettings auth = AuthSettings.Initialize();
            auth.UpdateStart = AutoUpdateBox.IsChecked.Value;
            auth.SaveJson();
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
            AuthSettings auth = AuthSettings.Initialize();
            auth.UpdateStart = AutoUpdateBox.IsChecked.Value;
            auth.SaveJson();
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