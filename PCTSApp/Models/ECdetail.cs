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
    
    public partial class ECdetail
    {
        public int SurveyYear { get; set; }
        public int UnitID { get; set; }
        public int AnganwariNo { get; set; }
        public int MemberID { get; set; }
        public string IFSCCode { get; set; }
        public string AccountNo { get; set; }
        public string PCTSID { get; set; }
        public Nullable<System.DateTime> MarraigeDate { get; set; }
        public Nullable<byte> AgeOnMarraige { get; set; }
        public Nullable<byte> HusbandAgeOnMarraige { get; set; }
        public Nullable<byte> Nisanthan { get; set; }
        public Nullable<byte> DeliveryInLastYear { get; set; }
        public Nullable<byte> InfantDeathBelow5YearInLastYear { get; set; }
        public Nullable<System.DateTime> LMPDate { get; set; }
        public Nullable<byte> WantBaby { get; set; }
        public System.DateTime Entrydate { get; set; }
        public System.DateTime LastUpdateDate { get; set; }
        public int EntryByUserNo { get; set; }
        public int UpdateByUserNo { get; set; }
        public int CoupleNo { get; set; }
        public string BhamashahID { get; set; }
        public Nullable<System.DateTime> Entrydate_online { get; set; }
        public Nullable<System.DateTime> LastUpdateDate_online { get; set; }
    }
}
