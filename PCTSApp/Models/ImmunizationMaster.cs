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
    using System.Collections.Generic;
    
    public partial class ImmunizationMaster
    {
        public byte ImmuCode { get; set; }
        public string ImmuName { get; set; }
        public string ImmuNameH { get; set; }
        public Nullable<int> duedays { get; set; }
        public Nullable<int> MaxDays { get; set; }
        public string DistrictCode { get; set; }
        public System.DateTime AppliedFromDate { get; set; }
        public Nullable<System.DateTime> AppliedToDate { get; set; }
        public int DiffBetweenDoses { get; set; }
        public int DiffDoseCode { get; set; }
        public Nullable<System.DateTime> EntryDate { get; set; }
        public Nullable<System.DateTime> LastUpdatedate { get; set; }
        public Nullable<int> UserNo { get; set; }
        public string IPaddress { get; set; }
        public byte DPTFullImmunizationFlag { get; set; }
        public byte PentaFullImmunizationFlag { get; set; }
        public byte BoosterDoseFlag { get; set; }
        public byte DPTBoosterDose2Flag { get; set; }
        public byte PentaBoosterDose2Flag { get; set; }
        public Nullable<int> RCHImmuCode { get; set; }
        public string RCHImmuName { get; set; }
    }
}
