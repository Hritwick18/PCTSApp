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
    
    public partial class uspDataforManageANC_Result
    {
        public Nullable<int> ancregid { get; set; }
        public Nullable<byte> CovidCase { get; set; }
        public Nullable<System.DateTime> CovidFromDate { get; set; }
        public Nullable<byte> CovidForeignTrip { get; set; }
        public Nullable<byte> CovidRelativePossibility { get; set; }
        public Nullable<byte> CovidRelativePositive { get; set; }
        public Nullable<byte> CovidQuarantine { get; set; }
        public Nullable<byte> CovidIsolation { get; set; }
        public string pctsid { get; set; }
        public string Mobileno { get; set; }
        public int MotherID { get; set; }
        public string Name { get; set; }
        public string Husbname { get; set; }
        public string Address { get; set; }
        public byte Age { get; set; }
        public string ECID { get; set; }
        public int VillageAutoID { get; set; }
        public System.DateTime RegDate { get; set; }
        public System.DateTime LMPDT { get; set; }
        public string AnganwariName { get; set; }
        public Nullable<System.DateTime> ANCDate { get; set; }
        public byte ANCFlag { get; set; }
        public Nullable<System.DateTime> TT1 { get; set; }
        public double weight { get; set; }
        public string CompL { get; set; }
        public int RTI { get; set; }
        public Nullable<System.DateTime> TT2 { get; set; }
        public Nullable<System.DateTime> TTB { get; set; }
        public Nullable<System.DateTime> IFA { get; set; }
        public Nullable<double> HB { get; set; }
        public Nullable<int> BloodPressureD { get; set; }
        public Nullable<int> BloodPressureS { get; set; }
        public string AshaName { get; set; }
        public string ReferUnitCode { get; set; }
        public Nullable<System.DateTime> CAL500 { get; set; }
        public Nullable<System.DateTime> ALBE400 { get; set; }
        public Nullable<byte> UrineA { get; set; }
        public Nullable<byte> UrineS { get; set; }
        public int TreatmentCode { get; set; }
        public string ReferDistrictCode { get; set; }
        public Nullable<short> ReferUnitType { get; set; }
        public string ReferUniName { get; set; }
        public int ashaAutoID { get; set; }
        public Nullable<System.DateTime> IronSucrose1 { get; set; }
        public Nullable<System.DateTime> IronSucrose2 { get; set; }
        public Nullable<System.DateTime> IronSucrose3 { get; set; }
        public Nullable<System.DateTime> IronSucrose4 { get; set; }
        public Nullable<byte> NormalLodingIronSucrose1 { get; set; }
        public Nullable<int> Height { get; set; }
        public int Freeze_ANC3Checkup { get; set; }
        public Nullable<int> RegUnitID { get; set; }
        public Nullable<short> RegUnittype { get; set; }
        public int DeliveryComplication { get; set; }
        public int anganwariNo { get; set; }
        public Nullable<byte> Media { get; set; }
        public Nullable<byte> ANMVerify { get; set; }
        public byte MinANCFlagUnVerify { get; set; }
        public Nullable<System.DateTime> ANC1Date { get; set; }
        public Nullable<System.DateTime> ANC2Date { get; set; }
        public Nullable<System.DateTime> ANC3Date { get; set; }
        public Nullable<System.DateTime> ANC4Date { get; set; }
        public Nullable<System.DateTime> PreviousTT1Date { get; set; }
        public Nullable<System.DateTime> PreviousTT2Date { get; set; }
        public Nullable<System.DateTime> PreviousTTBDate { get; set; }
        public byte HighRisk { get; set; }
    }
}