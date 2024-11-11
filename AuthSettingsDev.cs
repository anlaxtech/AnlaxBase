using AnlaxPackage.Auth;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AnlaxBase
{
    public class AuthSettingsDev
    {
        private static AuthSettingsDev settings = null;
        protected AuthSettingsDev() { }
        public static string AssemblyBaseLocation { get; set; }


        public string AssemblyLocation
        {
            get
            {
                string assemblyLocation = Assembly.GetExecutingAssembly().Location;
                string directoryPath = Path.GetDirectoryName(assemblyLocation);

                // Получаем папку на один уровень выше
                DirectoryInfo parentDirectory = Directory.GetParent(directoryPath);
                string parentPath = parentDirectory.FullName;
                return parentPath;
            }
        }
        /// <summary>
        /// Для всех работающих dll файл json ищется на уровень выше. Для базовой dll файл ищется в той же директории
        /// </summary>
        /// <param name="authInBase"></param>
        /// <returns></returns>
        public static AuthSettingsDev Initialize(bool authInBase = false)
        {
            if (authInBase)
            {
                string assemblyLocation = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(assemblyLocation))
                {
                    assemblyLocation = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "");

                }
                AssemblyBaseLocation = Path.GetDirectoryName(assemblyLocation);
            }
            else
            {
                string assemblyLocation = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(assemblyLocation))
                {
                    assemblyLocation = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "");

                }
                string directoryPath = Path.GetDirectoryName(assemblyLocation);

                // Получаем папку на один уровень выше
                DirectoryInfo parentDirectory = Directory.GetParent(directoryPath);
                AssemblyBaseLocation = parentDirectory.FullName;
            }
            string settingsFilePath = Path.Combine(AssemblyBaseLocation, "AuthSettingsDev.json");
            if (settings == null)
            {

                if (File.Exists(settingsFilePath))
                {
                    try
                    {
                        string json = File.ReadAllText(settingsFilePath);
                        settings = JsonConvert.DeserializeObject<AuthSettingsDev>(json);
                    }
                    catch
                    {
                        MessageBox.Show("Ошибка считывания файла настройек json. Проверьте файл settingsVoid.json на корректность");
                    }
                }
                else
                {
                    settings = new AuthSettingsDev();
                    settings.SetDefaultValue();
                    string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                    // Write JSON string to file
                    try
                    {
                        File.WriteAllText(settingsFilePath, json);
                    }
                    catch { }
                }
            }
            return settings;
        }

        public void SaveJson()
        {
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string settingsFilePath = Path.Combine(Path.GetDirectoryName(assemblyLocation), "AuthSettingsDev.json");
            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            // Write JSON string to file
            try
            {
                File.WriteAllText(settingsFilePath, json);
            }
            catch { MessageBox.Show("Не удалсоь сохранить файлы настроек"); }
        }


        public string Login { get; set; }
        public string Password { get; set; }
        public string TabName { get; set; }
        public bool UpdateStart { get; set; }
        public string URLBaseKnowledge { get; set; }
        [JsonIgnore]
        public UIControlledApplication Uiapp { get; set; }
        [JsonIgnore]
        public PostgresSQLValidate Validate { get; set; }

        [JsonIgnore]
        public int NumberLiscence { get; set; }

        internal void SetDefaultValue()
        {
            Login = "Введите логин";
            Password = "Введите пароль";
            TabName = "Anlax Dev";
            URLBaseKnowledge = "https://anlax.org/technology/knowledge-base/";
            UpdateStart = true;

        }
    }
}
