using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;
using System.IO;
using System.Net.Http;
namespace PCTSApp.Models
{

    public partial class ANCDetail
    {
        [DataMemberAttribute()]
        public int Height { get; set; }
        [DataMemberAttribute()]
        public DateTime LMPDate { get; set; }
        [DataMemberAttribute()]
        public DateTime RegDate { get; set; }
        [DataMemberAttribute()]
        public DateTime? LastANCDate { get; set; }
        [DataMemberAttribute()]
        public string ReferUnitCode { get; set; }
        [DataMemberAttribute()]
        public string AppVersion { set; get; }
        [DataMemberAttribute()]
        public string UserID { set; get; }
        [DataMemberAttribute()]
        public string TokenNo { set; get; }
        [DataMemberAttribute()]
        public DateTime? ANC1Date { get; set; }
        [DataMemberAttribute()]
        public DateTime? ANC2Date { get; set; }
        [DataMemberAttribute()]
        public DateTime? ANC3Date { get; set; }
        [DataMemberAttribute()]
        public DateTime? ANC4Date { get; set; }

        public string IOSAppVersion { get; set; }
    }

    public partial class HBPNC
    {
        [DataMemberAttribute()]
        public DateTime DeliveryDate { get; set; }

        [DataMemberAttribute()]
        public int DelplaceCode { get; set; }
        [DataMemberAttribute()]
        public string ReferUnitCode { get; set; }
        [DataMemberAttribute()]
        public string AppVersion { set; get; }
        [DataMemberAttribute()]
        public string UserID { set; get; }
        [DataMemberAttribute()]
        public string TokenNo { set; get; }

        public string IOSAppVersion { get; set; }
    }
    public partial class DeathDetail
    {
        [DataMemberAttribute()]
        public DateTime? DeliveryDate { get; set; }
        [DataMemberAttribute()]
        public string DeathUnitCode { get; set; }
        [DataMemberAttribute()]
        public string LoginUserID { get; set; }
        [DataMemberAttribute()]
        public string IPAddress { get; set; }
        [DataMemberAttribute()]
        public DateTime? BirthDate { get; set; }
        [DataMemberAttribute()]
        public string AppVersion { set; get; }
        [DataMemberAttribute()]
        public string UserID { set; get; }
        [DataMemberAttribute()]
        public string TokenNo { set; get; }

        public string IOSAppVersion { get; set; }

    }
    public class ResponseModel
    {
        public int AppVersion { set; get; }
        public string Message { set; get; }
        public bool Status { set; get; }
        public object ResposeData { set; get; }

    }
    public class ResponseModel1
    {
        public int AppVersion { set; get; }
        public string Message { set; get; }
        public bool Status { set; get; }
        public HttpResponseMessage ResposeData { set; get; }

    }
    public class Pcts
    {
        public int RegUnitid { get; set; }
        public int VillageAutoid { get; set; }
        public int DelplaceUnitid { get; set; }
        public int LoginUnitid { get; set; }
        public int RefUnittype { get; set; }
        public string RefUnitCode { get; set; }
        public int ANCRegID { get; set; }
        public int InfantID { get; set; }
        public string LoginUnitcode { get; set; }
        public string SaveImmuCodeList { get; set; }
        public string PCTSID { get; set; }
        public string TagName { get; set; }
        public int MotherID { get; set; }
        public int DeathUnittype { get; set; }
        public string DeathUnitCode { get; set; }
        public string action { get; set; }
        public int MthYr { get; set; }
        public DateTime? Birth_date { set; get; }
        public int DPTFlag { get; set; }
        public byte DelFlag { get; set; }
        public int LoginUnitType { get; set; }
        public int UnitType { get; set; }
        public string UserID { set; get; }
        public string TokenNo { set; get; }
        public Int16 Media { get; set; }
        public string IfscCode { set; get; }
        public string BankName { set; get; }
        public string BranchName { set; get; }
        public int UserNo { set; get; }
        public int Mother_AppID { set; get; }
        public int ANMAutoID { get; set; }
        public int Role { get; set; }
        public string Unitcode { get; set; }

        public string finyear { set; get; }
        public string FromDate { set; get; }

        public byte ServiceCode { get; set; }
        public byte type { get; set; }

        public int ASHAAutoid { get; set; }

    }

    public class UserAuthenticate
    {
        public string UserID { get; set; }
        public string Password { get; set; }
        public string Imei { get; set; }
        public string ConfirmPassword { get; set; }
        public string AppVersion { get; set; }
        public string MobileNo1 { get; set; }
        public string MobileNo2 { get; set; }
        public string TokenNo { get; set; }
        public string DeviceID { get; set; }
        public string OTP { get; set; }
        public byte SmsFlag { get; set; }
        public string IOSAppVersion { get; set; }
    }


    public class Search
    {
        public string unitcode { get; set; }
        public string Name { get; set; }
        public string HusbName { get; set; }
        public string Mobile { get; set; }
        public string AgeFrom { get; set; }
        public string AgeTo { get; set; }
        public byte Regunittype { get; set; }
        public string UserID { get; set; }
        public string TokenNo { get; set; }
    }
    public class ImmunizationData
    {
        public int InfantID { get; set; }
        public int ASHAAutoid { get; set; }
        public string ImmuCode { get; set; }
        public DateTime ImmuDate { get; set; }
        public int VillageAutoid { get; set; }
        public DateTime BirthDate { get; set; }
        public byte PartImmu { get; set; }
        public int LoginUnitid { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public float Weight { get; set; }
        public string EntryBy { get; set; }
        public string AppVersion { set; get; }
        public int? EntryUserNo { get; set; }
        public string UnitCode { get; set; }
        public int MotherID { get; set; }
        public string TokenNo { get; set; }
        public string UserID { get; set; }
        public byte Media { get; set; }
        public byte ANMVerify { get; set; }
        public string ANMVerificationDate { get; set; }

        public string IOSAppVersion { get; set; }
    }
    public class ImmunizationList
    {
        public string ImmuName;
        public string Immucode;
        public int DueDays;
        public int? MaxDays;
    }
    public class InfantListForImmunization
    {
        public string ChildName { set; get; }
        public DateTime Birth_date { set; get; }
        public byte? Sex { set; get; }
        public Int32 InfantID { set; get; }
        public string ChildID { set; get; }
        public Int32 PehchanRegFlag { set; get; }
        public double? Weight { set; get; }
    }
    public class MotherInfantListForImmunization
    {
        public string name { set; get; }
        public string Husbname { set; get; }
        public string Mobileno { set; get; }

        public string PctsId { set; get; }
        public object infantList { set; get; }

    }


    public class InfantDeathRason
    {
        public int AgeType { set; get; }
        public string UserID { get; set; }
        public string TokenNo { get; set; }
    }
    public class UnitMasterAdmin
    {
        public string UnitCode { set; get; }
        public short UnitType { set; get; }
        public string LoginUnitCode { set; get; }
        public short LoginUnitType { set; get; }
        public int UnitID { set; get; }
        public DateTime? FromDate { set; get; }
        public DateTime? ToDate { set; get; }
        public int Flag { set; get; }
        public string Mthyr { set; get; }
        public int Finyear { set; get; }
        public string UserID { get; set; }
        public string TokenNo { get; set; }
        public int AshaAutoID { get; set; }
        public byte AshaType { set; get; }
    }


