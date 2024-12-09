using AnlaxPackage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AnlaxBase.Validate
{
    public class LiscenceManagerViewModel : INotifyPropertyChanged
    {
        private AuthSettingsDev auth;
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        NewValidate NewValidateManager{ get; set; }
        public LiscenceManagerViewModel() 
        {
            auth = AuthSettingsDev.Initialize(true);
            NewValidateManager = new NewValidate(auth.Login, auth.Password);
            UserName = auth.Login;
            Password = auth.Password;
            NumberLiscenceInt = StaticAuthorization.GetLiscence();
            NumberLiscence = StaticAuthorization.GetLiscence().ToString();
            Expirationdate = StaticAuthorization.ExperationDate;
        }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int NumberLiscenceInt { get; set; }
        public string NumberLiscence { get; set; }
        public string Expirationdate { get; set; }

        private RelayCommand liscenceInfo;
        /// <summary>
        /// Команда для выбора папки проекта
        /// </summary>
        public RelayCommand LiscenceInfo
        {
            get
            {
                return liscenceInfo ??
                  (liscenceInfo = new RelayCommand(obj =>
                  {
                      MessageBox.Show("На этапе разработки");
                  }));
            }
        }

        private RelayCommand releaseLiscence;
        /// <summary>
        /// Команда для выбора папки проекта
        /// </summary>
        public RelayCommand ReleaseLiscence
        {
            get
            {
                return releaseLiscence ??
                  (releaseLiscence = new RelayCommand(obj =>
                  {
                      NewValidateManager.ReleaseLicense(NumberLiscenceInt);
                      StaticAuthorization.ExperationDate = "Нет лицензии";
                      StaticAuthorization.SetLiscence(0);
                      auth.NumberLiscence = 0;
                      
                  }));
            }
        }

        

    }
}
