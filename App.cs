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

namespace AnlaxBase
{
    internal class App : IExternalApplication
    {
        private string pluginDirectory { get; set; }
        private string pluginIncludeDllDirectory
        {
            get
            {
                return pluginDirectory + "\\IncludeDll";
            }
        }

        private string TabName { get; set; }

        private List<RevitRibbonPanelCustom> revitRibbonPanelCustoms = new List<RevitRibbonPanelCustom>();

        public UIControlledApplication uiappStart { get; set; }
        private ComboBox comboBoxChoose { get; set; }
        private string comboBoxName
        {
            get
            {
                return "ComboBoxChoose" + comboBoxCountReload;
            }
        }
        private int comboBoxCountReload { get; set; }
        public RibbonPanel ribbonPanelBase { get; set; }
        public static UIApplication UIApplicationCurrent { get; set; }

        public static string LastDllPath { get; set; }
        public static string LastNameClass { get; set; }

        private void LaunchAnlaxAutoUpdate()
        {
            try
            {
                Process.Start(pluginDirectory + "\\AutoUpdate\\AnlaxRevitUpdate.exe");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при запуске AutoUpdatePlugin.exe: {ex.Message}");
            }
        }
        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {
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
                    foreach (var panelName in revitRibbonPanelCustoms)
                    {
                        RemovePanelClear(TabName, panelName);
                        HotReload(panelName);
                    }
                    revitRibbonPanelCustoms.Clear();
                    RemoveItem(TabName, "Настройка плагина", comboBoxName);
                    comboBoxCountReload++;
                    CreateChoosenBox();
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
                LastDllPath = e.Item.GroupName;
                if (!string.IsNullOrEmpty(LastNameClass) && !string.IsNullOrEmpty(LastDllPath))
                {
                    string empty2 = $"CustomCtrl_%CustomCtrl_%{TabName}%Настройка плагина%EmptyCommand";
                    RevitCommandId id_addin_button_cmd = RevitCommandId.LookupCommandId(empty2);
                    UIApplicationCurrent.PostCommand(id_addin_button_cmd);
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
            TabName = auth.TabName;
            SubscribeOnButtonCliks();
            try
            {
                application.CreateRibbonTab(TabName);
            }
            catch { }
            ribbonPanelBase = application.CreateRibbonPanel(TabName, "Настройка плагина");
            PushButtonData pushButtonData = new PushButtonData(nameof(OpenWebHelp), "База\nзнаний", assemblyLocation, typeof(OpenWebHelp).FullName);
            pushButtonData.LargeImage = new BitmapImage(new Uri(@"/AnlaxBase;component/Icons/anlax-logo-red.png", UriKind.RelativeOrAbsolute));
            ribbonPanelBase.AddItem(pushButtonData);

            PushButtonData pushButtonDataAuth = new PushButtonData(nameof(AuthStart), "Войти в\nсистему", assemblyLocation, typeof(AuthStart).FullName);
            pushButtonDataAuth.LargeImage = new BitmapImage(new Uri(@"/AnlaxBase;component/Icons/anlax-logo-red.png", UriKind.RelativeOrAbsolute));
            ribbonPanelBase.AddItem(pushButtonDataAuth);

            PushButtonData pushButtonDataHotReload = new PushButtonData(nameof(HotLoad), "Обновить\nплагин", assemblyLocation, typeof(HotLoad).FullName);
            pushButtonDataHotReload.LargeImage = new BitmapImage(new Uri(@"/AnlaxBase;component/Icons/anlax-logo-red.png", UriKind.RelativeOrAbsolute));
            ribbonPanelBase.AddItem(pushButtonDataHotReload);
            PushButtonData pushButtonDataHotLoad = new PushButtonData(nameof(EmptyCommand), "Последняя\nкоманда", assemblyLocation, typeof(EmptyCommand).FullName);
            pushButtonDataHotLoad.LargeImage = new BitmapImage(new Uri(@"/AnlaxBase;component/Icons/anlax-logo-red.png", UriKind.RelativeOrAbsolute));
            ribbonPanelBase.AddItem(pushButtonDataHotLoad);

            CreateChoosenBox();
            List<string> list = FindDllsWithApplicationStart();
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


        private bool LoadPlugin(UIControlledApplication _app, string pathAssembly, ComboBox comboChoose)
        {
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            RevitRibbonPanelCustom revitRibbonPanelCustom = null;
            byte[] assemblyBytes = File.ReadAllBytes(pathAssembly);
            Assembly assembly = Assembly.Load(assemblyBytes);
            // Ищем класс "ApplicationStart"
            List<Type> typesStart = assembly.GetTypes()
    .Where(t => t.IsSubclassOf(typeof(ApplicationStartAnlax)))
    .ToList();
            foreach (Type typeStart in typesStart)
            {
                if (typeStart != null)
                {
                    object instance = Activator.CreateInstance(typeStart);
                    MethodInfo onStartupMethod = typeStart.GetMethod("GetRevitRibbonPanelCustom");
                    if (onStartupMethod != null)
                    {
                        revitRibbonPanelCustom = (RevitRibbonPanelCustom)onStartupMethod.Invoke(instance, new object[] { _app, pathAssembly, TabName });
                        revitRibbonPanelCustom.AssemlyPath = pathAssembly;
                    }
                }
                if (revitRibbonPanelCustom != null)
                {
                    if (revitRibbonPanelCustoms.Any(it => it.NamePanel == revitRibbonPanelCustom.NamePanel))
                    {
                        var oldPanel = revitRibbonPanelCustoms.Where(it => it.NamePanel == revitRibbonPanelCustom.NamePanel).FirstOrDefault();
                        List<PushButtonData> buttons = revitRibbonPanelCustom.Buttons;
                        oldPanel.Buttons.AddRange(buttons);
                    }
                    else
                    {
                        revitRibbonPanelCustom.AddToComboBox(comboChoose);
                        revitRibbonPanelCustoms.Add(revitRibbonPanelCustom);
                    }

                }
            }
            return true;
        }
        public bool FindAndLoadPlugin(UIControlledApplication _app, string pathAssembly, ComboBox comboChoose)
        {
            try
            {
                // Читаем DLL как байтовый массив
                byte[] assemblyBytes = File.ReadAllBytes(pathAssembly);

                // Загружаем сборку из байтов
                Assembly assembly = Assembly.Load(assemblyBytes);

                // Ищем классы, наследуемые от ApplicationStartAnlax
                List<Type> typesStart = assembly.GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(ApplicationStartAnlax)))
                    .ToList();

                bool loadedAtLeastOnePlugin = false;

                foreach (Type typeStart in typesStart)
                {
                    if (typeStart != null)
                    {
                        object instance = Activator.CreateInstance(typeStart);
                        MethodInfo onStartupMethod = typeStart.GetMethod("GetRevitRibbonPanelCustom");

                        if (onStartupMethod != null)
                        {
                            // Вызов метода "GetRevitRibbonPanelCustom"
                            RevitRibbonPanelCustom revitRibbonPanelCustom = (RevitRibbonPanelCustom)onStartupMethod.Invoke(instance, new object[] { _app, pathAssembly, TabName });
                            revitRibbonPanelCustom.AssemlyPath = pathAssembly;

                            if (revitRibbonPanelCustom != null)
                            {
                                if (revitRibbonPanelCustoms.Any(it => it.NamePanel == revitRibbonPanelCustom.NamePanel))
                                {
                                    var oldPanel = revitRibbonPanelCustoms.FirstOrDefault(it => it.NamePanel == revitRibbonPanelCustom.NamePanel);
                                    if (oldPanel != null)
                                    {
                                        oldPanel.Buttons.AddRange(revitRibbonPanelCustom.Buttons);
                                    }
                                }
                                else
                                {
                                    revitRibbonPanelCustom.AddToComboBox(comboChoose);
                                    revitRibbonPanelCustoms.Add(revitRibbonPanelCustom);
                                }

                                loadedAtLeastOnePlugin = true;
                            }
                        }
                    }
                }

                return loadedAtLeastOnePlugin;
            }
            catch (Exception ex)
            {
                // Логирование ошибок
                Console.WriteLine($"Ошибка при обработке {pathAssembly}: {ex.Message}");
                return false;
            }
        }
        private string HotReload(RevitRibbonPanelCustom revitRibbonPanelCustom)
        {
            byte[] assemblyBytes = File.ReadAllBytes(revitRibbonPanelCustom.AssemlyPath);
            bool isDebug = false;
            if (TabName.Contains("Anlax dev"))
            {
                isDebug = true;
            }
            Assembly assembly = Assembly.Load(assemblyBytes);
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
                    string message = (string)onStartupMethod.Invoke(instance, new object[] { revitRibbonPanelCustom.AssemlyPath, isDebug });
                    return message;
                }
            }
            return "Ошибка обновления";
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