    public class Dashboard
    {
        public List<ANCRegDashboard> ancRegDashboard { set; get; }
        public List<BirthDetails> birthDetails { set; get; }
        public List<DeliveryDetailDashboard> deliveryDetails { set; get; }
        public List<ImmunizationDetails> immunizationDetails { set; get; }
        public List<Top7DistrictPerformance> top7DistrictPerformance { set; get; }
        public List<IndicatorWisePerformance> indicatorWisePerformance { set; get; }
        public List<Top7BlockPerformance> top7BlockPerformance { set; get; }
        public List<MaternalDeaths> maternalDeaths { set; get; }
        public List<InfantDeaths> infantDeaths { set; get; }
        public List<Sterlization> sterlization { set; get; }
        public List<SexRatio> sexRatio { set; get; }
        public List<VaccineRequirement> vaccineRequirement { set; get; }
        public List<HighRisk> highRisk { set; get; }
        public List<Abortion> abortion { set; get; }
        public List<IUD> iud { set; get; }
    }

    public class ANCRegDashboard
    {
        public Int32 TotalANCReg { set; get; }
        public Int32 ANCRegTrimister { set; get; }
        public string finyear { set; get; }
        public string unitcode { get; set; }
        public byte unittype { get; set; }
    }
    public class Top7DistrictPerformance
    {
        public string unitcode { get; set; }
        public string unitname { get; set; }
        public byte unittype { get; set; }
        public double performacePer { get; set; }
        public int maxValueDistrict { get; set; }
        public string finyear { set; get; }
    }
    public class BirthDetails
    {
        public int totalBirth { get; set; }
        public int liveMaleBirth { get; set; }
        public int liveFeMaleBirth { get; set; }
        public int stillBirth { get; set; }
        public string finyear { set; get; }
        public string unitcode { get; set; }
        public byte unittype { get; set; }
    }
    public class DeliveryDetailDashboard
    {
        public int totalDelivery { get; set; }
        public int delPublic { get; set; }
        public int delPrivate { get; set; }
        public int delHome { get; set; }
        public string finyear { set; get; }
        public string unitcode { get; set; }
        public byte unittype { get; set; }
    }
    public class ImmunizationDetails
    {
        public int totalChilderenReg { get; set; }
        public int fullyImmunized { get; set; }
        public int partImmunized { get; set; }
        public int notImmunized { get; set; }
        public string finyear { set; get; }
        public string unitcode { get; set; }
        public byte unittype { get; set; }
    }
    public class Top7BlockPerformance
    {
        public string unitcode { get; set; }
        public string unitname { get; set; }
        public byte unittype { get; set; }
        public double performacePer { get; set; }
        public int maxValueBlock { get; set; }
        public string finyear { set; get; }
    }
    public class MaternalDeaths
    {
        public string unitcode { get; set; }
        public string unitname { get; set; }
        public byte unittype { get; set; }
        public int deathCount { get; set; }
        public string finyear { set; get; }
    }
    public class InfantDeaths
    {
        public string unitcode { get; set; }
        public string unitname { get; set; }
        public byte unittype { get; set; }
        public int deathCount { get; set; }
        public string finyear { set; get; }
    }
    public class VaccineRequirement
    {
        public string unitcode { get; set; }
        public string unitname { get; set; }
        public byte unittype { get; set; }
        public string immuName { get; set; }
        public int vaccineReqCount { get; set; }
        public byte vaccFlag { get; set; }
        public string finyear { set; get; }
        public string immuNameH { get; set; }

    }
    public class Sterlization
    {
        public string unitcode { get; set; }
        public string unitname { get; set; }
        public byte unittype { get; set; }
        public string monthName { get; set; }
        public string monthValue { get; set; }
        public int sterlizationCount { get; set; }
        public string finyear { set; get; }
    }
    public class Abortion
    {
        public string unitcode { get; set; }
        public string unitname { get; set; }
        public byte unittype { get; set; }
        public string monthName { get; set; }
        public string monthValue { get; set; }
        public int cnt { get; set; }
        public string finyear { set; get; }
    }
    public class IUD
    {
        public string unitcode { get; set; }
        public string unitname { get; set; }
        public byte unittype { get; set; }
        public string monthName { get; set; }
        public string monthValue { get; set; }
        public int cnt { get; set; }
        public string finyear { set; get; }
    }
    public class SexRatio
    {
        public string unitcode { get; set; }
        public string unitname { get; set; }
        public byte unittype { get; set; }
        public string monthName { get; set; }
        public int monthValue { get; set; }
        public int girlsRatio { get; set; }
        public string finyear { set; get; }
    }
    public class IndicatorWisePerformance
    {
        public string unitcode { get; set; }
        public string unitname { get; set; }
        public byte unittype { get; set; }
        public string indicatorName { get; set; }
        public string indicatorNameH { get; set; }
        public string indicatorValue { get; set; }
        public string finyear { set; get; }
        public byte indicatorFlag { set; get; }
    }
    public class DashboardModel
    {
        public string TableName { set; get; }
        public object TableData { set; get; }

    }
    public class HighRisk
    {
        public string unitcode { get; set; }
        public byte unittype { get; set; }
        public string finyear { set; get; }
        public string highRiskName { get; set; }
        public string highRiskNameH { get; set; }
        public double highRiskValue { get; set; }
    }
    public class MasterCodesList
    {
        public int parentid { get; set; }
        public string UserID { get; set; }
        public string TokenNo { get; set; }
    }
    public class DeliveryPlace
    {
        public int delPlace { get; set; }
        public int delflag { get; set; }
        public string loginunitcode { get; set; }
        public int deliveyPlaceCode { get; set; }
        public int loginunittype { get; set; }
    }
    public partial class DeliveryDetail
    {

        [DataMemberAttribute()]
        public string AppVersion { set; get; }
        [DataMemberAttribute()]
        public int DelPlace { set; get; }
        //  [DataMemberAttribute()]
        //public string Mobileno { set; get; }
        //[DataMemberAttribute()]
        //public string Location_Rajasthan { set; get; }
        //[DataMemberAttribute()]
        //public string DirectDelivery { set; get; }
        //[DataMemberAttribute()]
        //public string Ghamantu { set; get; }
        [DataMemberAttribute()]
        public string SubcenterPrivateHospitalUnitID { set; get; }

