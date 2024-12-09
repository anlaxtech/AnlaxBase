﻿using AnlaxPackage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AnlaxBase.Validate
{
    /// <summary>
    /// Логика взаимодействия для AuthView.xaml
    /// </summary>
    public partial class AuthViewNew : Window
    {
        public AuthViewNew()
        {
            InitializeComponent();
            try
            {
                loginTextBox.Text = AuthSettingsDev.Initialize().Login;
                passwordTextBox.Text = AuthSettingsDev.Initialize().Password;
            }
            catch { }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AuthSettingsDev.Initialize().Login = loginTextBox.Text;
            AuthSettingsDev.Initialize().Password = passwordTextBox.Text;
            AuthSettingsDev.Initialize().SaveJson();
            Close();
        }
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}