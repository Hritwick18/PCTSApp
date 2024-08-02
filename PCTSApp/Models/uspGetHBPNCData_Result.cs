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
    
    public partial class uspGetHBPNCData_Result
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
        public System.DateTime Prasav_date { get; set; }
        public string pctsid { get; set; }
        public int MotherID { get; set; }
        public string Name { get; set; }
        public string Husbname { get; set; }
        public string Address { get; set; }
        public int ANCRegID { get; set; }
        public Nullable<System.DateTime> pncEntryDate { get; set; }
        public Nullable<int> pnc_Motherid { get; set; }
        public Nullable<int> pnc_Ancregid { get; set; }
        public Nullable<byte> pnc_PNCFlag { get; set; }
        public Nullable<byte> pnc_PNCComp { get; set; }
        public Nullable<System.DateTime> pnc_entrydate { get; set; }
        public Nullable<System.DateTime> pnc_PNCDate { get; set; }
        public Nullable<int> pnc_Ashaautoid { get; set; }
        public Nullable<int> pnc_ReferUnitID { get; set; }
        public Nullable<int> pnc_Child1_InfantID { get; set; }
        public Nullable<double> pnc_Child1_Weight { get; set; }
        public Nullable<int> pnc_Child2_InfantID { get; set; }
        public Nullable<byte> pnc_Child2_Comp { get; set; }
        public Nullable<double> pnc_Child2_Weight { get; set; }
        public Nullable<int> pnc_Child3_InfantID { get; set; }
        public Nullable<byte> pnc_Child3_Comp { get; set; }
        public Nullable<double> pnc_Child3_Weight { get; set; }
        public Nullable<int> pnc_Child4_InfantID { get; set; }
        public Nullable<byte> pnc_Child4_Comp { get; set; }
        public Nullable<double> pnc_Child4_Weight { get; set; }
        public Nullable<int> pnc_Child5_InfantID { get; set; }
        public Nullable<byte> pnc_Child5_Comp { get; set; }
        public Nullable<double> pnc_Child5_Weight { get; set; }
        public Nullable<byte> pnc_Child1_Comp { get; set; }
        public Nullable<int> pnc_Freeze { get; set; }
        public Nullable<int> pnc_S_mthyr { get; set; }
        public Nullable<int> pnc_VillageAutoID { get; set; }
        public Nullable<int> pnc_ANMautoid { get; set; }
        public Nullable<byte> pnc_Child1_IsLive { get; set; }
        public Nullable<byte> pnc_Child2_IsLive { get; set; }
        public Nullable<byte> pnc_Child3_IsLive { get; set; }
        public Nullable<byte> pnc_Child4_IsLive { get; set; }
        public Nullable<byte> pnc_Child5_IsLive { get; set; }
        public Nullable<System.DateTime> pnc_LastUpdated { get; set; }
        public Nullable<byte> pnc_Media { get; set; }
        public string pnc_Latitude { get; set; }
        public string pnc_Longitude { get; set; }
        public Nullable<int> pnc_EntryUserNo { get; set; }
        public Nullable<int> pnc_UpdateUserNo { get; set; }
    }
}