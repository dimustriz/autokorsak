using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Tourtoss.DL;

namespace Tourtoss.BC
{
    public class BaseBC<T> where T : BaseDL
    {
        private T _dl;

        protected virtual T GetDL()
        { 
            return _dl ?? (_dl = Activator.CreateInstance<T>());
        }

        protected T DL
        {
            get { return GetDL(); }
        }
    }
}
