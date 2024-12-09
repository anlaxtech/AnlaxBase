using AnlaxPackage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AnlaxBase.Validate
{
    public class LiscenceManagerViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        NewValidate NewValidateManager{ get; set; }
        public LiscenceManagerViewModel() 
        {
            AuthSettings auth = AuthSettings.Initialize(true);
            NewValidateManager = new NewValidate(auth.Login, auth.Password);
        }

    }
}
