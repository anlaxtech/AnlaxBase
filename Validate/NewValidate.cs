using AnlaxPackage.Auth;
using AnlaxPackage;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.Revit.DB;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Octokit;
using System.Net;

namespace AnlaxBase.Validate
{
    [Serializable]
    public class NewValidate
    {
        private string _connectionString;

        private string _user;

        private string _email;

        private string IpAdress
        {
            get
            {
                string hostName = Dns.GetHostName(); // Получение имени хоста
                IPAddress[] addresses = Dns.GetHostAddresses(hostName); // Получение IP-адресов

                foreach (IPAddress ip in addresses)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) // Только IPv4
                    {
                        return ip.ToString();
                    }
                }
                return string.Empty;
            }
        }

        private string _password;

        private string userInfo;

        private string dataBase = "postgres";

        private string scheme = "public";

        public string Expirationdate { get; set; }

        private string _tableName
        {
            get
            {
                if (_user != null)
                {
                    return _user + "table";
                }

                return "Пользователь не надйен";
            }
        }

        public NewValidate(string username, string password, Document doc = null)
        {
            if (username.Contains("@"))
            {
                string pattern = @"@([^.]+)\."; // Регулярное выражение для извлечения части между @ и последней точкой
                Match match = Regex.Match(username, pattern);
                if (match.Success)
                {
                    _email = username;
                    username = match.Groups[1].Value;
                    
                }
            }
            _user = username;
            _password = password;
            _connectionString = "Host=91.245.227.212;Port=5432;Username=" + username + ";Password=" + password + ";Database=" + dataBase;

            if (doc != null)
            {
                userInfo = doc.Application.Username;
            }
        }
        public bool CheckLicenseSilence()
        {
            if (AuthSettingsBase.Initialize().NumberLiscence > 0)
            {
                return true;
            }

            if (!CanConnectToDatabase())
            {
                return false;
            }

            if (!CheckAvailableLicense())
            {
                return false;
            }

            int num = SetNumberLLiscence(userInfo);
            if (num == 0)
            {
                return false;
            }
            AuthSettingsBase.Initialize().NumberLiscence = num;
            StaticAuthorization.SetLiscence(num);
            return true;
        }

        public bool CheckAvailableLicense()
        {
            try
            {
                long num = 0L;
                using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(_connectionString))
                {
                    npgsqlConnection.Open();
                    string cmdText = "SELECT COUNT(*) FROM " + scheme + "." + _tableName + " WHERE userrevit IS NULL OR userrevit = ''";
                    NpgsqlCommand npgsqlCommand = new NpgsqlCommand(cmdText, npgsqlConnection);
                    num = (long)npgsqlCommand.ExecuteScalar();
                }

                return num > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool CanConnectToDatabase()
        {
            try
            {
                using NpgsqlConnection npgsqlConnection = new NpgsqlConnection(_connectionString);
                npgsqlConnection.Open();
                npgsqlConnection.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool CheckLicense(bool manualAuthorizatiom = false)
        {
            if (manualAuthorizatiom)
            {
                AuthViewNew authView = new AuthViewNew();
                authView.GreetingsBlock.Text = "Я Вас категорически приветствую. Введите логин и пароль.";
                authView.ShowDialog();
                _user = AuthSettingsBase.Initialize().Login;
                if (_user.Contains("@"))
                {
                    string pattern = @"@([^.]+)\."; // Регулярное выражение для извлечения части между @ и последней точкой
                    Match match = Regex.Match(_user, pattern);
                    if (match.Success)
                    {
                        _email = _user;
                        _user = match.Groups[1].Value;
                        
                    }
                }
                _password = AuthSettingsBase.Initialize().Password;
                _connectionString = "Host=91.245.227.212;Port=5432;Username=" + _user + ";Password=" + _password + ";Database=" + dataBase;
            }

            if (StaticAuthorization.GetLiscence() > 0)
            {
                return true;
            }

            if (string.IsNullOrEmpty(_user) || string.IsNullOrEmpty(_password))
            {
                AuthViewNew authView2 = new AuthViewNew();
                authView2.GreetingsBlock.Text = "В системе не задан логин или пароль. Введите логин или пароль в форме ниже.Тетовая версия работает до 50 элементов заданий и отверстий";
                authView2.ShowDialog();
                _user = AuthSettingsBase.Initialize().Login;
                _password = AuthSettingsBase.Initialize().Password;
                _connectionString = "Host=91.245.227.212;Port=5432;Username=" + _user + ";Password=" + _password + ";Database=" + dataBase;
            }

            if (!CanConnectToDatabase())
            {
                AuthViewNew authView3 = new AuthViewNew();
                authView3.GreetingsBlock.Text = "Не удалось войти по логину и паролю. Повторите ввод";
                authView3.ShowDialog();
                _user = AuthSettingsBase.Initialize().Login;
                _password = AuthSettingsBase.Initialize().Password;
                _connectionString = "Host=91.245.227.212;Port=5432;Username=" + _user + ";Password=" + _password + ";Database=" + dataBase;
                if (!CanConnectToDatabase())
                {
                    MessageBox.Show("Не удалось подключиться к серверу. Попробуйте повторить попытку через несколько минут.");
                    return false;
                }
            }

            if (!CheckAvailableLicense())
            {
                AuthViewNew authView4 = new AuthViewNew();
                authView4.GreetingsBlock.Text = "Все лицензии заняты. Повторите ввод";
                authView4.ShowDialog();
                _user = AuthSettingsBase.Initialize().Login;
                _password = AuthSettingsBase.Initialize().Password;
                _connectionString = "Host=91.245.227.212;Port=5432;Username=" + _user + ";Password=" + _password + ";Database=" + dataBase;
                if (!CheckAvailableLicense())
                {
                    MessageBox.Show("Все лицензии заняты. Увельчите количество версий. Или вежливо выдерните шнур питания у кого-то из ваших коллег");
                    return false;
                }
            }

            int num = SetNumberLLiscence(userInfo);
            if (num == 0)
            {
                MessageBox.Show("Не удалось активировать лицензию. Обратитесь в тех.поддержку");
                return false;
            }
            else
            {
                MessageBox.Show("Активирована лицензия с номером " + num);
            }

            AuthSettingsBase.Initialize().NumberLiscence = num;
            StaticAuthorization.SetLiscence(num);
            return true;
        }
        public ObservableCollection<ModelLiscence> GetAllLiscences()
        {
            ObservableCollection<ModelLiscence> liscences = new ObservableCollection<ModelLiscence>();

            try
            {
                using NpgsqlConnection npgsqlConnection = new NpgsqlConnection(_connectionString);
                npgsqlConnection.Open();

                string cmdText = "SELECT numberliscence, datofissue, email, ipadress, expirationdate, userrevit FROM " + scheme + "." + _tableName;

                using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(cmdText, npgsqlConnection))
                using (NpgsqlDataReader reader = npgsqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int numberLiscence = reader.GetInt32(reader.GetOrdinal("numberliscence"));
                        string dataOfIssue = reader.IsDBNull(reader.GetOrdinal("datofissue"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("datofissue"));
                        string expirationDate = reader.IsDBNull(reader.GetOrdinal("expirationdate"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("expirationdate"));
                        string userName = reader.IsDBNull(reader.GetOrdinal("userrevit"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("userrevit"));
                        string email = reader.IsDBNull(reader.GetOrdinal("email"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("email"));
                        string ipAdress = reader.IsDBNull(reader.GetOrdinal("ipadress"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("ipadress"));



                        liscences.Add(new ModelLiscence(dataOfIssue, expirationDate, userName, numberLiscence, ipAdress, email));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }

            return liscences;
        }

        public int SetNumberLLiscence(string userInfo)
        {
            try
            {
                using NpgsqlConnection npgsqlConnection = new NpgsqlConnection(_connectionString);
                npgsqlConnection.Open();
                using NpgsqlTransaction npgsqlTransaction = npgsqlConnection.BeginTransaction();

                // Проверяем, есть ли уже запись для текущего userInfo
                string cmdText = "SELECT numberliscence FROM " + scheme + "." + _tableName + " WHERE userrevit = @userInfo";
                using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(cmdText, npgsqlConnection, npgsqlTransaction))
                {
                    npgsqlCommand.Parameters.AddWithValue("@userInfo", userInfo);
                    object obj = npgsqlCommand.ExecuteScalar();
                    if (obj != null)
                    {
                        int result = (int)obj;
                        npgsqlTransaction.Rollback();
                        return result; // Возвращаем номер лицензии, если уже существует для userInfo
                    }
                }

                // Ищем первую свободную лицензию
                string cmdText2 = "SELECT numberliscence, expirationdate FROM " + scheme + "." + _tableName +
                                  " WHERE (userrevit IS NULL OR LENGTH(userrevit) < 1) " +
                                  "ORDER BY numberliscence LIMIT 1 FOR UPDATE";
                using (NpgsqlCommand npgsqlCommand2 = new NpgsqlCommand(cmdText2, npgsqlConnection, npgsqlTransaction))
                using (NpgsqlDataReader reader = npgsqlCommand2.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // Получаем номер лицензии и дату истечения
                        int num = reader.GetInt32(reader.GetOrdinal("numberliscence"));
                        Expirationdate = reader.IsDBNull(reader.GetOrdinal("expirationdate"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("expirationdate"));
                        StaticAuthorization.ExperationDate = Expirationdate;
                        reader.Close();

                        // Обновляем только одну строку
                        string cmdText3 = "UPDATE " + scheme + "." + _tableName +
                                          " SET userrevit = @userInfo, ipadress = @ipadress, email = @Email " +
                                          "WHERE numberliscence = @numberLiscence";
                        using (NpgsqlCommand npgsqlCommand3 = new NpgsqlCommand(cmdText3, npgsqlConnection, npgsqlTransaction))
                        {
                            npgsqlCommand3.Parameters.AddWithValue("@userInfo", userInfo);
                            npgsqlCommand3.Parameters.AddWithValue("@ipadress", IpAdress);
                            npgsqlCommand3.Parameters.AddWithValue("@Email", _email);
                            npgsqlCommand3.Parameters.AddWithValue("@numberLiscence", num);
                            npgsqlCommand3.ExecuteNonQuery();
                        }

                        npgsqlTransaction.Commit();
                        return num; // Возвращаем номер лицензии
                    }
                }

                npgsqlTransaction.Rollback();
                return 0; // Нет свободных лицензий
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
                return 0; // Ошибка
            }
        }

        public bool ReleaseLicense(int num)
        {
            if (!CanConnectToDatabase())
            {
                AuthViewNew authView = new AuthViewNew();
                authView.GreetingsBlock.Text = "Не удалось войти по логину и паролю. Повторите ввод";
                authView.ShowDialog();
                _user = AuthSettingsBase.Initialize().Login;
                _password = AuthSettingsBase.Initialize().Password;
                _connectionString = "Host=91.245.227.212;Port=5432;Username=" + _user + ";Password=" + _password + ";Database=" + dataBase;
                if (!CanConnectToDatabase())
                {
                    MessageBox.Show("Не удалось подключиться к серверу. Попробуйте повторить попытку через несколько минут.");
                    return false;
                }
            }

            if (num == 0)
            {
                using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(_connectionString))
                {
                    npgsqlConnection.Open();
                    using NpgsqlTransaction npgsqlTransaction = npgsqlConnection.BeginTransaction();
                    string cmdText = "UPDATE " + scheme + "." + _tableName + " SET userrevit = '', ipadress = '', email = '' WHERE userrevit = @userInfo";
                    using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(cmdText, npgsqlConnection, npgsqlTransaction);
                    npgsqlCommand.Parameters.AddWithValue("@userInfo", userInfo);
                    int num2 = npgsqlCommand.ExecuteNonQuery();
                    if (num2 > 0)
                    {
                        npgsqlTransaction.Commit();
                        MessageBox.Show("Лицензия успешно освобождена.");
                        AuthSettingsBase.Initialize().NumberLiscence = 0;
                        return true;
                    }

                    npgsqlTransaction.Rollback();
                    MessageBox.Show("Лицензия уже освобождена. Город может спать спокойно.");
                    return false;
                }
            }

            try
            {
                using NpgsqlConnection npgsqlConnection2 = new NpgsqlConnection(_connectionString);
                npgsqlConnection2.Open();
                using NpgsqlTransaction npgsqlTransaction2 = npgsqlConnection2.BeginTransaction();
                string cmdText2 = "UPDATE " + scheme + "." + _tableName + " SET userrevit = '', ipadress = '', email = '' WHERE numberliscence = @num";
                using NpgsqlCommand npgsqlCommand2 = new NpgsqlCommand(cmdText2, npgsqlConnection2, npgsqlTransaction2);
                npgsqlCommand2.Parameters.AddWithValue("@num", num);
                int num3 = npgsqlCommand2.ExecuteNonQuery();
                if (num3 > 0)
                {
                    npgsqlTransaction2.Commit();
                    MessageBox.Show("Лицензия успешно освобождена.");
                    AuthSettingsBase.Initialize().NumberLiscence = 0;
                    return true;
                }

                npgsqlTransaction2.Rollback();
                MessageBox.Show("Не удалось найти лицензию с указанным номером.");
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось освободить лицензию. Ошибка: " + ex.Message);
                return false;
            }
        }

            public bool ReleaseSilenceLicense()
        {
            int numberLiscence = AuthSettingsBase.Initialize().NumberLiscence;
            if (!CanConnectToDatabase())
            {
                return false;
            }

            try
            {
                using NpgsqlConnection npgsqlConnection = new NpgsqlConnection(_connectionString);
                npgsqlConnection.Open();
                using NpgsqlTransaction npgsqlTransaction = npgsqlConnection.BeginTransaction();
                string cmdText = "UPDATE " + scheme + "." + _tableName + " SET userrevit = '', ipadress = '', email = '' WHERE numberliscence = @num";
                using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(cmdText, npgsqlConnection, npgsqlTransaction);
                npgsqlCommand.Parameters.AddWithValue("@num", numberLiscence);
                int num = npgsqlCommand.ExecuteNonQuery();
                if (num > 0)
                {
                    npgsqlTransaction.Commit();
                    AuthSettingsBase.Initialize().NumberLiscence = 0;
                    return true;
                }

                npgsqlTransaction.Rollback();
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
