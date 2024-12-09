using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnlaxBase
{
    public static class StaticAuthorization
    {
        private static int _numberLiscence = 0;
        public static void SetLiscence (int number)
        {
            _numberLiscence = number;
        }
        public static int GetLiscence()
        {
            return _numberLiscence;
        }
    }
}
