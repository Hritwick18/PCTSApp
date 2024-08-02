using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;
using System.IO;
using System.Net.Http;
namespace PCTSApp.Models
{
    public class DOTS
    {
        public string AppVersion { set; get; }
        public string Latitude { set; get; }
        public string Longitude { set; get; }
        public string IOSAppVersion { set; get; }
        public string UserID { set; get; }
        public string TokenNo { set; get; }

        public int DOTsPID { set; get; }
        public int AshaautoId { set; get; }
        public int E_mthyr { set; get; }
        public string NameofPatient { set; get; }
        public string Address { set; get; }
        public int Age { set; get; }
        public int sex { set; get; }
        public string MobileNo { set; get; }
        public DateTime? RegistrationDate { set; get; }
        public DateTime? DateofStartingtheTreatment { set; get; }
        public DateTime? DateofCompletionofTreatment_IP { set; get; }
        public DateTime? DateofCompletionofTreatment { set; get; }
        public int Category { set; get; }
        
        public int V_mthyr { set; get; }
        public int E_mthyr_IP { set; get; }
        public int V_mthyr_IP { set; get; }
        
        public int IncentiveID { set; get; }
        public int IncentiveID_IP { set; get; }
        public int NikshayID { set; get; }

        public int PermType { get; set; }
        public int PermMedia { get; set; }
        public string AshaName { get; set; }
        public int? ANMVerify { get; set; }
        public int? FamilyID { get; set; }
        public int PermEntryUserNo { get; set; }
    }
}