        //[DataMemberAttribute()]
        //public DateTime RegDate { set; get; }
        [DataMemberAttribute()]
        public string ANCDate { set; get; }
        //[DataMemberAttribute()]
        //public DateTime LMPDT { set; get; }
        [DataMemberAttribute()]
        public string DischargeDT { set; get; }
        [DataMemberAttribute()]
        public int PrasavHour { set; get; }
        [DataMemberAttribute()]
        public int PrasavMinute { set; get; }
        [DataMemberAttribute()]
        public int PrasavAMPM { set; get; }
        [DataMemberAttribute()]
        public int? AdmissionHour { set; get; }
        [DataMemberAttribute()]
        public int? AdmissionMinute { set; get; }
        [DataMemberAttribute()]
        public int AdmissionAMPM { set; get; }
        [DataMemberAttribute()]
        public string InfantDeathDate { set; get; }
        [DataMemberAttribute()]
        public int LoginUnittype { set; get; }
        [DataMemberAttribute()]
        public string ReferUnitcode { set; get; }
        [DataMemberAttribute()]
        public string Infant1_infantid { set; get; }
        [DataMemberAttribute()]
        public string Infant1_vikrtiCode { set; get; }
        [DataMemberAttribute()]
        public string Infant1_NBCC_NBSU { set; get; }
        [DataMemberAttribute()]
        public string Infant1_ChildName { set; get; }
        [DataMemberAttribute()]
        public string Infant1_Sex { set; get; }
        [DataMemberAttribute()]
        public string Infant1_Weight { set; get; }
        [DataMemberAttribute()]
        public string Infant1_Bfeed { set; get; }
        [DataMemberAttribute()]
        public string Infant1_BloodGroup { set; get; }
        [DataMemberAttribute()]
        public string Infant1_LiveChild { set; get; }
        [DataMemberAttribute()]
        public string Infant1_IsDeleted { set; get; }
        [DataMemberAttribute()]
        public string Infant1_Sex_Pre { set; get; }
        [DataMemberAttribute()]
        public string Infant1_BabyKit { set; get; }
        [DataMemberAttribute()]
        public string Infant1_LiveChild_Pre { set; get; }
        [DataMemberAttribute()]
        public string Infant2_infantid { set; get; }
        [DataMemberAttribute()]
        public string Infant2_vikrtiCode { set; get; }
        [DataMemberAttribute()]
        public string Infant2_NBCC_NBSU { set; get; }
        [DataMemberAttribute()]
        public string Infant2_ChildName { set; get; }
        [DataMemberAttribute()]
        public string Infant2_Sex { set; get; }
        [DataMemberAttribute()]
        public string Infant2_Weight { set; get; }
        [DataMemberAttribute()]
        public string Infant2_Bfeed { set; get; }
        [DataMemberAttribute()]
        public string Infant2_BloodGroup { set; get; }
        [DataMemberAttribute()]
        public string Infant2_LiveChild { set; get; }
        [DataMemberAttribute()]
        public string Infant2_IsDeleted { set; get; }
        [DataMemberAttribute()]
        public string Infant2_BabyKit { set; get; }
        [DataMemberAttribute()]
        public string Infant3_infantid { set; get; }
        [DataMemberAttribute()]
        public string Infant3_vikrtiCode { set; get; }
        [DataMemberAttribute()]
        public string Infant3_NBCC_NBSU { set; get; }
        [DataMemberAttribute()]
        public string Infant3_ChildName { set; get; }
        [DataMemberAttribute()]
        public string Infant3_Sex { set; get; }
        [DataMemberAttribute()]
        public string Infant3_Weight { set; get; }
        [DataMemberAttribute()]
        public string Infant3_Bfeed { set; get; }
        [DataMemberAttribute()]
        public string Infant3_BloodGroup { set; get; }
        [DataMemberAttribute()]
        public string Infant3_LiveChild { set; get; }
        [DataMemberAttribute()]
        public string Infant3_IsDeleted { set; get; }
        [DataMemberAttribute()]
        public string Infant3_BabyKit { set; get; }
        [DataMemberAttribute()]
        public string Infant4_infantid { set; get; }
        [DataMemberAttribute()]
        public string Infant4_vikrtiCode { set; get; }
        [DataMemberAttribute()]
        public string Infant4_NBCC_NBSU { set; get; }
        [DataMemberAttribute()]
        public string Infant4_ChildName { set; get; }
        [DataMemberAttribute()]
        public string Infant4_Sex { set; get; }
        [DataMemberAttribute()]
        public string Infant4_Weight { set; get; }
        [DataMemberAttribute()]
        public string Infant4_Bfeed { set; get; }
        [DataMemberAttribute()]
        public string Infant4_BloodGroup { set; get; }
        [DataMemberAttribute()]
        public string Infant4_LiveChild { set; get; }
        [DataMemberAttribute()]
        public string Infant4_IsDeleted { set; get; }
        [DataMemberAttribute()]
        public string Infant4_BabyKit { set; get; }
        [DataMemberAttribute()]
        public string Infant5_infantid { set; get; }
        [DataMemberAttribute()]
        public string Infant5_vikrtiCode { set; get; }
        [DataMemberAttribute()]
        public string Infant5_NBCC_NBSU { set; get; }
        [DataMemberAttribute()]
        public string Infant5_ChildName { set; get; }
        [DataMemberAttribute()]
        public string Infant5_Sex { set; get; }
        [DataMemberAttribute()]
        public string Infant5_Weight { set; get; }
        [DataMemberAttribute()]
        public string Infant5_Bfeed { set; get; }
        [DataMemberAttribute()]
        public string Infant5_BloodGroup { set; get; }
        [DataMemberAttribute()]
        public string Infant5_LiveChild { set; get; }
        [DataMemberAttribute()]
        public string Infant5_IsDeleted { set; get; }
        [DataMemberAttribute()]
        public string Infant5_BabyKit { set; get; }
        [DataMemberAttribute()]
        public Int16? BloodFlag { set; get; }
        [DataMemberAttribute()]
        public Int16? FoodFlag { set; get; }
        [DataMemberAttribute()]
        public Int16 NFSA { set; get; }
        [DataMemberAttribute()]
        public string ReferFromUnitcode { set; get; }
        [DataMemberAttribute()]
        public Int16 ReferFrom { set; get; }

        [DataMemberAttribute()]
        public string UserID { set; get; }
        [DataMemberAttribute()]
        public string TokenNo { set; get; }
    }
    public partial class DeathReason
    {
        [DataMemberAttribute()]
        public Int16 Flag { set; get; }

        [DataMemberAttribute()]
        public string UserID { set; get; }
        [DataMemberAttribute()]
        public string TokenNo { set; get; }
    }
    public class BeneficiaryMobileNo_Maa
    {
        public string UserID { get; set; }
        public string MobileNo { get; set; }
        public string Flag { get; set; }
        public string Pin { get; set; }
        public string OTP { get; set; }
        public string OldPin { get; set; }
        public string DeviceID { get; set; }
        public string AppVersion { get; set; }
        public byte SmsFlag { get; set; }
        public string TokenNo { set; get; }
        public int MotherID { get; set; }


    }
    public class Pcts_Maa
    {

        public string PCTSID { get; set; }
        public string unitcode { get; set; }
        public int MotherID { get; set; }
        public string InfantID { get; set; }
        public string ImmuFlag { get; set; }
        public string ANCRegID { get; set; }
        public string AppVersion { get; set; }
        public string MobileNo { get; set; }
        public string TokenNo { set; get; }

    }
    public class MaaVideo
    {
        public int VideoId { get; set; }
        public byte VideoType { get; set; }
        public string VideoTypeName { get; set; }
        public string VideoName { set; get; }
        public string Descrption { set; get; }
        public string ImageName { set; get; }
    }
    public class InfantListForImmunization_Maa
    {
        public string ChildName { set; get; }
        public DateTime Birth_date { set; get; }
        public byte? Sex { set; get; }
        public Int32 InfantID { set; get; }
        public string ChildID { set; get; }
    }
    public class MotherInfantListForImmunization_Maa
    {
        public string name { set; get; }
        public string Husbname { set; get; }
        public string Mobileno { set; get; }
        public object infantList { set; get; }
    }

    public partial class ComplainSuggestion
    {

        [DataMemberAttribute()]
        public string PCTSID { set; get; }
        public string TokenNo { set; get; }
        public string MobileNo { set; get; }
    }
    public class WeightDetails
    {
        public int motherid { get; set; }
        public string infantid { get; set; }
        public int age { get; set; }
        public byte sex { get; set; }
        public float weight { get; set; }
        public DateTime Birth_date { set; get; }
        public string ChildName { set; get; }
        public string TokenNo { set; get; }
        public string MobileNo { set; get; }
        public string UserID { set; get; }
    }



