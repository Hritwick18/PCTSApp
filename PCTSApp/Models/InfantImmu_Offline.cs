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
    
    public partial class InfantImmu_Offline
    {
        public int InfantID { get; set; }
        public System.DateTime PartImmuDate { get; set; }
        public Nullable<System.DateTime> FullImmuDate { get; set; }
        public int FullImmuAshaautoid { get; set; }
        public Nullable<System.DateTime> BoosterDosesDate { get; set; }
        public int BoosterAshaautoid { get; set; }
        public System.DateTime LastUpdated { get; set; }
        public Nullable<System.DateTime> Entrydate { get; set; }
        public string DPTPentaFlag { get; set; }
        public Nullable<int> VillageAutoID { get; set; }
    }
}
