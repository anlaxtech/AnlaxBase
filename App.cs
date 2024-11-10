using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using System.Configuration.Assemblies;
using AW = Autodesk.Windows;
using Autodesk.Internal.Windows;
using ComboBox = Autodesk.Revit.UI.ComboBox;
using AnlaxPackage;
using System.Drawing;
using System.Windows.Media.Imaging;
using Mono.Cecil;
using AnlaxRevitUpdate;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using AnlaxBimManager;
using System.Windows.Input;

namespace AnlaxBase
{
    internal class App : IExternalApplication
    {
        public static bool AutoUpdateStart { get; set; }
        private string pluginDirectory { get; set; }
        private string pluginIncludeDllDirectory
        {
            get
            {
                return pluginDirectory + "\\IncludeDll";
            }
        }

        private string TabName { get; set; }

        public bool IsDebug
        {
            get
            {
                if (string.IsNullOrEmpty(TabName))
                {
                    return false;
                }
                if (TabName.Contains("Anlax dev"))
                {
                    return true;
                }
                return false;

            }
        }

        private List<RevitRibbonPanelCustom> revitRibbonPanelCustoms = new List<RevitRibbonPanelCustom>();

        public static UIControlledApplication uiappStart { get; set; }
        private ComboBox comboBoxChoose { get; set; }
        private string comboBoxName
        {
            get
            {
                return TabName+"ComboBoxChoose" + comboBoxCountReload;
            }
        }
        private int comboBoxCountReload { get; set; }
        public RibbonPanel ribbonPanelBase { get; set; }
        public static UIApplication UIApplicationCurrent { get; set; }

        public static Assembly LastAssembly { get; set; }
        public static string LastNameClass { get; set; }

        private void LaunchAnlaxAutoUpdate()
        {
            try
            {
                Process.Start(pluginDirectory + "\\AutoUpdate\\AnlaxRevitUpdate.exe");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при запуске обновления плагина Anlax AutoUpdatePlugin.exe: {ex.Message}");
            }
        }
        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {
                PackageLogManager.ClearLogFile();
                LaunchAnlaxAutoUpdate();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка обновления автообновления", $"An error occurred: {ex.Message}");
            }


            return Result.Succeeded;
        }
        private void SubscribeOnButtonCliks()
        {
            AW.ComponentManager.ItemExecuted += OnItemExecuted;
        }

        private void OnItemExecuted(object sender, RibbonItemExecutedEventArgs e)
        {
            string NameClass = e.Item.Id;
            int index = NameClass.LastIndexOf('%');
            if (index != -1 && index + 1 < NameClass.Length)
            {
                string result = NameClass.Substring(index + 1);
                if (result == "HotLoad")
                {
                    RemoveItem(TabName, "Настройка плагина", comboBoxName);
                    comboBoxCountReload++;
                    CreateChoosenBox();
                    foreach (var panelName in revitRibbonPanelCustoms)
                    {
                        RemovePanelClear(TabName, panelName);
                    }
                    MainWindow mainWindow = new MainWindow(revitRibbonPanelCustoms);
                    mainWindow.Show(); // Отображаем окно

                    // Создаем DispatcherFrame для ожидания завершения обновления
                    var frame = new DispatcherFrame();

                    // Подписываемся на событие завершения обновления
                    mainWindow.UpdateCompleted += (s, args) =>
                    {
                        // Завершаем DispatcherFrame, когда обновление завершено
                        frame.Continue = false;
                    };

                    mainWindow.StartUpdate(revitRibbonPanelCustoms); // Запускаем обновления

                    // Приостанавливаем выполнение до завершения обновлений
                    Dispatcher.PushFrame(frame);
                    revitRibbonPanelCustoms.Clear();
                    List<string> list = FindDllsWithApplicationStart();
                    foreach (RevitRibbonPanelCustom revitRibbonPanelCustom1 in revitRibbonPanelCustoms)
                    {
                        revitRibbonPanelCustom1.CreateRibbonPanel(uiappStart);
                    }

                }
            }
            else // если кнопка добавлена черз ad.windows
            {
                LastNameClass = e.Item.UID;
                string pathDll = e.Item.GroupName;
                RevitRibbonPanelCustom revitRibbonPanelCustom = revitRibbonPanelCustoms.Where(it => it.AssemlyPath == pathDll).FirstOrDefault();
                if (revitRibbonPanelCustom != null)
                {
                    LastAssembly = revitRibbonPanelCustom.AssemblyLoad;
                    if (!string.IsNullOrEmpty(LastNameClass) && LastAssembly != null)
                    {
                        string empty2 = $"CustomCtrl_%CustomCtrl_%{TabName}%Настройка плагина%EmptyCommand";
                        RevitCommandId id_addin_button_cmd = RevitCommandId.LookupCommandId(empty2);
                        UIApplicationCurrent.PostCommand(id_addin_button_cmd);
                    }
                }

            }
        }



