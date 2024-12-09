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
    /// Логика взаимодействия для LiscenceManager.xaml
    /// </summary>
    public partial class LiscenceManager : Window
    {
        public LiscenceManager()
        {
            LiscenceManagerViewModel liscenceManagerViewModel = new LiscenceManagerViewModel();
            DataContext = liscenceManagerViewModel;
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
