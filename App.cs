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
using static Autodesk.Revit.DB.SpecTypeId;
using ComboBox = Autodesk.Revit.UI.ComboBox;
using AnlaxPackage;

namespace AnlaxBase
{
    internal class App : IExternalApplication
    {
        private string pluginDirectory { get; set; }
        private string pluginIncludeDllDirectory { get
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
                Process.Start(pluginDirectory + "\\AutoUpdate\\AutoUpdatePlugin.exe");
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
                    LaunchAnlaxAutoUpdate();
                    foreach (var panelName in revitRibbonPanelCustoms)
                    {
                        RemovePanelClear(TabName, panelName);
                    }
                    revitRibbonPanelCustoms.Clear();
                    RemoveItem(TabName, "Настройка плагина", comboBoxName);
                    comboBoxCountReload++;
                    CreateChoosenBox();
                    List<string> list = FindDllsWithApplicationStart();
                    foreach (string item in list)
                    {
                        bool BimDownLoad = LoadPlugin(uiappStart, item, comboBoxChoose);
                    }
                    
                }
            }
            else // если кнопка добавлена черз ad.windows
            {
                LastNameClass = e.Item.UID;
                LastDllPath = e.Item.GroupName;
                if (!string.IsNullOrEmpty(LastNameClass) && !string.IsNullOrEmpty(LastDllPath))
                {
                    string empty2 = "CustomCtrl_%CustomCtrl_%Anlax%Настройка плагина%EmptyCommand";
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
            PushButtonData pushButtonData = new PushButtonData(nameof(OpenWebHelp), "База\nзнаний", assemblyLocation, typeof(OpenWebHelp).FullName)
            {
                LargeImage = RevitRibbonPanelCustom.NewBitmapImage(IconRevitPanel.anlax_logo_red, 32)
            };
            ribbonPanelBase.AddItem(pushButtonData);

            PushButtonData pushButtonDataAuth = new PushButtonData(nameof(AuthStart), "Войти в\nсистму", assemblyLocation, typeof(AuthStart).FullName)
            {
                LargeImage = RevitRibbonPanelCustom.NewBitmapImage(IconRevitPanel.anlax_logo_red, 32)
            };
            ribbonPanelBase.AddItem(pushButtonDataAuth);

            PushButtonData pushButtonDataHotReload = new PushButtonData(nameof(HotLoad), "Обновить\nплагин", assemblyLocation, typeof(HotLoad).FullName)
            {
                LargeImage = RevitRibbonPanelCustom.NewBitmapImage(IconRevitPanel.anlax_logo_red, 32)
            };
            ribbonPanelBase.AddItem(pushButtonDataHotReload);
            PushButtonData pushButtonDataHotLoad = new PushButtonData(nameof(EmptyCommand), "Последняя\nкоманда", assemblyLocation, typeof(EmptyCommand).FullName)
            {
                LargeImage = RevitRibbonPanelCustom.NewBitmapImage(IconRevitPanel.anlax_logo_red, 32)
            };
            ribbonPanelBase.AddItem(pushButtonDataHotLoad);
            
        CreateChoosenBox();
            List<string> list = FindDllsWithApplicationStart();
            foreach (string item in list)
            {
                bool BimDownLoad = LoadPlugin(application, item, comboBoxChoose);
            }
            return Result.Succeeded;

        }


        private void ControlledApplication_DocumentOpened(object sender, DocumentOpenedEventArgs e)
        {
            Document sa = e.Document;
            Autodesk.Revit.ApplicationServices.Application apView =sa.Application;
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
            Type typeStart = assembly.GetTypes()
.Where(t => t.GetInterfaces().Any(i => i == typeof(IApplicationStartAnlax)))
.FirstOrDefault();

            if (typeStart != null)
            {
                object instance = Activator.CreateInstance(typeStart);
                MethodInfo onStartupMethod = typeStart.GetMethod("GetRevitRibbonPanelCustom");
                if (onStartupMethod != null)
                {
                    revitRibbonPanelCustom = (RevitRibbonPanelCustom)onStartupMethod.Invoke(instance, new object[] { _app, pathAssembly,TabName });
                    revitRibbonPanelCustom.AssemlyPath = pathAssembly;
                }
            }
            if (revitRibbonPanelCustom != null)
            {
                revitRibbonPanelCustom.AddToComboBox(comboChoose);
                revitRibbonPanelCustom.CreateRibbonPanel(_app);
                revitRibbonPanelCustoms.Add(revitRibbonPanelCustom);
                return true;
            }
            return false;
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
    .Where(t => t.GetInterfaces().Any(i => i == typeof(IApplicationStartAnlax)))
    .FirstOrDefault();

                    if (typeStart != null)
                    {
                        // Ищем метод "GetRevitRibbonPanelCustom"
                        var method = typeStart.GetMethod("GetRevitRibbonPanelCustom");

                        if (method != null)
                        {
                            result.Add(dll);
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
    }
}

