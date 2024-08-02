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
    
    public partial class HBYC_Log
    {
        public int MotherId { get; set; }
        public int AncRegId { get; set; }
        public int InfantId { get; set; }
        public byte HBYCFlag { get; set; }
        public int VillageAutoID { get; set; }
        public int ASHAAutoID { get; set; }
        public System.DateTime VisitDate { get; set; }
        public byte ORSPacket { get; set; }
        public byte IFASirap { get; set; }
        public byte GrowthChart { get; set; }
        public Nullable<byte> Color { get; set; }
        public byte FoodAccordingAge { get; set; }
        public byte GrowthLate { get; set; }
        public Nullable<byte> Refer { get; set; }
        public int Freeze { get; set; }
        public int Smthyr { get; set; }
        public System.DateTime EntryDate { get; set; }
        public System.DateTime LastUpdateDate { get; set; }
        public int EntryUserNo { get; set; }
        public int UpdateUserNo { get; set; }
        public System.DateTime LogEntryDate { get; set; }
        public int LogEntryUserNo { get; set; }
        public Nullable<double> Weight { get; set; }
        public Nullable<double> Height { get; set; }
        public int ReferUnitID { get; set; }
        public Nullable<byte> ANMVerify { get; set; }
        public Nullable<System.DateTime> ANMVerificationDate { get; set; }
        public byte Media { get; set; }
        public byte IsDeleted { get; set; }
        public int HBYC_LogID { get; set; }
    }
}