    public partial class Mother_App
    {

        [DataMemberAttribute()]
        public string AppVersion { set; get; }
        [DataMemberAttribute()]
        public string VillageName { set; get; }
        [DataMemberAttribute()]
        public string UserID { set; get; }
        [DataMemberAttribute()]
        public string TokenNo { set; get; }

        public string IOSAppVersion { get; set; }
    }
    public partial class UnitMaster
    {
        [DataMemberAttribute()]
        public string UserID { set; get; }
        [DataMemberAttribute()]
        public string TokenNo { set; get; }

    }

    public partial class HealthUnitsPhoto
    {
        [DataMemberAttribute()]
        public string UserID { set; get; }
        [DataMemberAttribute()]
        public string TokenNo { set; get; }
        [DataMemberAttribute()]
        public Byte[] PhotoArray { set; get; }
        [DataMemberAttribute()]
        public int Unitid { set; get; }
        public string PhotoPath { set; get; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string PhotoType { set; get; }
    }

    public partial class SwasthyaMitrk
    {

        [DataMemberAttribute()]
        public string AppVersion { set; get; }
        [DataMemberAttribute()]
        public string UserID { set; get; }
        [DataMemberAttribute()]
        public string TokenNo { set; get; }
        [DataMemberAttribute()]
        public string AadhaarNo { set; get; }
        [DataMemberAttribute()]
        public string JanAadhaarNo { set; get; }
        [DataMemberAttribute()]
        public Int16? Consent { set; get; }
        [DataMemberAttribute()]
        public HttpPostedFileBase Photo { set; get; }
        [DataMemberAttribute()]
        public Byte[] PhotoArray { set; get; }
        [DataMemberAttribute()]
        public string PhotoType { set; get; }
        public int AnmUnitID { set; get; }
        public string PhotoPath { set; get; }
        public string VillageName { set; get; }
        public Int16 ChangePhoto { set; get; }

        public string IOSAppVersion { get; set; }
    }


    public class SwasthyaMitrkPhoto
    {
        public byte[] Photo { get; set; }
        public string PhotoType { set; get; }
    }
    public class SwasthyaMitrkJan
    {
        public string AadhaarNo { set; get; }
        public string JanAadhaarNo { set; get; }
    }

    public class SwasthyaMitrkVillageList
    {
        public string VillageName { get; set; }
        public int VillageAutoID { get; set; }
        public List<SwasthyaMitrk> SwasthyaMitrk { get; set; }
    }
    public class birthcertificates
    {
        public string infantid { get; set; }
        public string RegNo { get; set; }
        public string RegDate { get; set; }
        public string UserID { set; get; }
        public string TokenNo { set; get; }
        public string MobileNo { set; get; }
    }
    public class UnittypeLevel
    {
        public Int16 unittypecode { get; set; }
        public String UnittypeName { get; set; }
        public string UserID { set; get; }
        public string TokenNo { set; get; }
    }
    public partial class VerifyOffcer
    {
        [DataMemberAttribute()]
        public string UserID { set; get; }
        [DataMemberAttribute()]
        public string TokenNo { set; get; }

    }

    public partial class RecordVerification
    {
        [DataMemberAttribute()]
        public string UserID { set; get; }
        [DataMemberAttribute()]
        public string TokenNo { set; get; }
        [DataMemberAttribute()]
        public Int16 type { set; get; }
        [DataMemberAttribute()]
        public Int16 YesNo { set; get; }

    }
    public class MotherInfantListForVerification
    {
        public string villageName { set; get; }
        public string UnitName { set; get; }
        public string Block { set; get; }
        public string chcphc { set; get; }
        public string district { set; get; }
        public string name { set; get; }
        public string Husbname { set; get; }
        public Int64 ANCRegID { set; get; }
        public string PCTSID { set; get; }
        public object infantList { set; get; }
    }




    public class TableList
    {
        public string TableName { set; get; }
        public object ColumnList { set; get; }
        public string LoginUnitCode { set; get; }
        public short LoginUnitType { set; get; }
        public int LoginUnitId { set; get; }
        public int Year1 { set; get; }
        public int Year2 { set; get; }
        public DateTime? LastSyncDate { set; get; }
        public int ANMAutoID { set; get; }

    }
    public class DHSTableList
    {
        public string TableName { set; get; }
        public object ColumnList { set; get; }
        public string LoginUnitCode { set; get; }
        public short LoginUnitType { set; get; }
        public int LoginUnitId { set; get; }
        public DateTime? LastSyncDate { set; get; }

    }
    public class ColumnList
    {
        public string ColumnName { set; get; }
        public string IsNullable { set; get; }
        public string DataType { set; get; }
        public int? Columnlength { set; get; }
        public string DefaultValue { set; get; }
        public string PkColumnName { set; get; }
        public string UkColumnName { set; get; }
        public int? AutoIncrement { set; get; }
    }




    public class DHS_UnitMaster
    {
        public string UnitCode { get; set; }
        public short UnitType { get; set; }
        public string UnitName { get; set; }
        public string UnitNameHindi { get; set; }
        public int UnitID { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
    }
    public class DHS_Villages
    {
        public string UnitCode { get; set; }
        public string VillageName { get; set; }
        public string UnitNameHindi { get; set; }
        public Nullable<byte> type { get; set; }
        public Nullable<int> unitid { get; set; }
        public int VillageAutoID { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
    }
    public class DHS_AshaMaster
    {
        public string AshaName { get; set; }
        public int unitid { get; set; }
        public int ashaAutoID { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public byte Status { get; set; }
        public Nullable<byte> type { get; set; }
    }
    public class DHS_AnganwariMaster
    {
        public int AnganwariNo { get; set; }
        public string NameE { get; set; }
        public string NameH { get; set; }
        public int unitid { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public byte IsDeleted { get; set; }
        public string AWCID { get; set; }
    }
    public class DHS_Anganwari_Village
    {
        public int AnganwariNo { get; set; }
        public int VillageAutoID { get; set; }
    }

    public class DHS_TableData
    {
        public string tempData { get; set; }
        public string tempData1 { get; set; }
    }
    public class DHS_ResponseHouseFamilies
    {
        public int AnganwariNo { get; set; }
        public int HouseNo { get; set; }
        public int FamiliyNo { get; set; }
        public int HouseFamilyID { get; set; }
        public int HouseFamilyID_Offline { get; set; }
    }
    public class DHS_ResponseMemberDetails
    {
        public int AnganwariNo { get; set; }
        public int HouseFamilyID { get; set; }
        public string Name { get; set; }
        public int MemberID { get; set; }
        public int MemberID_Offline { get; set; }
        public int SpouseID_Offline { get; set; }
        public int Fatherid_Offline { get; set; }
        public int Motherid_Offline { get; set; }
    }
    public class DHS_ResponseEcDetails
    {
        public int AnganwariNo { get; set; }
        public int MemberID { get; set; }
    }

    public class DHS_ResponseUnMarriedGirlsDetail
    {
        public int AnganwariNo { get; set; }
        public int MemberID { get; set; }
    }
    public class DHS_ResponseChildInformation
    {
        public int MemberID { get; set; }
    }

