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
    
    public partial class Bank
    {
        public int ID { get; set; }
        public string Bank_Name { get; set; }
        public string IFSC_CODE { get; set; }
        public string Branch_Name { get; set; }
        public string OldIFSC_CODE { get; set; }
        public byte RuralFlag { get; set; }
        public Nullable<System.DateTime> Entrydate { get; set; }
        public Nullable<System.DateTime> Updatedate { get; set; }
        public string UpdateBy { get; set; }
        public string UpdateIP { get; set; }
        public string OldBank_Name { get; set; }
        public byte MergeBank { get; set; }
        public byte IsDeleted { get; set; }
        public int BankMasterID { get; set; }
        public int OldBankMasterID { get; set; }
        public byte IfscCodeAnm { get; set; }
        public byte IfscCodeAsha { get; set; }
        public byte IfscCodeBeneficiary { get; set; }
    }
}