            foreach (var dll in dllFiles)
            {
                try
                {
                    // Читаем DLL как байтовый массив
                    var assemblyBytes = File.ReadAllBytes(dll);

                    // Загружаем сборку из байтов
                    var assembly = Assembly.Load(assemblyBytes);

                    // Ищем класс "ApplicationStart"
                    Type typeStart = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ApplicationStartAnlax)))
            .FirstOrDefault();

                    if (typeStart != null)
                    {
                        // Ищем метод "GetRevitRibbonPanelCustom"
                        var onStartupMethod = typeStart.GetMethod("GetRevitRibbonPanelCustom");
                        object instance = Activator.CreateInstance(typeStart);
                        if (onStartupMethod != null)
                        {
                            if (onStartupMethod != null)
                            {
                                // Вызов метода "GetRevitRibbonPanelCustom"
                                RevitRibbonPanelCustom revitRibbonPanelCustom = (RevitRibbonPanelCustom)onStartupMethod.Invoke(instance, new object[] { uiappStart, dll, TabName });
                                revitRibbonPanelCustom.AssemlyPath = dll;

                                if (revitRibbonPanelCustom != null)
                                {
                                    if (revitRibbonPanelCustoms.Any(it => it.NamePanel == revitRibbonPanelCustom.NamePanel))
                                    {
                                        var oldPanel = revitRibbonPanelCustoms.FirstOrDefault(it => it.NamePanel == revitRibbonPanelCustom.NamePanel);
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
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Логируем ошибки
                    Console.WriteLine($"Ошибка при обработке {dll}: {ex.Message}");
                }
            }

            return result;
        }
        /// <summary>
        /// Метод сжимающий иконку под размеры ленты Revit
        /// </summary>
        /// <param name="img"></param>
        /// <param name="pixels"></param>
        /// <param name="dpi"></param>
        /// <returns></returns>
        public static BitmapImage NewBitmapImage(Image img, int pixels = 32, int dpi = 96)
        {
            // Создаем новое изображение с заданным разрешением DPI
            Bitmap newBitmap = new Bitmap(img.Width, img.Height);
            newBitmap.SetResolution(dpi, dpi);

            // Копируем содержимое изображения img в новое изображение с заданным разрешением
            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                g.DrawImage(img, 0, 0, img.Width, img.Height);
            }

            // Масштабируем изображение до указанных размеров пикселей
            Bitmap scaledBitmap = new Bitmap(newBitmap, new System.Drawing.Size(pixels, pixels));
            return ConvertBitmapToBitmapImage(scaledBitmap);
        }
        public static BitmapImage NewBitmapImage(Uri imageUri, int pixels = 32, int dpi = 96)
        {
            // Загружаем изображение из Uri
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = imageUri;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            // Преобразуем его в формат Bitmap
            Bitmap bitmap;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(memoryStream);
                memoryStream.Position = 0;
                bitmap = new Bitmap(memoryStream);
            }

            // Создаем новое изображение с заданным разрешением DPI
            Bitmap newBitmap = new Bitmap(bitmap.Width, bitmap.Height);
            newBitmap.SetResolution(dpi, dpi);

            // Копируем содержимое изображения в новое изображение с заданным разрешением
            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
            }

            // Масштабируем изображение до указанных размеров пикселей
            Bitmap scaledBitmap = new Bitmap(newBitmap, new System.Drawing.Size(pixels, pixels));
            return ConvertBitmapToBitmapImage(scaledBitmap);
        }
        public static BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            MemoryStream memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            memoryStream.Position = 0;
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }
    }

}