        public static void RemovePanelClear(string tabName, RevitRibbonPanelCustom revitRibbon)
        {
            AW.RibbonControl ribbon = AW.ComponentManager.Ribbon;
            AW.RibbonPanel ribbonPanel = GetPanel(tabName, revitRibbon.NamePanel);
            foreach (PushButtonData pushButtonData in revitRibbon.Buttons)
            {
                RemoveItem(tabName, revitRibbon.NamePanel, pushButtonData.Name);
            }
            //Remove panel
            foreach (AW.RibbonTab tab in ribbon.Tabs)
            {
                if (tab.Name == tabName)
                {
                    tab.Panels.Remove(ribbonPanel);
                    var uiApplicationType = typeof(UIApplication);
                    var ribbonItemsProperty = uiApplicationType.GetProperty("RibbonItemDictionary",
                        BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
                    var ribbonItems =
                        (Dictionary<string, Dictionary<string, Autodesk.Revit.UI.RibbonPanel>>)ribbonItemsProperty
                            .GetValue(typeof(UIApplication));
                    if (ribbonItems.TryGetValue(tab.Id, out var tabItem)) tabItem.Remove(revitRibbon.NamePanel);
                }
            }
        }
        public static AW.RibbonPanel GetPanel(string tabName, string panelName)
        {
            AW.RibbonControl ribbon = AW.ComponentManager.Ribbon;

            foreach (AW.RibbonTab tab in ribbon.Tabs)
            {
                if (tab.Name == tabName)
                {
                    foreach (AW.RibbonPanel panel in tab.Panels)
                    {
                        if (panel.Source.Title == panelName)
                        {
                            return panel;
                        }
                    }
                }
            }

            return null;
        }
        public static void RemoveItem(string tabName, string panelName, string itemName)
        {
            AW.RibbonControl ribbon = AW.ComponentManager.Ribbon;

            foreach (AW.RibbonTab tab in ribbon.Tabs)
            {
                if (tab.Name == tabName)
                {
                    foreach (AW.RibbonPanel panel in tab.Panels)
                    {
                        if (panel.Source.Title == panelName)
                        {
                            AW.RibbonItem findItem = panel.FindItem("CustomCtrl_%CustomCtrl_%"
                                                                    + tabName + "%" + panelName + "%" + itemName,
                                true);
                            if (findItem != null)
                            {
                                panel.Source.Items.Remove(findItem);
                            }
                        }
                    }
                }
            }
        }
        public AW.RibbonTab GetTab(string tabName)
        {
            AW.RibbonControl ribbon = AW.ComponentManager.Ribbon;

            foreach (AW.RibbonTab tab in ribbon.Tabs)
            {
                if (tab.Name == tabName)
                {
                    return tab;
                }
            }

            return null;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            application.ControlledApplication.DocumentOpened += ControlledApplication_DocumentOpened;
            application.ControlledApplication.DocumentCreated += ControlledApplication_DocumentCreated;
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            comboBoxCountReload = 0;
            pluginDirectory = Path.GetDirectoryName(assemblyLocation);
            LoadDependentAssemblies();
            uiappStart = application;
            AuthSettings auth = AuthSettings.Initialize(true);
            auth.Uiapp = uiappStart;
            AutoUpdateStart = auth.UpdateStart;
            TabName = auth.TabName;
            SubscribeOnButtonCliks();
            try
            {
                application.CreateRibbonTab(TabName);
            }
            catch { }
            ribbonPanelBase = application.CreateRibbonPanel(TabName, "Настройка плагина");
            PushButtonData pushButtonData = new PushButtonData(nameof(OpenWebHelp), "База\nзнаний", assemblyLocation, typeof(OpenWebHelp).FullName);
            pushButtonData.LargeImage = new BitmapImage(new Uri(@"/AnlaxBase;component/Icons/Day - Knowledge base.png", UriKind.RelativeOrAbsolute));
            ribbonPanelBase.AddItem(pushButtonData);

            PushButtonData pushButtonDataAuth = new PushButtonData(nameof(AuthStart), "Войти в\nсистему", assemblyLocation, typeof(AuthStart).FullName);
            pushButtonDataAuth.LargeImage = new BitmapImage(new Uri(@"/AnlaxBase;component/Icons/Day - Log in.png", UriKind.RelativeOrAbsolute));
            ribbonPanelBase.AddItem(pushButtonDataAuth);

            PushButtonData pushButtonDataHotReload = new PushButtonData(nameof(HotLoad), "Обновить\nплагин", assemblyLocation, typeof(HotLoad).FullName);
            pushButtonDataHotReload.LargeImage = new BitmapImage(new Uri(@"/AnlaxBase;component/Icons/Day - Update.png", UriKind.RelativeOrAbsolute));
            ribbonPanelBase.AddItem(pushButtonDataHotReload);
            PushButtonData pushButtonDataHotLoad = new PushButtonData(nameof(EmptyCommand), "Последняя\nкоманда", assemblyLocation, typeof(EmptyCommand).FullName);
            pushButtonDataHotLoad.LargeImage = new BitmapImage(new Uri(@"/AnlaxBase;component/Icons/Day - Last command.png", UriKind.RelativeOrAbsolute));
            ribbonPanelBase.AddItem(pushButtonDataHotLoad);

            CreateChoosenBox();
            List<string> list = FindDllsWithApplicationStart();
            if (AutoUpdateStart)
            {
                MainWindow mainWindow = new MainWindow(revitRibbonPanelCustoms);
                mainWindow.ShowActivated = false;
                mainWindow.Topmost = false;
                mainWindow.Show(); // Отображаем окно
                mainWindow.StartUpdateBehind(revitRibbonPanelCustoms); // Ожидает выполнения обновлений
            }
            foreach (RevitRibbonPanelCustom revitRibbonPanelCustom1 in revitRibbonPanelCustoms)
            {
                revitRibbonPanelCustom1.CreateRibbonPanel(uiappStart);
            }
            return Result.Succeeded;
        }

        private void ControlledApplication_DocumentCreated(object sender, DocumentCreatedEventArgs e)
        {
            Document sa = e.Document;
            Autodesk.Revit.ApplicationServices.Application apView = sa.Application;
            UIApplicationCurrent = new UIApplication(apView);
        }

        private void ControlledApplication_DocumentOpened(object sender, DocumentOpenedEventArgs e)
        {
            Document sa = e.Document;
            Autodesk.Revit.ApplicationServices.Application apView = sa.Application;
            UIApplicationCurrent = new UIApplication(apView);
        }

        private void CreateChoosenBox()
        {
            ComboBoxData cbData = new ComboBoxData(comboBoxName);

            comboBoxChoose = ribbonPanelBase.AddItem(cbData) as ComboBox;
            comboBoxChoose.CurrentChanged += ChangeBox;

            ComboBoxMemberData AllBox = new ComboBoxMemberData("all", "Все");
            ComboBoxMember comboBoxMemberAll = comboBoxChoose.AddItem(AllBox);
        }

        private void ChangeBox(object sender, ComboBoxCurrentChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            UIApplication uiapp = e.Application;
            List<RibbonPanel> boxes = uiapp.GetRibbonPanels(TabName);
            foreach (RibbonPanel ribboRevit in boxes)
            {
                if (comboBox.Current.ItemText == "Все")
                {
                    ribboRevit.Visible = true;
                    continue;
                }
                if (ribboRevit.Name != "Настройка плагина" && ribboRevit.Name != comboBox.Current.ItemText)
                {
                    ribboRevit.Visible = false;
                }
                if (ribboRevit.Name == comboBox.Current.ItemText)
                {
                    ribboRevit.Visible = true;
                }
            }

        }
        private void LoadDependentAssemblies()
        {
            if (Directory.Exists(pluginIncludeDllDirectory))
            {
                foreach (string dllPath in Directory.GetFiles(pluginIncludeDllDirectory, "*.dll"))
                {
                    try
                    {
                        Assembly.LoadFrom(dllPath);
                    }
                    catch
                    {
                        // Если сборка уже загружена или произошла другая ошибка, пропускаем
                    }
                }
            }

        }
        public List<string> FindDllsWithApplicationStart()
        {
            List<string> result = new List<string>();

            // Рекурсивно ищем все файлы с расширением .dll
            var dllFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories);
            PackageLogManager.LogInfo("Найдено " + dllFiles.Length + " сборок");
            foreach (var dll in dllFiles)
            {
                try
                {
                    // Читаем сборку через Mono.Cecil
                    using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(dll))
                    {
                        // Ищем все типы в сборке
                        var typeStart = assemblyDefinition.MainModule.Types.FirstOrDefault(t => t.Interfaces.Any(i => i.InterfaceType.FullName == typeof(IApplicationStartAnlax).FullName));

                        if (typeStart != null)
                        {
                            PackageLogManager.LogInfo("IApplicationStartAnlax найден в " + dll);
                            // Если тип найден, загружаем сборку
                            var assemblyBytes = File.ReadAllBytes(dll);
                            Assembly assembly = Assembly.Load(assemblyBytes);

                            // Попробуем обработать исключение
                            Type[] types;
                            try
                            {
                                types = assembly.GetTypes();
                            }
                            catch (ReflectionTypeLoadException ex)
                            {
                                // Логируем исключения загрузки типов
                                foreach (var loaderException in ex.LoaderExceptions)
                                {
                                    PackageLogManager.LogInfo("Ошибка в считывании " + loaderException.Message);
                                }

                                // Получаем уже загруженные типы
                                types = ex.Types.Where(t => t != null).ToArray();
                            }

                            // Ищем тип вручную среди уже загруженных типов
                            var runtimeType = types.FirstOrDefault(t => t.FullName == typeStart.FullName);

                            if (runtimeType != null)
                            {
                                    object instance = Activator.CreateInstance(runtimeType);
                                    // Создаем экземпляр класса
                                    if (instance != null)
                                    {
                                    AnlaxApplicationInfo anlaxApplicationInfo = new AnlaxApplicationInfo(uiappStart, dll, TabName, assembly);
                                    // Ищем метод "GetRevitRibbonPanelCustom"
                                    var onStartupMethod = runtimeType.GetMethod("GetRevitRibbonPanelCustom");

                                        if (onStartupMethod != null)
                                    {

                                        // Вызов метода "GetRevitRibbonPanelCustom"
                                        RevitRibbonPanelCustom revitRibbonPanelCustom =
                                                (RevitRibbonPanelCustom)onStartupMethod.Invoke(instance, new object[] { anlaxApplicationInfo });

                                            if (revitRibbonPanelCustom != null)
                                            {
                                                revitRibbonPanelCustom.AssemlyPath = dll;
                                                revitRibbonPanelCustom.AssemblyLoad = assembly;
                                                revitRibbonPanelCustom.TabName = TabName;
                                                // Дополнительная обработка, как и в вашем коде
                                                if (revitRibbonPanelCustoms.Any(it => it.NamePanel == revitRibbonPanelCustom.NamePanel))
                                                {
                                                    var oldPanel = revitRibbonPanelCustoms
                                                        .FirstOrDefault(it => it.NamePanel == revitRibbonPanelCustom.NamePanel);
                                                    if (oldPanel != null)
                                                    {
                                                        oldPanel.Buttons.AddRange(revitRibbonPanelCustom.Buttons);
                                                    }
                                                }
                                                else
                                                {
                                                    revitRibbonPanelCustom.AddToComboBox(comboBoxChoose);
                                                    revitRibbonPanelCustoms.Add(revitRibbonPanelCustom);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            PackageLogManager.LogError("Метод GetRevitRibbonPanelCustom не найден.");
                                        }
                                    }
                                
                                else
                                {
                                    PackageLogManager.LogError("Конструктор с требуемыми параметрами не найден.");
                                }
                            }
                            else
                            {
                                PackageLogManager.LogError($"Тип производного класса ApplicationStartAnlax не найден через рефлексию. Но найден через Mono.Cecil");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Логируем ошибки
                    PackageLogManager.LogError($"Ошибка при обработке {dll}: {ex.Message}");
                }
            }

            return result;
        }

    }

}

