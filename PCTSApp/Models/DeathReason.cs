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
    
    public partial class DeathReason
    {
        public string ReasonName { get; set; }
        public Nullable<byte> DeathType { get; set; }
        public byte ReasonID { get; set; }
        public string ReasonCode { get; set; }
        public string ReasonNameE { get; set; }
        public Nullable<int> ParentReasonId { get; set; }
        public int RCHDeathCauseID { get; set; }
        public string RCHDeathReason { get; set; }
    }
}
