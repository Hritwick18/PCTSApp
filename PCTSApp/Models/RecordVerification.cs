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
    
    public partial class RecordVerification
    {
        public int ANCRegID { get; set; }
        public int Infantid { get; set; }
        public byte RVFlag { get; set; }
        public byte Verify { get; set; }
        public string Remarks { get; set; }
        public System.DateTime EntryDate { get; set; }
        public int EntryUserNo { get; set; }
        public System.DateTime UpdateDate { get; set; }
        public int UpdateUserNo { get; set; }
    }
}
