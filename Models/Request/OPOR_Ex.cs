using DbClass;

namespace WMSWebAPI.Models.Request
{
    public class OPOR_Ex
    {
        public OPOR PO { get; set; }               
        public OPOR_Ex(OPOR newPO) { PO = newPO; }
    }
}