    public class AshaDashboard
    {
        public List<ANCRegDashboard> ancRegDashboard { set; get; }
        public List<BirthDetails> birthDetails { set; get; }
        public List<DeliveryDetailDashboard> deliveryDetails { set; get; }
        public List<ImmunizationDetails> immunizationDetails { set; get; }
        public List<VaccineRequirement> vaccineRequirement { set; get; }
    }
    public class InfantListForBirthCertificate
    {
        public string ChildName { set; get; }
        public DateTime Birth_date { set; get; }
        public string Sex { set; get; }
        public Int32 InfantID { set; get; }
        public string ChildID { set; get; }
        public Int32 PehchanRegFlag { set; get; }
    }




    public class AshaIncAshaIncentiv
    {
        public string Ashaname { set; get; }
        public string Ashaphone { set; get; }
        public string Accountno { set; get; }

        public string Ifsc_code { set; get; }
        public string Bank_Name { set; get; }
        public string PaymentDate { set; get; }
        public int TotalAmount { set; get; }
        public byte ServiceCode { set; get; }
        public Int32 Amount { set; get; }
        public string ServiceEnglish { set; get; }
        public string ServiceHindi { set; get; }

        public object Activitylist { set; get; }
    }


    public class AshaIncentivActivity
    {
        public string ServiceEnglish { set; get; }
        public string ServiceHindi { set; get; }
        public Int32 Amount { set; get; }
    }

    public partial class HBYC
    {
        [DataMemberAttribute()]
        public string AppVersion { set; get; }
        [DataMemberAttribute()]
        public string UserID { set; get; }
        [DataMemberAttribute()]
        public string TokenNo { set; get; }
        [DataMemberAttribute()]
        public DateTime BirthDate { get; set; }
        [DataMemberAttribute()]
        public string ReferUnitcode { set; get; }

        public string IOSAppVersion { get; set; }
    }

    public partial class HighRiskCas
    {
        [DataMemberAttribute()]
        public string AppVersion { set; get; }
        [DataMemberAttribute()]
        public string UserID { set; get; }
        [DataMemberAttribute()]
        public string TokenNo { set; get; }

        [DataMemberAttribute()]
        public string ReferUnitcode { set; get; }

        public string IOSAppVersion { get; set; }

        [DataMemberAttribute()]
        public DateTime? Visit5Date { get; set; }
        [DataMemberAttribute()]
        public DateTime? Visit6Date { get; set; }
        [DataMemberAttribute()]
        public DateTime? Visit7Date { get; set; }
        [DataMemberAttribute()]
        public DateTime? Visit8Date { get; set; }
        [DataMemberAttribute()]
        public DateTime? Visit9Date { get; set; }
        [DataMemberAttribute()]
        public string ContactUnitcode { set; get; }
        [DataMemberAttribute()]
        public string DelUnitcode { set; get; }

    }
    public class ExternalSMSApiInfo
    {
        public string UniqueID { get; set; }
        public string serviceName { get; set; }
        public string language { get; set; }
        public string message { get; set; }
        public List<string> mobileNo { get; set; }
        public string templateID { get; set; }
    }

    public class BHAMASHAH
    {
        public string UserID { set; get; }
        public string TokenNo { set; get; }
        public int MotherID { get; set; }
        public string BHAMASHAHID { get; set; }
        public int MemberID { get; set; }
        public string AadhaarNo { get; set; }
        public string BHAMASHAHAckID { get; set; }
        public string JanAadhaarNo { get; set; }
        public string JanMemberID { get; set; }
        public string MoblieNo { get; set; }
        public string Name { get; set; }
        public string NameH { get; set; }
    }


