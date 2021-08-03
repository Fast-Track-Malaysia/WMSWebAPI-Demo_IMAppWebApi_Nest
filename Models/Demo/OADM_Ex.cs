using DbClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WMSWebAPI.Models.Demo
{
    public class OADM_Ex : OADM, IDisposable
    {
        public string TextDisplay
        {
            get
            {
                return CompnyName;
            }
        }

        public string DetailsDisplay
        {
            get
            {

                return E_Mail;
            }
        }

        public void Dispose()
        {
            GC.Collect();
        }
    }
}
