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
    
    public partial class Immunization_Log
    {
        public int InfantID { get; set; }
        public byte ImmuCode { get; set; }
        public System.DateTime immudate { get; set; }
        public Nullable<System.DateTime> EntryDate { get; set; }
        public int ashaAutoID { get; set; }
        public int VillageAutoID { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public byte Media { get; set; }
        public string UpdatedBy { get; set; }
        public System.DateTime UpdateDate { get; set; }
        public string IPAddress { get; set; }
        public byte IsDeleted { get; set; }
        public int EntryUnitID { get; set; }
        public Nullable<int> EntryUserNo { get; set; }
        public Nullable<int> UpdateUserNo { get; set; }
        public Nullable<double> Weight { get; set; }
        public byte ANMVerify { get; set; }
        public Nullable<System.DateTime> ANMVerificationDate { get; set; }
    }
}
