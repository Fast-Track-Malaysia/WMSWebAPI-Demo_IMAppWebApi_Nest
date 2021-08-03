using DbClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WMSWebAPI.Models.Request
{
    public class POR1_Ex 
    {
        public POR1 POLine { get; set; }
        public OPOR_Ex PO { get; set; }
        public decimal receiptQuantity { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public POR1_Ex() { }
    }
}
