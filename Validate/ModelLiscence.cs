using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AnlaxBase.Validate
{

    public class ModelLiscence : INotifyPropertyChanged
    {
        public ModelLiscence (string DataofIssie,string Expirationdate,string UserName,int NumLiscence,string ipadress,string email)
        {
            this.DataofIssie= DataofIssie;
            this.Expirationdate=Expirationdate;
            this.UserName=UserName;
            this.NumLiscence = NumLiscence;
            ipAdress = ipadress;
            Email = email;
        }

        private string dataofIssie;
        public string DataofIssie
        {
            get { return dataofIssie; }
            set
            {
                dataofIssie = value;
                OnPropertyChanged("DataofIssie");
            }
        }
        private string expirationdate;
        public string Expirationdate
        {
            get { return expirationdate; }
            set
            {
                expirationdate = value;
                OnPropertyChanged("Expirationdate");
            }
        }

        private string userName;
        public string UserName
        {
            get { return userName; }
            set
            {
                userName = value;
                OnPropertyChanged("UserName");
            }
        }


        private int numLiscence;
        public int NumLiscence
        {
            get { return numLiscence; }
            set
            {
                numLiscence = value;
                OnPropertyChanged("NumLiscence");
            }
        }
        private string email;
        public string Email
        {
            get { return email; }
            set
            {
                email = value;
                OnPropertyChanged("Email");
            }
        }

        private string ipAdress;
        public string IpAdress
        {
            get { return ipAdress; }
            set
            {
                ipAdress = value;
                OnPropertyChanged("IpAdress");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }

}
