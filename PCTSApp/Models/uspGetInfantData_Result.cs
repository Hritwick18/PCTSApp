//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PCTSApp.Models
{
    using System;
    
    public partial class uspGetInfantData_Result
    {
        public int Mother_MotherID { get; set; }
        public string Mother_ECID { get; set; }
        public string Mother_Name { get; set; }
        public string Mother_Address { get; set; }
        public byte Mother_Age { get; set; }
        public byte Mother_Cast { get; set; }
        public byte Mother_LiveChild { get; set; }
        public string Mother_Mobileno { get; set; }
        public string Mother_Husbname { get; set; }
        public string Mother_pctsid { get; set; }
        public int Mother_ancregid { get; set; }
        public byte Mother_IsHusband { get; set; }
        public Nullable<int> Mother_Height { get; set; }
        public byte Mother_Status { get; set; }
        public int ANCRegDetail_ANCRegID { get; set; }
        public int ANCRegDetail_MotherID { get; set; }
        public System.DateTime ANCRegDetail_RegDate { get; set; }
        public System.DateTime ANCRegDetail_LMPDT { get; set; }
        public byte ANCRegDetail_DelFlag { get; set; }
        public Nullable<System.DateTime> ANCRegDetail_EntryDate { get; set; }
        public int ANCRegDetail_ashaAutoID { get; set; }
        public byte ANCRegDetail_HighRisk { get; set; }
        public byte ANCRegDetail_Freeze_AadhaarBankInfo { get; set; }
        public int ANCRegDetail_VillageAutoID { get; set; }
        public byte ANCRegDetail_BPL { get; set; }
        public Nullable<System.DateTime> ANCRegDetail_LastUpdated { get; set; }
        public Nullable<int> ANCRegDetail_EntryUserNo { get; set; }
        public Nullable<int> ANCRegDetail_UpdateUserNo { get; set; }
        public int DeliveryDetails_ANCRegID { get; set; }
        public int DeliveryDetails_MotherID { get; set; }
        public Nullable<System.DateTime> DeliveryDetails_Prasav_date { get; set; }
        public Nullable<byte> DeliveryDetails_Deltype { get; set; }
        public Nullable<System.DateTime> DeliveryDetails_EntryDate { get; set; }
        public Nullable<System.DateTime> DeliveryDetails_LastUpdated { get; set; }
        public Nullable<int> DeliveryDetails_DelplaceCode { get; set; }
        public Nullable<System.DateTime> DeliveryDetails_DeathDate { get; set; }
        public int DeliveryDetails_villageautoid { get; set; }
        public Nullable<int> Transport_ANCRegID { get; set; }
        public Nullable<System.DateTime> Transport_DischargeDT { get; set; }
        public int Infant_MotherID { get; set; }
        public byte Infant_ID { get; set; }
        public int Infant_InfantID { get; set; }
        public string Infant_ChildName { get; set; }
        public Nullable<byte> Infant_Sex { get; set; }
        public Nullable<double> Infant_Weight { get; set; }
        public Nullable<byte> Infant_Bfeed { get; set; }
        public Nullable<byte> Infant_BloodGroup { get; set; }
        public System.DateTime Infant_Birth_date { get; set; }
        public Nullable<System.DateTime> Infant_EntryDate { get; set; }
        public Nullable<System.DateTime> Infant_LastUpdated { get; set; }
        public int Infant_ANCRegID { get; set; }
        public int Infant_VillageAutoID { get; set; }
        public byte Infant_Status { get; set; }
        public Nullable<int> immu_InfantID { get; set; }
        public Nullable<System.DateTime> immu_immudate { get; set; }
        public Nullable<byte> immu_ImmuCode { get; set; }
        public Nullable<System.DateTime> immu_EntryDate { get; set; }
        public Nullable<int> immu_ashaAutoID { get; set; }
        public Nullable<int> immu_VillageAutoID { get; set; }
        public Nullable<System.DateTime> immu_LastUpdated { get; set; }
        public Nullable<byte> immu_Media { get; set; }
        public string immu_Latitude { get; set; }
        public string immu_Longitude { get; set; }
        public Nullable<double> immu_Weight { get; set; }
        public Nullable<int> immu_EntryUserNo { get; set; }
        public Nullable<int> immu_UpdateUserNo { get; set; }
        public Nullable<int> immu_EntryUnitID { get; set; }
        public Nullable<int> infimmu_InfantID { get; set; }
        public Nullable<System.DateTime> infimmu_PartImmuDate { get; set; }
        public Nullable<System.DateTime> infimmu_FullImmuDate { get; set; }
        public Nullable<int> infimmu_FullImmuAshaautoid { get; set; }
        public Nullable<System.DateTime> infimmu_BoosterDosesDate { get; set; }
        public Nullable<int> infimmu_BoosterAshaautoid { get; set; }
        public Nullable<System.DateTime> infimmu_LastUpdated { get; set; }
        public Nullable<System.DateTime> infimmu_Entrydate { get; set; }
        public string infimmu_DPTPentaFlag { get; set; }
        public Nullable<int> infimmu_VillageAutoID { get; set; }
        public int Freeze_Immu { get; set; }
        public int Freeze_DBooster { get; set; }
    }
}
