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
        public ModelLiscence (string DataofIssie,string Expirationdate,string UserName,int NumLiscence)
        {
            this.DataofIssie= DataofIssie;
            this.Expirationdate=Expirationdate;
            this.UserName=UserName;
            this.NumLiscence = NumLiscence;
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


        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }

}