    public class MotherAncRegDetail
    {
        public int IsNewRegistration { get; set; }
        public string UnitCode { get; set; }
        public string JanAadhaarNo { get; set; }
        public string AadhaarNo { get; set; }
        public byte Consent { get; set; }
        public string VillageName { get; set; }
        public string AppVersion { set; get; }
        public string UserID { set; get; }
        public string TokenNo { set; get; }
        public string IOSAppVersion { get; set; }
        public int MotherID { get; set; }
        public string ECID { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public byte Age { get; set; }
        public byte Cast { get; set; }
        public byte LiveChild { get; set; }
        public string Mobileno { get; set; }
        public string Husbname { get; set; }
        public string Accountno { get; set; }
        public Nullable<System.DateTime> EntryDate { get; set; }
        public string pctsid { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public string Ifsc_code { get; set; }
        public string RationCardNo { get; set; }
        public int VillageAutoID { get; set; }
        public string AccountName { get; set; }

        public byte IsHusband { get; set; }
        public Nullable<byte> CastGroup { get; set; }
        public Nullable<int> Height { get; set; }
        public byte Status { get; set; }
        public string RCHID { get; set; }
        public byte Religion { get; set; }
        public string NameE { get; set; }
        public string NameH { get; set; }
        public string BPLCardNo { get; set; }
        public Nullable<byte> Divyang { get; set; }
        public int ANCRegID { get; set; }
        public System.DateTime RegDate { get; set; }
        public string LMPDT { get; set; }
        public byte DelFlag { get; set; }
        public int ashaAutoID { get; set; }
        public byte HighRisk { get; set; }
        public int EntryUnitVillage { get; set; }
        public byte Freeze_AadhaarBankInfo { get; set; }
        public byte BeforeDelivery500 { get; set; }
        public Nullable<byte> Location_Rajasthan { get; set; }
        public Nullable<byte> DirectDelivery { get; set; }
        public Nullable<byte> Ghamantu { get; set; }
        public byte NFSA { get; set; }
        public byte? BPL { get; set; }
        public byte Media { get; set; }
        public Nullable<int> EntryUserNo { get; set; }
        public Nullable<int> UpdateUserNo { get; set; }
        public Nullable<System.DateTime> VillageUpdationDate { get; set; }
        public string PermanentAddressE { get; set; }
        public string CurrentAddressE { get; set; }
        public byte QualificationW { get; set; }
        public byte QualificationH { get; set; }
        public byte BusinessW { get; set; }
        public byte BusinessH { get; set; }
        public string HusbnameE { get; set; }
        public byte CaseNo { get; set; }
        public byte IcdsRegistrationFlag { get; set; }
        public Nullable<System.DateTime> ICDS2ndFlagDate { get; set; }
        public string JanMemberID { get; set; }
        public string MemberIDVerifyBy { get; set; }
    }

    public class MotherAncRegDetailReReg
    {      
        public int ReMarriageFlag { get; set; }
        public int Unitid { get; set; }
        public string UnitCode { get; set; }
        public string JanAadhaarNo { get; set; }
        public string AadhaarNo { get; set; }
        public byte Consent { get; set; }
        public string VillageName { get; set; }
        public string AppVersion { set; get; }
        public string UserID { set; get; }
        public string TokenNo { set; get; }
        public string IOSAppVersion { get; set; }
        public int MotherID { get; set; }
        public string ECID { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public byte Age { get; set; }
        public byte Cast { get; set; }
        public byte LiveChild { get; set; }
        public string Mobileno { get; set; }
        public string Husbname { get; set; }
        public string Accountno { get; set; }
        public Nullable<System.DateTime> EntryDate { get; set; }
        public string pctsid { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public string Ifsc_code { get; set; }
        public string RationCardNo { get; set; }
        public int VillageAutoID { get; set; }
        public string AccountName { get; set; }

        public byte IsHusband { get; set; }
        public Nullable<byte> CastGroup { get; set; }
        public Nullable<int> Height { get; set; }
        public byte Status { get; set; }
        public string RCHID { get; set; }
        public byte Religion { get; set; }
        public string NameE { get; set; }
        public string NameH { get; set; }
        public string BPLCardNo { get; set; }
        public Nullable<byte> Divyang { get; set; }
        public int ANCRegID { get; set; }
        public System.DateTime RegDate { get; set; }
        public string LMPDT { get; set; }
        public byte DelFlag { get; set; }
        public int ashaAutoID { get; set; }
        public byte HighRisk { get; set; }
        public int EntryUnitVillage { get; set; }
        public byte Freeze_AadhaarBankInfo { get; set; }
        public byte BeforeDelivery500 { get; set; }
        public Nullable<byte> Location_Rajasthan { get; set; }
        public Nullable<byte> DirectDelivery { get; set; }
        public Nullable<byte> Ghamantu { get; set; }
        public byte NFSA { get; set; }
        public byte? BPL { get; set; }
        public byte Media { get; set; }
        public Nullable<int> EntryUserNo { get; set; }
        public Nullable<int> UpdateUserNo { get; set; }
        public Nullable<System.DateTime> VillageUpdationDate { get; set; }
        public string PermanentAddressE { get; set; }
        public string CurrentAddressE { get; set; }
        public byte QualificationW { get; set; }
        public byte QualificationH { get; set; }
        public byte BusinessW { get; set; }
        public byte BusinessH { get; set; }
        public string HusbnameE { get; set; }
        public byte CaseNo { get; set; }
        public byte IcdsRegistrationFlag { get; set; }
        public Nullable<System.DateTime> ICDS2ndFlagDate { get; set; }
        public string JanMemberID { get; set; }
        public string MemberIDVerifyBy { get; set; }
    }

    public class AncCheckupClaimForm
    {
        public string AshaName { get; set; }
        public string PctsID { get; set; }
        public DateTime? ANC1 { get; set; }
        public DateTime? ANC2 { get; set; }
        public DateTime? ANC3 { get; set; }
        public DateTime? ANC4 { get; set; }
        public DateTime? LMPDate { get; set; }
        public string WomenName { get; set; }
        public string HusbName { get; set; }
        public int HighRiskFlag { get; set; }

        public int ReferFlag { get; set; }
        public string MotherMobileNo { get; set; }
    }

    public class ASHAClaimForm
    {
        public object AncCheckup { get; set; }
        public object HBNC { get; set; }
        public object CollectingAadhaar { get; set; }
        public object Delivery { get; set; }
        public object MaternalDeath { get; set; }

        public object InfantDeath { get; set; }
        public object HBYC { get; set; }

        //     public object FullImmunization { get; set; }

        public object DTPBoosterDetails { get; set; }
        public object AshaBasicDetail { get; set; }
        public object AncPayment { get; set; }
        //public object Booster2 { get; set; }

    }
    public class HBNCClaimForm
    {
        public string AshaName { get; set; }
        public string PctsID { get; set; }

        public string WomenName { get; set; }
        public string Husbname { get; set; }
        public DateTime? PncDate { get; set; }
        public string PncFlag { get; set; }
    }
    public class CollectingAadhaarClaimForm
    {
        public string AshaName { get; set; }
        public string PctsID { get; set; }

        public string WomenName { get; set; }
        public string Husbname { get; set; }
        public DateTime? AadhaarBankInfoUpdatedDate { get; set; }
        public string Ifsc_code { get; set; }
        public string BankName { get; set; }
        public string AccountNo { get; set; }
    }
    public class DeliveryClaimForm
    {
        public string AshaName { get; set; }
        public string PctsID { get; set; }

        public string WomenName { get; set; }
        public string HusbName { get; set; }
        public DateTime? Prasav_date { get; set; }
        public string DeliveryLocationName { get; set; }
        public string RuralUrban { get; set; }
        public string DeliveyType { get; set; }
        public string OutCome { get; set; }
        public string InfantGender { get; set; }

        public string DeliveryComplication { get; set; }
    }
    public class MaternalDeathClaimForm
    {
        public string AshaName { get; set; }
        public string PctsID { get; set; }

        public string Name { get; set; }
        public string HusbName { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public byte Age { get; set; }
        public DateTime? DeathDate { get; set; }
        public string ReasonName { get; set; }
        public string DeathPlace { get; set; }
        public string MobileNo { get; set; }
        public string Preventable { get; set; }
    }

    public class InfantDeathClaimForm
    {
        public string AshaName { get; set; }
        public string PctsID { get; set; }

        public string Name { get; set; }
        public string Husbname { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string AgeDetail { get; set; }
        public DateTime? DeathDate { get; set; }
        public string ReasonName { get; set; }
        public string DeathPlace { get; set; }
    }

    public class MtcList
    {
        public int UnitID { get; set; }
        public string UnitName { get; set; }
    }

    public class HBYCClaimForm
    {
        public string AshaName { get; set; }
        public string PctsID { get; set; }

        public string WomenName { get; set; }
        public string infantname { get; set; }
        public string Husbname { get; set; }
        public DateTime? PncDate { get; set; }
        public string PncFlag { get; set; }

        public string ORS { get; set; }
        public string IFASyrup { get; set; }
    }
    public class FullImmunizationClaimForm
    {
        public string AshaName { get; set; }
        public string PctsID { get; set; }

        public string motherName { get; set; }
        public string infantname { get; set; }
        public string Husbname { get; set; }
        public DateTime? FullImmuDate { get; set; }
    }
    public class Booster1ClaimForm
    {
        public string AshaName { get; set; }
        public string PctsID { get; set; }

        public string motherName { get; set; }
        public string infantname { get; set; }
        public string Husbname { get; set; }
        public DateTime? DptBoosterDate { get; set; }
        public DateTime? OpvBoosterDate { get; set; }
        public DateTime? Mr2BoosterDate { get; set; }
    }
    public class Booster2ClaimForm
    {
        public string AshaName { get; set; }
        public string PctsID { get; set; }

        public string motherName { get; set; }
        public string infantname { get; set; }
        public string Husbname { get; set; }
        public DateTime? BoosterDate { get; set; }
    }

    public class SamChild
    {
        public string ChildID { get; set; }
        public string Name { get; set; }
        public DateTime Dob { get; set; }
        public string Address { get; set; }
        public int MtcID { get; set; }
        public DateTime AdmitDate { get; set; }
        public byte Transport { get; set; }
        public string MotherName { get; set; }
        public string FatherName { get; set; }
        public string MobileNo { get; set; }
        public byte Sex { get; set; }
        public DateTime ReferDate { get; set; }
        public double MuacMap { get; set; }
        public double Weight { get; set; }
        public string Remarks { get; set; }
        public string AppVersion { set; get; }
        public string IOSAppVersion { set; get; }
        public int ActivityMthyr { set; get; }
        public int SamAutoid { set; get; }
        public int AshaAutoid { set; get; }
        public string UserID { set; get; }
        public string TokenNo { set; get; }
        public int UnitID { set; get; }
        public int? ANMVerify { set; get; }
        public int UpdateUserNo { set; get; }
        public byte Type { set; get; }
        public int Media { set; get; }
        public string MTCName { get; set; }
        public string AshaName { get; set; }
        public DateTime? DischargeDate { get; set; }
        public string MonthName { get; set; }
    }
    public class AshaBasicDetail
    {
        public int? Population { get; set; }
        public int? ECCount { get; set; }
        public int? FamilyCount { get; set; }
        public int? HouseHoldCount { get; set; }
        public string HealthLocationName { get; set; }
        public string HealthLocationTypeName { get; set; }
        public string PHCCHCName { get; set; }
        public string PHCCHCTypeName { get; set; }
        public string V_FromDate { get; set; }
        public string V_ToDate { get; set; }
        public string ScheduleMonthName { get; set; }
        public string ScheduleYear { get; set; }
        public int? ClaimAmount { get; set; }

    }
    public class AncPayment
    {
        public int? Urban12Week { get; set; }
        public int? UrbanTD { get; set; }
        public int? UrbanSecondAnc { get; set; }
        public int? UrbanThirdAnc { get; set; }
        public int? UrbanForthAnc { get; set; }
        public int? Rural12Week { get; set; }
        public int? RuralTD { get; set; }
        public int? RuralSecondAnc { get; set; }
        public int? RuralThirdAnc { get; set; }
        public int? RuralForthAnc { get; set; }

    }
    public class ClaimMonthList
    {
        public string MonthName { get; set; }
        public int MonthValue { get; set; }
        public string V_FromDate { get; set; }
        public string V_ToDate { get; set; }
    }

    public class SamDetail
    {
        public string Name { get; set; }
        public DateTime? Dob { get; set; }
        public string Address { get; set; }
        public DateTime? AdmitDate { get; set; }
        public string AshaName { get; set; }
        public int Ashaautoid { get; set; }
        public string MTCName { get; set; }
        public string Transport { get; set; }
        public int? samautoid { get; set; }
        public string VerificationBy { get; set; }
        public string Mothername { get; set; }
        public string Fathername { get; set; }
        public string Mobile { get; set; }
        public string MuacMap { get; set; }
        public string Sex { get; set; }
        public DateTime? Referdate { get; set; }
        public float? Weight { get; set; }
        public string Remark { get; set; }
        public DateTime? VerificationDate { get; set; }
        public int? mthyr { get; set; }
        public DateTime? DischargeDate { get; set; }
        public int? ANMVerify { get; set; }
    }

    public class SamFollowupDetails
    {
        public string AppVersion { set; get; }
        public string IOSAppVersion { set; get; }

        public string UserID { set; get; }
        public string TokenNo { set; get; }
        public int SamAutoid { get; set; }

        public DateTime DischargeDate { get; set; }

        public int Media { get; set; }
        public int PermType { get; set; }
        public int UpdateUserNo { get; set; }
        public int E_mthyr { get; set; }
        public int AshaautoId { get; set; }


        public byte? FollowupFlag1 { get; set; }
        public DateTime? FollowupDate1 { get; set; }
        public double? Height1 { get; set; }
        public double? Weight1 { get; set; }

        public byte? FollowupFlag2 { get; set; }
        public DateTime? FollowupDate2 { get; set; }
        public double? Height2 { get; set; }
        public double? Weight2 { get; set; }

        public byte? FollowupFlag3 { get; set; }
        public DateTime? FollowupDate3 { get; set; }
        public double? Height3 { get; set; }
        public double? Weight3 { get; set; }

        public byte? FollowupFlag4 { get; set; }
        public DateTime? FollowupDate4 { get; set; }
        public double? Height4 { get; set; }
        public double? Weight4 { get; set; }
        public int? ANMVerify { get; set; }

    }
    public class SamRecovered
    {
        public string AppVersion { set; get; }
        public string IOSAppVersion { set; get; }
        public string UserID { set; get; }
        public string TokenNo { set; get; }
        public int SamAutoid { get; set; }
        public int PermMedia { get; set; }
        public int PermType { get; set; }
        public int UpdateUserNo { get; set; }
        public int E_mthyr { get; set; }
        public int AshaautoId { get; set; }
        public int IsFreeSam { get; set; }
        public DateTime? SamFreeDate { get; set; }
        public double Height { get; set; }
        public double Weight { get; set; }
        public int? V_mthyr { get; set; }
        public DateTime? VerificationDate { get; set; }
        public int? S_mthyr { get; set; }
        public byte? ANMVerify { get; set; }
        public DateTime? ANMVerificationDate { get; set; }
        public int PermEntryUserNo { get; set; }

        public string Name { set; get; }
        public DateTime Dob { set; get; }
        public string Address { set; get; }
        public string AshaName { set; get; }
        public DateTime? followupdate4 { get; set; }

    }
    public class SncuFollowup
    {
        public string AppVersion { set; get; }
        public string IOSAppVersion { set; get; }
        public int ActivityMthyr { set; get; }

        public int AshaAutoid { set; get; }
        public string UserID { set; get; }
        public string TokenNo { set; get; }

        public int UpdateUserNo { set; get; }
        public byte Type { set; get; }
        public int Media { set; get; }
        public int InfantID { get; set; }
        public DateTime? DischargeDate { get; set; }
        public string ChildName { get; set; }
        public string MotherName { get; set; }
        public DateTime? BirthDate { get; set; }

        public string Address { get; set; }
        public string MobileNo { get; set; }
        public int Motherid { get; set; }
        public int? ANMVerify { get; set; }
    }
    public class SncuFollowupDetails
    {
        public string AppVersion { set; get; }
        public string IOSAppVersion { set; get; }

        public string UserID { set; get; }
        public string TokenNo { set; get; }
        public int SamAutoid { get; set; }

        public DateTime DischargeDate { get; set; }

        public int Media { get; set; }
        public int UpdateUserNo { get; set; }
        public int E_mthyr { get; set; }
        public int AshaautoId { get; set; }

        //public byte? FollowupFlag1 { get; set; }
        public DateTime? FollowupDate1 { get; set; }

        //public byte? FollowupFlag2 { get; set; }
        public DateTime? FollowupDate2 { get; set; }

        //public byte? FollowupFlag3 { get; set; }
        public DateTime? FollowupDate3 { get; set; }

        //public byte? FollowupFlag4 { get; set; }
        public DateTime? FollowupDate4 { get; set; }

        public int Infantid { get; set; }
        public int Motherid { get; set; }
        //public int PermMedia { get; set; }
        public int? ANMVerify { get; set; }
    }
    public class MaaProgram
    {
        public string AppVersion { set; get; }
        public string IOSAppVersion { set; get; }

        public string UserID { set; get; }
        public string TokenNo { set; get; }
        public int PermEntryUserNo { get; set; }

        public int Autoid { get; set; }
        public int FinYear { get; set; }
        public int TrimesterNo { get; set; }
        public string TrimesterString { get; set; }
        public int NoofParticipant { get; set; }
        public string DateofProgram { get; set; }
        public int E_mthyr { get; set; }
        public int PermMedia { get; set; }
        public int AshaautoId { get; set; }
        public int PermType { get; set; }
        public int RowID { get; set; }
        public int? V_mthyr { get; set; }
        public int? S_mthyr { get; set; }
        public string AshaName { get; set; }
        public string Msg { get; set; }
        public int? ANMVerify { get; set; }

    }
    public class IDCFProgram
    {
        public string AppVersion { set; get; }
        public string IOSAppVersion { set; get; }
        public string UserID { set; get; }
        public string TokenNo { set; get; }

        public int FinYear { get; set; }
        public int Population { get; set; }
        public int PossibleChildren { get; set; }
        public int ChildrenGivenTablet { get; set; }
        public int AnganwadiNo { get; set; }
        public string AngName { get; set; }

        public int RowID { get; set; }
        public int PermType { get; set; }
        public int AshaautoId { get; set; }
        public int E_mthyr { get; set; }
        public int PermMedia { get; set; }
        public int PermEntryUserNo { get; set; }
        public int? V_mthyr { get; set; }
        public int? S_mthyr { get; set; }
        public string AshaName { get; set; }
        public string AnganwariDetails { get; set; }
        public int? ANMVerify { get; set; }
    }
    public class NIPIProgram
    {
        public string AppVersion { set; get; }
        public string IOSAppVersion { set; get; }
        public string UserID { set; get; }
        public string TokenNo { set; get; }

        public int FinYear { get; set; }
        public int Mthyr { get; set; }
        public int Population { get; set; }
        public int PossibleChildren { get; set; }
        public int ChildrenGivenTablet { get; set; }
        public int AnganwadiNo { get; set; }
        public int RowID { get; set; }
        public int PermType { get; set; }
        public int AshaautoId { get; set; }
        public int E_mthyr { get; set; }
        public int PermMedia { get; set; }
        //public int UpdateUserNo { get; set; }
        public int PermEntryUserNo { get; set; }
        public int? V_mthyr { get; set; }
        public int? S_mthyr { get; set; }
        public string AshaName { get; set; }
        public string AnganwariDetails { get; set; }
        public int? ANMVerify { get; set; }
    }
    public class NDDProgram
    {
        public string AppVersion { set; get; }
        public string IOSAppVersion { set; get; }
        public string UserID { set; get; }
        public string TokenNo { set; get; }

        public int FinYear { get; set; }
        public int Mthyr { get; set; }

        public int TotChildAnganwari1to5 { get; set; }
        public int ChildAnganwair1to5SyrupGiven { get; set; }
        public int TotChildAnganwari6to19 { get; set; }
        public int ChildAnganwair6to19SyrupGiven { get; set; }
        public int RowID { get; set; }
        public int PermType { get; set; }
        public int AshaautoId { get; set; }
        public int E_mthyr { get; set; }
        public int PermMedia { get; set; }
        public int PermEntryUserNo { get; set; }
        public int? V_mthyr { get; set; }
        public int? S_mthyr { get; set; }
        public string AshaName { get; set; }
        public int? ANMVerify { get; set; }
    }
    public class AAMRProgram
    {
        public string AppVersion { set; get; }
        public string IOSAppVersion { set; get; }
        public string UserID { set; get; }
        public string TokenNo { set; get; }
        public int AnganwadiNo { get; set; }
        public string AnganwadiName { get; set; }
        public int Mthyr { get; set; }
        public int TotNoOfNoSchoolGoingKishori { get; set; }
        public int NoSchoolGoingKishoriGivenTablet { get; set; }
        public int TotNoofWoman { get; set; }
        public int NoofWomanGivenTablet { get; set; }

        public int RowID { get; set; }
        public int PermType { get; set; }
        public int AshaautoId { get; set; }
        public int E_mthyr { get; set; }
        public int PermMedia { get; set; }
        public int PermEntryUserNo { get; set; }
        public int? V_mthyr { get; set; }
        public int? S_mthyr { get; set; }
        public string AshaName { get; set; }
        public int? ANMVerify { get; set; }


    }
    public class MthyrList
    {
        public string AppVersion { set; get; }
        public string IOSAppVersion { set; get; }
        public string UserID { set; get; }
        public string TokenNo { set; get; }
        public int Monthyr { get; set; }
        public int ActivityCode { get; set; }
        public string MonthYrName { get; set; }
        public int PermType { get; set; }
    }
    public class ServicesList
    {
        public string AppVersion { set; get; }
        public string IOSAppVersion { set; get; }
        public string UserID { set; get; }
        public string TokenNo { set; get; }
        public string Txt { get; set; }
        public int Val { get; set; }
        public int ServiceCode { get; set; }
        public int PermType { get; set; }
        public string IconPath { get; set; }
    }
    public class ANMVerificationPendingList
    {
        public string AppVersion { set; get; }
        public string IOSAppVersion { set; get; }
        public string UserID { set; get; }
        public string TokenNo { set; get; }
        public int AshaAutoid { get; set; }
        public int Unitid { get; set; }
        public string AshaName { get; set; }
        public string AcitivityNameHindi { get; set; }
        
        public int TotalASHACases { get; set; }
        public int TotalUnVerifiedCases { get; set; }
        public int TotalVerifiedCases { get; set; }
        public int TotalCases { get; set; }
        public int PermType { get; set; }
    }

    public class ANTRA
    {
        public string AppVersion { set; get; }
        public string Latitude { set; get; }
        public string Longitude { set; get; }
        public string IOSAppVersion { set; get; }
        public string UserID { set; get; }
        public string TokenNo { set; get; }
        public string PctsId { set; get; }
        
        public int? MotherID { set; get; }
        public int? WomanID { set; get; }
        public int Unitid { set; get; }
        public int DelFlag { set; get; }
        public string WomanName { set; get; }
        public string HusbandName { set; get; }
        public int Children { set; get; }
        public int Age { set; get; }
        public DateTime? LastDeliveryDate { set; get; }
        public string AadhaarNo { set; get; }
        public string BhamashahID { set; get; }
        public string Address { set; get; }
        public string MobileNo { set; get; }
        public int DhatriWoman { set; get; }
        public DateTime? VerificationBy { set; get; }
        public DateTime? EntryDate { set; get; }
        public DateTime VerificationDate { set; get; }
        public DateTime? InjectionDate { set; get; }
        public int DoseNO { set; get; }
        public int InjectionType { set; get; }
        public int WhenInjected { set; get; }
        public int InjectionUnitID { set; get; }
        public int? Mthyr { get; set; }      
        public int InjectionID { get; set; }
        
        public int PermType { get; set; }
        public int AshaautoId { get; set; }
        public int? E_mthyr { get; set; }
        public int PermMedia { get; set; }
        public int PermEntryUserNo { get; set; }
        public int? V_mthyr { get; set; }
        public int? S_mthyr { get; set; }
        public string AshaName { get; set; }
        public int? ANMVerify { get; set; }
        public int AbortionDeliveryType { get; set; }
    }
    public class UnitList
    {
        public string AppVersion { set; get; }
        public string IOSAppVersion { set; get; }
        public string UserID { set; get; }
        public string TokenNo { set; get; }     
        public string UnitCode { get; set; }
        public int UnitType { get; set; }
        public int UnitID { get; set; }
        public string UnitName { get; set; }
        public string UnitNameHindi { get; set; }
        public int PermType { get; set; }
        public int ExtraFlag { get; set; }

    }

    public class GetFamilyDetails
    {
        public string UserID { set; get; }
        public string TokenNo { set; get; }
        public string UnitCode { get; set; }
        public string JAN_AADHAR { get; set; }
        public string JAN_MEMBER_ID { get; set; }
        public string AADHAR_ID { get; set; }

        public string SexType { get; set; }
        public string NAME { get; set; }
        public string DOB { get; set; }
        public string Bhamashah_ID { get; set; }
        public string NAME_HINDI { get; set; }
        public string BHAMASHAHMEMBER_ID { get; set; }
        public string BHAMASHAHAckID { get; set; }
        public string NFSA_STATUS { get; set; }
        public string MOBILE_NO { get; set; }
        public string AADHAR_REF_NO { get; set; }
        public string GENDER { get; set; }
        public string MEMBER_TYPE { get; set; }
    }
}

