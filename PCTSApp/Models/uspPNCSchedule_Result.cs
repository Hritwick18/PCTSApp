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
    
    public partial class uspPNCSchedule_Result
    {
        public string Name { get; set; }
        public string HusbName { get; set; }
        public string PCTSID { get; set; }
        public string Villagename { get; set; }
        public System.DateTime DeliveryDate { get; set; }
        public string PNC2due { get; set; }
        public string PNC3due { get; set; }
        public string PNC4due { get; set; }
        public string PNC5due { get; set; }
        public string PNC6due { get; set; }
        public string PNC7due { get; set; }
        public int MotherID { get; set; }
        public int ANCRegID { get; set; }
        public string Complication { get; set; }
        public byte PNCFlag { get; set; }
        public string ANMName { get; set; }
        public string ASHAName { get; set; }
        public Nullable<System.DateTime> PNCDate { get; set; }
    }
}
