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
    
    public partial class Transport
    {
        public int ANCRegID { get; set; }
        public int MotherID { get; set; }
        public Nullable<byte> VehichleType_UP { get; set; }
        public string VehichleNo_UP { get; set; }
        public string VehichleOwnerName_UP { get; set; }
        public Nullable<double> KM_UP { get; set; }
        public Nullable<byte> VehichleType_DOWN { get; set; }
        public string VehichleNo_DOWN { get; set; }
        public string VehichleOwnerName_DOWN { get; set; }
        public Nullable<double> KM_DOWN { get; set; }
        public int Amount_UP { get; set; }
        public int Amount_DOWN { get; set; }
        public System.DateTime DischargeDT { get; set; }
        public System.DateTime EntryDate { get; set; }
        public byte LDDFlag { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public byte Media { get; set; }
        public Nullable<int> EntryUserNo { get; set; }
        public Nullable<int> UpdateUserNo { get; set; }
    }
}