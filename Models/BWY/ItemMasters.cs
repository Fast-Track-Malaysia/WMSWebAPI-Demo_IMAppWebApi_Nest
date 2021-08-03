using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WMSWebAPI.Models.BWY
{
    /// <summary>
    /// For check and verify the item code is valid
    /// </summary>
    public class ItemMasters
    {
        public int OID { get; set; }
        public Guid CreateUser { get; set; }
        public DateTime CreateDate { get; set; }
        public string Item { get; set; }
        public string ItemDesc { get; set; }
        public decimal Quantity { get; set; }
        public decimal Packsize { get; set; }
        public bool ManageByBatch { get; set; }
        public bool ReturnToWarehouse { get; set; }
        public bool IBT { get; set; }
        public bool Inventory { get; set; }
        public bool Sales { get; set; }
        public bool Purchase { get; set; }
        public DateTime EffectiveDate { get; set; }
        public int UOM { get; set; }
        public int UOMName { get; set; }
        public string Departments { get; set; }
        public string SubDepartments { get; set; }
        public int ItemCategory { get; set; }
        public bool IsActive { get; set; }
        public bool PANDA { get; set; }
        public bool SAP { get; set; }
        public string ItemLink { get; set; }
        public bool CleranceStock { get; set; }
        public bool BOMItem { get; set; }
        public double Length { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Weight { get; set; }
        public string ExPandaCode { get; set; }
        public int PurchaseUOM { get; set; }
        public int SalesUOM { get; set; }
        public int InvenUOM { get; set; }
        public string PreVendor { get; set; }
        public string StorageCondition { get; set; }
        public string Division { get; set; }
        public double Ti { get; set; }
        public double Hi { get; set; }
        public int CtnPerPallets { get; set; }
        public string WarehouseTagging { get; set; }
        public bool ExpiredDateControl { get; set; }
        public bool LotNoControl { get; set; }
        public int MinDaysToShip { get; set; }
        public int MinDaysToRcv { get; set; }
        public int AllocationType { get; set; }
        public bool ShowUsedUDF { get; set; }
        public string UDF1 { get; set; }
        public string UDF1Data { get; set; }
        public string UDF2 { get; set; }
        public string UDF2Data { get; set; }
        public string UDF3 { get; set; }
        public string UDF3Data { get; set; }
        public string UDF4 { get; set; }
        public string UDF4Data { get; set; }
        public string UDF5 { get; set; }
        public string UDF5Data { get; set; }
        public string UDF6 { get; set; }
        public string UDF6Data { get; set; }
        public string UDF7 { get; set; }
        public string UDF7Data { get; set; }
        public string UDF8 { get; set; }
        public string UDF8Data { get; set; }
        public string UDF9 { get; set; }
        public string UDF9Data { get; set; }
        public string UDF10 { get; set; }
        public string UDF10Data { get; set; }
        public int OptimisticLockField { get; set; }
        public int GCRecord { get; set; }
        public Guid UpdateUser { get; set; }
        public DateTime UpdateDate { get; set; }
        public string PandaItemType { get; set; }
        public bool ConsignItem { get; set; }
        public string CountryOfOrigin { get; set; }
        public double ItemLength { get; set; }
        public double ItemWidth { get; set; }
        public double ItemHeight { get; set; }
        public double DiscontinueItem { get; set; }
        public byte[] Image { get; set; }
        public decimal PacksizeKg { get; set; }
        public string Brand { get; set; }
        public bool ExHouseBrand { get; set; }
        public int EAPerPallets { get; set; }
        public bool Downpack { get; set; }
        public bool ProductionItem { get; set; }
        public decimal MOQ { get; set; }
        public decimal DeliveryLeadTime { get; set; }
        public bool SeasonalItem { get; set; }
        public string SubsituteSKU { get; set; }
        public decimal StandardPacking { get; set; }
        public int ProDepartment { get; set; }
        public bool ReturnToVendor { get; set; }
    }
}
