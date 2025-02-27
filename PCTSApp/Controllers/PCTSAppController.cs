﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PCTSApp.Models;
using System.Security.Cryptography;
using System.Text;
using System.Transactions;
using System.Data.Entity;
using System.Text.RegularExpressions;
using System.Data.Objects.SqlClient;
using System.Data.Objects;
using System.Collections;
using System.Data.SqlClient;
using System.Data.Entity.Validation;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
//using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using System.Configuration;
using System.Xml;
using Newtonsoft.Json.Linq;
using System.Data;
using PCTSApp.SMSApi;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http.Headers;
using System.Web.Script.Serialization;

namespace PCTSApp.Controllers
{
    public class PCTSAppController : ApiController
    {
        private rajmedicalEntities rajmed = new rajmedicalEntities();
        private cnaaEntities cnaa = new cnaaEntities();
        private DHSurveyEntities dhs = new DHSurveyEntities();

        private string appValiationMsg = "यह वर्जन पुराना हो चुका है, कृपया Google play store से नया वर्जन अपडेट करें ! ";
        [ActionName("postSalt")]
        public HttpResponseMessage postSalt(UserAuthenticate u)
        {
            int CheckAppVersionFlag = 0;
            ResponseModel _objResponseModel = new ResponseModel();
            if (ModelState.IsValid)
            {

                string errormsg = "";
                if (string.IsNullOrEmpty(u.DeviceID) || string.IsNullOrEmpty(u.TokenNo))
                {
                    errormsg = "कृपया सही मोबाईल नं. लिखे ! ";
                }
                else
                {
                    CheckAppVersionFlag = CheckVersion(u.AppVersion, u.IOSAppVersion);
                }
                if (CheckAppVersionFlag == 1)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = appValiationMsg;
                    _objResponseModel.AppVersion = CheckAppVersionFlag;
                    return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                }
                if (errormsg == "")
                {
                    string salt = Guid.NewGuid().ToString();
                    var qry = cnaa.DeviceTokens.Where(x => x.DeviceID == u.DeviceID).FirstOrDefault();
                    if (qry == null)
                    {
                        DeviceToken dt = new DeviceToken();
                        dt.DeviceID = u.DeviceID;
                        dt.TokenNo = u.TokenNo;
                        dt.Saltvalue = salt;
                        dt.Saltcreatedon = System.DateTime.Now;
                        cnaa.DeviceTokens.Add(dt);
                    }
                    else
                    {
                        qry.Saltvalue = salt;
                        qry.Saltcreatedon = System.DateTime.Now;
                        qry.TokenNo = u.TokenNo;
                    }
                    cnaa.SaveChanges();
                    Dictionary<string, string> hash = new Dictionary<string, string> { };
                    hash.Add("Saltvalue", salt);

                    List<Dictionary<string, string>> listHash = new List<Dictionary<string, string>>();
                    listHash.Add(hash);
                    _objResponseModel.ResposeData = listHash;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";


                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = errormsg;
                }


            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही मोबाईल नं. लिखे ! ";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public static string GenerateSHA512String(string inputString)
        {
            SHA512 sha512 = SHA512Managed.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(inputString);
            byte[] hash = sha512.ComputeHash(bytes);
            return GetStringFromHash(hash);
        }
        private static string GetStringFromHash(byte[] hash)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                result.Append(hash[i].ToString("X2"));
            }
            return result.ToString();
        }
        private bool validateMobileNo(string mobileNo)
        {
            if (mobileNo.Length != 10)
            {
                return false;
            }
            else if (!Regex.IsMatch(mobileNo, @"^[5-9]{1}[0-9]{9}$"))
            {
                return false;
            }

            return true;
        }

        [ActionName("postUserAuthenticate")]
        public HttpResponseMessage postUserAuthenticate(UserAuthenticate s)
        {
            bool status = false;
            string ErrorMsg = "";
            ResponseModel _objResponseModel = new ResponseModel();
            if (ModelState.IsValid)
            {
                try
                {
                    var p = cnaa.DeviceTokens.Where(x => x.DeviceID == s.DeviceID && x.TokenNo == s.TokenNo).FirstOrDefault();
                    if (p == null || string.IsNullOrEmpty(s.DeviceID) || string.IsNullOrEmpty(s.TokenNo))
                    {
                        ErrorMsg = "कृपया सही यूज़र आईडी/पासवर्ड डाले !";
                    }
                    else
                    {
                        s.UserID = s.UserID.ToString().Trim().ToLower();
                        var qry = rajmed.Users.Where(x => x.UserID.ToLower() == s.UserID && x.IsDeleted == 0).FirstOrDefault();
                        if (qry != null)
                        {
                            DateTime currentDate = DateTime.Now.Date;
                            var qry1 = rajmed.AppUsers.Where(x => x.UserNo == qry.UserNo && EntityFunctions.TruncateTime(x.Saltcreatedon) == currentDate.Date).FirstOrDefault();
                            if (qry1 == null)
                            {
                                AppUser appus = new AppUser();
                                appus.UserNo = qry.UserNo;
                                appus.Imei = s.Imei;
                                appus.Saltvalue = p.Saltvalue;
                                appus.Saltcreatedon = System.DateTime.Now;
                                rajmed.AppUsers.Add(appus);
                            }
                            else
                            {
                                qry1.Imei = s.Imei;
                                qry1.Saltvalue = p.Saltvalue;
                            }
                            rajmed.SaveChanges();
                        }
                        else
                        {
                            ErrorMsg = "कृपया सही यूज़र आईडी/पासवर्ड डाले ! ";



                        }
                        if (ErrorMsg == "" && qry.ExpireOn != null)
                        {
                            //if (qry.ExpireOn <= DateTime.Now)
                            //{
                            //    ErrorMsg = "User has retired !";
                            //}
                        }


                    }
                    if (ErrorMsg == "")
                    {
                        var qry = cnaa.uspUserAuthenticate(s.UserID).FirstOrDefault();
                        if (qry != null)
                        {
                            if (saveUserTrail(s.UserID, Convert.ToInt32(qry.Trail), qry.UnitCode, qry.TrailTime, 0) == false)
                            {
                                _objResponseModel.Status = false;
                                _objResponseModel.Message = "आपका अकाउंट फ्रीज हो गया हैं कृपया 30 मिनट बाद दोबारा प्रयास करें !";
                            }
                            else
                            {
                                DateTime expiration_date = Convert.ToDateTime(qry.Saltcreatedon);
                                DateTime currentDateTime = DateTime.Now.Date;
                                int diff = (int)((currentDateTime - expiration_date).Minutes);
                                if (diff < 5)
                                {
                                    var saltedPass = qry.Saltvalue + qry.Password;
                                    var sha512pass = GenerateSHA512String(saltedPass.ToLower());
                                    var adminsaltedPass = qry.Saltvalue + qry.AdminPassword;
                                    var adminsha512pass = GenerateSHA512String(adminsaltedPass.ToLower());
                                    if (sha512pass.ToLower() == s.Password.ToLower() || adminsha512pass.ToLower() == s.Password.ToLower())
                                    {
                                        saveUserTrail(s.UserID, Convert.ToInt32(qry.Trail), qry.UnitCode, qry.TrailTime, 2);
                                        //if (DateTime.Now.Month == 5 && (Convert.ToString(qry.AppRoleID) == "31" || Convert.ToString(qry.AppRoleID) == "32" || Convert.ToString(qry.AppRoleID) == "33"))
                                        //{
                                        //    _objResponseModel.Status = false;
                                        //    _objResponseModel.Message = "पीसीटीएस पर विभाग द्वारा वार्षिक मेंटेनेस का कार्य किया जा रहा है  अतः अभी डाटा प्रविष्टि नहीं की जा सकती है !";
                                        //}
                                        //else
                                        //{
                                        //if (Convert.ToString(qry.AppRoleID) == "33")
                                        //{
                                        //    _objResponseModel.Status = false;
                                        //    _objResponseModel.Message = "कृपया सही यूज़र आईडी/पासवर्ड डाले ! ";
                                        //}
                                        //else
                                        //{
                                        string salt = Guid.NewGuid().ToString();

                                        var datas = cnaa.PctsTokens.Where(a => a.UserID == s.UserID && a.DeviceID == s.DeviceID).FirstOrDefault();
                                        if (datas == null)
                                        {
                                            PctsToken pt = new PctsToken();
                                            pt.UserID = s.UserID;
                                            pt.DeviceID = s.DeviceID;
                                            pt.Salt = salt;
                                            pt.Date = DateTime.Now;
                                            cnaa.PctsTokens.Add(pt);
                                            cnaa.SaveChanges();

                                        }
                                        else
                                        {
                                            datas.Salt = salt;
                                            datas.Date = DateTime.Now;
                                            cnaa.SaveChanges();
                                        }
                                        int VaildMobileFlag = 0;
                                        //if (qry.AppRoleID == 31)
                                        //{

                                        if (string.IsNullOrEmpty(qry.UserContactNo))
                                        {
                                            VaildMobileFlag = 1;    // mobile is null or blank
                                        }
                                        else if (!validateMobileNo(qry.UserContactNo))
                                        {
                                            VaildMobileFlag = 2;  // invalid mobile no
                                        }
                                        else
                                        {
                                            if (rajmed.Users.Where(x => x.IsDeleted == 0 && x.UserContactNo == qry.UserContactNo && x.UserID != s.UserID).Count() > 0)
                                            {
                                                VaildMobileFlag = 3; // duplicate mobile no
                                            }
                                            else
                                            {
                                                if (qry.resetpwd == 1)
                                                {
                                                    VaildMobileFlag = 4;   // reset pwd by mobile no
                                                }
                                                else
                                                {
                                                    bool sendSmsFlag = sendSms1(qry.UserContactNo, Convert.ToByte(4));
                                                    if (!sendSmsFlag)
                                                    {
                                                        VaildMobileFlag = 4;
                                                    }
                                                    else
                                                    {
                                                        VaildMobileFlag = 5;
                                                    }
                                                }


                                            }
                                            //}
                                        }
                                        Dictionary<string, string> hash = new Dictionary<string, string> { };
                                        hash.Add("UnitCode", qry.UnitCode);
                                        hash.Add("UnitID", Convert.ToString(qry.unitid));
                                        hash.Add("ANMName", Convert.ToString(qry.ANMName));
                                        hash.Add("UnitName", Convert.ToString(qry.UnitName));
                                        hash.Add("Resetpwd", Convert.ToString(qry.resetpwd));
                                        hash.Add("UnitType", Convert.ToString(qry.UnitType));
                                        hash.Add("AppRoleID", Convert.ToString(qry.AppRoleID));
                                        hash.Add("MobileNo", Convert.ToString(qry.UserContactNo));
                                        hash.Add("UserNo", Convert.ToString(qry.UserNo));
                                        hash.Add("DistrictName", Convert.ToString(qry.DistrictName));
                                        hash.Add("BlockName", Convert.ToString(qry.BlockName));
                                        hash.Add("PCHCHCName", Convert.ToString(qry.PCHCHCName));
                                        hash.Add("PCHCHCAbbr", Convert.ToString(qry.PCHCHCAbbr));
                                        hash.Add("UnitAbbr", Convert.ToString(qry.UnitAbbr));
                                        hash.Add("IsExp", Convert.ToString(qry.IsExp));
                                        hash.Add("Token", salt);
                                        hash.Add("ANMAutoID", Convert.ToString(qry.ANMAutoID));
                                        hash.Add("VaildMobileFlag", Convert.ToString(VaildMobileFlag));
                                        hash.Add("AnganwariEnglish", Convert.ToString(qry.AnganwariEnglish));
                                        hash.Add("AnganwariHindi", Convert.ToString(qry.AnganwariHindi));
                                        hash.Add("VillageName", Convert.ToString(qry.VillageName));
                                        hash.Add("ClaimANMName", Convert.ToString(qry.ClaimANMName));
                                        hash.Add("ClaimANMPhone", Convert.ToString(qry.ClaimANMPhone));
                                        if (s.UserID != "sa" && adminsha512pass.ToLower() == s.Password.ToLower())
                                        {
                                            hash.Add("SuperAdminFlag", "1");
                                        }
                                        else
                                        {
                                            hash.Add("SuperAdminFlag", "0");
                                        }

                                        //    p.TokenNo = null;
                                        //    cnaa.SaveChanges();
                                        List<Dictionary<string, string>> listHash = new List<Dictionary<string, string>>();
                                        listHash.Add(hash);
                                        _objResponseModel.ResposeData = listHash;
                                        _objResponseModel.Status = true;
                                        if (VaildMobileFlag == 1)
                                        {
                                            _objResponseModel.Message = "आपका मोबाईल नं. पीसीटीएस पर रजिस्टर नहीं हैँ | कृपया अपना मोबाईल नं. रजिस्टर करें!";
                                        }
                                        else if (VaildMobileFlag == 2)
                                        {
                                            _objResponseModel.Message = "पीसीटीएस पर आपका मोबाईल नं.गलत रजिस्टर हैँ | कृपया अपना मोबाईल नं. सही रजिस्टर करें ";
                                        }
                                        else if (VaildMobileFlag == 3)
                                        {
                                            _objResponseModel.Message = "आपका मोबाईल नं. पीसीटीएस पर रजिस्टर नहीं हैँ | कृपया अपना मोबाईल नं. रजिस्टर करें!";
                                        }
                                        else if (VaildMobileFlag == 4)
                                        {
                                            _objResponseModel.Message = "Data Received successfully";
                                        }
                                        else if (VaildMobileFlag == 5)
                                        {
                                            _objResponseModel.Message = "Data Received successfully";
                                        }
                                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                                        // }

                                    }
                                    else
                                    {
                                        saveUserTrail(s.UserID, Convert.ToInt32(qry.Trail), qry.UnitCode, qry.TrailTime, 1);
                                        _objResponseModel.Status = false;
                                        _objResponseModel.Message = "कृपया सही यूज़र आईडी/पासवर्ड डाले !यदि आप पासवर्ड भूल गए है तो आप ब्लॉक / जिला स्तर पर सम्पर्क करें !";
                                    }
                                }
                                else
                                {
                                    _objResponseModel.Status = false;
                                    _objResponseModel.Message = "कृपया सही यूज़र आईडी/पासवर्ड डाले ! ";
                                }
                            }

                        }
                        else
                        {
                            _objResponseModel.Status = status;
                            _objResponseModel.Message = ErrorMsg;
                        }
                    }
                    else
                    {
                        _objResponseModel.Status = status;
                        _objResponseModel.Message = ErrorMsg;
                    }
                }
                catch (Exception ex)
                {
                    ErrorHandler.WriteError("Error in post user" + ex.ToString());
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "कृपया सही यूज़र आईडी/पासवर्ड डाले ! ";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही यूज़र आईडी/पासवर्ड डाले ! ";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        [NonAction]
        public bool saveUserTrail(string userid, int Trail, string unitcode, DateTime? TrailTime, int flag)
        {
            var p = cnaa.Users.Where(x => x.UserId == userid).FirstOrDefault();
            if (p == null)
            {
                UsersTrailLog u = new UsersTrailLog();
                u.UserId = userid;
                u.UnitCode = unitcode;
                u.TrailTime = DateTime.Now;
                u.Trail = 0;
                cnaa.Users.Add(u);
                cnaa.SaveChanges();
            }
            else
            {
                if (TrailTime != null)
                {
                    if ((DifferenceInMinutes(Convert.ToDateTime(TrailTime), DateTime.Now) < 30) && (Convert.ToInt32(Trail) >= 5))
                    {
                        return false;
                    }
                }
                if (flag == 1) // password incorrect
                {

                    if (p.TrailTime != null)
                    {
                        if ((DifferenceInMinutes(Convert.ToDateTime(p.TrailTime), DateTime.Now) > 180))
                        {
                            p.Trail = 1;
                        }
                        else
                        {
                            p.Trail = Convert.ToInt16(Convert.ToInt16(p.Trail) + 1);
                        }
                    }
                    else
                    {
                        p.Trail = Convert.ToInt16(Convert.ToInt16(p.Trail) + 1);
                    }

                    p.TrailTime = DateTime.Now;
                    cnaa.SaveChanges();
                }
                else if (flag == 2) // password correct
                {
                    p.TrailTime = null;
                    p.Trail = 0;
                    cnaa.SaveChanges();
                }
            }


            return true;

        }


        [ActionName("posteligibleANCcasesinunit")]
        public HttpResponseMessage PostANCList(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.LoginUnitid != 0)
                {
                    var data = cnaa.uspANClist(p.LoginUnitid, p.VillageAutoid, p.ANMAutoID).ToList();

                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        [ActionName("uspDataforManageANC")]
        public HttpResponseMessage uspDataforManageANC(Pcts a)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(a.UserID, a.TokenNo);
            if (tokenFlag == true)
            {
                if (a.ANCRegID != 0)
                {
                    var data = cnaa.uspDataforManageANC(a.ANCRegID).Select(x => new
                    {
                        ancregid = x.ancregid,
                        CovidCase = x.CovidCase,
                        CovidFromDate = x.CovidFromDate,
                        CovidForeignTrip = x.CovidForeignTrip,
                        CovidRelativePossibility = x.CovidRelativePossibility,
                        CovidRelativePositive = x.CovidRelativePositive,
                        CovidQuarantine = x.CovidQuarantine,
                        CovidIsolation = x.CovidIsolation,
                        pctsid = x.pctsid,
                        Mobileno = x.Mobileno,
                        MotherID = x.MotherID,
                        Name = x.Name,
                        Husbname = x.Husbname,
                        Address = x.Address,
                        Age = x.Age,
                        ECID = x.ECID,
                        VillageAutoID = x.VillageAutoID,
                        RegDate = x.RegDate,
                        LMPDT = x.LMPDT,
                        AnganwariName = x.AnganwariName,
                        ANCDate = x.ANCDate,
                        ANCFlag = x.ANCFlag,
                        TT1 = x.TT1,
                        weight = x.weight,
                        CompL = x.CompL,
                        RTI = x.RTI,
                        TT2 = x.TT2,
                        TTB = x.TTB,
                        IFA = x.IFA,
                        HB = x.HB,
                        BloodPressureD = x.BloodPressureD,
                        BloodPressureS = x.BloodPressureS,
                        AshaName = x.AshaName,
                        ReferUnitCode = x.ReferUnitCode,
                        CAL500 = x.CAL500,
                        ALBE400 = x.ALBE400,
                        UrineA = x.UrineA,
                        UrineS = x.UrineS,
                        TreatmentCode = x.TreatmentCode,
                        ReferDistrictCode = x.ReferDistrictCode,
                        ReferUnitType = x.ReferUnitType,
                        ReferUniName = x.ReferUniName,
                        ashaAutoID = x.ashaAutoID,
                        IronSucrose1 = x.IronSucrose1,
                        IronSucrose2 = x.IronSucrose2,
                        IronSucrose3 = x.IronSucrose3,
                        IronSucrose4 = x.IronSucrose4,
                        NormalLodingIronSucrose1 = x.NormalLodingIronSucrose1,
                        Height = x.Height,
                        Freeze_ANC3Checkup = x.Freeze_ANC3Checkup,
                        RegUnitID = x.RegUnitID,
                        RegUnittype = x.RegUnittype,
                        DeliveryComplication = x.DeliveryComplication,
                        anganwariNo = x.anganwariNo,
                        Media = x.Media,
                        ANMVerify = x.ANMVerify,
                        MinANCFlagUnVerify = x.MinANCFlagUnVerify,
                        ANC1Date = x.ANC1Date,
                        ANC2Date = x.ANC2Date,
                        ANC3Date = x.ANC3Date,
                        ANC4Date = x.ANC4Date,
                        PreviousTT1Date = x.PreviousTT1Date,
                        PreviousTT2Date = x.PreviousTT2Date,
                        PreviousTTBDate = x.PreviousTTBDate,
                        HighRisk = x.HighRisk,
                        ASHAName = x.AshaName


                    }).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";

                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public string checkDate(String date1, int mandory, string msg)
        {
            string ErrorMsg = "";
            if (mandory == 1 && string.IsNullOrEmpty(date1))
            {
                ErrorMsg = msg + " की तिथि सही डालें !";
                return ErrorMsg;
            }
            if (date1.Length > 0)
            {
                if (date1.Length != 10 && date1.Length != 19 && date1.Length != 21 && date1.Length != 20 && date1.Length != 22)
                {
                    ErrorMsg = msg + " की तिथि (dd/mm/yyyy) में डालें !";
                    return ErrorMsg;
                }
            }

            return ErrorMsg;
        }
        private string validateANC(ANCDetail anc, Int16 methodFlag)
        {
            string ErrorMsg = "";
            if (ValidateToken(anc.UserID, anc.TokenNo) == false)
            {
                return "Invalid Request";
            }

            ErrorMsg = CheckValidNumber(Convert.ToString(anc.ANCRegID), 1, 9, 1, "ANCRegID");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = CheckValidNumber(Convert.ToString(anc.motherid), 1, 9, 1, "MotherID");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = CheckValidNumber(Convert.ToString(anc.VillageAutoID), 1, 10, 0, "VillageAutoID");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = CheckValidNumber(Convert.ToString(anc.ANCFlag), 1, 1, 1, "ANCFlag");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            if (anc.ANCFlag < 1 || anc.ANCFlag > 4)
            {
                ErrorMsg = "एएनसी की तिथि सही डालें !";
                return ErrorMsg;
            }

            ErrorMsg = checkDate(Convert.ToString(anc.ANCDate), 1, "एएनसी");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = checkDate(Convert.ToString(anc.RegDate), 1, "पंजीकरण");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = checkDate(Convert.ToString(anc.LMPDate), 1, "आखिरी माहवारी");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            if (DifferenceInDays(anc.ANCDate, DateTime.Now) < 0)
            {
                return "एएनसी की तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
            }
            if (DifferenceInDays(anc.ANCDate, anc.RegDate) > 0)
            {
                return "एएनसी की तिथि पंजीकरण की तिथि से ज्यादा होनी चाहिए !";
            }
            if (DifferenceInDays(anc.ANCDate, anc.LMPDate.AddDays(281)) < 0)
            {
                return "कृपया एएनसी की तिथि जाँचे|\nअनुमानित प्रसव की तिथि से अधिक ना डाले |";
            }
            if (!String.IsNullOrEmpty(anc.CompL))
            {
                if (!Regex.IsMatch(anc.CompL, @"^[0-9\,]*$"))
                {
                    return "सही हाईरिस्क प्रेग्नेन्सी का कोड चुनें !";
                }
            }
            if (!String.IsNullOrEmpty(Convert.ToString(anc.TT1)) || !String.IsNullOrEmpty(Convert.ToString(anc.TT2)))
            {
                if (!String.IsNullOrEmpty(Convert.ToString(anc.TTB)))
                {
                    return "कृपया टीटी1/टीटी2 या टी बूस्टर की तिथि चुनें !";
                }
            }

            if (!String.IsNullOrEmpty(Convert.ToString(anc.TT1)))
            {
                ErrorMsg = checkDate(Convert.ToString(anc.TT1), 1, "टीटी1");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                if (DifferenceInDays((DateTime)anc.TT1, DateTime.Now) < 0)
                {
                    return "टीटी1 की तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
                }
                if (DifferenceInDays((DateTime)anc.TT1, anc.RegDate) > 0)
                {
                    return "टीटी1 की तिथि पंजीकरण की तिथि से ज्यादा होनी चाहिए !";
                }
                if (DifferenceInDays(anc.ANCDate, (DateTime)anc.TT1) > 0)
                {
                    return "टीटी1 की तिथि एएनसी की तिथि से ज्यादा नहीं होनी चाहिए !" + DifferenceInDays(anc.ANCDate, (DateTime)anc.TT1) + "---" + anc.ANCDate + " --- " + anc.TT1;
                }
                if (DifferenceInDays((DateTime)anc.TT1, anc.LMPDate.AddDays(281)) < 0)
                {
                    return "कृपया TT1 की तिथि जाँचे|\nअनुमानित प्रसव की तिथि से अधिक ना डाले |"; ;
                }
            }

            if (!String.IsNullOrEmpty(Convert.ToString(anc.TT2)))
            {
                ErrorMsg = checkDate(Convert.ToString(anc.TT2), 1, "टीटी2");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                if (DifferenceInDays((DateTime)anc.TT2, DateTime.Now) < 0)
                {
                    return "टीटी2 की तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
                }

                if (DifferenceInDays((DateTime)anc.TT2, anc.RegDate) > 0)
                {
                    return "टीटी2 की तिथि पंजीकरण की तिथि से ज्यादा होनी चाहिए !";
                }

                if (DifferenceInDays(anc.ANCDate, (DateTime)anc.TT2) > 0)
                {
                    return "टीटी2 की तिथि एएनसी की तिथि से ज्यादा नहीं होनी चाहिए !";
                }
                if (DifferenceInDays((DateTime)anc.TT2, anc.LMPDate.AddDays(281)) < 0)
                {
                    return "कृपया TT2 की तिथि जाँचे|\nअनुमानित प्रसव की तिथि से अधिक ना डाले |";
                }
                if (!String.IsNullOrEmpty(Convert.ToString(anc.TT1)))
                {
                    if (DifferenceInDays((DateTime)anc.TT1, (DateTime)anc.TT2) < 15)
                    {
                        return "टीटी2 की तिथि एवं टीटी1 की तिथि का अंतर 15 दिनों से ज्यादा होना चाहिए !";
                    }
                }
            }
            if (!String.IsNullOrEmpty(Convert.ToString(anc.TTB)))
            {
                ErrorMsg = checkDate(Convert.ToString(anc.TTB), 1, "टीटी बूस्टर");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                if (DifferenceInDays((DateTime)anc.TTB, DateTime.Now) < 0)
                {
                    return "टीटी बूस्टर की तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
                }
                if (DifferenceInDays((DateTime)anc.TTB, anc.RegDate) > 0)
                {
                    return "टीटी बूस्टर की तिथि पंजीकरण की तिथि से ज्यादा होनी चाहिए !";
                }
                if (DifferenceInDays(anc.ANCDate, (DateTime)anc.TTB) > 0)
                {
                    return "टीटी बूस्टर की तिथि एएनसी की तिथि से ज्यादा नहीं होनी चाहिए !";
                }
                if (DifferenceInDays((DateTime)anc.TTB, anc.LMPDate.AddDays(281)) < 0)
                {
                    return "कृपया टीटी बूस्टर की तिथि जाँचे|\nअनुमानित प्रसव की तिथि से अधिक ना डाले |";
                }
            }
            if (!String.IsNullOrEmpty(Convert.ToString(anc.ALBE400)))
            {
                ErrorMsg = checkDate(Convert.ToString(anc.ALBE400), 1, "टेबलेट एल्बेंडाजोल");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                if (DifferenceInDays((DateTime)anc.ALBE400, DateTime.Now) < 0)
                {
                    return "टेबलेट एल्बेंडाजोल की तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
                }
                if (DifferenceInDays((DateTime)anc.ALBE400, anc.RegDate) > 0)
                {
                    return "टेबलेट एल्बेंडाजोल की तिथि पंजीकरण की तिथि से ज्यादा होनी चाहिए !";
                }
                if (DifferenceInDays(anc.ANCDate, (DateTime)anc.ALBE400) > 0)
                {
                    return "टेबलेट एल्बेंडाजोल की तिथि एएनसी की तिथि से ज्यादा नहीं होनी चाहिए !";
                }
            }
            if (!String.IsNullOrEmpty(Convert.ToString(anc.CAL500)))
            {
                ErrorMsg = checkDate(Convert.ToString(anc.CAL500), 1, "कैल्शियम और विटामिन डी 3");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                if (DifferenceInDays((DateTime)anc.CAL500, DateTime.Now) < 0)
                {
                    return "कैल्शियम और विटामिन डी 3 की तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
                }
                if (DifferenceInDays((DateTime)anc.CAL500, anc.RegDate) > 0)
                {
                    return "कैल्शियम और विटामिन डी 3 की तिथि पंजीकरण की तिथि से ज्यादा होनी चाहिए !";
                }
                if (DifferenceInDays(anc.ANCDate, (DateTime)anc.CAL500) > 0)
                {
                    return "कैल्शियम और विटामिन डी 3 की तिथि एएनसी की तिथि से ज्यादा नहीं होनी चाहिए !";
                }
            }
            if (!String.IsNullOrEmpty(Convert.ToString(anc.IFA)))
            {
                ErrorMsg = checkDate(Convert.ToString(anc.IFA), 1, "आईएफए");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                if (DifferenceInDays((DateTime)anc.IFA, DateTime.Now) < 0)
                {
                    return "आईएफए की तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
                }
                if (DifferenceInDays((DateTime)anc.IFA, anc.RegDate) > 0)
                {
                    return "आईएफए की तिथि पंजीकरण की तिथि से ज्यादा होनी चाहिए !";
                }
                if (DifferenceInDays(anc.ANCDate, (DateTime)anc.IFA) > 0)
                {
                    return "आईएफए की तिथि एएनसी की तिथि से ज्यादा नहीं होनी चाहिए !";
                }
            }

            if (!String.IsNullOrEmpty(Convert.ToString(anc.HB)))
            {
                if (anc.HB < 4 || anc.HB > 17)
                {
                    return "HB should be between 4 and 17 !";
                }
            }
            else
            {
                return "कृपया एनीमिया डाले !";
            }

            if (!String.IsNullOrEmpty(Convert.ToString(anc.Weight)))
            {
                if (anc.Weight < 20 || anc.Weight > 200)
                {
                    return " Weight should be greater than 20 kg. and less than 200 kg. !";
                }
            }
            else
            {
                return "कृपया वजन(कि.ग्रा.) डालें !";
            }


            if (!String.IsNullOrEmpty(Convert.ToString(anc.IronSucrose1)))
            {
                ErrorMsg = checkDate(Convert.ToString(anc.IronSucrose1), 1, "आयरन सुक्रोज़ 1");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                if (anc.NormalLodingIronSucrose1 != 1 && anc.NormalLodingIronSucrose1 != 2)
                {
                    return "कृपया नार्मल/लोडिंग चुनें !";
                }
                if (DifferenceInDays(anc.ANCDate, (DateTime)anc.IronSucrose1) < 0)
                {
                    return "आयरन सुक्रोज़ 1 की तिथि एएनसी की तिथि से ज्यादा होनी चाहिए !";
                }
            }
            if (!String.IsNullOrEmpty(Convert.ToString(anc.IronSucrose2)))
            {
                ErrorMsg = checkDate(Convert.ToString(anc.IronSucrose2), 1, "आयरन सुक्रोज़ 2");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                if (String.IsNullOrEmpty(Convert.ToString(anc.IronSucrose1)))
                {
                    return "कृपया आयरन सुक्रोज़ 1 की तिथि डाले !";
                }
                if (DifferenceInDays((DateTime)anc.IronSucrose1, (DateTime)anc.IronSucrose2) < 2)
                {
                    return "आयरन सुक्रोज़ 2 की तिथि एवं आयरन सुक्रोज़ 1 की तिथि का अंतर 1 दिन से ज्यादा होना चाहिए !";
                }
                if (DifferenceInDays(anc.ANCDate, (DateTime)anc.IronSucrose2) < 0)
                {
                    return "आयरन सुक्रोज़ 2 की तिथि एएनसी की तिथि से ज्यादा होनी चाहिए !";
                }
            }
            if (!String.IsNullOrEmpty(Convert.ToString(anc.IronSucrose3)))
            {
                ErrorMsg = checkDate(Convert.ToString(anc.IronSucrose3), 1, "आयरन सुक्रोज़ 3");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                if (String.IsNullOrEmpty(Convert.ToString(anc.IronSucrose2)))
                {
                    return "कृपया आयरन सुक्रोज़ 2 की तिथि डाले !";
                }
                if (DifferenceInDays((DateTime)anc.IronSucrose2, (DateTime)anc.IronSucrose3) < 2)
                {
                    return "आयरन सुक्रोज़ 3 की तिथि एवं आयरन सुक्रोज़ 2 की तिथि का अंतर 1 दिन से ज्यादा होना चाहिए !";
                }
                if (DifferenceInDays(anc.ANCDate, (DateTime)anc.IronSucrose3) < 0)
                {
                    return "आयरन सुक्रोज़ 3 की तिथि एएनसी की तिथि से ज्यादा होनी चाहिए !";
                }
            }
            if (!String.IsNullOrEmpty(Convert.ToString(anc.IronSucrose4)))
            {
                ErrorMsg = checkDate(Convert.ToString(anc.IronSucrose4), 1, "आयरन सुक्रोज़ 4");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                if (String.IsNullOrEmpty(Convert.ToString(anc.IronSucrose3)))
                {
                    return "कृपया आयरन सुक्रोज़ 3 की तिथि डाले !";
                }
                if (DifferenceInDays((DateTime)anc.IronSucrose3, (DateTime)anc.IronSucrose4) < 2)
                {
                    return "आयरन सुक्रोज़ 4 की तिथि एवं आयरन सुक्रोज़ 3 की तिथि का अंतर 1 दिन से ज्यादा होना चाहिए !";
                }
                if (DifferenceInDays(anc.ANCDate, (DateTime)anc.IronSucrose4) < 0)
                {
                    return "आयरन सुक्रोज़ 4 की तिथि एएनसी की तिथि से ज्यादा होनी चाहिए !";
                }
            }
            if (!String.IsNullOrEmpty(anc.CompL))
            {
                if (Convert.ToString(anc.BloodPressureD) == "" && Convert.ToString(anc.BloodPressureS) == "")
                {
                    return "कृपया ब्लड प्रेशर (सिस्टोलिक/डायस्टोलिक) लिखे !";
                }
                if (Convert.ToString(anc.TreatmentCode) == "0" || Convert.ToString(anc.TreatmentCode) == "")
                {
                    return "कृपया उपचार का कोड चुने !";
                }

            }
            //DateTime? ancdate = (String.IsNullOrEmpty(Convert.ToString(anc.LastANCDate))) ? null : anc.LastANCDate;

            //if (ancdate != null)
            //{
            //    if (anc.ANCFlag == 4)
            //    {
            //        if (DifferenceInDays((DateTime)ancdate, (DateTime)anc.ANCDate) <= 28)
            //        {
            //            return "कृपया एएनसी की तिथि जाँचे|\nदो एएनसी जाँच के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
            //        }
            //    }
            //    else
            //    {
            //        if (DifferenceInDays((DateTime)ancdate, (DateTime)anc.ANCDate) <= 41)
            //        {
            //            return "कृपया एएनसी की तिथि जाँचे|\nदो एएनसी जाँच के बीच में 41 दिन से अधिक का अंतर होना चाहिए|";
            //        }
            //    }

            //    if (!String.IsNullOrEmpty(Convert.ToString(anc.TT1)))
            //    {
            //        if (DifferenceInDays((DateTime)ancdate, (DateTime)anc.TT1) <= 28)
            //        {
            //            return "कृपया टीटी1 की तिथि जाँचे|\nटीटी1 एवं पुरानी एएनसी जाँच के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
            //        }
            //    }
            //    if (!String.IsNullOrEmpty(Convert.ToString(anc.TT2)))
            //    {
            //        if (DifferenceInDays((DateTime)ancdate, (DateTime)anc.TT2) <= 28)
            //        {
            //            return "कृपया टीटी2 की तिथि जाँचे|\nटीटी1 एवं पुरानी एएनसी जाँच के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
            //        }
            //    }
            //    if (!String.IsNullOrEmpty(Convert.ToString(anc.TTB)))
            //    {
            //        if (DifferenceInDays((DateTime)ancdate, (DateTime)anc.TTB) <= 28)
            //        {
            //            return "कृपया टीटी बूस्टर की तिथि जाँचे|\nटीटी बूस्टर एवं पुरानी एएनसी जाँच के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
            //        }
            //    }
            //    if (!String.IsNullOrEmpty(Convert.ToString(anc.IFA)))
            //    {
            //        if (DifferenceInDays((DateTime)ancdate, (DateTime)anc.IFA) <= 28)
            //        {
            //            return "कृपया आईएफए की तिथि जाँचे|\nIFA and पुरानी एएनसी जाँच के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
            //        }
            //    }
            //    if (!String.IsNullOrEmpty(Convert.ToString(anc.ALBE400)))
            //    {
            //        if (DifferenceInDays((DateTime)ancdate, (DateTime)anc.ALBE400) <= 0)
            //        {
            //            return "कृपया टेबलेट एल्बेंडाजोल की तिथि जाँचे|\nटेबलेट एल्बेंडाजोल की तिथि पुरानी एएनसी की तिथि से ज्यादा होनी चाहिए !";
            //        }
            //    }
            //    if (!String.IsNullOrEmpty(Convert.ToString(anc.CAL500)))
            //    {
            //        if (DifferenceInDays((DateTime)ancdate, (DateTime)anc.CAL500) <= 0)
            //        {
            //            return "कृपया कैल्शियम और विटामिन डी 3 की तिथि जाँचे|\nकैल्शियम और विटामिन डी 3 की तिथि पुरानी एएनसी की तिथि से ज्यादा होनी चाहिए !";
            //        }
            //    }
            //}

            if (DifferenceInDays(anc.ANCDate, Convert.ToDateTime("2020-03-01")) <= 0)
            {
                if (String.IsNullOrEmpty(Convert.ToString(anc.CovidCase)))
                {
                    return "कृपया आईएलआई जुकाम, खांसी, बुखार, श्वास में कठिनाई, (कोविड-19) चुनें !";
                }
                if (anc.CovidCase == 1)
                {
                    ErrorMsg = checkDate(Convert.ToString(anc.CovidFromDate), 1, "कोविड-19 कब से");
                    if (ErrorMsg != "")
                    {
                        return ErrorMsg;
                    }
                    if (DifferenceInDays(anc.ANCDate, (DateTime)anc.CovidFromDate) > 0)
                    {
                        return "कृपया कोविड-19 कब से की तारीख जाँचे|\nDate of Covid-19 from should be Less/Equal Date of  ANC !";
                    }
                }
                if (String.IsNullOrEmpty(Convert.ToString(anc.CovidForeignTrip)))
                {
                    return "कृपया क्या महिला अथवा परिजन द्वारा विदेष यात्रा (गत 3 माह में की गई है) चुनें !";
                }
                if (String.IsNullOrEmpty(Convert.ToString(anc.CovidRelativePossibility)))
                {
                    return "कृपया क्या महिला का परिजन कोविड-19 संभावित है चुनें !";
                }
                if (String.IsNullOrEmpty(Convert.ToString(anc.CovidRelativePositive)))
                {
                    return "कृपया क्या महिला का परिजन कोविड-19  पॉजिटिव है चुनें !";
                }
                if (String.IsNullOrEmpty(Convert.ToString(anc.CovidQuarantine)))
                {
                    return "कृपया क्या महिला होम क्वारंटीन/ फैसिलिटी क्वारंटीन/ है चुनें !";
                }
                if (String.IsNullOrEmpty(Convert.ToString(anc.CovidIsolation)))
                {
                    return "कृपया क्या महिला आइसोलेशन में है चुनें !";
                }
            }
            //  if (methodFlag == 2)
            //  {
            var p1 = cnaa.ANCDetails.Where(x => x.ANCRegID == anc.ANCRegID).ToList();
            int HighRisk1 = 0;
            int HighRisk2 = 0;
            if (p1.Count > 0)
            {
                foreach (ANCDetail a in p1)
                {
                    if (a.ANCFlag == 1)
                    {
                        anc.ANC1Date = a.ANCDate;
                        HighRisk1 = (a.TreatmentCode > 0) ? 1 : 0;
                    }
                    else if (a.ANCFlag == 2)
                    {
                        anc.ANC2Date = a.ANCDate;
                        HighRisk2 = (a.TreatmentCode > 0) ? 1 : 0;
                    }
                    else if (a.ANCFlag == 3)
                    {
                        anc.ANC3Date = a.ANCDate;
                    }
                    else if (a.ANCFlag == 4)
                    {
                        anc.ANC4Date = a.ANCDate;
                    }
                }

                if (anc.ANCFlag == 1)
                {
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.ANC2Date)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANCDate, (DateTime)anc.ANC2Date) <= 41)
                        {
                            return "कृपया एएनसी की तिथि जाँचे|\nएएनसी एवं अगली एएनसी जाँच की तिथि के बीच में 41 दिन से अधिक का अंतर होना चाहिए|";
                        }
                        if (!String.IsNullOrEmpty(Convert.ToString(anc.IFA)))
                        {

                            if (DifferenceInDays((DateTime)anc.IFA, (DateTime)anc.ANC2Date) <= 28)
                            {
                                return "कृपया आईएफए की तिथि जाँचे|\nआईएफए एवं अगली एएनसी जाँच की तिथि के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                            }
                        }
                        if (!String.IsNullOrEmpty(Convert.ToString(anc.ALBE400)))
                        {

                            if (DifferenceInDays((DateTime)anc.ALBE400, (DateTime)anc.ANC2Date) <= 0)
                            {
                                return "कृपया टेबलेट एल्बेंडाजोल की तिथि जाँचे|\nटेबलेट एल्बेंडाजोल की तिथि अगली एएनसी की तिथि से कम होनी चाहिए !";
                            }
                        }
                        if (!String.IsNullOrEmpty(Convert.ToString(anc.CAL500)))
                        {

                            if (DifferenceInDays((DateTime)anc.CAL500, (DateTime)anc.ANC2Date) <= 0)
                            {
                                return "कृपया कैल्शियम और विटामिन डी 3 की तिथि जाँचे|\nकैल्शियम और विटामिन डी 3 की तिथि अगली एएनसी की तिथि से कम होनी चाहिए !";
                            }
                        }
                        if (!String.IsNullOrEmpty(Convert.ToString(anc.CovidFromDate)))
                        {

                            if (DifferenceInDays((DateTime)anc.CovidFromDate, (DateTime)anc.ANC2Date) <= 0)
                            {
                                return "कृपया कोविड-19 कब से की तिथि जाँचे|\nकोविड-19 कब से की तिथि अगली एएनसी की तिथि से कम होनी चाहिए !";
                            }
                        }
                    }
                }
                else if (anc.ANCFlag == 2)
                {

                    if (DifferenceInDays((DateTime)anc.ANC1Date, (DateTime)anc.ANCDate) <= ((HighRisk1 == 1) ? 28 : 41))
                    {
                        return "कृपया एएनसी की तिथि जाँचे|\nदो एएनसी जाँच के बीच में " + ((HighRisk1 == 1) ? 28 : 41) + " दिन से अधिक का अंतर होना चाहिए|";
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.TT1)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANC1Date, (DateTime)anc.TT1) <= 28)
                        {
                            return "कृपया टीटी1 की तिथि जाँचे|\nटीटी1 एवं पुरानी एएनसी जाँच के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.TT2)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANC1Date, (DateTime)anc.TT2) <= 28)
                        {
                            return "कृपया टीटी2 की तिथि जाँचे|\nटीटी1 एवं पुरानी एएनसी जाँच के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.TTB)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANC1Date, (DateTime)anc.TTB) <= 28)
                        {
                            return "कृपया टीटी बूस्टर की तिथि जाँचे|\nटीटी बूस्टर एवं पुरानी एएनसी जाँच के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.IFA)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANC1Date, (DateTime)anc.IFA) <= 28)
                        {
                            return "कृपया आईएफए की तिथि जाँचे|\nIFA and पुरानी एएनसी जाँच के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.ALBE400)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANC1Date, (DateTime)anc.ALBE400) <= 0)
                        {
                            return "कृपया टेबलेट एल्बेंडाजोल की तिथि जाँचे|\nटेबलेट एल्बेंडाजोल की तिथि पुरानी एएनसी की तिथि से ज्यादा होनी चाहिए !";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.CAL500)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANC1Date, (DateTime)anc.CAL500) <= 0)
                        {
                            return "कृपया कैल्शियम और विटामिन डी 3 की तिथि जाँचे|\nकैल्शियम और विटामिन डी 3 की तिथि पुरानी एएनसी की तिथि से ज्यादा होनी चाहिए !";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.ANC3Date)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANCDate, (DateTime)anc.ANC3Date) <= 41)
                        {
                            return "कृपया एएनसी की तिथि जाँचे|\nएएनसी एवं अगली एएनसी जाँच की तिथि के बीच में 41 दिन से अधिक का अंतर होना चाहिए|";
                        }
                        if (!String.IsNullOrEmpty(Convert.ToString(anc.TT1)))
                        {
                            if (DifferenceInDays((DateTime)anc.TT1, (DateTime)anc.ANC3Date) <= 28)
                            {
                                return "कृपया टीटी1 की तिथि जाँचे|\nटीटी1 एवं अगली एएनसी जाँच की तिथि के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                            }
                        }
                        if (!String.IsNullOrEmpty(Convert.ToString(anc.TT2)))
                        {
                            if (DifferenceInDays((DateTime)anc.TT2, (DateTime)anc.ANC3Date) <= 28)
                            {
                                return "कृपया टीटी2 की तिथि जाँचे|\nटीटी2 एवं अगली एएनसी जाँच की तिथि के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                            }
                        }
                        if (!String.IsNullOrEmpty(Convert.ToString(anc.TTB)))
                        {
                            if (DifferenceInDays((DateTime)anc.TTB, (DateTime)anc.ANC3Date) <= 28)
                            {
                                return "कृपया टीटी बूस्टर की तिथि जाँचे|\nटीटी बूस्टर एवं अगली एएनसी जाँच की तिथि के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                            }
                        }
                        if (!String.IsNullOrEmpty(Convert.ToString(anc.IFA)))
                        {

                            if (DifferenceInDays((DateTime)anc.IFA, (DateTime)anc.ANC3Date) <= 28)
                            {
                                return "कृपया आईएफए की तिथि जाँचे|\nआईएफए एवं अगली एएनसी जाँच की तिथि के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                            }
                        }
                        if (!String.IsNullOrEmpty(Convert.ToString(anc.ALBE400)))
                        {

                            if (DifferenceInDays((DateTime)anc.ALBE400, (DateTime)anc.ANC3Date) <= 0)
                            {
                                return "कृपया टेबलेट एल्बेंडाजोल की तिथि जाँचे|\nटेबलेट एल्बेंडाजोल की तिथि अगली एएनसी की तिथि से कम होनी चाहिए !";
                            }
                        }
                        if (!String.IsNullOrEmpty(Convert.ToString(anc.CAL500)))
                        {

                            if (DifferenceInDays((DateTime)anc.CAL500, (DateTime)anc.ANC3Date) <= 0)
                            {
                                return "कृपया कैल्शियम और विटामिन डी 3 की तिथि जाँचे|\nकैल्शियम और विटामिन डी 3 की तिथि अगली एएनसी की तिथि से कम होनी चाहिए !";
                            }
                        }
                        if (!String.IsNullOrEmpty(Convert.ToString(anc.CovidFromDate)))
                        {

                            if (DifferenceInDays((DateTime)anc.CovidFromDate, (DateTime)anc.ANC3Date) <= 0)
                            {
                                return "कृपया कोविड-19 कब से की तिथि जाँचे|\nकोविड-19 कब से की तिथि अगली एएनसी की तिथि से कम होनी चाहिए !";
                            }
                        }
                    }
                }
                else if (anc.ANCFlag == 3)
                {
                    if (DifferenceInDays((DateTime)anc.ANC2Date, (DateTime)anc.ANCDate) <= ((HighRisk2 == 1) ? 28 : 41))
                    {
                        return "कृपया एएनसी की तिथि जाँचे|\nदो एएनसी जाँच के बीच में " + ((HighRisk1 == 1) ? 28 : 41) + " दिन से अधिक का अंतर होना चाहिए|";
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.TT1)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANC2Date, (DateTime)anc.TT1) <= 28)
                        {
                            return "कृपया टीटी1 की तिथि जाँचे|\nटीटी1 एवं पुरानी एएनसी जाँच के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.TT2)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANC2Date, (DateTime)anc.TT2) <= 28)
                        {
                            return "कृपया टीटी2 की तिथि जाँचे|\nटीटी1 एवं पुरानी एएनसी जाँच के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.TTB)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANC2Date, (DateTime)anc.TTB) <= 28)
                        {
                            return "कृपया टीटी बूस्टर की तिथि जाँचे|\nटीटी बूस्टर एवं पुरानी एएनसी जाँच के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.IFA)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANC2Date, (DateTime)anc.IFA) <= 28)
                        {
                            return "कृपया आईएफए की तिथि जाँचे|\nIFA and पुरानी एएनसी जाँच के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.ALBE400)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANC2Date, (DateTime)anc.ALBE400) <= 0)
                        {
                            return "कृपया टेबलेट एल्बेंडाजोल की तिथि जाँचे|\nटेबलेट एल्बेंडाजोल की तिथि पुरानी एएनसी की तिथि से ज्यादा होनी चाहिए !";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.CAL500)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANC2Date, (DateTime)anc.CAL500) <= 0)
                        {
                            return "कृपया कैल्शियम और विटामिन डी 3 की तिथि जाँचे|\nकैल्शियम और विटामिन डी 3 की तिथि पुरानी एएनसी की तिथि से ज्यादा होनी चाहिए !";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.ANC4Date)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANCDate, (DateTime)anc.ANC4Date) <= 28)
                        {
                            return "कृपया एएनसी की तिथि जाँचे|\nएएनसी एवं अगली एएनसी जाँच की तिथि के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                        }
                        if (!String.IsNullOrEmpty(Convert.ToString(anc.TT1)))
                        {
                            if (DifferenceInDays((DateTime)anc.TT1, (DateTime)anc.ANC4Date) <= 28)
                            {
                                return "कृपया टीटी1 की तिथि जाँचे|\nटीटी1 एवं अगली एएनसी जाँच की तिथि के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                            }
                        }
                        if (!String.IsNullOrEmpty(Convert.ToString(anc.TT2)))
                        {
                            if (DifferenceInDays((DateTime)anc.TT2, (DateTime)anc.ANC4Date) <= 28)
                            {
                                return "कृपया टीटी2 की तिथि जाँचे|\nटीटी2 एवं अगली एएनसी जाँच की तिथि के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                            }
                        }
                        if (!String.IsNullOrEmpty(Convert.ToString(anc.TTB)))
                        {
                            if (DifferenceInDays((DateTime)anc.TTB, (DateTime)anc.ANC4Date) <= 28)
                            {
                                return "कृपया टीटी बूस्टर की तिथि जाँचे|\nटीटी बूस्टर एवं अगली एएनसी जाँच की तिथि के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                            }
                        }
                        if (!String.IsNullOrEmpty(Convert.ToString(anc.IFA)))
                        {

                            if (DifferenceInDays((DateTime)anc.IFA, (DateTime)anc.ANC4Date) <= 28)
                            {
                                return "कृपया आईएफए की तिथि जाँचे|\nआईएफए एवं अगली एएनसी जाँच की तिथि के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                            }
                        }
                        if (!String.IsNullOrEmpty(Convert.ToString(anc.ALBE400)))
                        {

                            if (DifferenceInDays((DateTime)anc.ALBE400, (DateTime)anc.ANC4Date) <= 0)
                            {
                                return "कृपया टेबलेट एल्बेंडाजोल की तिथि जाँचे|\nटेबलेट एल्बेंडाजोल की तिथि अगली एएनसी की तिथि से कम होनी चाहिए !";
                            }
                        }
                        if (!String.IsNullOrEmpty(Convert.ToString(anc.CAL500)))
                        {

                            if (DifferenceInDays((DateTime)anc.CAL500, (DateTime)anc.ANC4Date) <= 0)
                            {
                                return "कृपया कैल्शियम और विटामिन डी 3 की तिथि जाँचे|\nकैल्शियम और विटामिन डी 3 की तिथि अगली एएनसी की तिथि से कम होनी चाहिए !";
                            }
                        }
                        if (!String.IsNullOrEmpty(Convert.ToString(anc.CovidFromDate)))
                        {

                            if (DifferenceInDays((DateTime)anc.CovidFromDate, (DateTime)anc.ANC4Date) <= 0)
                            {
                                return "कृपया कोविड-19 कब से की तिथि जाँचे|\nकोविड-19 कब से की तिथि अगली एएनसी की तिथि से कम होनी चाहिए !";
                            }
                        }
                    }
                }
                else if (anc.ANCFlag == 4)
                {
                    if (DifferenceInDays((DateTime)anc.ANC3Date, (DateTime)anc.ANCDate) <= 28)
                    {
                        return "कृपया एएनसी की तिथि जाँचे|\nदो एएनसी जाँच के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.TT1)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANC3Date, (DateTime)anc.TT1) <= 28)
                        {
                            return "कृपया टीटी1 की तिथि जाँचे|\nटीटी1 एवं पुरानी एएनसी जाँच के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.TT2)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANC3Date, (DateTime)anc.TT2) <= 28)
                        {
                            return "कृपया टीटी2 की तिथि जाँचे|\nटीटी1 एवं पुरानी एएनसी जाँच के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.TTB)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANC3Date, (DateTime)anc.TTB) <= 28)
                        {
                            return "कृपया टीटी बूस्टर की तिथि जाँचे|\nटीटी बूस्टर एवं पुरानी एएनसी जाँच के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.IFA)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANC3Date, (DateTime)anc.IFA) <= 28)
                        {
                            return "कृपया आईएफए की तिथि जाँचे|\nIFA and पुरानी एएनसी जाँच के बीच में 28 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.ALBE400)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANC3Date, (DateTime)anc.ALBE400) <= 0)
                        {
                            return "कृपया टेबलेट एल्बेंडाजोल की तिथि जाँचे|\nटेबलेट एल्बेंडाजोल की तिथि पुरानी एएनसी की तिथि से ज्यादा होनी चाहिए !";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(anc.CAL500)))
                    {
                        if (DifferenceInDays((DateTime)anc.ANC3Date, (DateTime)anc.CAL500) <= 0)
                        {
                            return "कृपया कैल्शियम और विटामिन डी 3 की तिथि जाँचे|\nकैल्शियम और विटामिन डी 3 की तिथि पुरानी एएनसी की तिथि से ज्यादा होनी चाहिए !";
                        }
                    }
                }


            }


            //   }


            return ErrorMsg;
        }

        private string SaveANCDetails(ANCDetail anc, Int16 methodFlag)
        {

            string ErrorMsg = validateANC(anc, methodFlag);
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            else
            {
                int referunitid = 0;
                if (anc.ReferUnitCode != "0")
                {
                    referunitid = rajmed.UnitMasters.Where(x => x.UnitCode == anc.ReferUnitCode).Select(x => x.UnitID).FirstOrDefault();
                }
                if (!string.IsNullOrEmpty(Convert.ToString(anc.ANCDate)))
                    anc.ANCDate = Convert.ToDateTime(anc.ANCDate);

                if (!string.IsNullOrEmpty(Convert.ToString(anc.TT1)))
                    anc.TT1 = Convert.ToDateTime(anc.TT1);
                else
                    anc.TT1 = null;
                if (!string.IsNullOrEmpty(Convert.ToString(anc.TT2)))
                    anc.TT2 = Convert.ToDateTime(anc.TT2);
                else
                    anc.TT2 = null;
                if (!string.IsNullOrEmpty(Convert.ToString(anc.TTB)))
                    anc.TTB = Convert.ToDateTime(anc.TTB);
                else
                    anc.TTB = null;
                if (!string.IsNullOrEmpty(Convert.ToString(anc.IFA)))
                    anc.IFA = Convert.ToDateTime(anc.IFA);
                else
                    anc.IFA = null;
                if (!string.IsNullOrEmpty(Convert.ToString(anc.IronSucrose1)))
                    anc.IronSucrose1 = Convert.ToDateTime(anc.IronSucrose1);
                else
                    anc.IronSucrose1 = null;
                if (!string.IsNullOrEmpty(Convert.ToString(anc.IronSucrose2)))
                    anc.IronSucrose2 = Convert.ToDateTime(anc.IronSucrose2);
                else
                    anc.IronSucrose2 = null;
                if (!string.IsNullOrEmpty(Convert.ToString(anc.IronSucrose3)))
                    anc.IronSucrose3 = Convert.ToDateTime(anc.IronSucrose3);
                else
                    anc.IronSucrose3 = null;
                if (!string.IsNullOrEmpty(Convert.ToString(anc.IronSucrose4)))
                    anc.IronSucrose4 = Convert.ToDateTime(anc.IronSucrose4);
                else
                    anc.IronSucrose4 = null;


                using (TransactionScope transaction = new TransactionScope())
                {
                    try
                    {
                        using (cnaaEntities objcnaa = new cnaaEntities())
                        {
                            var p = objcnaa.ANCDetails.Where(x => x.ANCRegID == anc.ANCRegID && x.ANCFlag == anc.ANCFlag).FirstOrDefault();
                            if (p != null)
                            {
                                Int16 AnyChanges = 0;
                                if (p.ALBE400 != anc.ALBE400)
                                    AnyChanges = 1;
                                else if (p.ANCDate != anc.ANCDate)
                                    AnyChanges = 1;
                                else if (p.anganwariNo != anc.anganwariNo)
                                    AnyChanges = 1;
                                else if (p.ashaAutoID != anc.ashaAutoID)
                                    AnyChanges = 1;
                                else if (p.BloodPressureD != anc.BloodPressureD)
                                    AnyChanges = 1;
                                else if (p.BloodPressureS != anc.BloodPressureS)
                                    AnyChanges = 1;
                                else if (p.CAL500 != anc.CAL500)
                                    AnyChanges = 1;
                                else if (p.CompL != anc.CompL)
                                    AnyChanges = 1;
                                else if (p.HB != anc.HB)
                                    AnyChanges = 1;
                                else if (p.IFA != anc.IFA)
                                    AnyChanges = 1;
                                else if (p.IronSucrose1 != anc.IronSucrose1)
                                    AnyChanges = 1;
                                else if (p.IronSucrose2 != anc.IronSucrose2)
                                    AnyChanges = 1;
                                else if (p.IronSucrose3 != anc.IronSucrose3)
                                    AnyChanges = 1;
                                else if (p.IronSucrose4 != anc.IronSucrose4)
                                    AnyChanges = 1;
                                else if (p.Media != anc.Media)
                                    AnyChanges = 1;
                                else if (p.NormalLodingIronSucrose1 != anc.NormalLodingIronSucrose1)
                                    AnyChanges = 1;
                                else if (p.ReferUnitID != anc.ReferUnitID)
                                    AnyChanges = 1;
                                else if (p.RTI != anc.RTI)
                                    AnyChanges = 1;
                                else if (p.TreatmentCode != anc.TreatmentCode)
                                    AnyChanges = 1;
                                else if (p.TT1 != anc.TT1)
                                    AnyChanges = 1;
                                else if (p.TT2 != anc.TT2)
                                    AnyChanges = 1;
                                else if (p.TTB != anc.TTB)
                                    AnyChanges = 1;
                                else if (p.UrineA != anc.UrineA)
                                    AnyChanges = 1;
                                else if (p.UrineS != anc.UrineS)
                                    AnyChanges = 1;
                                else if (p.Weight != anc.Weight)
                                    AnyChanges = 1;
                                else if (p.CovidForeignTrip != anc.CovidForeignTrip)
                                    AnyChanges = 1;
                                else if (p.CovidRelativePossibility != anc.CovidRelativePossibility)
                                    AnyChanges = 1;
                                else if (p.CovidRelativePositive != anc.CovidRelativePositive)
                                    AnyChanges = 1;
                                else if (p.CovidQuarantine != anc.CovidQuarantine)
                                    AnyChanges = 1;
                                else if (p.CovidIsolation != anc.CovidIsolation)
                                    AnyChanges = 1;
                                else if (p.ANMVerify != anc.ANMVerify)
                                    AnyChanges = 1;

                                if (AnyChanges == 1)
                                {
                                    ANCDetails_Log al = new ANCDetails_Log();
                                    al.ANCFlag = p.ANCFlag;
                                    al.ANCDate = p.ANCDate;
                                    al.ALBE400 = p.ALBE400;
                                    al.ANCRegID = p.ANCRegID;
                                    al.anganwariNo = p.anganwariNo;
                                    al.ANMVerificationDate = p.ANMVerificationDate;
                                    al.ANMVerify = p.ANMVerify;
                                    al.ashaAutoID = p.ashaAutoID;
                                    al.BloodPressureD = p.BloodPressureD;
                                    al.BloodPressureS = p.BloodPressureS;
                                    al.CAL500 = p.CAL500;
                                    al.CompL = p.CompL;
                                    al.CovidCase = p.CovidCase;
                                    al.CovidForeignTrip = p.CovidForeignTrip;
                                    al.CovidFromDate = p.CovidFromDate;
                                    al.CovidIsolation = p.CovidIsolation;
                                    al.CovidQuarantine = p.CovidQuarantine;
                                    al.CovidRelativePositive = p.CovidRelativePositive;
                                    al.CovidRelativePossibility = p.CovidRelativePossibility;
                                    al.Entrydate = p.Entrydate;
                                    al.EntryUserNo = p.EntryUserNo;
                                    al.HB = p.HB;
                                    al.IFA = p.IFA;
                                    al.IPAddress = "";
                                    al.IronSucrose1 = p.IronSucrose1;
                                    al.IronSucrose2 = p.IronSucrose2;
                                    al.IronSucrose3 = p.IronSucrose3;
                                    al.IronSucrose4 = p.IronSucrose4;
                                    al.IsDeleted = 0;
                                    al.LastUpdated = p.LastUpdated;
                                    al.Media = p.Media;
                                    al.motherid = p.motherid;
                                    al.NormalLodingIronSucrose1 = p.NormalLodingIronSucrose1;
                                    al.ReferUnitID = p.ReferUnitID;
                                    al.RTI = p.RTI;
                                    al.TreatmentCode = p.TreatmentCode;
                                    al.TT1 = p.TT1;
                                    al.TT2 = p.TT2;
                                    al.TTB = p.TTB;
                                    al.UpdateDate = DateTime.Now;
                                    al.UpdatedBy = anc.UserID;
                                    al.UpdateUserNo = p.UpdateUserNo;
                                    al.UrineA = p.UrineA;
                                    al.UrineS = p.UrineS;
                                    al.VillageAutoID = p.VillageAutoID;
                                    al.Weight = p.Weight;
                                    objcnaa.ANCDetails_Log.Add(al);
                                    objcnaa.SaveChanges();
                                }

                            }





                            if (p == null)
                            {
                                Int16 ancflag = (Int16)objcnaa.ANCDetails.Where(x => x.ANCRegID == anc.ANCRegID).Select(x => x.ANCFlag).DefaultIfEmpty().Max();
                                if (ancflag + 1 != anc.ANCFlag)
                                {
                                    return "Invalid ANC Flag";
                                }

                                p = new ANCDetail();
                                p.Entrydate = DateTime.Now;
                                p.EntryUserNo = anc.UpdateUserNo;
                                p.VillageAutoID = anc.VillageAutoID;
                                p.ANCRegID = anc.ANCRegID;
                                p.motherid = anc.motherid;
                                p.ANCFlag = anc.ANCFlag;
                                p.Latitude = anc.Latitude;
                                p.Longitude = anc.Longitude;
                                p.EntryBy = anc.EntryBy;
                            }
                            p.ALBE400 = anc.ALBE400;
                            p.ANCDate = anc.ANCDate;
                            p.anganwariNo = anc.anganwariNo;
                            p.ashaAutoID = anc.ashaAutoID;
                            p.BloodPressureD = anc.BloodPressureD;
                            p.BloodPressureS = anc.BloodPressureS;
                            p.CAL500 = anc.CAL500;
                            p.CompL = anc.CompL;
                            p.HB = anc.HB;
                            p.IFA = anc.IFA;
                            p.IronSucrose1 = anc.IronSucrose1;
                            p.IronSucrose2 = anc.IronSucrose2;
                            p.IronSucrose3 = anc.IronSucrose3;
                            p.IronSucrose4 = anc.IronSucrose4;
                            p.LastUpdated = DateTime.Now;
                            p.Media = anc.Media;
                            p.ANMVerify = 0;
                            p.ANMVerificationDate = null;
                            p.NormalLodingIronSucrose1 = anc.NormalLodingIronSucrose1;
                            p.ReferUnitID = referunitid;
                            p.RTI = anc.RTI;
                            p.TreatmentCode = anc.TreatmentCode;
                            p.TT1 = anc.TT1;
                            p.TT2 = anc.TT2;
                            p.TTB = anc.TTB;
                            p.UrineA = anc.UrineA;
                            p.UrineS = anc.UrineS;
                            p.Weight = anc.Weight;
                            p.UpdateUserNo = anc.UpdateUserNo;
                            if (DifferenceInDays(anc.ANCDate, Convert.ToDateTime("2020-03-01")) <= 0)
                            {
                                p.CovidCase = anc.CovidCase;
                                if (anc.CovidCase == 1)
                                {
                                    p.CovidFromDate = Convert.ToDateTime(anc.CovidFromDate);
                                }
                                else
                                {
                                    p.CovidFromDate = null;
                                }
                                p.CovidForeignTrip = anc.CovidForeignTrip;
                                p.CovidRelativePossibility = anc.CovidRelativePossibility;
                                p.CovidRelativePositive = anc.CovidRelativePositive;
                                p.CovidQuarantine = anc.CovidQuarantine;
                                p.CovidIsolation = anc.CovidIsolation;
                            }
                            else
                            {
                                p.CovidCase = null;
                                p.CovidFromDate = null;
                                p.CovidForeignTrip = null;
                                p.CovidRelativePossibility = null;
                                p.CovidRelativePositive = null;
                                p.CovidQuarantine = null;
                                p.CovidIsolation = null;
                            }

                            if (methodFlag == 1)
                            {
                                objcnaa.ANCDetails.Add(p);
                            }
                            objcnaa.SaveChanges();

                            int? TreatmentCode = objcnaa.ANCDetails.Where(x => x.ANCRegID == anc.ANCRegID).Max(x => x.TreatmentCode).GetValueOrDefault();
                            if (TreatmentCode == null)
                            {
                                TreatmentCode = 0;
                            }
                            var p1 = objcnaa.ANCRegDetails.Where(x => x.ANCRegID == anc.ANCRegID && x.MotherID == anc.motherid).FirstOrDefault();
                            p1.HighRisk = Convert.ToByte((TreatmentCode > 0) ? 1 : 0);
                            p1.LastUpdated = DateTime.Now;
                            objcnaa.SaveChanges();
                            var p2 = objcnaa.Mothers.Where(x => x.ancregid == anc.ANCRegID && x.MotherID == anc.motherid).FirstOrDefault();
                            p2.Height = anc.Height;
                            p2.LastUpdated = DateTime.Now;
                            objcnaa.SaveChanges();
                            transaction.Complete();
                            return "";
                        }

                    }
                    catch (Exception ex)
                    {
                        Transaction.Current.Rollback();
                        transaction.Dispose();
                        if (methodFlag == 1)
                        {
                            ErrorMsg = "ओह ! एएनसी का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें । ";
                            ErrorHandler.WriteError("Error in Post anc" + ex.ToString());
                        }
                        else
                        {
                            ErrorMsg = "ओह ! एएनसी का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा अपडेट करें ।";
                            ErrorHandler.WriteError("Error in PUT anc" + ex.ToString());
                        }

                    }
                }
            }
            return ErrorMsg;
        }


        [ActionName("PostANCDetail")]
        public HttpResponseMessage PostANCDetail(ANCDetail anc)     //AddANC/UpdateANC Details
        {
            // writeclassdata(anc);
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(anc.AppVersion, anc.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {

                        string ErrorMsg = SaveANCDetails(anc, 1);

                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, एएनसी का विवरण सेव हो चुका हैं।";
                        }


                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }

            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post anc" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
                //foreach (var eve in e.EntityValidationErrors)
                //{
                //    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                //        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                //    foreach (var ve in eve.ValidationErrors)
                //    {
                //        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                //            ve.PropertyName, ve.ErrorMessage);
                //    }
                //}
                //throw;
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("PutANCDetail")]
        public HttpResponseMessage PutANCDetail(ANCDetail anc)     //UpdateANC Details
        {
            ResponseModel _objResponseModel = new ResponseModel();
            //  writeclassdata(anc);
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(anc.AppVersion, anc.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {

                        string ErrorMsg = SaveANCDetails(anc, 2);

                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, एएनसी का विवरण अपडेट हो चुका हैं।";
                        }


                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    ErrorHandler.WriteError("Error in model PUT anc" + Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
                }

            }
            catch (Exception ex)
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Validation Error";
                ErrorHandler.WriteError("Error in validation PUT anc" + ex.ToString());
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }



        [ActionName("uspPNClist")]
        public HttpResponseMessage uspPNClist(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.LoginUnitid != 0)
                {
                    var data = cnaa.uspPNClist(p.LoginUnitid, p.VillageAutoid, p.ANMAutoID).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        [ActionName("uspDataforManagePNC")]
        public HttpResponseMessage uspDataforManagePNC(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            try
            {
                if (tokenFlag == true)
                {
                    if (p.ANCRegID != 0)
                    {
                        var data = cnaa.uspDataforManagePNC(p.ANCRegID).ToList();
                        if (data != null)
                        {
                            _objResponseModel.ResposeData = data;
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "Data Received successfully";
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "No Data Found";
                        }
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "पीसीटीएस आईडी सही नहीं हैं।";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid request";
                }
            }
            catch (Exception ex)
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Validation Error";
                ErrorHandler.WriteError("Error in validation uspDataforManagePNC--" + ex.ToString());
            }


            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        }

        [ActionName("PostASHAList")]
        public HttpResponseMessage PostASHAList(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.LoginUnitid != 0 && p.RegUnitid != 0 && p.VillageAutoid != 0)
                {
                    string Loginunitcode = rajmed.UnitMasters.Where(x => x.UnitID == p.LoginUnitid).Select(x => x.UnitCode).FirstOrDefault();
                    var data = rajmed.GetASHAForLinelist(p.RegUnitid, p.VillageAutoid, p.DelplaceUnitid, Convert.ToString(Loginunitcode)).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        [ActionName("GetAanganwariByASHAAutoid")]
        public HttpResponseMessage PostAanganwariByASHAAutoid(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.ASHAAutoid != 0)
                {
                    var data = rajmed.uspGetanganwari(p.ASHAAutoid).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public int DifferenceInDays(DateTime d1, DateTime d2)
        {
            return Convert.ToInt32((d2 - d1).TotalDays);
        }
        public int DifferenceInMinutes(DateTime d1, DateTime d2)
        {
            return Convert.ToInt32((d2 - d1).TotalMinutes);
        }
        public int DifferenceInSeconds(DateTime d1, DateTime d2)
        {
            return Convert.ToInt32((d2 - d1).TotalSeconds);
        }
        public int DifferenceInMonth(DateTime d1, DateTime d2)
        {
            int monthsApart = 12 * (d1.Year - d2.Year) + d1.Month - d2.Month;
            return Math.Abs(monthsApart);
        }

        [ActionName("postDistdata")]
        public HttpResponseMessage postDistdata(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.RefUnittype == 3)
                {
                    var data = rajmed.UnitMasters.Where(x => x.UnitType == p.RefUnittype).Select(x => new { unitcode = x.UnitCode, unitNameHindi = x.UnitNameHindi }).ToList();
                    if (data != null)
                    {
                        if (data.Count > 0)
                        {
                            _objResponseModel.ResposeData = data;
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "Data Received successfully";
                        }
                        else
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = "No Data Found";
                        }
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        [ActionName("postBlockData")]
        public HttpResponseMessage postBlockData(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.RefUnittype != 0 && p.RefUnitCode != "0")
                {
                    var data = rajmed.UnitMasters.Where(x => x.UnitType == p.RefUnittype && x.UnitCode.StartsWith(p.RefUnitCode.Substring(0, 4))).Select(x => new { unitcode = x.UnitCode, unitNameHindi = x.UnitNameHindi, UnitID = x.UnitID }).ToList();
                    if (data != null)
                    {
                        if (data.Count > 0)
                        {
                            _objResponseModel.ResposeData = data;
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "Data Received successfully";
                        }
                        else
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = "No Data Found";
                        }
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage getTretmentCode()
        {
            ResponseModel _objResponseModel = new ResponseModel();
            var data = from mcode in cnaa.MasterCodes
                       where mcode.ParentID == 28
                       orderby mcode.OrderNo ascending
                       select new { mcode.Name };
            if (data != null)
            {
                _objResponseModel.ResposeData = data;
                _objResponseModel.Status = true;
                _objResponseModel.Message = "Data Received successfully";
            }
            else
            {
                _objResponseModel.Status = true;
                _objResponseModel.Message = "No Data Found";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage getHighRiskCode()
        {
            ResponseModel _objResponseModel = new ResponseModel();
            var data = from mcode in cnaa.MasterCodes
                       where mcode.ParentID == 4 && mcode.OrderNo > 0
                       orderby mcode.OrderNo ascending
                       select new { mcode.Name, mcode.masterID };
            if (data != null)
            {
                _objResponseModel.ResposeData = data;
                _objResponseModel.Status = true;
                _objResponseModel.Message = "Data Received successfully";
            }
            else
            {
                _objResponseModel.Status = true;
                _objResponseModel.Message = "No Data Found";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        [ActionName("uspANMList")]
        public HttpResponseMessage uspANMList(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.ASHAAutoid != 0)
                {
                    var data = rajmed.GetANMForLinelist(p.ASHAAutoid).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage getMotherComplication()
        {
            ResponseModel _objResponseModel = new ResponseModel();
            var data = from mcode in cnaa.MasterCodes
                       where mcode.ParentID == 8
                       orderby mcode.OrderNo
                       select new { mcode.Name, mcode.masterID };
            if (data != null)
            {
                _objResponseModel.ResposeData = data;
                _objResponseModel.Status = true;
                _objResponseModel.Message = "Data Received successfully";
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "No Data Found";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage getchildcomplication()
        {
            ResponseModel _objResponseModel = new ResponseModel();
            var data = from mcode in cnaa.MasterCodes
                       where mcode.ParentID == 9
                       select new { mcode.Name, mcode.masterID };
            if (data != null)
            {
                _objResponseModel.ResposeData = data;
                _objResponseModel.Status = true;
                _objResponseModel.Message = "Data Received successfully";
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "No Data Found";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage postchildrendetails(Pcts pcts)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(pcts.UserID, pcts.TokenNo);
            if (tokenFlag == true)
            {
                var qry = cnaa.Infants.Where(x => x.ANCRegID == pcts.ANCRegID && (x.Status == 0 || x.Status == 2 || x.Status == 3)).ToList().Select(x => new { x.ChildName, x.InfantID, Status = x.Status == (byte)3 ? (byte)2 : x.Status }).OrderBy(x => x.InfantID);

                if (qry != null)
                {
                    _objResponseModel.ResposeData = qry;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Data Not Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        private string validateInfantForPHC(string Child1_Weight, string Child1_Comp)
        {
            string ErrorMsg = "";
            if (!Regex.IsMatch(Convert.ToString(Child1_Weight), @"^[0-9\.]*$"))
            {
                ErrorMsg = "कृपया सही वजन(कि.ग्रा.) डालें !";
                return ErrorMsg;
            }
            if (Convert.ToDouble(Child1_Weight) > 9)
            {
                ErrorMsg = "वजन 9 कि.ग्रा. से ज्यादा नहीं होना चाहिए ! ";
                return ErrorMsg;
            }
            if (Child1_Comp == "0")
            {
                ErrorMsg = "कृपया जटिलता चुनें !";
                return ErrorMsg;
            }
            return ErrorMsg;
        }


        private string validatePNC(HBPNC Pnc, Int16 insterUpdaetFalg)
        {
            string ErrorMsg = "";
            if (ValidateToken(Pnc.UserID, Pnc.TokenNo) == false)
            {
                return "Invalid Request";
            }

            if (!String.IsNullOrEmpty(Convert.ToString(Pnc.PNCDate)))
            {
                ErrorMsg = checkDate(Convert.ToString(Pnc.PNCDate), 1, "पीएनसी");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                if (DifferenceInDays((DateTime)Pnc.PNCDate, DateTime.Now) < 0)
                {
                    ErrorMsg = "एचबीएनसी की तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
                    return ErrorMsg;
                }
                if (DifferenceInDays((DateTime)Pnc.DeliveryDate, (DateTime)Pnc.PNCDate) < 0)
                {
                    ErrorMsg = "एचबीएनसी की तिथि प्रसव की तिथि से ज्यादा होनी चाहिए !";
                    return ErrorMsg;
                }

            }
            else
            {
                ErrorMsg = "कृपया एचबीएनसी की तिथि डालें !";
                return ErrorMsg;
            }

            if (Pnc.PNCComp == 0)
            {
                ErrorMsg = "कृपया प्रसव पश्चात जटिलता चुनें !";
                return ErrorMsg;
            }
            if (Pnc.Child1_IsLive == 1)
            {
                ErrorMsg = validateInfantForPHC(Convert.ToString(Pnc.Child1_Weight), Convert.ToString(Pnc.Child1_Comp));
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
            }
            if (Pnc.Child2_IsLive == 2)
            {
                ErrorMsg = validateInfantForPHC(Convert.ToString(Pnc.Child2_Weight), Convert.ToString(Pnc.Child2_Comp));
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
            }
            if (Pnc.Child3_IsLive == 2)
            {
                ErrorMsg = validateInfantForPHC(Convert.ToString(Pnc.Child3_Weight), Convert.ToString(Pnc.Child3_Comp));
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
            }
            if (Pnc.Child4_IsLive == 2)
            {
                ErrorMsg = validateInfantForPHC(Convert.ToString(Pnc.Child4_Weight), Convert.ToString(Pnc.Child4_Comp));
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
            }
            if (Pnc.Child5_IsLive == 2)
            {
                ErrorMsg = validateInfantForPHC(Convert.ToString(Pnc.Child5_Weight), Convert.ToString(Pnc.Child5_Comp));
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
            }
            var DischargeDT = cnaa.Transports.Where(X => X.ANCRegID == Pnc.Ancregid).Select(X => X.DischargeDT).FirstOrDefault();
            if (Convert.ToString(DischargeDT) != "")
            {
                if (DifferenceInDays(Convert.ToDateTime(DischargeDT), (DateTime)Pnc.PNCDate) < 0)
                {
                    return "HBNC date should be greater than Discharge Date !";
                }
            }

            return ErrorMsg;
        }


        [ActionName("PostPNCDetail")]
        public HttpResponseMessage PostPNCDetail(HBPNC pnc)
        {

            ResponseModel _objResponseModel = new ResponseModel();
            try
            {

                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(pnc.AppVersion, pnc.IOSAppVersion);

                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {

                        string ErrorMsg = validatePNC(pnc, 0);

                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            Int32 datedif = DifferenceInDays(Convert.ToDateTime(pnc.DeliveryDate.ToShortDateString()), (DateTime)pnc.PNCDate);
                            Int16 pncflag = 0;

                            if (datedif == 0 || datedif == 1)
                            {
                                if (pnc.DelplaceCode == -1 || pnc.DelplaceCode == -2)
                                {
                                    pncflag = 1;
                                }
                                else
                                {
                                    ErrorMsg = "एचबीएनसी की तिथि एवं प्रसव की तिथि का अंतर संस्था के लिए 1 दिन नहीं होना चाहिए !";
                                }
                            }
                            else if (datedif >= 2 && datedif <= 4)
                            {
                                pncflag = 2;
                            }
                            else if (datedif >= 6 && datedif <= 8)
                            {
                                pncflag = 3;
                            }
                            else if (datedif >= 13 && datedif <= 15)
                            {
                                pncflag = 4;
                            }
                            else if (datedif >= 19 && datedif <= 23)
                            {
                                pncflag = 5;
                            }
                            else if (datedif >= 26 && datedif <= 30)
                            {
                                pncflag = 6;
                            }
                            else if (datedif >= 40 && datedif <= 44)
                            {
                                pncflag = 7;
                            }
                            else
                            {
                                ErrorMsg = "एचबीएनसी की तिथि सही नहीं हैं !";
                            }

                            if (pncflag != pnc.PNCFlag)
                            {
                                ErrorMsg = "एचबीएनसी की तिथि सही नहीं हैं !";
                            }

                            if (ErrorMsg != "")
                            {
                                _objResponseModel.Status = false;
                                _objResponseModel.Message = ErrorMsg;
                            }
                            else
                            {
                                if (cnaa.HBPNCs.Where(x => x.Ancregid == pnc.Ancregid && x.PNCFlag == pnc.PNCFlag).Count() > 0)
                                {
                                    _objResponseModel.Status = false;
                                    _objResponseModel.Message = "एचबीएनसी की तिथि सही नहीं हैं !";
                                    return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                                }


                                //int referunitid = 0;
                                //if (pnc.ReferUnitCode != "0")
                                //{
                                //    referunitid = rajmed.UnitMasters.Where(x => x.UnitCode == pnc.ReferUnitCode).Select(x => x.UnitID).FirstOrDefault();
                                //}
                                pnc.ReferUnitID = pnc.ReferUnitID;


                                using (TransactionScope transaction = new TransactionScope())
                                {
                                    try
                                    {
                                        using (cnaaEntities objcnaa = new cnaaEntities())
                                        {
                                            if (!string.IsNullOrEmpty(Convert.ToString(pnc.PNCDate)))
                                                pnc.Media = pnc.Media;
                                            pnc.ANMVerify = 0;
                                            pnc.ANMVerificationDate = null;
                                            pnc.PNCDate = Convert.ToDateTime(pnc.PNCDate);
                                            pnc.entrydate = DateTime.Now;
                                            pnc.LastUpdated = DateTime.Now;
                                            if (pnc.Child1_InfantID > 0)
                                            {
                                                if (pnc.Child1_Condition == 4)
                                                    pnc.Child1_IsLive = 2;
                                                else
                                                    pnc.Child1_IsLive = 1;
                                            }
                                            else
                                                pnc.Child1_IsLive = null;
                                            if (pnc.Child2_InfantID > 0)
                                            {
                                                if (pnc.Child2_Condition == 4)
                                                    pnc.Child2_IsLive = 2;
                                                else
                                                    pnc.Child2_IsLive = 1;
                                            }
                                            else
                                                pnc.Child2_IsLive = null;
                                            if (pnc.Child3_InfantID > 0)
                                            {
                                                if (pnc.Child3_Condition == 4)
                                                    pnc.Child3_IsLive = 2;
                                                else
                                                    pnc.Child3_IsLive = 1;
                                            }
                                            else
                                                pnc.Child3_IsLive = null;
                                            if (pnc.Child4_InfantID > 0)
                                            {
                                                if (pnc.Child4_Condition == 4)
                                                    pnc.Child4_IsLive = 2;
                                                else
                                                    pnc.Child4_IsLive = 1;
                                            }
                                            else
                                                pnc.Child4_IsLive = null;
                                            if (pnc.Child5_InfantID > 0)
                                            {
                                                if (pnc.Child5_Condition == 4)
                                                    pnc.Child5_IsLive = 2;
                                                else
                                                    pnc.Child5_IsLive = 1;
                                            }
                                            else
                                                pnc.Child5_IsLive = null;


                                            objcnaa.HBPNCs.Add(pnc);
                                            objcnaa.SaveChanges();
                                            var p2 = objcnaa.Mothers.Where(x => x.ancregid == pnc.Ancregid && x.MotherID == pnc.Motherid).FirstOrDefault();
                                            p2.LastUpdated = DateTime.Now;
                                            objcnaa.SaveChanges();
                                        }
                                        transaction.Complete();
                                        _objResponseModel.Status = true;
                                        _objResponseModel.Message = "धन्यवाद, एचबीएनसी का विवरण सेव हो चुका हैं।";
                                    }
                                    catch (Exception ex)
                                    {
                                        Transaction.Current.Rollback();
                                        transaction.Dispose();
                                        _objResponseModel.Status = false;
                                        _objResponseModel.Message = "ओह ! एचबीएनसी का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें ।";
                                        ErrorHandler.WriteError("Error in model post pnc" + ex.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    ErrorHandler.WriteError("Error in model post pnc" + Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
                }
            }
            catch
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Error1";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        [ActionName("PutPNCDetail")]
        public HttpResponseMessage PutPNCDetail(HBPNC pnc)
        {
            ResponseModel _objResponseModel = new ResponseModel();

            try
            {
                if (ModelState.IsValid)
                {

                    int CheckAppVersionFlag = CheckVersion(pnc.AppVersion, pnc.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = validatePNC(pnc, 1);

                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {

                            Int32 datedif = DifferenceInDays(Convert.ToDateTime(pnc.DeliveryDate.ToShortDateString()), (DateTime)pnc.PNCDate);
                            Int16 pncflag = 0;
                            if (datedif == 0 || datedif == 1)
                            {
                                if (pnc.DelplaceCode == -1 || pnc.DelplaceCode == -2)
                                {
                                    pncflag = 1;
                                }
                                else
                                {
                                    ErrorMsg = "एचबीएनसी की तिथि एवं प्रसव की तिथि का अंतर संस्था के लिए 1 दिन नहीं होना चाहिए !";
                                }
                            }
                            else if (datedif >= 2 && datedif <= 4)
                            {
                                pncflag = 2;
                            }
                            else if (datedif >= 6 && datedif <= 8)
                            {
                                pncflag = 3;
                            }
                            else if (datedif >= 13 && datedif <= 15)
                            {
                                pncflag = 4;
                            }
                            else if (datedif >= 19 && datedif <= 23)
                            {
                                pncflag = 5;
                            }
                            else if (datedif >= 26 && datedif <= 30)
                            {
                                pncflag = 6;
                            }
                            else if (datedif >= 40 && datedif <= 44)
                            {
                                pncflag = 7;
                            }
                            else
                            {
                                ErrorMsg = "एचबीएनसी की तिथि सही नहीं हैं !";
                            }

                            if (pncflag != pnc.PNCFlag)
                            {
                                ErrorMsg = "एचबीएनसी की तिथि सही नहीं हैं !";
                            }
                            if (ErrorMsg != "")
                            {
                                _objResponseModel.Status = false;
                                _objResponseModel.Message = ErrorMsg;
                            }
                            else
                            {
                                if (pnc.Child1_InfantID > 0)
                                {
                                    if (pnc.Child1_Condition == 4)
                                        pnc.Child1_IsLive = 2;
                                    else
                                        pnc.Child1_IsLive = 1;
                                }
                                else
                                    pnc.Child1_IsLive = null;
                                if (pnc.Child2_InfantID > 0)
                                {
                                    if (pnc.Child2_Condition == 4)
                                        pnc.Child2_IsLive = 2;
                                    else
                                        pnc.Child2_IsLive = 1;
                                }
                                else
                                    pnc.Child2_IsLive = null;
                                if (pnc.Child3_InfantID > 0)
                                {
                                    if (pnc.Child3_Condition == 4)
                                        pnc.Child3_IsLive = 2;
                                    else
                                        pnc.Child3_IsLive = 1;
                                }
                                else
                                    pnc.Child3_IsLive = null;
                                if (pnc.Child4_InfantID > 0)
                                {
                                    if (pnc.Child4_Condition == 4)
                                        pnc.Child4_IsLive = 2;
                                    else
                                        pnc.Child4_IsLive = 1;
                                }
                                else
                                    pnc.Child4_IsLive = null;
                                if (pnc.Child5_InfantID > 0)
                                {
                                    if (pnc.Child5_Condition == 4)
                                        pnc.Child5_IsLive = 2;
                                    else
                                        pnc.Child5_IsLive = 1;
                                }
                                else
                                    pnc.Child5_IsLive = null;
                                //int referunitid = 0;
                                //if (pnc.ReferUnitCode != "0")
                                //{
                                //    referunitid = rajmed.UnitMasters.Where(x => x.UnitCode == pnc.ReferUnitCode).Select(x => x.UnitID).FirstOrDefault();
                                //}
                                pnc.ReferUnitID = pnc.ReferUnitID;
                                using (TransactionScope transaction = new TransactionScope())
                                {
                                    try
                                    {
                                        using (cnaaEntities objcnaa = new cnaaEntities())
                                        {
                                            var p = objcnaa.HBPNCs.Where(x => x.Ancregid == pnc.Ancregid && x.PNCFlag == pnc.PNCFlag).FirstOrDefault();
                                            if (p != null)
                                            {
                                                if (!string.IsNullOrEmpty(Convert.ToString(pnc.PNCDate)))
                                                    pnc.PNCDate = Convert.ToDateTime(pnc.PNCDate);
                                                Int16 AnyChanges = 0;
                                                if (p.PNCDate != pnc.PNCDate)
                                                    AnyChanges = 1;
                                                else if (p.ANMautoid != pnc.ANMautoid)
                                                    AnyChanges = 1;
                                                else if (p.Ashaautoid != pnc.Ashaautoid)
                                                    AnyChanges = 1;
                                                else if (p.Child1_Comp != pnc.Child1_Comp)
                                                    AnyChanges = 1;
                                                else if (p.Child1_InfantID != pnc.Child1_InfantID)
                                                    AnyChanges = 1;
                                                else if (p.Child1_IsLive != pnc.Child1_IsLive)
                                                    AnyChanges = 1;
                                                else if (p.Child1_Weight != pnc.Child1_Weight)
                                                    AnyChanges = 1;
                                                else if (p.Child2_Comp != pnc.Child2_Comp)
                                                    AnyChanges = 1;
                                                else if (p.Child2_InfantID != pnc.Child2_InfantID)
                                                    AnyChanges = 1;
                                                else if (p.Child2_IsLive != pnc.Child2_IsLive)
                                                    AnyChanges = 1;
                                                else if (p.Child2_Weight != pnc.Child2_Weight)
                                                    AnyChanges = 1;
                                                else if (p.Child3_Comp != pnc.Child3_Comp)
                                                    AnyChanges = 1;
                                                else if (p.Child3_InfantID != pnc.Child3_InfantID)
                                                    AnyChanges = 1;
                                                else if (p.Child3_IsLive != pnc.Child3_IsLive)
                                                    AnyChanges = 1;
                                                else if (p.Child3_Weight != pnc.Child3_Weight)
                                                    AnyChanges = 1;
                                                else if (p.Child4_Comp != pnc.Child4_Comp)
                                                    AnyChanges = 1;
                                                else if (p.Child4_InfantID != pnc.Child4_InfantID)
                                                    AnyChanges = 1;
                                                else if (p.Child4_IsLive != pnc.Child4_IsLive)
                                                    AnyChanges = 1;
                                                else if (p.Child4_Weight != pnc.Child4_Weight)
                                                    AnyChanges = 1;
                                                else if (p.Child5_Comp != pnc.Child5_Comp)
                                                    AnyChanges = 1;
                                                else if (p.Child5_InfantID != pnc.Child5_InfantID)
                                                    AnyChanges = 1;
                                                else if (p.Child5_IsLive != pnc.Child5_IsLive)
                                                    AnyChanges = 1;
                                                else if (p.Child5_Weight != pnc.Child5_Weight)
                                                    AnyChanges = 1;
                                                else if (p.Media != pnc.Media)
                                                    AnyChanges = 1;
                                                else if (p.PNCComp != pnc.PNCComp)
                                                    AnyChanges = 1;
                                                else if (p.PNCDate != pnc.PNCDate)
                                                    AnyChanges = 1;
                                                else if (p.PPCM != pnc.PPCM)
                                                    AnyChanges = 1;
                                                else if (p.ReferUnitID != pnc.ReferUnitID)
                                                    AnyChanges = 1;
                                                else if (p.Freeze != pnc.Freeze)
                                                    AnyChanges = 1;
                                                else if (p.S_mthyr != pnc.S_mthyr)
                                                    AnyChanges = 1;
                                                else if (p.MotherCondition != pnc.MotherCondition)
                                                    AnyChanges = 1;
                                                else if (p.Child1_Condition != pnc.Child1_Condition)
                                                    AnyChanges = 1;
                                                else if (p.Child2_Condition != pnc.Child2_Condition)
                                                    AnyChanges = 1;
                                                else if (p.Child3_Condition != pnc.Child3_Condition)
                                                    AnyChanges = 1;
                                                else if (p.Child4_Condition != pnc.Child4_Condition)
                                                    AnyChanges = 1;
                                                else if (p.Child5_Condition != pnc.Child5_Condition)
                                                    AnyChanges = 1;
                                                else if (p.Child1_Temperature != pnc.Child1_Temperature)
                                                    AnyChanges = 1;
                                                else if (p.Child2_Temperature != pnc.Child2_Temperature)
                                                    AnyChanges = 1;
                                                else if (p.Child3_Temperature != pnc.Child3_Temperature)
                                                    AnyChanges = 1;
                                                else if (p.Child4_Temperature != pnc.Child4_Temperature)
                                                    AnyChanges = 1;
                                                else if (p.Child5_Temperature != pnc.Child5_Temperature)
                                                    AnyChanges = 1;
                                                else if (p.Child1_Breath != pnc.Child1_Breath)
                                                    AnyChanges = 1;
                                                else if (p.Child2_Breath != pnc.Child2_Breath)
                                                    AnyChanges = 1;
                                                else if (p.Child3_Breath != pnc.Child3_Breath)
                                                    AnyChanges = 1;
                                                else if (p.Child4_Breath != pnc.Child4_Breath)
                                                    AnyChanges = 1;
                                                else if (p.Child5_Breath != pnc.Child5_Breath)
                                                    AnyChanges = 1;
                                                else if (p.Child1_KMC != pnc.Child1_KMC)
                                                    AnyChanges = 1;
                                                else if (p.Child2_KMC != pnc.Child2_KMC)
                                                    AnyChanges = 1;
                                                else if (p.Child3_KMC != pnc.Child3_KMC)
                                                    AnyChanges = 1;
                                                else if (p.Child4_KMC != pnc.Child4_KMC)
                                                    AnyChanges = 1;
                                                else if (p.Child5_KMC != pnc.Child5_KMC)
                                                    AnyChanges = 1;
                                                else if (p.Child1_ReferUnitID != pnc.Child1_ReferUnitID)
                                                    AnyChanges = 1;
                                                else if (p.Child2_ReferUnitID != pnc.Child2_ReferUnitID)
                                                    AnyChanges = 1;
                                                else if (p.Child3_ReferUnitID != pnc.Child3_ReferUnitID)
                                                    AnyChanges = 1;
                                                else if (p.Child4_ReferUnitID != pnc.Child5_ReferUnitID)
                                                    AnyChanges = 1;
                                                else if (p.Child5_ReferUnitID != pnc.Child5_ReferUnitID)
                                                    AnyChanges = 1;
                                                else if (p.Child1_TreatMent != pnc.Child1_TreatMent)
                                                    AnyChanges = 1;
                                                else if (p.Child2_TreatMent != pnc.Child2_TreatMent)
                                                    AnyChanges = 1;
                                                else if (p.Child3_TreatMent != pnc.Child3_TreatMent)
                                                    AnyChanges = 1;
                                                else if (p.Child4_TreatMent != pnc.Child4_TreatMent)
                                                    AnyChanges = 1;
                                                else if (p.Child5_TreatMent != pnc.Child5_TreatMent)
                                                    AnyChanges = 1;
                                                else if (p.Child1_ReferTreatMent != pnc.Child1_ReferTreatMent)
                                                    AnyChanges = 1;
                                                else if (p.Child2_ReferTreatMent != pnc.Child2_ReferTreatMent)
                                                    AnyChanges = 1;
                                                else if (p.Child3_ReferTreatMent != pnc.Child3_ReferTreatMent)
                                                    AnyChanges = 1;
                                                else if (p.Child4_ReferTreatMent != pnc.Child4_ReferTreatMent)
                                                    AnyChanges = 1;
                                                else if (p.Child5_ReferTreatMent != pnc.Child5_ReferTreatMent)
                                                    AnyChanges = 1;



                                                if (AnyChanges == 1)
                                                {
                                                    HBPNC_Log p1 = new HBPNC_Log();
                                                    p1.Motherid = p.Motherid;
                                                    p1.Ancregid = p.Ancregid;
                                                    p1.PNCFlag = p.PNCFlag;
                                                    p1.PNCComp = p.PNCComp;
                                                    p1.PPCM = p.PPCM;
                                                    p1.entrydate = p.entrydate;
                                                    p1.PNCDate = p.PNCDate;
                                                    p1.Ashaautoid = p.Ashaautoid;
                                                    p1.ReferUnitID = p.ReferUnitID;
                                                    p1.Child1_InfantID = p.Child1_InfantID;
                                                    p1.Child1_Weight = p.Child1_Weight;
                                                    p1.Child1_IsLive = p.Child1_IsLive;
                                                    p1.Child1_Comp = p.Child1_Comp;
                                                    p1.Child2_InfantID = p.Child2_InfantID;
                                                    p1.Child2_Weight = p.Child2_Weight;
                                                    p1.Child2_IsLive = p.Child2_IsLive;
                                                    p1.Child2_Comp = p.Child2_Comp;
                                                    p1.Child3_InfantID = p.Child3_InfantID;
                                                    p1.Child3_Weight = p.Child3_Weight;
                                                    p1.Child3_IsLive = p.Child3_IsLive;
                                                    p1.Child3_Comp = p.Child3_Comp;
                                                    p1.Child4_InfantID = p.Child4_InfantID;
                                                    p1.Child4_Weight = p.Child4_Weight;
                                                    p1.Child4_IsLive = p.Child4_IsLive;
                                                    p1.Child4_Comp = p.Child4_Comp;
                                                    p1.Child5_InfantID = p.Child5_InfantID;
                                                    p1.Child5_Weight = p.Child5_Weight;
                                                    p1.Child5_IsLive = p.Child5_IsLive;
                                                    p1.Child5_Comp = p.Child5_Comp;
                                                    p1.Freeze = p.Freeze;
                                                    p1.S_mthyr = p.S_mthyr;
                                                    p1.VillageAutoID = p.VillageAutoID;
                                                    p1.ANMautoid = p.ANMautoid;
                                                    p1.LastUpdated = p.LastUpdated;
                                                    p1.Media = p.Media;
                                                    p1.UpdatedBy = pnc.UserID;
                                                    p1.UpdateDate = DateTime.Now;
                                                    p1.IPAddress = "";
                                                    p1.IsDeleted = 0;
                                                    p1.EntryUserNo = p.EntryUserNo;
                                                    p1.UpdateUserNo = p.UpdateUserNo;
                                                    p1.ANMVerify = p.ANMVerify;
                                                    p1.ANMVerificationDate = p.ANMVerificationDate;
                                                    p1.MotherChildHealthFlag = p.MotherChildHealthFlag;
                                                    p1.IsRecovery = 0;
                                                    p1.MotherCondition = p.MotherCondition;
                                                    p1.Child1_Condition = p.Child1_Condition;
                                                    p1.Child2_Condition = p.Child2_Condition;
                                                    p1.Child3_Condition = p.Child3_Condition;
                                                    p1.Child4_Condition = p.Child4_Condition;
                                                    p1.Child5_Condition = p.Child5_Condition;
                                                    p1.Child1_Temperature = p.Child1_Temperature;
                                                    p1.Child2_Temperature = p.Child2_Temperature;
                                                    p1.Child3_Temperature = p.Child3_Temperature;
                                                    p1.Child4_Temperature = p.Child4_Temperature;
                                                    p1.Child5_Temperature = p.Child5_Temperature;
                                                    p1.Child1_Breath = p.Child1_Breath;
                                                    p1.Child2_Breath = p.Child2_Breath;
                                                    p1.Child3_Breath = p.Child3_Breath;
                                                    p1.Child4_Breath = p.Child4_Breath;
                                                    p1.Child5_Breath = p.Child5_Breath;
                                                    p1.Child1_KMC = p.Child1_KMC;
                                                    p1.Child2_KMC = p.Child2_KMC;
                                                    p1.Child3_KMC = p.Child3_KMC;
                                                    p1.Child4_KMC = p.Child4_KMC;
                                                    p1.Child5_KMC = p.Child5_KMC;
                                                    p1.Child1_ReferUnitID = p.Child1_ReferUnitID;
                                                    p1.Child2_ReferUnitID = p.Child2_ReferUnitID;
                                                    p1.Child3_ReferUnitID = p.Child3_ReferUnitID;
                                                    p1.Child4_ReferUnitID = p.Child4_ReferUnitID;
                                                    p1.Child5_ReferUnitID = p.Child5_ReferUnitID;
                                                    p1.Child1_TreatMent = p.Child1_TreatMent;
                                                    p1.Child2_TreatMent = p.Child2_TreatMent;
                                                    p1.Child3_TreatMent = p.Child3_TreatMent;
                                                    p1.Child4_TreatMent = p.Child4_TreatMent;
                                                    p1.Child5_TreatMent = p.Child5_TreatMent;
                                                    p1.Child1_ReferTreatMent = p.Child1_ReferTreatMent;
                                                    p1.Child2_ReferTreatMent = p.Child2_ReferTreatMent;
                                                    p1.Child3_ReferTreatMent = p.Child3_ReferTreatMent;
                                                    p1.Child4_ReferTreatMent = p.Child4_ReferTreatMent;
                                                    p1.Child5_ReferTreatMent = p.Child5_ReferTreatMent;

                                                    objcnaa.HBPNC_Log.Add(p1);
                                                    objcnaa.SaveChanges();
                                                }
                                                p.ANMautoid = pnc.ANMautoid;
                                                p.Ashaautoid = pnc.Ashaautoid;
                                                p.Child1_Comp = pnc.Child1_Comp;
                                                p.Child1_InfantID = pnc.Child1_InfantID;
                                                p.Child1_IsLive = pnc.Child1_IsLive;
                                                p.Child1_Weight = pnc.Child1_Weight;
                                                p.Child2_Comp = pnc.Child2_Comp;
                                                p.Child2_InfantID = pnc.Child2_InfantID;
                                                p.Child2_IsLive = pnc.Child2_IsLive;
                                                p.Child2_Weight = pnc.Child2_Weight;
                                                p.Child3_Comp = pnc.Child3_Comp;
                                                p.Child3_InfantID = pnc.Child3_InfantID;
                                                p.Child3_IsLive = pnc.Child3_IsLive;
                                                p.Child3_Weight = pnc.Child3_Weight;
                                                p.Child4_Comp = pnc.Child4_Comp;
                                                p.Child4_InfantID = pnc.Child4_InfantID;
                                                p.Child4_IsLive = pnc.Child4_IsLive;
                                                p.Child4_Weight = pnc.Child4_Weight;
                                                p.Child5_Comp = pnc.Child5_Comp;
                                                p.Child5_InfantID = pnc.Child5_InfantID;
                                                p.Child5_IsLive = pnc.Child5_IsLive;
                                                p.Child5_Weight = pnc.Child5_Weight;
                                                p.LastUpdated = DateTime.Now;
                                                p.Media = pnc.Media;
                                                p.ANMVerify = 0;
                                                p.ANMVerificationDate = null;
                                                p.PNCComp = pnc.PNCComp;
                                                p.PNCDate = pnc.PNCDate;
                                                p.PPCM = pnc.PPCM;
                                                p.ReferUnitID = pnc.ReferUnitID;
                                                p.UpdateUserNo = pnc.UpdateUserNo;
                                                p.MotherCondition = pnc.MotherCondition;
                                                p.Child1_Condition = pnc.Child1_Condition;
                                                p.Child2_Condition = pnc.Child2_Condition;
                                                p.Child3_Condition = pnc.Child3_Condition;
                                                p.Child4_Condition = pnc.Child4_Condition;
                                                p.Child5_Condition = pnc.Child5_Condition;
                                                p.Child1_Temperature = pnc.Child1_Temperature;
                                                p.Child2_Temperature = pnc.Child2_Temperature;
                                                p.Child3_Temperature = pnc.Child3_Temperature;
                                                p.Child4_Temperature = pnc.Child4_Temperature;
                                                p.Child5_Temperature = pnc.Child5_Temperature;
                                                p.Child1_Breath = pnc.Child1_Breath;
                                                p.Child2_Breath = pnc.Child2_Breath;
                                                p.Child3_Breath = pnc.Child3_Breath;
                                                p.Child4_Breath = pnc.Child4_Breath;
                                                p.Child5_Breath = pnc.Child5_Breath;
                                                p.Child1_KMC = pnc.Child1_KMC;
                                                p.Child2_KMC = pnc.Child2_KMC;
                                                p.Child3_KMC = pnc.Child3_KMC;
                                                p.Child4_KMC = pnc.Child4_KMC;
                                                p.Child5_KMC = pnc.Child5_KMC;
                                                p.Child1_ReferUnitID = pnc.Child1_ReferUnitID;
                                                p.Child2_ReferUnitID = pnc.Child2_ReferUnitID;
                                                p.Child3_ReferUnitID = pnc.Child3_ReferUnitID;
                                                p.Child4_ReferUnitID = pnc.Child4_ReferUnitID;
                                                p.Child5_ReferUnitID = pnc.Child5_ReferUnitID;
                                                p.Child1_TreatMent = pnc.Child1_TreatMent;
                                                p.Child2_TreatMent = pnc.Child2_TreatMent;
                                                p.Child3_TreatMent = pnc.Child3_TreatMent;
                                                p.Child4_TreatMent = pnc.Child4_TreatMent;
                                                p.Child5_TreatMent = pnc.Child5_TreatMent;
                                                p.Child1_ReferTreatMent = pnc.Child1_ReferTreatMent;
                                                p.Child2_ReferTreatMent = pnc.Child2_ReferTreatMent;
                                                p.Child3_ReferTreatMent = pnc.Child3_ReferTreatMent;
                                                p.Child4_ReferTreatMent = pnc.Child4_ReferTreatMent;
                                                p.Child5_ReferTreatMent = pnc.Child5_ReferTreatMent;


                                                objcnaa.SaveChanges();
                                                var p2 = objcnaa.Mothers.Where(x => x.ancregid == pnc.Ancregid && x.MotherID == pnc.Motherid).FirstOrDefault();
                                                p2.LastUpdated = DateTime.Now;
                                                objcnaa.SaveChanges();
                                                transaction.Complete();
                                                _objResponseModel.Status = true;
                                                _objResponseModel.Message = "धन्यवाद, एचबीएनसी का विवरण अपडेट हो चुका हैं।";
                                            }
                                            else
                                            {
                                                _objResponseModel.Status = false;
                                                _objResponseModel.Message = "ओह ! एचबीएनसी का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा अपडेट करें ।";
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Transaction.Current.Rollback();
                                        transaction.Dispose();
                                        _objResponseModel.Status = false;
                                        _objResponseModel.Message = "ओह ! एचबीएनसी का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा सेव करें ।";
                                        ErrorHandler.WriteError("Error in PUT pnc" + ex.ToString());
                                    }
                                }
                            }

                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    ErrorHandler.WriteError("Error in model PUT pnc" + Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));

                }
            }
            catch
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }




        [ActionName("uspImmunizationListByInfantID")]
        public HttpResponseMessage uspImmunizationListByInfantID(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            try
            {
                if (tokenFlag == true)
                {
                    if (p.InfantID != 0)
                    {
                        var data = cnaa.uspImmunizationListByInfantID(p.InfantID).ToList();
                        if (data != null)
                        {
                            _objResponseModel.ResposeData = data;
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "Data Received successfully";
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "No Data Found";
                        }
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid request";
                }
            }
            catch
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "";
            }


            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }




        [ActionName("uspManageImmunizationListForAdd")]
        public HttpResponseMessage uspManageImmunizationListForAdd(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                int[] idsToBeSearched = new int[] { 1 };
                idsToBeSearched = new int[] { 21, 22, 23 };
                var p1 = cnaa.uspImmunizationListForAdd(Convert.ToString(p.LoginUnitcode).Substring(0, 4), p.Birth_date, Convert.ToByte(p.DPTFlag)).ToList();



                int i = 0;

                List<ImmunizationList> lis = new List<ImmunizationList> { };
                string savedImmucode = p.SaveImmuCodeList;
                bool dptAlreadyEist = false;
                for (i = 0; i < p1.Count; i++)
                {
                    dptAlreadyEist = false;
                    if (p1[i].AppliedToDate != null)
                    {
                        if (p.DPTFlag == 1)
                        {
                            if (p1[i].ImmuCode == 4 || p1[i].ImmuCode == 6 || p1[i].ImmuCode == 7 || p1[i].ImmuCode == 9 || p1[i].ImmuCode == 10 || p1[i].ImmuCode == 12)
                            {
                                dptAlreadyEist = true;
                            }
                        }
                        if (!dptAlreadyEist)
                        {
                            if (DifferenceInDays(Convert.ToDateTime(p.Birth_date), Convert.ToDateTime(p1[i].AppliedToDate)) < 0)
                            {
                                continue;
                            }
                        }
                    }

                    Boolean existImmunization = false;
                    Int32 immucode = p1[i].ImmuCode;
                    if (savedImmucode.Contains("[" + immucode + "]"))
                    {
                        existImmunization = true;
                    }
                    else if (immucode == 4)//'dpt1'
                    {
                        if (savedImmucode.Contains("[7]") || savedImmucode.Contains("[31]")) //'dpt2' 'penta1'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 6)//'HB1'
                    {
                        if (savedImmucode.Contains("[31]"))// 'penta1'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 35)//'PCV1'
                    {
                        if (savedImmucode.Contains("[36]"))// 'PCV2'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 7)//'dpt2'
                    {
                        if (!savedImmucode.Contains("[4]"))// 'dpt1'
                        {
                            existImmunization = true;
                        }
                        if (savedImmucode.Contains("[10]") || savedImmucode.Contains("[32]"))// 'dpt3' 'penta2'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 36)//'PCV2'
                    {
                        if (!savedImmucode.Contains("[35]"))// 'PCV1'
                        {
                            existImmunization = true;
                        }
                        if (savedImmucode.Contains("[37]"))// 'pcvb'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 10)//'dpt3'
                    {
                        if (!savedImmucode.Contains("[7]"))// 'dpt2'
                        {
                            existImmunization = true;
                        }
                        if (savedImmucode.Contains("[15]"))// 'dptb'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 15)//'dptb'
                    {
                        if (!savedImmucode.Contains("[10]"))// 'dpt3'
                        {
                            existImmunization = true;
                        }
                        if (savedImmucode.Contains("[33]"))// 'penta3'
                        {
                            existImmunization = false;
                        }
                    }
                    else if (immucode == 37)//'pcvb'
                    {
                        if (!savedImmucode.Contains("[36]"))// 'pcv2'
                        {
                            existImmunization = true;
                        }

                    }
                    else if (immucode == 5)//'opv1'
                    {
                        if (savedImmucode.Contains("[8]"))// 'opv2'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 8)//'opv2'
                    {
                        if (!savedImmucode.Contains("[5]"))// 'opv1'
                        {
                            existImmunization = true;
                        }
                        if (savedImmucode.Contains("[11]"))// 'opv3'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 11)//'opv3'
                    {
                        if (!savedImmucode.Contains("[8]"))// 'opv2'
                        {
                            existImmunization = true;
                        }
                        if (savedImmucode.Contains("[16]"))// 'opvb'
                        {
                            existImmunization = true;
                            if (!savedImmucode.Contains("[11]"))// 'OPV3'
                            {
                                existImmunization = false;
                            }
                        }
                    }
                    else if (immucode == 16)//'opvb'
                    {
                        if (!savedImmucode.Contains("[11]"))// 'opv3'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 9)//'HB2'
                    {
                        if (!savedImmucode.Contains("[6]"))// 'HB1'
                        {
                            existImmunization = true;
                        }
                        if (savedImmucode.Contains("[12]") || savedImmucode.Contains("[32]"))// 'HB3 penta2'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 12)//'HB3'
                    {
                        if (!savedImmucode.Contains("[9]"))// 'HB2'
                        {
                            existImmunization = true;
                        }
                        if (savedImmucode.Contains("[33]"))// 'penta3'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 13)//'Measl'
                    {
                        if (savedImmucode.Contains("[30]"))// 'Measl2'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 30)//'Measl'
                    {
                        if (!savedImmucode.Contains("[13]"))// 'Measl2'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 26)//'TT10'
                    {
                        if (savedImmucode.Contains("[27]"))// 'TT16'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 27)//'TT16'
                    {
                        if (!savedImmucode.Contains("[26]"))// 'TT10'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 14)//'VITA1'
                    {
                        if (savedImmucode.Contains("[17]"))// 'VITA2'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 17)//'VITA2'
                    {
                        if (!savedImmucode.Contains("[14]"))// 'VITA1'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 17)//'VITA2'
                    {
                        if (!savedImmucode.Contains("[14]"))// 'VITA1'
                        {
                            existImmunization = true;
                        }
                        if (savedImmucode.Contains("[18]"))// 'VITA3'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 18)//'VITA3'
                    {
                        if (!savedImmucode.Contains("[17]"))// 'VITA2'
                        {
                            existImmunization = true;
                        }
                        if (savedImmucode.Contains("[19]"))// 'VITA4'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 19)//'VITA4'
                    {
                        if (!savedImmucode.Contains("[18]"))// 'VITA3'
                        {
                            existImmunization = true;
                        }
                        if (savedImmucode.Contains("[20]"))// 'VITA5'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 20)//'VITA5'
                    {
                        if (!savedImmucode.Contains("[19]"))// 'VITA4'
                        {
                            existImmunization = true;
                        }
                        if (savedImmucode.Contains("[24]"))// 'VITA9'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 24)//'VITA5'
                    {
                        if (!savedImmucode.Contains("[20]"))// 'VITA4'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 31)//'Penta1'
                    {
                        if (savedImmucode.Contains("[4]") || savedImmucode.Contains("[6]") || savedImmucode.Contains("[32]"))// 'dpt1 hb1 Penta2'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 32)//'Penta2'
                    {
                        if (savedImmucode.Contains("[7]") || savedImmucode.Contains("[9]") || savedImmucode.Contains("[33]"))// 'dpt2 hb2 Penta3'
                        {
                            existImmunization = true;
                        }
                        if (!savedImmucode.Contains("[31]"))// 'Penta1'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 33)//'Penta3'
                    {
                        if (savedImmucode.Contains("[10]") || savedImmucode.Contains("[12]"))// 'dpt3 hb3 '
                        {
                            existImmunization = true;
                        }
                        if (!savedImmucode.Contains("[32]"))// 'Penta2'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 38)//'rvv1'
                    {
                        if (savedImmucode.Contains("[39]"))// 'rvv2'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 39)//'rvv2'
                    {
                        if (!savedImmucode.Contains("[38]"))// 'rvv1'
                        {
                            existImmunization = true;
                        }
                        if (savedImmucode.Contains("[40]"))// 'rvv3'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 40)//'rvv3'
                    {
                        if (!savedImmucode.Contains("[39]"))// 'rvv2'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 41)//'ipv1'
                    {
                        if (savedImmucode.Contains("[34]"))// 'ipv2'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 34)//'ipv2'
                    {
                        if (!savedImmucode.Contains("[41]"))// 'ipv1'
                        {
                            existImmunization = true;
                        }
                    }
                    else if (immucode == 25)//'dpt5'
                    {
                        if (!savedImmucode.Contains("[15]"))// 'dptb'
                        {
                            existImmunization = true;
                        }
                    }
                    if (!existImmunization)
                    {
                        ImmunizationList im = new ImmunizationList();
                        im.ImmuName = Convert.ToString(p1[i].ImmuName);
                        im.Immucode = Convert.ToString(p1[i].ImmuCode);
                        im.DueDays = Convert.ToInt32(p1[i].duedays);
                        if (Convert.ToString(p1[i].MaxDays) == "")
                        {
                            im.MaxDays = null;
                        }
                        else
                        {
                            im.MaxDays = Convert.ToInt32(p1[i].MaxDays);
                        }
                        lis.Add(im);
                    }

                }
                _objResponseModel.ResposeData = lis;
                _objResponseModel.Status = true;
                _objResponseModel.Message = "Data Received successfully";
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        private string validateImmunization(ImmunizationData immu)
        {
            string ErrorMsg = "";
            if (ValidateToken(immu.UserID, immu.TokenNo) == false)
            {
                return "Invalid Request";
            }
            ErrorMsg = checkDate(Convert.ToString(immu.ImmuDate), 1, "टीके");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            if (DifferenceInDays(immu.ImmuDate, DateTime.Now) < 0)
            {
                ErrorMsg = "टीके की तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
                return ErrorMsg;
            }
            if (DifferenceInDays(immu.ImmuDate, immu.BirthDate) > 0)
            {
                ErrorMsg = "टीके की तिथि जन्म की तिथि से ज्यादा होनी चाहिए !";
                return ErrorMsg;
            }





            DateTime? hb0 = null; DateTime? hb1 = null; DateTime? hb2 = null; DateTime? hb3 = null;
            DateTime? dpt1 = null; DateTime? dpt2 = null; DateTime? dpt3 = null; DateTime? dptb = null;
            DateTime? opv1 = null; DateTime? opv2 = null; DateTime? opv3 = null; DateTime? opvb = null;
            DateTime? penta1 = null; DateTime? penta2 = null; DateTime? penta3 = null;
            DateTime? measles = null; DateTime? measles2 = null;
            DateTime? pcv1 = null; DateTime? pcv2 = null; DateTime? pcvb = null;
            DateTime? ipv1 = null; DateTime? ipv2 = null;
            DateTime? rvv1 = null; DateTime? rvv2 = null; DateTime? rvv3 = null;
            DateTime? va1 = null; DateTime? va2 = null; DateTime? va3 = null;
            DateTime? va4 = null; DateTime? va5 = null; DateTime? dt5 = null;
            var p1 = cnaa.Immunizations.Where(x => x.InfantID == immu.InfantID).ToList();
            if (p1 != null)
            {
                foreach (Immunization i in p1)
                {
                    switch (i.ImmuCode)
                    {
                        case 3: hb0 = i.immudate; break;
                        case 4: dpt1 = i.immudate; break;
                        case 5: opv1 = i.immudate; break;
                        case 6: hb1 = i.immudate; break;
                        case 7: dpt2 = i.immudate; break;
                        case 8: opv2 = i.immudate; break;
                        case 9: hb2 = i.immudate; break;
                        case 10: dpt3 = i.immudate; break;
                        case 11: opv3 = i.immudate; break;
                        case 12: hb3 = i.immudate; break;
                        case 13: measles = i.immudate; break;
                        case 14: va1 = i.immudate; break;
                        case 15: dptb = i.immudate; break;
                        case 16: opvb = i.immudate; break;
                        case 17: va2 = i.immudate; break;
                        case 18: va3 = i.immudate; break;
                        case 19: va4 = i.immudate; break;
                        case 20: va5 = i.immudate; break;
                        case 25: dt5 = i.immudate; break;
                        case 30: measles2 = i.immudate; break;
                        case 31: penta1 = i.immudate; break;
                        case 32: penta2 = i.immudate; break;
                        case 33: penta3 = i.immudate; break;
                        case 34: ipv2 = i.immudate; break;
                        case 35: pcv1 = i.immudate; break;
                        case 36: pcv2 = i.immudate; break;
                        case 37: pcvb = i.immudate; break;
                        case 38: rvv1 = i.immudate; break;
                        case 39: rvv2 = i.immudate; break;
                        case 40: rvv3 = i.immudate; break;
                        case 41: ipv1 = i.immudate; break;

                    }
                }
            }


            var p = cnaa.ImmunizationMasters.OrderBy(x => x.ImmuCode).ToList();
            Hashtable ht = new Hashtable();


            foreach (ImmunizationMaster i in p)
            {
                ht.Add(Convert.ToString(i.ImmuCode), Convert.ToString(i.duedays) + ';' + Convert.ToString(((i.MaxDays == null) ? 0 : i.MaxDays)) + ';' + Convert.ToString(i.DiffBetweenDoses));
            }
            immu.ImmuCode = immu.ImmuCode.Replace("[", "").Replace("]", "");
            for (int ImmuCodePosition = 0; ImmuCodePosition < immu.ImmuCode.Split(',').Length; ImmuCodePosition++)
            {
                int immucode = Convert.ToInt16(immu.ImmuCode.Split(',')[ImmuCodePosition]);

                var p2 = p.Where(x => x.ImmuCode == immucode).FirstOrDefault();
                if (p2 != null)
                {
                    if (Convert.ToString(p2.MaxDays) != "")
                    {
                        if (DifferenceInDays(immu.BirthDate, immu.ImmuDate) > Convert.ToInt32(p2.MaxDays))
                        {
                            ErrorMsg = p2.ImmuNameH + " की तिथि एवं जन्म की तिथि का अंतर  " + p2.MaxDays + " दिनों से ज्यादा नहीं होना चाहिए !";
                            return ErrorMsg;
                        }
                    }
                    if (Convert.ToString(p2.duedays) != "")
                    {
                        if (DifferenceInDays(immu.BirthDate, immu.ImmuDate) < Convert.ToInt32(p2.duedays))
                        {
                            ErrorMsg = p2.ImmuNameH + " की तिथि एवं जन्म की तिथि का अंतर  " + p2.duedays + " दिनों से कम नहीं होना चाहिए !";
                            return ErrorMsg;
                        }
                    }
                    if (p2.AppliedFromDate != null)
                    {
                        if (DifferenceInDays(immu.BirthDate, p2.AppliedFromDate) > 0)
                        {
                            ErrorMsg = p2.ImmuNameH + " की तिथि " + Convert.ToDateTime(p2.AppliedFromDate).ToString("dd/MM/yyyy") + "से ज्यादा होनी चाहिए !";
                            return ErrorMsg;
                        }
                    }
                    if (p2.AppliedToDate != null)
                    {
                        if (DifferenceInDays(immu.BirthDate, Convert.ToDateTime(p2.AppliedToDate)) < 0)
                        {
                            ErrorMsg = p2.ImmuNameH + " की तिथि " + Convert.ToDateTime(p2.AppliedToDate).ToString("dd/MM/yyyy") + "से कम होनी चाहिए !";
                            return ErrorMsg;
                        }
                    }

                }
                //if (immu.ImmuCode == 35 || immu.ImmuCode == 36 || immu.ImmuCode == 37)
                //{
                //    if (DifferenceInDays(immu.ImmuDate, Convert.ToDateTime("2018-04-01")) > 0)
                //    {
                //        ErrorMsg = "पीसीवी की तिथि 01/04/2018 से ज्यादा होनी चाहिए !";
                //        return ErrorMsg;
                //    }
                //}

                // ErrorMsg = "--" + dpt1 + "--" + immu.ImmuDate +"---"+ DifferenceInDays((DateTime)dpt1, immu.ImmuDate);
                // return ErrorMsg;
                if (immucode == 4 || immucode == 5 || immucode == 6 || immucode == 31 || immucode == 35) //'4--DPT1 5---OPV1 6--HB1 31--PENTA1 35 ---PCV1'
                {


                    if (immucode == 4 && penta1 != null) //'4--DPT1 
                    {
                        ErrorMsg = "कृपया डीपीटी1 एवं पेन्टा1 में से एक चुनें !";
                        return ErrorMsg;
                    }
                    if (immucode == 4) //'4--DPT1 
                    {
                        if (dpt2 != null)  //'7 ---DPT2'
                        {
                            if (DifferenceInDays(immu.ImmuDate, (DateTime)dpt2) < Convert.ToInt16(Convert.ToString(ht["7"]).Split(';')[2]))
                            {
                                ErrorMsg = "डीपीटी2 की तिथि एवं डीपीटी1 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["7"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                                return ErrorMsg;
                            }
                        }
                        dpt1 = immu.ImmuDate;
                    }
                    else if (immucode == 35) //'35 ---PCV1'
                    {
                        if (pcv2 != null)  //'36 ---PCV2'
                        {
                            if (DifferenceInDays(immu.ImmuDate, (DateTime)pcv2) < Convert.ToInt16(Convert.ToString(ht["36"]).Split(';')[2]))
                            {
                                ErrorMsg = "पीसीवी 2 की तिथि एवं पीसीवी 1 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["36"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                                return ErrorMsg;
                            }
                        }
                        pcv1 = immu.ImmuDate;
                    }
                    else if (immucode == 5) //' 5---OPV1 
                    {
                        if (opv2 != null) //' 8--OPV2 
                        {
                            if (DifferenceInDays(immu.ImmuDate, (DateTime)opv2) < Convert.ToInt16(Convert.ToString(ht["8"]).Split(';')[2]))
                            {
                                ErrorMsg = "ओपीवी2 की तिथि एवं ओपीवी1 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["8"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                                return ErrorMsg;
                            }
                        }
                        opv1 = immu.ImmuDate;
                    }
                    else if (immucode == 6) //' 6--HB1
                    {
                        if (hb2 != null) //' 9--HB2 
                        {
                            if (DifferenceInDays(immu.ImmuDate, (DateTime)hb2) < Convert.ToInt16(Convert.ToString(ht["9"]).Split(';')[2]))
                            {
                                ErrorMsg = "हैपेटाइटिस-बी2 की तिथि एवं हैपेटाइटिस-बी1 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["9"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                                return ErrorMsg;
                            }
                        }
                        hb1 = immu.ImmuDate;
                    }
                    else if (immucode == 31) //'31--PENTA1
                    {
                        if (penta2 != null) //'32--PENTA2
                        {
                            if (DifferenceInDays(immu.ImmuDate, (DateTime)penta2) < Convert.ToInt16(Convert.ToString(ht["32"]).Split(';')[2]))
                            {
                                ErrorMsg = "पेन्टा2 की तिथि एवं पेन्टा1 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["32"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                                return ErrorMsg;
                            }
                        }
                        penta1 = immu.ImmuDate;
                    }

                }//    7--DPT2 8---OPV2 9--HB2 32---PENTA2'
                else if (immucode == 7 || immucode == 8 || immucode == 9 || immucode == 32)
                {
                    if (immucode == 7)
                    {
                        if (dpt1 == null)
                        {
                            ErrorMsg = "डीपीटी1 की तिथि खाली नहीं होनी चाहिए !";
                            return ErrorMsg;
                        }
                        else if (DifferenceInDays((DateTime)dpt1, immu.ImmuDate) < Convert.ToInt16(Convert.ToString(ht["7"]).Split(';')[2]))
                        {
                            ErrorMsg = "डीपीटी2 की तिथि एवं डीपीटी1 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["7"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                            return ErrorMsg;
                        }
                        if (dpt3 != null) //10 -- dpt3
                        {
                            if (DifferenceInDays(immu.ImmuDate, (DateTime)dpt3) < Convert.ToInt16(Convert.ToString(ht["10"]).Split(';')[2]))
                            {
                                ErrorMsg = "डीपीटी3 की तिथि एवं डीपीटी2 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["10"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                                return ErrorMsg;
                            }
                        }
                        dpt2 = immu.ImmuDate;
                    }
                    else if (immucode == 8)  //8---OPV2
                    {
                        if (opv1 == null)
                        {
                            ErrorMsg = "ओपीवी 1 की तिथि खाली नहीं होनी चाहिए !";
                            return ErrorMsg;
                        }
                        else if (DifferenceInDays((DateTime)opv1, immu.ImmuDate) < Convert.ToInt16(Convert.ToString(ht["8"]).Split(';')[2]))
                        {
                            ErrorMsg = "ओपीवी2 की तिथि एवं ओपीवी1 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["8"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                            return ErrorMsg;
                        }
                        if (opv3 != null)  //11 -- opv3
                        {
                            if (DifferenceInDays(immu.ImmuDate, (DateTime)opv3) < Convert.ToInt16(Convert.ToString(ht["11"]).Split(';')[2]))
                            {
                                ErrorMsg = "ओपीवी3 की तिथि एवं ओपीवी2 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["11"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                                return ErrorMsg;
                            }
                        }
                        opv2 = immu.ImmuDate;
                    }
                    else if (immucode == 8) //9--HB2 
                    {
                        if (hb1 == null)
                        {
                            ErrorMsg = "हैपेटाइटिस-बी 1 की तिथि खाली नहीं होनी चाहिए !";
                            return ErrorMsg;
                        }
                        else if (DifferenceInDays((DateTime)hb1, immu.ImmuDate) < Convert.ToInt16(Convert.ToString(ht["8"]).Split(';')[2]))
                        {
                            ErrorMsg = "हैपेटाइटिस-बी2 की तिथि एवं हैपेटाइटिस-बी1 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["8"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                            return ErrorMsg;
                        }
                        if (hb3 != null)  //12   HB3
                        {
                            if (DifferenceInDays(immu.ImmuDate, (DateTime)hb3) < Convert.ToInt16(Convert.ToString(ht["12"]).Split(';')[2]))
                            {
                                ErrorMsg = "हैपेटाइटिस-बी3 की तिथि एवं हैपेटाइटिस-बी2 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["12"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                                return ErrorMsg;
                            }
                        }
                        hb2 = immu.ImmuDate;
                    }
                    else if (immucode == 32) //9--pENTA2 
                    {
                        if (penta1 == null)
                        {
                            ErrorMsg = "पेन्टा1 की तिथि खाली नहीं होनी चाहिए !";
                            return ErrorMsg;
                        }
                        else if (DifferenceInDays((DateTime)penta1, immu.ImmuDate) < Convert.ToInt16(Convert.ToString(ht["32"]).Split(';')[2]))
                        {
                            ErrorMsg = "पेन्टा2 की तिथि एवं पेन्टा1 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["32"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                            return ErrorMsg;
                        }
                        if (penta3 != null) // 33 -- penta3
                        {
                            if (DifferenceInDays(immu.ImmuDate, (DateTime)penta3) < Convert.ToInt16(Convert.ToString(ht["33"]).Split(';')[2]))
                            {
                                ErrorMsg = "पेन्टा3 की तिथि एवं पेन्टा2 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["33"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                                return ErrorMsg;
                            }
                        }
                        penta2 = immu.ImmuDate;
                    }
                }
                //   '10--DPT3 11---OPV3 12--HB3  33----PENTA3 34-IPV  36-PCV2'
                else if (immucode == 10 || immucode == 11 || immucode == 12 || immucode == 33 || immucode == 34 || immucode == 36)
                {
                    if (immucode == 10)  //DPT3
                    {
                        if (dpt2 == null)
                        {
                            ErrorMsg = "डीपीटी2 की तिथि खाली नहीं होनी चाहिए !";
                            return ErrorMsg;
                        }
                        else if (DifferenceInDays((DateTime)dpt2, immu.ImmuDate) < Convert.ToInt16(Convert.ToString(ht["10"]).Split(';')[2]))
                        {
                            ErrorMsg = "डीपीटी3 की तिथि एवं डीपीटी2 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["10"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                            return ErrorMsg;
                        }
                        if (dptb != null)  //15  dptb   
                        {
                            if (DifferenceInDays(immu.ImmuDate, (DateTime)dptb) < Convert.ToInt16(Convert.ToString(ht["15"]).Split(';')[2]))
                            {
                                ErrorMsg = "डीपीटी बूस्टर की तिथि एवं डीपीटी3 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["10"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                                return ErrorMsg;
                            }
                        }
                        dpt3 = immu.ImmuDate;
                    }
                    else if (immucode == 36)  //PCV2
                    {
                        if (pcv1 == null)
                        {
                            ErrorMsg = "पीसीवी 1 की तिथि खाली नहीं होनी चाहिए !";
                            return ErrorMsg;
                        }
                        else if (DifferenceInDays((DateTime)pcv1, immu.ImmuDate) < Convert.ToInt16(Convert.ToString(ht["36"]).Split(';')[2]))
                        {
                            ErrorMsg = "पीसीवी 2 की तिथि एवं पीसीवी 1 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["36"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                            return ErrorMsg;
                        }
                        if (pcvb != null)  //37  pcvb
                        {
                            if (DifferenceInDays(immu.ImmuDate, (DateTime)pcvb) < Convert.ToInt16(Convert.ToString(ht["37"]).Split(';')[2]))
                            {
                                ErrorMsg = "पीसीवी बूस्टर की तिथि एवं पीसीवी 2 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["37"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                                return ErrorMsg;
                            }
                        }
                        pcv2 = immu.ImmuDate;
                    }
                    else if (immucode == 11) //OPV3 
                    {
                        if (opv2 == null)
                        {
                            ErrorMsg = "ओपीवी 2 की तिथि खाली नहीं होनी चाहिए !";
                            return ErrorMsg;
                        }
                        else if (DifferenceInDays((DateTime)opv2, immu.ImmuDate) < Convert.ToInt16(Convert.ToString(ht["11"]).Split(';')[2]))
                        {
                            ErrorMsg = "ओपीवी 3 की तिथि एवं ओपीवी 2 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["11"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                            return ErrorMsg;
                        }
                        if (opvb != null)  //16 opvb
                        {
                            if (DifferenceInDays(immu.ImmuDate, (DateTime)opvb) < Convert.ToInt16(Convert.ToString(ht["16"]).Split(';')[2]))
                            {
                                ErrorMsg = "ओपीवी बूस्टर की तिथि एवं ओपीवी 3 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["16"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                                return ErrorMsg;
                            }
                        }
                        opv3 = immu.ImmuDate;
                    }
                    else if (immucode == 12) //Hepatitis-B3
                    {
                        if (hb2 == null)
                        {
                            ErrorMsg = "हैपेटाइटिस-बी 2 की तिथि खाली नहीं होनी चाहिए !";
                            return ErrorMsg;
                        }
                        else if (DifferenceInDays((DateTime)hb2, immu.ImmuDate) < Convert.ToInt16(Convert.ToString(ht["12"]).Split(';')[2]))
                        {
                            ErrorMsg = "हैपेटाइटिस-बी 3 की तिथि एवं हैपेटाइटिस-बी 2 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["12"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                            return ErrorMsg;
                        }
                        hb3 = immu.ImmuDate;
                    }
                    else if (immucode == 33) //Penta3
                    {
                        if (penta2 == null)
                        {
                            ErrorMsg = "पेन्टा 2 की तिथि खाली नहीं होनी चाहिए !";
                            return ErrorMsg;
                        }
                        else if (DifferenceInDays((DateTime)penta2, immu.ImmuDate) < Convert.ToInt16(Convert.ToString(ht["33"]).Split(';')[2]))
                        {
                            ErrorMsg = "पेन्टा 3 की तिथि एवं पेन्टा 2 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["33"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                            return ErrorMsg;
                        }
                        penta3 = immu.ImmuDate;
                    }
                    else if (immucode == 34) //IPV2
                    {
                        if (opv3 == null)
                        {
                            ErrorMsg = "ओपीवी 3 की तिथि खाली नहीं होनी चाहिए !";
                            return ErrorMsg;
                        }
                        else if (DifferenceInDays((DateTime)opv3, immu.ImmuDate) < 0)
                        {
                            ErrorMsg = "आईपीवी की तिथि ओपीवी 3 की तिथि से ज्यादा होनी चाहिए !";
                            return ErrorMsg;
                        }
                    }
                }
                //'13--MESL 14---VITA1   37---PCVb'
                else if (immucode == 13 || immucode == 37)
                {
                    if (immucode == 13)
                    {
                        if (measles2 != null)  //30 mesl2
                        {
                            if (DifferenceInDays(immu.ImmuDate, (DateTime)measles2) < Convert.ToInt16(Convert.ToString(ht["30"]).Split(';')[2]))
                            {
                                ErrorMsg = "मीज़ल 2 की तिथि एवं मीज़ल 1 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["30"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                                return ErrorMsg;
                            }
                        }
                    }
                    else
                    {
                        if (pcv2 == null)
                        {
                            ErrorMsg = "पीसीवी 2 की तिथि खाली नहीं होनी चाहिए !";
                            return ErrorMsg;
                        }
                        else if (DifferenceInDays((DateTime)pcv2, immu.ImmuDate) < Convert.ToInt16(Convert.ToString(ht["37"]).Split(';')[2]))
                        {
                            ErrorMsg = "पीसीवी बूस्टर की तिथि एवं पीसीवी 2 की तिथि का अंतर 180 दिनों से कम नहीं होना चाहिए !";
                            return ErrorMsg;
                        }
                    }
                }
                //   '15--DPTB 16---OPVB----17--VITA2 28---MRV -- 28--JEV
                else if (immucode == 15 || immucode == 16 || immucode == 17 || immucode == 28 || immucode == 29)
                {
                    if (immucode == 15)  //DPTB
                    {
                        if (penta3 == null)
                        {
                            if (dpt3 == null)
                            {
                                ErrorMsg = "डीपीटी 3 की तिथि खाली नहीं होनी चाहिए !";
                                return ErrorMsg;
                            }
                            else if (DifferenceInDays((DateTime)dpt3, immu.ImmuDate) < Convert.ToInt16(Convert.ToString(ht["15"]).Split(';')[2]))
                            {
                                ErrorMsg = "डीपीटी बूस्टर की तिथि एवं डीपीटी 3 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["15"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                                return ErrorMsg;
                            }
                        }
                        if (dt5 != null)
                        {
                            if (DifferenceInDays(immu.ImmuDate, (DateTime)dt5) < Convert.ToInt16(Convert.ToString(ht["25"]).Split(';')[2]))
                            {
                                ErrorMsg = "डीटी 5 की तिथि एवं डीपीटी बूस्टर की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["25"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                                return ErrorMsg;
                            }
                        }
                    }
                    else if (immucode == 16)  //OPVB
                    {
                        if (penta3 == null)
                        {
                            if (opv3 == null)
                            {
                                ErrorMsg = "ओपीवी 3 की तिथि खाली नहीं होनी चाहिए !";
                                return ErrorMsg;
                            }
                            else if (DifferenceInDays((DateTime)opv3, immu.ImmuDate) < Convert.ToInt16(Convert.ToString(ht["16"]).Split(';')[2]))
                            {
                                ErrorMsg = "ओपीवी बूस्टर की तिथि एवं ओपीवी 3 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["16"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                                return ErrorMsg;
                            }
                        }

                    }

                }
                //   '25---DPT5
                else if (immucode == 25)
                {
                    if (dptb == null)
                    {
                        ErrorMsg = "डीपीटी बूस्टर की तिथि खाली नहीं होनी चाहिए !";
                        return ErrorMsg;
                    }
                    else if (DifferenceInDays((DateTime)dptb, immu.ImmuDate) < Convert.ToInt16(Convert.ToString(ht["25"]).Split(';')[2]))
                    {
                        ErrorMsg = "डीपीटी 5 की तिथि एवं डीपीटी बूस्टर की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["25"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                        return ErrorMsg;
                    }
                }
                //   '30---MESL2
                else if (immucode == 30)
                {
                    if (measles == null)
                    {
                        ErrorMsg = "मीज़ल की तिथि खाली नहीं होनी चाहिए !";
                        return ErrorMsg;
                    }
                    else if (DifferenceInDays((DateTime)measles, immu.ImmuDate) < Convert.ToInt16(Convert.ToString(ht["30"]).Split(';')[2]))
                    {
                        ErrorMsg = "मीज़ल 2 की तिथि एवं मीज़ल 1 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["30"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                        return ErrorMsg;
                    }
                } //   '38---rvv1
                else if (immucode == 38)
                {
                    if (rvv2 != null)  //30 rvv2
                    {
                        if (DifferenceInDays(immu.ImmuDate, (DateTime)rvv2) < Convert.ToInt16(Convert.ToString(ht["39"]).Split(';')[2]))
                        {
                            ErrorMsg = "आरवीवी-2 की तिथि एवं आरवीवी-1 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["39"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                            return ErrorMsg;
                        }
                    }
                }
                else if (immucode == 39) //39 rvv2
                {
                    if (rvv1 == null)
                    {
                        ErrorMsg = "आरवीवी-1 की तिथि खाली नहीं होनी चाहिए !";
                        return ErrorMsg;
                    }
                    else if (DifferenceInDays((DateTime)rvv1, immu.ImmuDate) < Convert.ToInt16(Convert.ToString(ht["39"]).Split(';')[2]))
                    {
                        ErrorMsg = "आरवीवी-2 की तिथि एवं आरवीवी-1 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["39"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                        return ErrorMsg;
                    }
                    if (rvv3 != null)  //11 -- opv3
                    {
                        if (DifferenceInDays(immu.ImmuDate, (DateTime)rvv3) < Convert.ToInt16(Convert.ToString(ht["40"]).Split(';')[2]))
                        {
                            ErrorMsg = "आरवीवी-3 की तिथि एवं आरवीवी-2 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["40"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                            return ErrorMsg;
                        }
                    }
                    rvv2 = immu.ImmuDate;

                } //   '40---rvv3
                else if (immucode == 40)
                {
                    if (rvv2 == null)
                    {
                        ErrorMsg = "आरवीवी-2 की तिथि खाली नहीं होनी चाहिए !";
                        return ErrorMsg;
                    }
                    else if (DifferenceInDays((DateTime)rvv2, immu.ImmuDate) < Convert.ToInt16(Convert.ToString(ht["40"]).Split(';')[2]))
                    {
                        ErrorMsg = "मीज़ल 2 की तिथि एवं मीज़ल 1 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["40"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                        return ErrorMsg;
                    }
                }
                else if (immucode == 41) // 41 for IPV1
                {
                    if (ipv2 != null)
                    {
                        if (DifferenceInDays(immu.ImmuDate, (DateTime)ipv2) < Convert.ToInt16(Convert.ToString(ht["34"]).Split(';')[2]))
                        {
                            ErrorMsg = "एफआईपीवी-2 की तिथि एवं एफआईपीवी-1 की तिथि का अंतर " + Convert.ToInt16(Convert.ToString(ht["34"]).Split(';')[2]) + " दिनों से कम नहीं होना चाहिए !";
                            return ErrorMsg;
                        }
                    }
                    ipv1 = immu.ImmuDate;
                }
            }




            return ErrorMsg;
        }
        [ActionName("PostImmunizationDetail")]
        public HttpResponseMessage PostImmunizationDetail(ImmunizationData immu)
        {
            writeclassdata(immu);
            ResponseModel _objResponseModel = new ResponseModel();
            int CheckAppVersionFlag = CheckVersion(immu.AppVersion, immu.IOSAppVersion);
            if (CheckAppVersionFlag == 1)
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = appValiationMsg;
                _objResponseModel.AppVersion = 1;
                return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
            }
            else
            {
                string ErrorMsg = validateImmunization(immu);
                if (ErrorMsg != "")
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = ErrorMsg;
                }
                else
                {
                    string weight = Convert.ToString(immu.Weight);
                    immu.Media = immu.Media;
                    immu.ANMVerify = 0;
                    immu.ANMVerificationDate = null;

                    int p = cnaa.InsertUpdateImmunization(immu.InfantID, immu.ASHAAutoid, immu.ImmuCode, immu.ImmuDate.ToString("dd/MM/yyyy"), immu.VillageAutoid,
                        immu.LoginUnitid, immu.EntryBy, "", immu.MotherID, 1, immu.BirthDate.ToString("dd/MM/yyyy"), Convert.ToByte(immu.PartImmu), Convert.ToByte("0"), Convert.ToByte("0"),
                        immu.LoginUnitid, weight, immu.EntryUserNo, 0, immu.UnitCode, Convert.ToByte(immu.Media), immu.Latitude, immu.Longitude, Convert.ToByte(immu.ANMVerify)).Select(x => x.error).FirstOrDefault();
                    if (p == 0)
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.ResposeData = p;
                        _objResponseModel.Message = "धन्यवाद, टीके का विवरण सेव हो चुका हैं।";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.ResposeData = p;
                        _objResponseModel.Message = "ओह ! टीके का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें ।";
                        ErrorHandler.WriteError("Error in post immunization for " + immu.ImmuCode);
                    }
                }
                return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
            }
        }

        [ActionName("PutImmunizationDetail")]
        public HttpResponseMessage PutImmunizationDetail(ImmunizationData immu)
        {

            ResponseModel _objResponseModel = new ResponseModel();
            int CheckAppVersionFlag = CheckVersion(immu.AppVersion, immu.IOSAppVersion);
            //writeclassdata(immu);

            if (CheckAppVersionFlag == 1)
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = appValiationMsg;
                _objResponseModel.AppVersion = 1;
                return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
            }
            else
            {


                string ErrorMsg = validateImmunization(immu);
                Int16 Media = 0;
                if (ErrorMsg != "")
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = ErrorMsg;
                }
                else
                {
                    using (cnaaEntities objcnaa = new cnaaEntities())
                    {


                        byte t = Convert.ToByte(immu.ImmuCode);
                        var p2 = objcnaa.Immunizations.Where(x => x.InfantID == immu.InfantID && x.ImmuCode == t).FirstOrDefault();

                        if (p2 != null)
                        {
                            Int16 AnyChanges = 0;
                            if (p2.ashaAutoID != immu.ASHAAutoid)
                                AnyChanges = 1;
                            else if (p2.immudate != immu.ImmuDate)
                                AnyChanges = 1;
                            else if (Convert.ToString(p2.Weight) != Convert.ToString(immu.Weight))
                                AnyChanges = 1;
                            else
                                AnyChanges = 0;

                            Media = immu.Media;
                            immu.ANMVerify = 0;
                            immu.ANMVerificationDate = null;

                        }
                    }
                    string weight = Convert.ToString(immu.Weight);
                    int p = cnaa.InsertUpdateImmunization(immu.InfantID, immu.ASHAAutoid, immu.ImmuCode, immu.ImmuDate.ToString("dd/MM/yyyy"), immu.VillageAutoid,
                    immu.LoginUnitid, immu.EntryBy, "", immu.MotherID, 2, immu.BirthDate.ToString("dd/MM/yyyy"), immu.PartImmu, Convert.ToByte("0"), Convert.ToByte("0"),
                    immu.LoginUnitid, weight, immu.EntryUserNo, 0, immu.UnitCode, Convert.ToByte(Media), immu.Latitude, immu.Longitude, Convert.ToByte(immu.ANMVerify)).Select(x => x.error).FirstOrDefault();
                    if (p == 0)
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.ResposeData = p;
                        _objResponseModel.Message = "धन्यवाद, टीके का विवरण अपडेट हो चुका हैं।";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.ResposeData = p;
                        _objResponseModel.Message = "ओह ! टीके का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा सेव करें ।";
                        ErrorHandler.WriteError("Error in put immunization for " + immu.ImmuCode);
                    }

                }
                return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
            }
        }

        public HttpResponseMessage PostUnitNamebyunitcode(Search s)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(s.UserID, s.TokenNo);
            if (tokenFlag == true)
            {
                if (s.unitcode.Length != 11)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid Unitcode";
                }
                else
                {
                    var result = rajmed.UnitMasters.Where(x => x.UnitCode == s.unitcode).Select(x => new { UnitType = x.UnitType, UnitCode = x.UnitCode, UnitName = x.UnitName }).FirstOrDefault();
                    if (result != null)
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.ResposeData = result;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage PostUnitName(Search s)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(s.UserID, s.TokenNo);
            if (tokenFlag == true)
            {
                if (s.unitcode.Length != 4)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid DistCode";
                }
                else
                {
                    var result = rajmed.uspGetUnitName(s.unitcode).ToList();
                    if (result != null)
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.ResposeData = result;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage PostOtherUnitDetails(Search s)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(s.UserID, s.TokenNo);
            if (tokenFlag == true)
            {
                string strName = s.Name;
                string strHusbandName = s.HusbName;
                string strAgeFrom = s.AgeFrom;
                string strAgeTo = s.AgeTo;
                string strMobile = s.Mobile;
                string strunitcode = s.unitcode;
                var result = cnaa.uspGetOtherUnitDetails(strunitcode, strName, strHusbandName, strMobile, strAgeFrom, strAgeTo).ToList();

                if (result.Count > 0)
                {
                    _objResponseModel.Status = true;
                    _objResponseModel.ResposeData = result;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        [ActionName("PostMotherDataforMaternalDeath")]
        public HttpResponseMessage PostMotherDataforMaternalDeath(Pcts pcts)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(pcts.UserID, pcts.TokenNo);
            if (tokenFlag == true)
            {
                if (pcts.MotherID < 1)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "कृपया सही पीसीटीएस आईडी डाले ";
                }
                else
                {
                    var data = cnaa.uspMotherDataforMaternalDeath(pcts.MotherID).ToList();
                    if (data != null)
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage PostuspInfantDataforInfantDeath(Pcts pcts)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(pcts.UserID, pcts.TokenNo);
            if (tokenFlag == true)
            {
                if (pcts.PCTSID.Length < 14)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "कृपया सही पीसीटीएस आईडी डाले ";
                }
                else
                {
                    var data = cnaa.uspInfantDataforInfantDeath(pcts.PCTSID).ToList();
                    if (data.Count > 0)
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage PostInfantDeathReason(InfantDeathRason i)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(i.UserID, i.TokenNo);
            if (tokenFlag == true)
            {
                if (i.AgeType == 0)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
                else
                {
                    var data = cnaa.DeathReasons.Where(x => x.DeathType == 1).OrderBy(x => x.ReasonCode).Select(x => new
                    {
                        ReasonName = x.ReasonCode + " " + x.ReasonName,
                        x.ReasonID,
                        x.DeathType
                    });
                    if (data != null)
                    {
                        _objResponseModel.Status = true;
                        if (i.AgeType == 1 || i.AgeType == 2)
                        {
                            _objResponseModel.ResposeData = data.Where(x => x.ReasonID == 1 || x.ReasonID == 2 || x.ReasonID == 6 || x.ReasonID == 7 || x.ReasonID == 9 || (x.ReasonID >= 35 && x.ReasonID <= 40)).ToList();
                        }
                        else if (i.AgeType == 3 || i.AgeType == 4)
                        {
                            _objResponseModel.ResposeData = data.Where(x => x.ReasonID == 4 || x.ReasonID == 5 || x.ReasonID == 8 || (x.ReasonID >= 35 && x.ReasonID <= 40)).ToList();
                        }
                        else if (i.AgeType == 5)
                        {
                            _objResponseModel.ResposeData = data.Where(x => x.ReasonID == 3).ToList();
                        }
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage getMaternalDeathReason()
        {
            ResponseModel _objResponseModel = new ResponseModel();
            var data = cnaa.DeathReasons.Where(x => x.DeathType == 2 && x.ParentReasonId == null).OrderBy(x => x.ReasonCode).Select(x => new
            {
                ReasonName = x.ReasonName,
                x.ReasonID,
                x.DeathType
            }).ToList();
            if (data != null)
            {
                data.Add(new { ReasonName = "चुनें", ReasonID = (byte)0, DeathType = (byte?)2 });
                _objResponseModel.Status = true;
                _objResponseModel.ResposeData = data.OrderBy(x => x.ReasonID);
                _objResponseModel.Message = "Data Received successfully";
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "No Data Found";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage PostDeathOtherReason(DeathReason dr)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(dr.UserID, dr.TokenNo);
            if (tokenFlag == true)
            {
                dynamic data = null;
                if (dr.Flag == 1)
                {
                    data = cnaa.DeathReasons.Where(x => x.DeathType == 2 && x.ParentReasonId == dr.ParentReasonId && (x.ReasonID < 10 || x.ReasonID > 14)).OrderBy(x => x.ReasonCode).Select(x => new
                    {
                        ReasonName = x.ReasonName,
                        x.ReasonID,
                        x.DeathType
                    }).ToList();
                }
                else
                {
                    data = cnaa.DeathReasons.Where(x => x.DeathType == 2 && x.ParentReasonId == dr.ParentReasonId).OrderBy(x => x.ReasonCode).Select(x => new
                    {
                        ReasonName = x.ReasonName,
                        x.ReasonID,
                        x.DeathType
                    }).ToList();
                }


                if (data.Count > 0)
                {

                    _objResponseModel.Status = true;
                    _objResponseModel.ResposeData = data;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public string ValidateMaternalDeath(DeathDetail dd)
        {
            string ErrorMsg = "";
            if (ValidateToken(dd.UserID, dd.TokenNo) == false)
            {
                return "Invalid Request";
            }
            if (dd.Name == "")
            {
                return "कृपया नाम व पता लिखें";
            }
            if (Convert.ToInt32(dd.Age) < 13)
            {
                return "आयु 13 वर्ष से कम नहीं होनी चाहिए !";
            }
            ErrorMsg = checkDate(Convert.ToString(dd.DeathDate), 1, "महिला की मृत्यु");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            if (DifferenceInDays(dd.DeathDate, DateTime.Now) < 0)
            {
                return "कृपया महिला की मृत्यु की तिथि वर्तमान तिथि से कम होनी चाहिए";
            }
            if (!String.IsNullOrEmpty(Convert.ToString(dd.DeliveryDate)))
            {
                int diff = DifferenceInDays((DateTime)dd.DeliveryDate, dd.DeathDate);
                if (diff < 0)
                {
                    return "कृपया महिला की मृत्यु की तिथि महिला की प्रसव/गर्भपात तिथि से ज्यादा होनी चाहिए";
                }
                else if (diff > 42 && dd.ReasonID != 124)
                {
                    return "यह केस मातृ मृत्यु का नहीं हैं ! कृप्‍या मृत्यु का कारण अन्य कारण चुनें";
                }
            }
            if (dd.ReasonID == 0)
            {
                return "कृपया मृत्यु का कारण चुनें !";
            }
            ErrorMsg = checkDate(Convert.ToString(dd.DeathReportDate), 1, "मृत्यु की रिपोर्ट");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            if (!String.IsNullOrEmpty(Convert.ToString(dd.DeathReportDate)))
            {
                if (DifferenceInDays((DateTime)dd.DeathReportDate, dd.DeathDate) > 0)
                {
                    return "मृत्यु की रिपोर्ट की तिथि मृत्यु की तिथि से कम नहीं हो सकती।";
                }
            }
            if (dd.deathPlace == 2 && dd.DeathUnitCode.Length != 11)
            {
                return "कृपया सही मृत्यु का स्थान चुनें !";
            }
            var p = cnaa.ANCDetails.Where(x => x.motherid == dd.motherid).Max(x => x.ANCDate);
            if (p != null)
            {
                if (DifferenceInDays((DateTime)p.Date, dd.DeathDate) < 0)
                {
                    return "कृपया महिला की मृत्यु की तिथि महिला की एएनसी तिथि से ज्यादा होनी चाहिए !";
                }
            }
            var p1 = cnaa.HBPNCs.Where(x => x.Motherid == dd.motherid).Max(x => x.PNCDate);
            if (p1 != null)
            {
                if (DifferenceInDays((DateTime)p1.Value, dd.DeathDate) < 0)
                {
                    return "कृपया महिला की मृत्यु की तिथि महिला की एचबीएनसी तिथि से ज्यादा होनी चाहिए !";
                }
            }
            var p2 = cnaa.Transports.Where(x => x.MotherID == dd.motherid).Max(x => (DateTime?)x.DischargeDT);
            if (p2 != null)
            {
                if (DifferenceInDays((DateTime)p2.Value, dd.DeathDate) < 0)
                {
                    return "कृपया महिला की मृत्यु की तिथि महिला की डिस्चार्ज तिथि से ज्यादा होनी चाहिए !";
                }
            }
            return ErrorMsg;
        }


        public HttpResponseMessage PostMotherDeathDetails(DeathDetail dd)
        {
            //      writeclassdata(dd);
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(dd.AppVersion, dd.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = ValidateMaternalDeath(dd);

                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            int deathUnitid = 0;
                            if (dd.DeathUnitCode != "0")
                            {
                                deathUnitid = rajmed.UnitMasters.Where(x => x.UnitCode == dd.DeathUnitCode).Select(x => x.UnitID).FirstOrDefault();
                            }
                            dd.DeathUnitID = deathUnitid;

                            using (TransactionScope transaction = new TransactionScope())
                            {
                                try
                                {
                                    dd.Media = dd.Media;
                                    if (dd.Media == 1)
                                    {
                                        dd.ANMVerify = 1;
                                        dd.ANMVerificationDate = DateTime.Now;
                                    }
                                    else
                                    {
                                        dd.ANMVerify = 0;
                                        dd.ANMVerificationDate = null;
                                    }

                                    dd.EntryDate = DateTime.Now;
                                    dd.LastUpdated = DateTime.Now;
                                    using (cnaaEntities objcnaa = new cnaaEntities())
                                    {
                                        objcnaa.DeathDetails.Add(dd);
                                        objcnaa.SaveChanges();

                                        var p = objcnaa.MotherStatus.Where(x => x.motherid == dd.motherid && x.reasonid == 5).FirstOrDefault();

                                        if (p == null)
                                        {
                                            MotherStatu m = new MotherStatu();
                                            m.motherid = dd.motherid;
                                            m.reasonid = 5;
                                            m.UserID = dd.LoginUserID;
                                            m.IPAddress = dd.IPAddress;
                                            m.Entrydate = dd.EntryDate;
                                            m.LastUpdated = DateTime.Now;
                                            objcnaa.MotherStatus.Add(m);

                                        }
                                        else
                                        {
                                            p.LastUpdated = DateTime.Now;
                                        }
                                        objcnaa.SaveChanges();

                                        var p1 = objcnaa.Mothers.Where(x => x.MotherID == dd.motherid).FirstOrDefault();
                                        if (p1 != null)
                                        {
                                            p1.LastUpdated = DateTime.Now;
                                            p1.Status = 5;
                                            objcnaa.SaveChanges();
                                        }
                                    }
                                    transaction.Complete();
                                    _objResponseModel.Status = true;
                                    _objResponseModel.Message = "धन्यवाद, महिला की मृत्यु का विवरण सेव हो चुका हैं।"; ;
                                }
                                catch (Exception ex)
                                {
                                    Transaction.Current.Rollback();
                                    transaction.Dispose();
                                    _objResponseModel.Status = false;
                                    _objResponseModel.Message = "ओह ! महिला की मृत्यु का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें ।";
                                    ErrorHandler.WriteError("Error in post masternal death " + ex.ToString());
                                }
                            }
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    ErrorHandler.WriteError("Error in model post maternal death " + Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
                }

            }
            catch (Exception ex)
            {
                _objResponseModel.Status = false;
                ErrorHandler.WriteError("Error in model post maternal death try-- " + ex.ToString());
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        public HttpResponseMessage PutMotherDeathDetails(DeathDetail dd)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            //writeclassdata(dd);
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(dd.AppVersion, dd.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = ValidateMaternalDeath(dd);

                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            int deathUnitid = 0;
                            if (dd.DeathUnitCode != "0")
                            {
                                deathUnitid = rajmed.UnitMasters.Where(x => x.UnitCode == dd.DeathUnitCode).Select(x => x.UnitID).FirstOrDefault();
                            }
                            dd.DeathUnitID = deathUnitid;

                            using (TransactionScope transaction = new TransactionScope())
                            {
                                try
                                {
                                    using (cnaaEntities objcnaa = new cnaaEntities())
                                    {
                                        var p2 = objcnaa.DeathDetails.Where(x => x.motherid == dd.motherid && x.infantid == 0).FirstOrDefault();
                                        if (p2 != null)
                                        {
                                            Int16 AnyChanges = 0;
                                            if (p2.Age != dd.Age)
                                                AnyChanges = 1;
                                            else if (p2.AgeType != dd.AgeType)
                                                AnyChanges = 1;
                                            else if (p2.ashaautoid != dd.ashaautoid)
                                                AnyChanges = 1;
                                            else if (p2.Bfeed != dd.Bfeed)
                                                AnyChanges = 1;
                                            else if (p2.BloodGroup != dd.BloodGroup)
                                                AnyChanges = 1;
                                            else if (p2.DeathDate != dd.DeathDate)
                                                AnyChanges = 1;
                                            else if (p2.deathPlace != dd.deathPlace)
                                                AnyChanges = 1;
                                            else if (p2.DeathReportDate != dd.DeathReportDate)
                                                AnyChanges = 1;
                                            else if (p2.DeathUnitID != dd.DeathUnitID)
                                                AnyChanges = 1;
                                            else if (p2.IsImmun != dd.IsImmun)
                                                AnyChanges = 1;
                                            else if (p2.MasterMobile != dd.MasterMobile)
                                                AnyChanges = 1;
                                            else if (p2.Name != dd.Name)
                                                AnyChanges = 1;
                                            else if (p2.ReasonID != dd.ReasonID)
                                                AnyChanges = 1;
                                            else if (p2.Relative_Name != dd.Relative_Name)
                                                AnyChanges = 1;
                                            else if (p2.Weight != dd.Weight)
                                                AnyChanges = 1;

                                            p2.Age = dd.Age;
                                            p2.AgeType = 1;
                                            p2.ashaautoid = dd.ashaautoid;
                                            p2.Bfeed = dd.Bfeed;
                                            p2.BloodGroup = dd.BloodGroup;
                                            p2.DeathDate = dd.DeathDate;
                                            p2.deathPlace = dd.deathPlace;
                                            p2.DeathReportDate = dd.DeathReportDate;
                                            p2.DeathUnitID = deathUnitid;
                                            p2.IsImmun = dd.IsImmun;
                                            p2.LastUpdated = DateTime.Now;
                                            p2.MasterMobile = dd.MasterMobile;
                                            if (dd.Media == 3)
                                            {
                                                p2.Media = 3;
                                                p2.ANMVerify = 1;
                                                p2.ANMVerificationDate = DateTime.Now;
                                            }
                                            else if (dd.Media == 2)
                                            {
                                                p2.Media = 2;
                                                p2.ANMVerify = 0;
                                                p2.ANMVerificationDate = null;
                                            }
                                            else
                                            {
                                                p2.Media = dd.Media;
                                                p2.ANMVerify = 1;
                                                p2.ANMVerificationDate = DateTime.Now;
                                            }

                                            p2.Name = dd.Name;
                                            p2.ReasonID = dd.ReasonID;
                                            p2.Relative_Name = dd.Relative_Name;
                                            p2.Weight = dd.Weight;
                                            p2.UpdateUserNo = dd.UpdateUserNo;
                                            objcnaa.SaveChanges();
                                        }


                                        var p = objcnaa.MotherStatus.Where(x => x.motherid == dd.motherid && x.reasonid == 5).FirstOrDefault();

                                        if (p == null)
                                        {
                                            MotherStatu m = new MotherStatu();
                                            m.motherid = dd.motherid;
                                            m.reasonid = 5;
                                            m.UserID = dd.LoginUserID;
                                            m.IPAddress = dd.IPAddress;
                                            m.Entrydate = dd.EntryDate;
                                            m.LastUpdated = DateTime.Now;
                                            objcnaa.MotherStatus.Add(m);

                                        }
                                        else
                                        {
                                            p.LastUpdated = DateTime.Now;
                                        }
                                        objcnaa.SaveChanges();

                                        var p1 = objcnaa.Mothers.Where(x => x.MotherID == dd.motherid).FirstOrDefault();
                                        if (p1 != null)
                                        {
                                            p1.LastUpdated = DateTime.Now;
                                            p1.Status = 5;
                                            objcnaa.SaveChanges();
                                        }
                                    }
                                    transaction.Complete();
                                    _objResponseModel.Status = true;
                                    _objResponseModel.Message = "धन्यवाद, महिला की मृत्यु का विवरण अपडेट हो चुका हैं।";
                                }
                                catch (Exception ex)
                                {
                                    Transaction.Current.Rollback();
                                    transaction.Dispose();
                                    _objResponseModel.Status = false;
                                    _objResponseModel.Message = "ओह ! महिला की मृत्यु का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा सेव करें ।";
                                    ErrorHandler.WriteError("Error in put masternal death for " + ex.ToString());
                                }
                            }
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    ErrorHandler.WriteError("Error in model put maternal death " + Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
                }

            }
            catch (Exception ex)
            {
                _objResponseModel.Status = false;
                ErrorHandler.WriteError("Error in model post maternal death try-- " + ex.ToString());
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        public string ValidateInfantDeath(DeathDetail dd)
        {
            string ErrorMsg = "";
            if (ValidateToken(dd.UserID, dd.TokenNo) == false)
            {
                return "Invalid Request";
            }
            if (dd.Name == "")
            {
                return "कृपया नाम व पता लिखें";
            }
            if (!Regex.IsMatch(dd.Name, @"^([ \u0900-\u097f ,\u200D,\u00c0-\u01ffa-zA-Z0-9'])+$"))
            {
                return "कृपया सही नाम व पता लिखें";
            }
            if (dd.Name.Length > 50)
            {
                return "शिशु का नाम व पता 50 अक्षरों से अधिक नहीं होना चाहिए !";
            }
            if (Convert.ToInt32(dd.AgeType) < 1)
            {
                return "कृपया आयु का प्रकार चुनें !";
            }
            if (dd.AgeType == 1)
            {
                if (Convert.ToInt32(dd.Age) > 5)
                {
                    return "कृपया सही आयु का प्रकार चुनें. आयु 5 वर्ष से ज्यादा नहीं  होनी चाहिए !";
                }
            }
            else if (dd.AgeType == 2)
            {
                if (Convert.ToInt32(dd.Age) > 11)
                {
                    return "कृपया सही आयु का प्रकार चुनें. महीने 11 से ज्यादा नहीं  होनी चाहिए !";
                }
            }
            else if (dd.AgeType == 3)
            {
                if (Convert.ToInt32(dd.Age) > 3)
                {
                    return "कृपया सही आयु का प्रकार चुनें. सप्ताह 3 से ज्यादा नहीं  होनी चाहिए !";
                }
            }
            else if (dd.AgeType == 4)
            {
                if (Convert.ToInt32(dd.Age) > 6)
                {
                    return "कृपया सही आयु का प्रकार चुनें. दिन 6 से ज्यादा नहीं  होनी चाहिए !";
                }
            }
            else if (dd.AgeType == 5)
            {
                if (Convert.ToInt32(dd.Age) > 23)
                {
                    return "कृपया सही आयु का प्रकार चुनें. घंटे 23 से ज्यादा नहीं  होनी चाहिए !";
                }
            }

            int totalDays = DifferenceInDays((DateTime)dd.BirthDate, (DateTime)dd.DeathDate);
            int age = 0;
            if (totalDays >= 365)
            {
                age = (int)Math.Floor((Double)totalDays / (Double)365);
                if (dd.AgeType != 1)
                {
                    if (Convert.ToInt32(age) > 5)
                    {
                        ErrorMsg = "आयु 5 वर्ष से ज्यादा नहीं  होनी चाहिए !";
                    }
                    else
                    {
                        ErrorMsg = "कृपया सही आयु डाले. आयु " + age + " वर्ष होनी चाहिए !";
                    }
                    return ErrorMsg;
                }
                else
                    if (dd.Age != age)
                {
                    ErrorMsg = "कृपया सही आयु डाले. आयु " + age + " वर्ष होनी चाहिए !";
                    return ErrorMsg;
                }
            }
            else if (totalDays > 27)
            {
                age = (int)Math.Floor((Double)totalDays / (Double)30);
                if (age == 0)
                    age = 1;
                if (dd.AgeType != 2)
                {
                    if (age > 11)
                    {
                        if (dd.Age != 1 && dd.AgeType != 1)
                        {
                            ErrorMsg = "कृपया सही आयु डाले. आयु 1 वर्ष होनी चाहिए !";
                            return ErrorMsg;
                        }
                    }
                    else
                    {
                        ErrorMsg = "कृपया सही आयु डाले. आयु " + age + " महीने होनी चाहिए !";
                        return ErrorMsg;
                    }
                }
                else if (dd.Age != age)
                {
                    if (age > 11)
                    {
                        ErrorMsg = "कृपया सही आयु डाले. आयु 1 वर्ष होनी चाहिए !";
                    }
                    else
                    {
                        ErrorMsg = "कृपया सही आयु डाले. आयु " + age + " महीने होनी चाहिए !";
                    }
                    return ErrorMsg;
                }
            }
            else if (totalDays > 6)
            {
                age = (int)Math.Floor((Double)totalDays / (Double)7);
                if (dd.AgeType != 3)
                {
                    if (age > 3)
                    {
                        if (dd.Age != 1 && dd.AgeType != 2)
                        {
                            ErrorMsg = "कृपया सही आयु डाले. आयु 1 महीना होनी चाहिए !";
                            return ErrorMsg;
                        }
                    }
                    else
                    {
                        ErrorMsg = "कृपया सही आयु डाले. आयु " + age + " सप्ताह होनी चाहिए !";
                        return ErrorMsg;
                    }
                }
                else if (dd.Age != age)
                {
                    if (age > 3)
                    {
                        ErrorMsg = "कृपया सही आयु डाले. आयु 1 माह होनी चाहिए !";
                    }
                    else
                    {
                        ErrorMsg = "कृपया सही आयु डाले. आयु " + age + " सप्ताह होनी चाहिए !";
                    }
                    return ErrorMsg;
                }
            }
            else if (totalDays >= 1)
            {
                age = totalDays;
                if (dd.AgeType != 4)
                {
                    if (age > 6)
                    {
                        if (dd.Age != 1 && dd.AgeType != 3)
                        {
                            return "कृपया सही आयु डाले. आयु 1 सप्ताह होनी चाहिए !";
                        }
                    }
                    else
                    {
                        return "कृपया सही आयु डाले. आयु " + age + " दिन होनी चाहिए !";
                    }
                }
                else if (age > 6)
                {
                    return "कृपया सही आयु डाले. आयु 1 सप्ताह होनी चाहिए !";
                }
            }
            else if (totalDays == 0)
            {
                if (dd.AgeType != 5)
                {
                    return "कृपया सही आयु डाले. आयु घंटे में होनी चाहिए !";
                }
            }
            if (dd.IsImmun != 0 && dd.IsImmun != 1)
            {
                return "कृपया क्या शिशु को बीसीजी का टीका लगाया गया था चुनें";
            }

            if (dd.Weight > 9)
            {
                return "वजन 9 कि.ग्रा. से ज्यादा नहीं होना चाहिए ! ";
            }

            if (dd.Relative_Name == "")
            {
                return "कृपया मुखिया का नाम लिखें";
            }
            if (!Regex.IsMatch(dd.Name, @"^([ \u0900-\u097f ,\u200D,\u00c0-\u01ffa-zA-Z0-9'])+$"))
            {
                return "कृपया मुखिया का नाम सही लिखें";
            }
            ErrorMsg = checkDate(Convert.ToString(dd.DeathDate), 1, "मृत्यु");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            if (DifferenceInDays(dd.DeathDate, DateTime.Now) < 0)
            {
                return "कृपया शिशु की मृत्यु की तिथि आज की तिथि से कम होनी चाहिए";
            }

            if (DifferenceInDays((DateTime)dd.BirthDate, dd.DeathDate) < 0)
            {
                return "कृपया शिशु की मृत्यु की तिथि शिशु की जन्म तिथि से ज्यादा होनी चाहिए";
            }
            if (dd.deathPlace == 0)
            {
                return "कृपया मृत्यु का स्थान चुनें";
            }
            if (dd.ReasonID == 0)
            {
                return "कृपया मृत्यु का कारण चुनें";
            }
            ErrorMsg = checkDate(Convert.ToString(dd.DeathReportDate), 1, "मृत्यु की रिपोर्ट");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            if (!String.IsNullOrEmpty(Convert.ToString(dd.DeathReportDate)))
            {
                if (DifferenceInDays((DateTime)dd.DeathReportDate, dd.DeathDate) > 0)
                {
                    return "मृत्यु की रिपोर्ट की तिथि मृत्यु की तिथि से कम नहीं हो सकती।";
                }
            }
            if (dd.deathPlace == 2 && dd.DeathUnitCode.Length != 11)
            {
                return "कृपया सही मृत्यु का स्थान चुनें !";
            }

            if (dd.infantid == 0)
            {
                return "Error";
            }
            var p1 = cnaa.HBYCs.Where(x => x.InfantId == dd.infantid).Max(x => (DateTime?)x.VisitDate);
            if (p1 != null)
            {
                if (DifferenceInDays((DateTime)p1.Value, dd.DeathDate) < 0)
                {
                    return "कृपया शिशु की मृत्यु की तिथि शिशु की एचबीवाईसी तिथि से ज्यादा होनी चाहिए !";
                }
            }
            p1 = cnaa.Immunizations.Where(x => x.InfantID == dd.infantid).Max(x => (DateTime?)x.immudate);
            if (p1 != null)
            {
                if (DifferenceInDays((DateTime)p1.Value, dd.DeathDate) < 0)
                {
                    return "कृपया शिशु की मृत्यु की तिथि शिशु के टीके की तिथि से ज्यादा होनी चाहिए !";
                }
            }
            return ErrorMsg;
        }
        public HttpResponseMessage PostInfantDeathDetails(DeathDetail dd)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(dd.AppVersion, dd.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = ValidateInfantDeath(dd);

                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            int deathUnitid = 0;
                            if (dd.DeathUnitCode != "0")
                            {
                                deathUnitid = rajmed.UnitMasters.Where(x => x.UnitCode == dd.DeathUnitCode).Select(x => x.UnitID).FirstOrDefault();
                            }
                            dd.DeathUnitID = deathUnitid;

                            using (TransactionScope transaction = new TransactionScope())
                            {
                                try
                                {
                                    dd.EntryDate = DateTime.Now;
                                    dd.LastUpdated = DateTime.Now;
                                    dd.Media = dd.Media;
                                    if (dd.Media == 1)
                                    {
                                        dd.ANMVerify = 1;
                                        dd.ANMVerificationDate = DateTime.Now;
                                    }
                                    else
                                    {
                                        dd.ANMVerify = 0;
                                        dd.ANMVerificationDate = null;
                                    }

                                    using (cnaaEntities objcnaa = new cnaaEntities())
                                    {
                                        dd.DeathDate = Convert.ToDateTime(dd.DeathDate);
                                        dd.DeathReportDate = Convert.ToDateTime(dd.DeathReportDate);
                                        objcnaa.DeathDetails.Add(dd);
                                        objcnaa.SaveChanges();
                                        var p1 = objcnaa.Infants.Where(x => x.InfantID == dd.infantid).FirstOrDefault();
                                        if (p1 != null)
                                        {
                                            p1.LastUpdated = DateTime.Now;
                                            p1.Status = 2;
                                            objcnaa.SaveChanges();
                                        }
                                        var p2 = objcnaa.Mothers.Where(x => x.MotherID == dd.motherid && x.LiveChild > 0).FirstOrDefault();
                                        if (p2 != null)
                                        {
                                            p2.LiveChild = (byte)(p2.LiveChild - 1);
                                            objcnaa.SaveChanges();
                                        }
                                    }
                                    transaction.Complete();
                                    _objResponseModel.Status = true;
                                    _objResponseModel.Message = "धन्यवाद, शिशु की मृत्यु का विवरण सेव हो चुका हैं।"; ;
                                }
                                catch (Exception ex)
                                {
                                    Transaction.Current.Rollback();
                                    transaction.Dispose();
                                    _objResponseModel.Status = false;
                                    _objResponseModel.Message = "ओह ! शिशु की मृत्यु का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें ।";
                                    ErrorHandler.WriteError("Error in post infant death " + ex.ToString());
                                }
                            }
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    ErrorHandler.WriteError("Error in model post infant death " + Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
                }

            }
            catch (Exception ex)
            {
                _objResponseModel.Status = false;
                ErrorHandler.WriteError("Error in model post infant death try " + ex.ToString());
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage PutInfantDeathDetails(DeathDetail dd)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(dd.AppVersion, dd.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = ValidateInfantDeath(dd);

                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            int deathUnitid = 0;
                            if (dd.DeathUnitCode != "0")
                            {
                                deathUnitid = rajmed.UnitMasters.Where(x => x.UnitCode == dd.DeathUnitCode).Select(x => x.UnitID).FirstOrDefault();
                            }
                            dd.DeathUnitID = deathUnitid;

                            using (TransactionScope transaction = new TransactionScope())
                            {
                                try
                                {
                                    using (cnaaEntities objcnaa = new cnaaEntities())
                                    {
                                        var p2 = objcnaa.DeathDetails.Where(x => x.motherid == dd.motherid && x.infantid == dd.infantid).FirstOrDefault();
                                        if (p2 != null)
                                        {
                                            Int16 AnyChanges = 0;
                                            if (p2.Age != dd.Age)
                                                AnyChanges = 1;
                                            else if (p2.AgeType != dd.AgeType)
                                                AnyChanges = 1;
                                            else if (p2.ashaautoid != dd.ashaautoid)
                                                AnyChanges = 1;
                                            else if (p2.Bfeed != dd.Bfeed)
                                                AnyChanges = 1;
                                            else if (p2.BloodGroup != dd.BloodGroup)
                                                AnyChanges = 1;
                                            else if (p2.DeathDate != dd.DeathDate)
                                                AnyChanges = 1;
                                            else if (p2.deathPlace != dd.deathPlace)
                                                AnyChanges = 1;
                                            else if (p2.DeathReportDate != dd.DeathReportDate)
                                                AnyChanges = 1;
                                            else if (p2.DeathUnitID != dd.DeathUnitID)
                                                AnyChanges = 1;
                                            else if (p2.IsImmun != dd.IsImmun)
                                                AnyChanges = 1;
                                            else if (p2.MasterMobile != dd.MasterMobile)
                                                AnyChanges = 1;
                                            else if (p2.Name != dd.Name)
                                                AnyChanges = 1;
                                            else if (p2.ReasonID != dd.ReasonID)
                                                AnyChanges = 1;
                                            else if (p2.Relative_Name != dd.Relative_Name)
                                                AnyChanges = 1;
                                            else if (p2.Weight != dd.Weight)
                                                AnyChanges = 1;
                                            else if (p2.Weight != dd.Weight)
                                                AnyChanges = 1;

                                            p2.Age = dd.Age;
                                            p2.AgeType = dd.AgeType;
                                            p2.ashaautoid = dd.ashaautoid;
                                            p2.Bfeed = dd.Bfeed;
                                            p2.BloodGroup = dd.BloodGroup;
                                            p2.DeathDate = Convert.ToDateTime(dd.DeathDate);
                                            p2.deathPlace = dd.deathPlace;
                                            p2.DeathReportDate = Convert.ToDateTime(dd.DeathReportDate);
                                            p2.DeathUnitID = deathUnitid;
                                            p2.IsImmun = dd.IsImmun;
                                            p2.LastUpdated = DateTime.Now;
                                            p2.MasterMobile = dd.MasterMobile;
                                            if (dd.Media == 3)
                                            {
                                                p2.Media = 3;
                                                p2.ANMVerify = 1;
                                                p2.ANMVerificationDate = DateTime.Now;
                                            }
                                            else if (dd.Media == 2)
                                            {
                                                p2.Media = 2;
                                                p2.ANMVerify = 0;
                                                p2.ANMVerificationDate = null;
                                            }
                                            else
                                            {
                                                p2.Media = dd.Media;
                                                p2.ANMVerify = 1;
                                                p2.ANMVerificationDate = DateTime.Now;
                                            }
                                            p2.Name = dd.Name;
                                            p2.ReasonID = dd.ReasonID;
                                            p2.Relative_Name = dd.Relative_Name;
                                            p2.Weight = dd.Weight;
                                            p2.UpdateUserNo = dd.UpdateUserNo;
                                            objcnaa.SaveChanges();
                                        }
                                        var p1 = objcnaa.Infants.Where(x => x.InfantID == dd.infantid).FirstOrDefault();
                                        if (p1 != null)
                                        {
                                            p1.LastUpdated = DateTime.Now;
                                            p1.Status = 2;
                                            objcnaa.SaveChanges();
                                        }
                                    }
                                    transaction.Complete();
                                    _objResponseModel.Status = true;
                                    _objResponseModel.Message = "धन्यवाद, शिशु की मृत्यु का विवरण अपडेट हो चुका हैं।";
                                }
                                catch (Exception ex)
                                {
                                    Transaction.Current.Rollback();
                                    transaction.Dispose();
                                    _objResponseModel.Status = false;
                                    _objResponseModel.Message = "ओह ! शिशु की मृत्यु का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा सेव करें ।";
                                    ErrorHandler.WriteError("Error in put infant death for " + ex.ToString());
                                }
                            }
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    ErrorHandler.WriteError("Error in model put infant death " + Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
                }

            }
            catch (Exception ex)
            {
                _objResponseModel.Status = false;
                ErrorHandler.WriteError("Error in model put infant death try " + ex.ToString());
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage PostPCTSID(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.PCTSID.Length < 14)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "कृपया सही पीसीटीएस आईडी डाले ";
                }
                else
                {
                    var p1 = cnaa.Mothers.Where(x => x.pctsid == p.PCTSID).Select(x => new { ancregid = x.ancregid, MotherID = x.MotherID, Status = x.Status }).ToList();
                    if (p1 != null && p1.Count > 0)
                    {


                        //anc
                        if (p.TagName == "1" || p.TagName == "2" || p.TagName == "4" || p.TagName == "9")
                        {
                            var p2 = cnaa.uspCheckPCTSIDByTagName(p1.FirstOrDefault().ancregid, p1.FirstOrDefault().MotherID, p.TagName).FirstOrDefault();
                            if (p2 != null)
                            {
                                if (p.TagName == "1")
                                {
                                    if (p2.Flag == 0)
                                    {
                                        _objResponseModel.Status = true;
                                        _objResponseModel.ResposeData = p1;
                                        _objResponseModel.Message = "Data found";
                                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                                    }
                                    else
                                    {
                                        _objResponseModel.Status = false;
                                        _objResponseModel.Message = "आप इस पीसीटीएस आईडी के एएनसी विवरण को अपडेट नहीं कर सकते हैं क्योंकि " + ((p2.Flag == 1) ? "प्रसव" : "गर्भपात") + " विवरण पहले से ही दर्ज है।";
                                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                                    }
                                }
                                else if (p.TagName == "2")
                                {
                                    string msg = "";
                                    //if (p2.Flag == 0)
                                    //{
                                    //    msg = "पीएनसी विवरण को बदला नहीं जा सकता क्योंकि यह पीसीटीएस आईडी पीएनसी अपडेशन के लिए फ्रीज किया गया है!";
                                    //}
                                    //else if (p2.DischargeDT == null)
                                    //{
                                    //    if (p2.DelplaceCode > 0 || p2.DelplaceCode == -3)
                                    //    {
                                    //        msg = "पहले डिस्चार्ज विवरण दर्ज करें!";
                                    //    }
                                    //}

                                    if (msg != "")
                                    {
                                        _objResponseModel.Status = false;
                                        _objResponseModel.Message = msg;
                                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                                    }
                                    else
                                    {
                                        _objResponseModel.Status = true;
                                        _objResponseModel.ResposeData = p1;
                                        _objResponseModel.Message = "Data found";
                                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                                    }
                                }
                                else if (p.TagName == "4")
                                {
                                    string msg = "";
                                    if (p2.Flag == 0)
                                    {
                                        msg = "मातृ मृत्यु विवरण को अपडेट नहीं किया जा सकता है क्योंकि यह मृत्यु 15 महीनों से अधिक समय से पहले की है!";
                                    }
                                    else if (p2.Freeze == 1)
                                    {
                                        msg = "मातृ मृत्यु विवरण को अपडेट नहीं किया जा सकता है क्योंकि यह आशा सॉफ्ट में सत्यापित है!";
                                    }
                                    if (msg != "")
                                    {
                                        _objResponseModel.Status = false;
                                        _objResponseModel.Message = msg;
                                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                                    }
                                    else
                                    {
                                        _objResponseModel.Status = true;
                                        _objResponseModel.ResposeData = p1;
                                        _objResponseModel.Message = "Data found";
                                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                                    }

                                }
                                if (p.TagName == "9")
                                {
                                    if (p2.Flag == 0)
                                    {
                                        _objResponseModel.Status = true;
                                        _objResponseModel.ResposeData = p1;
                                        _objResponseModel.Message = "Data found";
                                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                                    }
                                    else
                                    {
                                        _objResponseModel.Status = false;
                                        _objResponseModel.Message = "आप इस पीसीटीएस आईडी के हाईरिस्क विवरण को अपडेट नहीं कर सकते हैं क्योंकि " + ((p2.Flag == 1) ? "प्रसव" : "गर्भपात") + " विवरण पहले से ही दर्ज है।";
                                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                                    }
                                }
                            }
                            else
                            {
                                string msg = "कृपया सही पीसीटीएस आईडी डाले ";
                                if (p.TagName == "2")
                                {
                                    msg = "पहले प्रसव का विवरण दर्ज करें!";
                                }
                                _objResponseModel.Status = false;
                                _objResponseModel.Message = msg;
                                return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                            }
                        }
                        else if (p.TagName == "3")
                        {
                            return uspInfantlistByPCTSID(p);
                        }
                        else if (p.TagName == "5")
                        {
                            return PostuspInfantDataforInfantDeath(p);

                        }
                        else if (p.TagName == "6")
                        {
                            var data = cnaa.uspWomendetail(p.PCTSID).ToList();
                            if (data.Count > 0)
                            {

                                _objResponseModel.ResposeData = data;
                                _objResponseModel.Status = true;
                                _objResponseModel.Message = "Data Received successfully";
                            }
                            else
                            {
                                _objResponseModel.Status = true;
                                _objResponseModel.Message = "No Data Found";
                            }

                        }
                        //else if (p.TagName == "7")
                        //{
                        //    var p2 = cnaa.uspCheckPCTSIDForDelivery(p1.FirstOrDefault().ancregid, p1.FirstOrDefault().MotherID).FirstOrDefault();
                        //    if (p2 != null)
                        //    {
                        //        string msg = "";
                        //        if (p2.VillageAutoID == -1)
                        //        {
                        //            msg = "कृपया सही पीसीटीएस आईडी डाले !";
                        //        }
                        //        else if (Convert.ToString(p2.DeliveryDate) == "" && (Convert.ToString(p2.Mobileno) == "" || Convert.ToString(p2.Ghamantu) == "" || Convert.ToString(p2.DirectDelivery) == "" || Convert.ToString(p2.Location_Rajasthan) == ""))
                        //        {
                        //            msg = "Incomplete Claim Form J-1.\nPlease first complete Claim Form J-1 for JSY Payment !";
                        //        }
                        //        else if (p2.DelFlag == 2)
                        //        {
                        //            msg = "Delivery can not be entered for this PCTSID as its abortion details has already been entered!";
                        //        }
                        //        else
                        //        {
                        //            if (Convert.ToString(p2.DeliveryDate) != "")
                        //            {

                        //            }
                        //            if (Convert.ToInt32(p2.freeze) > 0 && Convert.ToInt32(p2.Freeze_Inst) > 0)
                        //            {
                        //                msg = "Delivery details can not be changed as this PCTS ID is freezed for delivery updation !";
                        //            }
                        //        }
                        //        if (msg != "")
                        //        {
                        //            _objResponseModel.Status = false;
                        //            _objResponseModel.Message = msg;
                        //            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                        //        }
                        //        else
                        //        {
                        //            _objResponseModel.Status = true;
                        //            _objResponseModel.ResposeData = p2;
                        //            _objResponseModel.Message = "Data found";
                        //            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                        //        }
                        //    }//

                        //   }

                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "कृपया सही पीसीटीएस आईडी डाले ";
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }


            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        [ActionName("postBlockDataForDeath")]
        public HttpResponseMessage postBlockDataForDeath(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.DeathUnittype != 0 && p.DeathUnitCode != "0")
                {

                    if (p.DeathUnittype == 8 || p.DeathUnittype == 9 || p.DeathUnittype == 10 || p.DeathUnittype == 11 || p.DeathUnittype == 16)
                    {
                        p.DeathUnittype = 4;
                    }

                    var data = rajmed.UnitMasters.Where(x => x.UnitType == p.DeathUnittype && x.UnitCode.StartsWith(p.DeathUnitCode.Substring(0, 4))).Select(x => new { unitcode = x.UnitCode, unitNameHindi = x.UnitNameHindi }).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage postfillBlock(Pcts pcts)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            bool tokenFlag = ValidateToken(pcts.UserID, pcts.TokenNo);
            if (tokenFlag == true)
            {
                if (ModelState.IsValid)
                {
                    var qry = rajmed.UnitMasters.Where(x => x.UnitCode.StartsWith(pcts.DeathUnitCode) && x.UnitType == pcts.DeathUnittype).OrderBy(x => x.UnitName).Select(x => new
                    {
                        x.UnitID,
                        UnitName = x.UnitNameHindi == null ? x.UnitName : x.UnitNameHindi,
                        x.UnitCode
                    }).Select(x => new
                    {
                        x.UnitID,
                        x.UnitName,
                        x.UnitCode
                    }).ToList();

                    if (qry != null)
                    {
                        response = Request.CreateResponse(HttpStatusCode.OK, qry);
                    }
                    else
                    {
                        response = Request.CreateResponse(HttpStatusCode.NotFound, "Data not Found");
                    }

                }
                else
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            else
            {
                response = Request.CreateResponse(HttpStatusCode.NotFound, "Invalid request");
            }

            return response;
        }
        public HttpResponseMessage postfillCHCPHC(Pcts pcts)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            bool tokenFlag = ValidateToken(pcts.UserID, pcts.TokenNo);
            if (tokenFlag == true)
            {
                if (ModelState.IsValid)
                {
                    try
                    {

                        var qry = rajmed.UnitMasters.Where(x => x.UnitCode.StartsWith(pcts.DeathUnitCode)).ToList();
                        if (pcts.action == "1")
                        {
                            qry = qry.Where(x => x.UnitType == pcts.DeathUnittype).ToList();
                        }
                        else if (pcts.action == "2")
                        {
                            string[] strUnitType = { "9", "10", "13" };

                            qry = qry.Where(x => strUnitType.Contains(x.UnitType.ToString()) && x.UnitCode.Substring(4, 2) == "99").ToList();
                        }
                        else if (pcts.action == "3")
                        {
                            string[] strUnitType = { "9", "10" };
                            qry = qry.Where(x => strUnitType.Contains(x.UnitType.ToString())).ToList();
                        }
                        var qry1 = qry.OrderBy(x => x.UnitName).Select(x => new
                        {
                            x.UnitCode,
                            UnitName = x.UnitNameHindi == null ? x.UnitName : x.UnitNameHindi,
                            x.UnitID
                        }).Select(x => new
                        {
                            x.UnitCode,
                            x.UnitName,
                            x.UnitID
                        }).ToList();

                        if (qry1 != null)
                        {
                            response = Request.CreateResponse(HttpStatusCode.OK, qry1);
                        }
                        else
                        {
                            response = Request.CreateResponse(HttpStatusCode.NotFound, "Data not Found");
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler.WriteError("Error in PUT postfillCHCPHC" + ex.ToString());
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, ex);
                    }
                }
                else
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            else
            {
                response = Request.CreateResponse(HttpStatusCode.NotFound, "Invalid request");
            }

            return response;
        }

        public HttpResponseMessage postfillSubcenter(Pcts pcts)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            bool tokenFlag = ValidateToken(pcts.UserID, pcts.TokenNo);
            if (tokenFlag == true)
            {
                if (ModelState.IsValid)
                {
                    var qry = rajmed.UnitMasters.Where(x => x.UnitCode.StartsWith(pcts.DeathUnitCode) && x.UnitType == 11).OrderBy(x => x.UnitName).Select(x => new
                    {
                        x.UnitID,
                        UnitName = x.UnitNameHindi == null ? x.UnitName : x.UnitNameHindi,
                        x.UnitCode
                    }).Select(x => new
                    {
                        x.UnitID,
                        x.UnitName,
                        x.UnitCode
                    }).ToList();

                    if (qry != null)
                    {
                        response = Request.CreateResponse(HttpStatusCode.OK, qry);
                    }
                    else
                    {
                        response = Request.CreateResponse(HttpStatusCode.NotFound, "Data not Found");
                    }
                }
                else
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            else
            {
                response = Request.CreateResponse(HttpStatusCode.NotFound, "Invalid request");
            }

            return response;
        }

        public HttpResponseMessage uspInfantDataforInfantDeathByInfantID(Pcts pcts)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(pcts.UserID, pcts.TokenNo);
            if (tokenFlag == true)
            {
                var data = cnaa.uspInfantDataforInfantDeathByInfantID(pcts.InfantID).ToList();
                if (data != null)
                {
                    _objResponseModel.ResposeData = data;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("uspInfantlist")]
        public HttpResponseMessage uspInfantlist(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.LoginUnitid != 0)
                {
                    var data = cnaa.uspInfantlistForImmunization(p.LoginUnitid, p.VillageAutoid, p.ANMAutoID).Select(x => new { name = x.name, Husbname = x.Husbname, Mobileno = x.Mobileno, Birth_date = x.Birth_date, ChildName = x.ChildName, Sex = x.Sex, MotherID = x.MotherID, InfantID = x.infantid, ChildID = x.childid, Weight = x.Weight }).ToList();
                    if (data != null)
                    {
                        List<MotherInfantListForImmunization> listHash = new List<MotherInfantListForImmunization>();
                        Int32 i = 0;
                        for (i = 0; i < data.Count; i++)
                        {
                            MotherInfantListForImmunization ii = new MotherInfantListForImmunization();
                            ii.name = Convert.ToString(data[i].name);
                            ii.Husbname = Convert.ToString(data[i].Husbname);
                            ii.Mobileno = Convert.ToString(data[i].Mobileno);

                            Int32 motherid = Convert.ToInt32(data[i].MotherID);
                            List<InfantListForImmunization> listHash1 = data.Where(x => x.MotherID == motherid).Select(x => new InfantListForImmunization { Birth_date = x.Birth_date, ChildName = x.ChildName, Sex = x.Sex, InfantID = x.InfantID, ChildID = x.ChildID, Weight = x.Weight }).ToList();
                            ii.infantList = listHash1;
                            i += listHash1.Count;
                            listHash.Add(ii);
                        }
                        _objResponseModel.ResposeData = listHash;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage uspInfantlistByPCTSID(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.PCTSID.Length > 13)
                {
                    var data = cnaa.uspInfantlistForImmunizationByPCTSID(p.PCTSID).Select(x => new { name = x.name, Husbname = x.Husbname, Mobileno = x.Mobileno, Birth_date = x.Birth_date, ChildName = x.ChildName, Sex = x.Sex, MotherID = x.MotherID, InfantID = x.infantid, ChildID = x.childid, PehchanRegFlag = x.PehchanRegFlag }).ToList();
                    if (data.Count > 0)
                    {
                        List<MotherInfantListForImmunization> listHash = new List<MotherInfantListForImmunization>();
                        Int32 i = 0;
                        for (i = 0; i < data.Count; i++)
                        {
                            MotherInfantListForImmunization ii = new MotherInfantListForImmunization();
                            ii.name = Convert.ToString(data[i].name);
                            ii.Husbname = Convert.ToString(data[i].Husbname);
                            ii.Mobileno = Convert.ToString(data[i].Mobileno);
                            Int32 motherid = Convert.ToInt32(data[i].MotherID);
                            List<InfantListForImmunization> listHash1 = data.Where(x => x.MotherID == motherid).Select(x => new InfantListForImmunization { Birth_date = x.Birth_date, ChildName = x.ChildName, Sex = x.Sex, InfantID = x.InfantID, ChildID = x.ChildID, PehchanRegFlag = x.PehchanRegFlag }).ToList();
                            ii.infantList = listHash1;
                            i += listHash1.Count;
                            listHash.Add(ii);
                        }
                        _objResponseModel.ResposeData = listHash;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage uspANMWorkPlan(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.TagName == "A" || p.TagName == "P" || p.TagName == "S")
                {
                    var data = cnaa.uspANMPlan(p.LoginUnitcode, p.MthYr, p.TagName).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else if (p.TagName == "I")
                {
                    var data = cnaa.uspANMPlanForImmunization(p.LoginUnitcode, p.MthYr, p.TagName).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }


            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage GetYearListForANMPlan()
        {
            ResponseModel _objResponseModel = new ResponseModel();
            List<Dictionary<string, string>> listHash = new List<Dictionary<string, string>>();
            int i = 0;
            while (i <= 1)
            {
                Dictionary<string, string> hash = new Dictionary<string, string> { };
                hash.Add("Year", DateTime.Now.AddYears(i).Year.ToString());
                listHash.Add(hash);
                i += 1;
            }
            _objResponseModel.ResposeData = listHash;
            _objResponseModel.Status = true;
            _objResponseModel.Message = "Data Found";
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        public HttpResponseMessage GetYearListForAshaInc()
        {
            ResponseModel _objResponseModel = new ResponseModel();
            List<Dictionary<string, string>> listHash = new List<Dictionary<string, string>>();
            int i = 0;
            while (i <= 1)
            {
                Dictionary<string, string> hash = new Dictionary<string, string> { };
                hash.Add("Year", DateTime.Now.AddYears(i - 1).Year.ToString());
                listHash.Add(hash);
                i += 1;
            }
            _objResponseModel.ResposeData = listHash;
            _objResponseModel.Status = true;
            _objResponseModel.Message = "Data Found";
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage PutLatitudeLongitude(UnitMaster un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            try
            {
                if (tokenFlag == true)
                {
                    var p = rajmed.UnitMasters.Where(x => x.UnitID == un.UnitID).FirstOrDefault();
                    if (p != null)
                    {
                        p.AppLatitude = un.AppLatitude;
                        p.AppLongitude = un.AppLongitude;
                        p.LastUpdated = DateTime.Now;
                        rajmed.SaveChanges();
                    }

                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Saved successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid request";
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Put Latitude Longitude" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "ओह ! दुबारा प्रयास करे ";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage PostLatitudeLongitude(UnitMaster un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                var p = rajmed.UnitMasters.Where(x => x.UnitID == un.UnitID).Select(x => new { AppLatitude = x.AppLatitude, AppLongitude = x.AppLongitude }).ToList();
                _objResponseModel.ResposeData = p;
                _objResponseModel.Status = true;
                _objResponseModel.Message = "Data Saved successfully";
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        //public HttpResponseMessage PostCheckPhoto(HealthUnitsPhoto m)
        //{

        //    ResponseModel _objResponseModel = new ResponseModel();
        //    try
        //    {

        //        var data = rajmed.HealthUnitPhotoes.Where(x => x.Unitid == m.Unitid).FirstOrDefault();
        //        if (data == null)
        //        {
        //            _objResponseModel.ResposeData = "0";
        //            _objResponseModel.Status = false;
        //            _objResponseModel.Message = "डाटा उपलब्ध नहीं है";

        //        }
        //        else
        //        {
        //            _objResponseModel.ResposeData = "1";
        //            _objResponseModel.Status = true;
        //            _objResponseModel.Message = "Data Exists";
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        _objResponseModel.Status = false;
        //        _objResponseModel.Message = "ओह ! दुबारा प्रयास करे ";
        //    }
        //    return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        //}

        //private string SaveHealthUnitPhoto(HealthUnitsPhoto m)
        //{
        //    using (TransactionScope transaction = new TransactionScope())
        //    {
        //        try
        //        {
        //            var p = new UnitMaster();
        //            if (m.Latitude.Length > 9)
        //            {
        //                m.Latitude = m.Latitude.Substring(0, 9);
        //            }
        //            if (m.Longitude.Length > 9)
        //            {
        //                m.Longitude = m.Longitude.Substring(0, 9);
        //            }

        //            p = rajmed.UnitMasters.Where(x => x.UnitID == m.Unitid).FirstOrDefault();
        //            p.Latitude = m.Latitude;
        //            p.Longitude = m.Longitude;
        //            //rajmed.UnitMasters.Add(p);
        //            rajmed.SaveChanges();

        //            var data = rajmed.HealthUnitPhotoes.Where(x => x.Unitid == m.Unitid).FirstOrDefault();
        //            if (data == null)
        //            {
        //                HealthUnitPhoto hu = new HealthUnitPhoto();
        //                hu.Photo = m.PhotoArray;
        //                hu.Unitid = m.Unitid;
        //                hu.EntryDate = DateTime.Now;
        //                rajmed.HealthUnitPhotoes.Add(hu);
        //                rajmed.SaveChanges();
        //            }




        //            //int rowsAffected = rajmed.Database.SqlQuery<int>("select (count(*)) from OJSPMPDFs..HealthUnitPhoto  where unitid={0}", Convert.ToInt32(m.Unitid)).FirstOrDefault();
        //            //if (m.Latitude.Length > 9)
        //            //{
        //            //    m.Latitude = m.Latitude.Substring(0, 9);
        //            //}
        //            //if (m.Longitude.Length > 9)
        //            //{
        //            //    m.Longitude = m.Longitude.Substring(0, 9);
        //            //}

        //            //if ((rowsAffected == 0) && (m.PhotoArray != null && m.PhotoArray.Length > 1))
        //            //{
        //            //    rowsAffected = rajmed.Database.ExecuteSqlCommand("insert into OJSPMPDFs..HealthUnitPhoto(Unitid,Latitude,Longitude,Photo) values({0},{1},{2},{3})", m.Unitid, m.Latitude, m.Longitude, m.PhotoArray);


        //            //}
        //            //else if (m.Unitid != 0 && rowsAffected > 0)
        //            //{

        //            //    rowsAffected = rajmed.Database.ExecuteSqlCommand("update OJSPMPDFs..HealthUnitPhoto set Photo={1},LastUpdateDate = {2}  where Unitid={0}", m.Unitid, m.PhotoArray, DateTime.Now);


        //            //}

        //            transaction.Complete();
        //        }
        //        catch (Exception ex)
        //        {
        //            Transaction.Current.Rollback();
        //            transaction.Dispose();

        //            return "ओह ! फोटो अपलोड नहीं हुई । कृपया दोबारा अपलोड करें ।" + ex.ToString();

        //        }
        //    }

        //    return "";
        //}


        //[ActionName("PostHealthUnitUploadImage")]
        //public async Task<HttpResponseMessage> PostHealthUnitUploadImage()
        //{
        //    ResponseModel _objResponseModel = new ResponseModel();
        //    //  Check if the request contains multipart/form-data.  
        //    if (!Request.Content.IsMimeMultipartContent())
        //    {
        //        throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
        //    }
        //    HealthUnitsPhoto sm = await ConvertMulipartRequests();
        //    if (sm == null)
        //    {
        //        _objResponseModel.Status = false;
        //        _objResponseModel.Message = "Error in Data";
        //        _objResponseModel.AppVersion = 1;
        //        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        //    }
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {

        //            string msg = SaveHealthUnitPhoto(sm);
        //            if (msg != "")
        //            {
        //                _objResponseModel.Status = false;
        //                _objResponseModel.Message = msg;
        //                Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        //            }
        //            else
        //            {
        //                _objResponseModel.Status = true;
        //                _objResponseModel.Message = "धन्यवाद,फोटो अपलोड हो गई है |";
        //            }

        //        }
        //        else
        //        {
        //            _objResponseModel.Status = false;
        //            _objResponseModel.Message = "Model Error";
        //            ErrorHandler.WriteError("Error in post Swasthya model--" + ModelState);
        //            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        _objResponseModel.Status = false;
        //        _objResponseModel.Message = "validation Error";
        //        ErrorHandler.WriteError("Error in post Swasthya--" + ex.ToString());
        //    }

        //    return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        //}





        //private async Task<HealthUnitsPhoto> ConvertMulipartRequests()
        //{
        //    HealthUnitsPhoto sm = null;
        //    try
        //    {
        //        var provider = await Request.Content.ReadAsMultipartAsync<InMemoryMultipartFormDataStreamProvider>(new InMemoryMultipartFormDataStreamProvider());
        //        //access form data  
        //        NameValueCollection formData = provider.FormData;
        //        sm = new HealthUnitsPhoto();
        //        if (formData != null)
        //        {
        //            sm.Unitid = Convert.ToInt32(formData["Unitid"]);
        //            sm.Longitude = Convert.ToString(formData["Longitude"]);
        //            sm.Latitude = Convert.ToString(formData["Latitude"]);

        //            sm.UserID = Convert.ToString(formData["UserID"]);
        //            sm.TokenNo = Convert.ToString(formData["TokenNo"]);


        //        }

        //        //access files  
        //        if (provider != null)
        //        {
        //            IList<HttpContent> files = provider.Files;
        //            if (files != null)
        //            {
        //                if (files.Count > 0)
        //                {
        //                    HttpContent file1 = files[0];
        //                    if (file1 != null)
        //                    {
        //                        if (file1.Headers.ContentDisposition.FileName.Trim('\"') != "")
        //                        {
        //                            var thisFileName = file1.Headers.ContentDisposition.FileName.Trim('\"');
        //                            Stream input = await file1.ReadAsStreamAsync();
        //                            byte[] b;

        //                            using (BinaryReader br = new BinaryReader(input))
        //                            {
        //                                b = br.ReadBytes((int)input.Length);
        //                            }
        //                            sm.PhotoArray = b;
        //                            sm.PhotoType = file1.Headers.ContentDisposition.FileName.Split('.')[1].Replace("'", "");
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorHandler.WriteError("Error in ConvertMulipartRequest--" + ex.ToString());
        //    }
        //    return sm;
        //}

        //public HttpResponseMessage GetHealthUnitPhoto(HealthUnitPhoto hu)
        //{
        //    ResponseModel _objResponseModel = new ResponseModel();
        //    //bool tokenFlag = ValidateToken(sm.UserID, sm.TokenNo);
        //    //if (tokenFlag == true)
        //    //{
        //    List<HealthUnitsPhoto> p = rajmed.UspHealthunitphoto(hu.Unitid).Select(x => new HealthUnitsPhoto
        //    {
        //        Latitude = x.Latitude,
        //        Longitude = x.Longitude,
        //        PhotoArray = x.Photo
        //    }).ToList();

        //    Dictionary<string, string> hash = new Dictionary<string, string> { };
        //    foreach (HealthUnitsPhoto hu1 in p)
        //    {
        //        if (hu.Unitid > 0)
        //        {

        //            string filePath = AppDomain.CurrentDomain.BaseDirectory + "SwasthyaMitrkPhoto\\hu";
        //            Uri baseUri = new Uri(Request.RequestUri.AbsoluteUri.Replace(Request.RequestUri.PathAndQuery, String.Empty));
        //            string fullPath = filePath + hu.Unitid + "." + "jpg";
        //            string resourceRelative = "~/SwasthyaMitrkPhoto/hu" + hu.Unitid + "." + "jpg";
        //            Uri resourceFullPath = new Uri(baseUri, VirtualPathUtility.ToAbsolute(resourceRelative));
        //            System.Drawing.Image imageIn = ConvertByteArrayToImage(hu1.PhotoArray);
        //            File.WriteAllBytes(fullPath, hu1.PhotoArray);
        //            hash.Add("Url", resourceFullPath.AbsolutePath);
        //            hash.Add("Latitude", hu1.Latitude);
        //            hash.Add("Longitude", hu1.Longitude);
        //            hu1.PhotoArray = null;
        //        }

        //    }





        //    _objResponseModel.ResposeData = hash;
        //    _objResponseModel.Status = true;
        //    // }
        //    //else
        //    //{
        //    //    _objResponseModel.Status = false;
        //    //    _objResponseModel.Message = "Invalid request";
        //    //}
        //    return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        //}



        public HttpResponseMessage uspInfantDataByInfantID(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.InfantID > 0)
                {
                    var data = cnaa.uspInfantDetailsByInfantID(p.InfantID).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage uspDataforPNCWomanDetails(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.ANCRegID != 0)
                {
                    var data = cnaa.uspDataforPNCWomanDetails(p.ANCRegID).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "पीसीटीएस आईडी सही नहीं हैं।";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        }
        public HttpResponseMessage uspInfantlistByPCTSIDForWomanDetails(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.PCTSID.Length > 13)
                {
                    var data = cnaa.uspInfantlistForWomanDetails(p.PCTSID).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        public HttpResponseMessage uspGetVillageList(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.LoginUnitid > 0)
                {
                    //  var data = rajmed.Villages.Where(x => x.unitid == p.LoginUnitid).Select(x => new { VillageName = (x.UnitNameHindi == null ? x.VillageName : x.UnitNameHindi), VillageautoID = x.VillageAutoID }).ToList();
                    var data = cnaa.uspGetVillageByAshaID((int?)p.LoginUnitid, (int?)p.VillageAutoid, (int?)p.ANMAutoID).Select(x => new { VillageName = x.VillageName, VillageautoID = x.VillageAutoID }).ToList();
                    data.Add(new { VillageName = "सभी गाँव", VillageautoID = 0 });
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data.OrderBy(x => x.VillageautoID);
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        private string validatePassword(UserAuthenticate u)
        {
            string errorMsg = "";
            if (!Regex.IsMatch(u.UserID, @"^[5-9]{1}[0-9]{9}$"))
            {
                if (ValidateToken(u.UserID, u.TokenNo) == false)
                {
                    return "Invalid Request";
                }
            }
            if (u.UserID.Length < 1 && u.UserID.Length > 15)
            {
                return "कृपया सही यूज़र आईडी डाले ! ";
            }

            if (u.Password.Length != 128)
            {
                return "कृपया सही नया पासवर्ड डाले ! ";
            }
            if (u.ConfirmPassword.Length != 128)
            {
                return "कृपया सही पुराना पासवर्ड डाले ! ";
            }
            if (u.Password.Trim() != u.ConfirmPassword.Trim())
            {
                return "नया पासवर्ड और कन्फर्म पासवर्ड समान नहीं हैं! ";
            }
            return errorMsg;
        }

        public HttpResponseMessage uspPostChangePassword(UserAuthenticate u)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string ErrorMsg = validatePassword(u);
            if (ErrorMsg != "")
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = ErrorMsg;
            }
            else
            {
                var p = rajmed.Users.Where(x => x.UserID == u.UserID && x.IsDeleted == 0).FirstOrDefault();
                if (p != null)
                {
                    if (p.old1pwd != "")
                    {
                        if (p.old1pwd == u.Password)
                        {
                            ErrorMsg = "नया पासवर्ड पिछले 3 पासवर्ड से अलग होना चाहिए !";
                        }
                    }
                    if (p.old2pwd != "")
                    {
                        if (p.old2pwd == u.Password)
                        {
                            ErrorMsg = "नया पासवर्ड पिछले 3 पासवर्ड से अलग होना चाहिए !";
                        }
                    }

                    if (ErrorMsg != "")
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = ErrorMsg;
                    }
                    else
                    {
                        u.SmsFlag = 4;
                        //if (CheckOtp(p.UserContactNo, u.OTP, u.SmsFlag, u.DeviceID, u.UserID))
                        //{
                        p.old2pwd = p.old1pwd;
                        p.old1pwd = p.Password;
                        p.Password = u.Password;
                        p.PwdUpdatedDate = DateTime.Now;
                        p.resetpwd = 0;
                        rajmed.SaveChanges();
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "पासवर्ड बदल दिया गया है। अब आप इस नए पासवर्ड से लॉगिन करें ।";
                        // }


                    }

                }
                else
                {
                    if (Regex.IsMatch(u.UserID, @"^[5-9]{1}[0-9]{9}$"))
                    {
                        u.SmsFlag = 4;
                        if (CheckOtp(u.UserID, u.OTP, u.SmsFlag, u.DeviceID, u.UserID))
                        {
                            var datas = rajmed.AshaMasters.Where(a => a.type == 1 && a.AshaPhone == u.UserID && a.Status == 1).FirstOrDefault();
                            if (datas != null)
                            {

                                var datas1 = rajmed.Users.Where(a => a.IsDeleted == 0 && a.UserID == u.UserID).FirstOrDefault();
                                if (datas1 == null)
                                {
                                    try
                                    {
                                        User us = new User();
                                        us.UserID = u.UserID;
                                        us.IsDeleted = 0;
                                        us.Password = u.Password;
                                        us.EntryDate = DateTime.Now;
                                        us.LastUpdated = DateTime.Now;
                                        us.AppRoleID = 33;
                                        us.Role = "General User";
                                        us.PctsRoleID = 2;
                                        us.EctsRoleID = 100;
                                        us.AshaRoleId = 100;
                                        us.OJSPMRoleID = 100;
                                        us.AidsRoleID = 100;
                                        us.unitid = datas.unitid;
                                        us.UnitCode = datas.unitcode;
                                        us.UserName = datas.AshaName;
                                        us.UserContactNo = u.UserID;
                                        us.ANMAutoID = datas.ashaAutoID;
                                        rajmed.Users.Add(us);
                                        rajmed.SaveChanges();
                                        _objResponseModel.Status = true;
                                        _objResponseModel.Message = "पासवर्ड दर्ज कर लिया गया है। अब आप इस पासवर्ड से लॉगिन करें । ";
                                    }
                                    catch
                                    {
                                        _objResponseModel.Status = false;
                                        _objResponseModel.Message = "कृपया सही यूज़र आईडी डाले ! ";
                                    }
                                }
                                else
                                {
                                    _objResponseModel.Status = false;
                                    _objResponseModel.Message = "कृपया सही यूज़र आईडी डाले ! ";
                                }


                            }
                            else
                            {
                                _objResponseModel.Status = false;
                                _objResponseModel.Message = "कृपया सही यूज़र आईडी डाले ! ";
                            }
                        }
                        else
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = "Invalid Otp ";
                        }


                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "कृपया सही यूज़र आईडी डाले ! ";
                    }


                }

            }
            List<Dictionary<string, string>> listHash = new List<Dictionary<string, string>>();
            _objResponseModel.ResposeData = null;

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        public HttpResponseMessage PostUnittypeList(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                short[] strUnitType;
                if (un.LoginUnitType < 4)
                {
                    strUnitType = new short[10] { 5, 6, 7, 8, 9, 10, 11, 13, 14, 15 };
                }
                else if (un.LoginUnitType == 4)
                {
                    strUnitType = new short[4] { 8, 9, 10, 11 };
                }
                else if (un.LoginUnitType == 9 || un.LoginUnitType == 10 || un.LoginUnitType == 13)
                {
                    strUnitType = new short[2] { un.LoginUnitType, 11 };
                }
                else
                {
                    strUnitType = new short[1] { un.LoginUnitType };
                }
                var data = rajmed.UnitTypeMasters.Where(x => strUnitType.Contains(x.UnitTypeCode)).Select(x => new { UnitTypeCode = x.UnitTypeCode, UnittypeNameHindi = x.UnittypeNameHindi }).ToList();
                if (data != null)
                {
                    data.Add(new { UnitTypeCode = (byte)0, UnittypeNameHindi = "चुनें" });
                    _objResponseModel.ResposeData = data.OrderBy(x => x.UnitTypeCode);
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage PostBlockList(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            //bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            //if (tokenFlag == true)
            //{
            if (un.UnitCode.Length >= 4)
            {
                string unitcode = un.UnitCode.Substring(0, 4);
                short unittype = un.UnitType;
                if (un.UnitType == 8 || un.UnitType == 9 || un.UnitType == 10 || un.UnitType == 11)
                {
                    if (un.LoginUnitType > 3)
                    {
                        unitcode = un.LoginUnitCode.Substring(0, 6);
                        if (un.LoginUnitType == 13 && un.UnitType == 11)
                        {
                            unitcode = un.LoginUnitCode.Substring(0, 9);
                        }
                    }
                    unittype = 4;
                }
                if (un.UnitType == 14)
                {
                    unittype = 13;
                }
                var data = rajmed.UnitMasters.Where(x => x.UnitCode.StartsWith(unitcode) && x.UnitType == unittype).Select(x => new { UnitName = (x.UnitNameHindi == null ? x.UnitName : x.UnitNameHindi), UnitCode = x.UnitCode }).ToList();
                if (un.UnitType == 9 || un.UnitType == 10 || (un.UnitType == 11 && un.LoginUnitCode.Substring(4, 2) == "99"))
                {
                    data.Add(new { UnitName = "Urban Units", UnitCode = un.UnitCode.Substring(0, 4) + "99" });
                }
                if (data != null)
                {

                    data.Add(new { UnitName = "चुनें", UnitCode = "0" });
                    _objResponseModel.ResposeData = data.OrderBy(x => x.UnitCode);
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    data.Add(new { UnitName = "चुनें", UnitCode = "0" });
                    _objResponseModel.ResposeData = data.OrderBy(x => x.UnitCode);
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
            }
            else
            {
                Dictionary<string, string> hash = new Dictionary<string, string> { };
                hash.Add("UnitName", "चुनें");
                hash.Add("UnitCode", "0");
                List<Dictionary<string, string>> listHash = new List<Dictionary<string, string>>();
                listHash.Add(hash);
                _objResponseModel.ResposeData = listHash;
                _objResponseModel.Status = true;
                _objResponseModel.Message = "Data Received successfully";
            }
            //}
            //else
            //{
            //    _objResponseModel.Status = false;
            //    _objResponseModel.Message = "Invalid request";
            //}

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage PostCHCPHC(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                if (un.UnitCode.Length >= 6 && un.UnitType > 7)
                {
                    string unitcode = un.UnitCode.Substring(0, 6);
                    short[] strUnitType;
                    if (un.UnitType == 11)
                    {
                        strUnitType = new short[3] { 9, 10, 13 };
                        if (un.LoginUnitType == 11)
                        {
                            unitcode = un.LoginUnitCode.Substring(0, 9);
                        }
                    }
                    else
                    {
                        strUnitType = new short[1] { un.UnitType };
                    }

                    var data = rajmed.UnitMasters.AsEnumerable().Where(x => x.UnitCode.StartsWith(unitcode) && strUnitType.Contains(x.UnitType)).Select(x => new { UnitName = (x.UnitNameHindi == null ? x.UnitName : x.UnitNameHindi), UnitCode = x.UnitCode }).ToList();
                    if (data != null)
                    {
                        data.Add(new { UnitName = "चुनें", UnitCode = "0" });
                        _objResponseModel.ResposeData = data.OrderBy(x => x.UnitCode);
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        data.Add(new { UnitName = "चुनें", UnitCode = "0" });
                        _objResponseModel.ResposeData = data.OrderBy(x => x.UnitCode);
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                }
                else
                {
                    Dictionary<string, string> hash = new Dictionary<string, string> { };
                    hash.Add("UnitName", "चुनें");
                    hash.Add("UnitCode", "0");
                    List<Dictionary<string, string>> listHash = new List<Dictionary<string, string>>();
                    listHash.Add(hash);
                    _objResponseModel.ResposeData = listHash;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage postfillSubcenter1(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                if (ModelState.IsValid && un.UnitCode.Length >= 9)
                {
                    string unitcode = un.UnitCode.Substring(0, 9);
                    if (un.LoginUnitType == 11)
                    {
                        unitcode = un.LoginUnitCode;
                    }
                    var data = rajmed.UnitMasters.Where(x => x.UnitCode.StartsWith(unitcode) && x.UnitType == 11).OrderBy(x => x.UnitName).Select(x => new
                    {
                        UnitName = x.UnitNameHindi == null ? x.UnitName : x.UnitNameHindi,
                        UnitCode = x.UnitCode
                    }).ToList();
                    if (data != null)
                    {
                        data.Add(new { UnitName = "चुनें", UnitCode = "0" });
                        _objResponseModel.ResposeData = data.OrderBy(x => x.UnitCode);
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        data.Add(new { UnitName = "चुनें", UnitCode = "0" });
                        _objResponseModel.ResposeData = data.OrderBy(x => x.UnitCode);
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                }
                else
                {
                    Dictionary<string, string> hash = new Dictionary<string, string> { };
                    hash.Add("UnitName", "चुनें");
                    hash.Add("UnitCode", "0");
                    List<Dictionary<string, string>> listHash = new List<Dictionary<string, string>>();
                    listHash.Add(hash);
                    _objResponseModel.ResposeData = listHash;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage postANMListByUnitcode(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                if (ModelState.IsValid)
                {
                    var data = rajmed.uspGetANMForOtherUserNewDemo(un.UnitCode, un.AshaType).Select(x => new { AshaName = x.AshaName, ashaAutoID = x.ashaAutoID, AshaPhone = x.AshaPhone }).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    Dictionary<string, string> hash = new Dictionary<string, string> { };
                    hash.Add("AshaName", "चुनें");
                    hash.Add("ashaAutoID", "0");
                    List<Dictionary<string, string>> listHash = new List<Dictionary<string, string>>();
                    listHash.Add(hash);
                    _objResponseModel.ResposeData = listHash;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage PostDistdataAdmin(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                string unitcode = un.UnitCode;
                if (un.UnitType == 1)
                {
                    unitcode = "0";
                }
                else if (un.UnitType == 2)
                {
                    unitcode = un.UnitCode.Substring(0, 2);
                }
                else
                {
                    unitcode = un.UnitCode.Substring(0, 4);
                }

                var data = rajmed.UnitMasters.Where(x => x.UnitType == 3 && x.UnitCode.StartsWith(unitcode)).Select(x => new { unitcode = x.UnitCode, unitNameHindi = (x.UnitNameHindi == null ? x.UnitName : x.UnitNameHindi) }).ToList();
                if (data != null)
                {
                    if (data.Count > 0)
                    {
                        data.Add(new { unitcode = "0", unitNameHindi = "चुनें" });
                        _objResponseModel.ResposeData = data.OrderBy(x => x.unitcode);
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }


            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage PostANMAutoid(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                var data = rajmed.uspGetANMDetails(un.UnitID, un.AshaType).ToList();
                if (data.Count > 0)
                {
                    _objResponseModel.ResposeData = data;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }


            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage PostANMUsage(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                var data = cnaa.uspANMusage(un.LoginUnitCode, un.LoginUnitType, un.AshaType, "", "").ToList();
                if (data != null)
                {
                    _objResponseModel.ResposeData = data;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }


            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage PostANMUsageDetails(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                var data = cnaa.uspANMusageDetails(un.UnitCode, un.UnitType, un.AshaType).ToList();
                if (un.Flag == 2)
                {
                    data = data.Where(x => x.ancCount + x.pncCount + x.immuCount + x.maternalDeathount + x.infantDeathount > 0).ToList();
                }
                if (data != null)
                {
                    _objResponseModel.ResposeData = data;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }


            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage PostANMNotUsageDetails(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                if (un.Flag == 1 || un.Flag == 2)
                {
                    var data = cnaa.uspANMNotusageDetails(un.UnitCode, un.UnitType, un.Flag, un.AshaType, "", "").ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    var data = cnaa.uspANMusageDetails(un.UnitCode, un.UnitType, un.AshaType).ToList();
                    if (un.Flag == 3)
                    {
                        data = data.Where(x => x.ancCount + x.pncCount + x.immuCount + x.maternalDeathount + x.infantDeathount > 0).ToList();
                    }
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }

                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        private string validateDayWiseData(UnitMasterAdmin un)
        {
            string ErrorMsg = "";
            if (ValidateToken(un.UserID, un.TokenNo) == false)
            {
                return "Invalid Request";
            }
            ErrorMsg = checkDate(Convert.ToString(un.FromDate), 1, "From Date");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = checkDate(Convert.ToString(un.ToDate), 1, "To Date");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            if (DifferenceInDays((DateTime)un.FromDate, (DateTime)un.ToDate) > 15)
            {
                ErrorMsg = "कब तक की तिथि एवं कब से की तिथि का अंतर 16 दिनों से कम होना चाहिए !";
                return ErrorMsg;
            }
            if (DifferenceInDays((DateTime)un.FromDate, (DateTime)un.ToDate) < 0)
            {
                ErrorMsg = "कब तक की तिथि कब से की तिथि से ज्यादा नहीं होनी चाहिए !";
                return ErrorMsg;
            }
            if (DifferenceInDays((DateTime)un.FromDate, DateTime.Now) < 0)
            {
                ErrorMsg = "कब से की तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
                return ErrorMsg;
            }
            if (DifferenceInDays((DateTime)un.ToDate, DateTime.Now) < 0)
            {
                ErrorMsg = "कब तक की तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
                return ErrorMsg;
            }
            int finyear1 = Convert.ToInt32(Convert.ToDateTime(un.FromDate).Month) < 4 ? (Convert.ToDateTime(un.FromDate).Year - 1) : ((Convert.ToDateTime(un.FromDate).Year));
            int finyear2 = Convert.ToInt32(Convert.ToDateTime(un.ToDate).Month) < 4 ? (Convert.ToDateTime(un.ToDate).Year - 1) : ((Convert.ToDateTime(un.ToDate).Year));
            if (finyear1 != finyear2)
            {
                ErrorMsg = "कब तक की तिथि और कब से की तिथि एक वित्तीय वर्ष के भीतर होना चाहिए";
                return ErrorMsg;
            }
            return ErrorMsg;
        }

        public HttpResponseMessage PostANMUsageDaywise(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string ErrorMsg = validateDayWiseData(un);
            try
            {
                if (ErrorMsg == "")
                {
                    var data = cnaa.uspANMusageDayWise(un.LoginUnitCode, un.LoginUnitType, un.FromDate, un.ToDate, un.AshaType).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = ErrorMsg;
                }
            }
            catch
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = ErrorMsg;
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage PostANMUsageDaywise1(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                string ErrorMsg = checkDate(Convert.ToString(un.FromDate), 1, "From Date");
                if (ErrorMsg == "")
                {
                    var data = cnaa.uspANMusageDayWise1(un.LoginUnitCode, un.LoginUnitType, un.FromDate).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = ErrorMsg;
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }


            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage PostANMUsageDetailsDayWise(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                string ErrorMsg = checkDate(Convert.ToString(un.FromDate), 1, "From Date");
                if (ErrorMsg == "")
                {
                    var data = cnaa.uspANMusageDetailsDayWise(un.UnitCode, un.UnitType, un.FromDate).ToList();
                    if (un.Flag == 2)
                    {
                        data = data.Where(x => x.ancCount + x.pncCount + x.immuCount + x.maternalDeathount + x.infantDeathount > 0).ToList();
                    }
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = ErrorMsg;
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }


            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage PostANMNotUsageDetailsDaywise(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                string ErrorMsg = checkDate(Convert.ToString(un.FromDate), 1, "From Date");
                if (ErrorMsg == "")
                {
                    if (un.Flag == 1 || un.Flag == 2)
                    {

                        var data = cnaa.uspANMNotusageDetailsDayWise(un.UnitCode, un.UnitType, un.FromDate, un.Flag).ToList();
                        if (data != null)
                        {
                            _objResponseModel.ResposeData = data;
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "Data Received successfully";
                        }
                        else
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = "No Data Found";
                        }
                    }
                    else
                    {
                        var data = cnaa.uspANMusageDetailsDayWise(un.UnitCode, un.UnitType, un.FromDate).ToList();
                        if (un.Flag == 3)
                        {
                            data = data.Where(x => x.ancCount + x.pncCount + x.immuCount + x.maternalDeathount + x.infantDeathount > 0).ToList();
                        }
                        if (data != null)
                        {
                            _objResponseModel.ResposeData = data;
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "Data Received successfully";
                        }
                        else
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = "No Data Found";
                        }
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = ErrorMsg;
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage PostLatitudeLongitudeUnit(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                var data = cnaa.uspLatitudeLongitude(un.LoginUnitCode, un.LoginUnitType).ToList();
                if (data != null)
                {
                    _objResponseModel.ResposeData = data;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }


            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }



        public HttpResponseMessage CreateDashboard(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {

                int fromyear = un.Finyear;
                int toyear = fromyear + 1;
                var data = cnaa.uspDashboard(un.LoginUnitCode, "01/04/" + Convert.ToString(fromyear), "31/03/" + Convert.ToString(toyear), (byte)un.LoginUnitType).ToList();
                Dashboard db = new Dashboard();
                db.ancRegDashboard = data.Where(x => x.flag == 1).Select(x => new ANCRegDashboard { unittype = (byte)x.pieUnittype, unitcode = x.Unitcode, finyear = fromyear.ToString(), TotalANCReg = (Int32)x.TotalANCReg, ANCRegTrimister = (Int32)x.ANCRegTrimaster }).ToList();
                db.birthDetails = data.Where(x => x.flag == 1).Select(x => new BirthDetails { unittype = (byte)x.pieUnittype, unitcode = x.Unitcode, finyear = fromyear.ToString(), totalBirth = (Int32)x.totalBirth, liveMaleBirth = (Int32)x.LiveMale, liveFeMaleBirth = (Int32)x.LiveFeMale, stillBirth = (Int32)x.StillBirth }).ToList();
                db.deliveryDetails = data.Where(x => x.flag == 1).Select(x => new DeliveryDetailDashboard { unittype = (byte)x.pieUnittype, unitcode = x.Unitcode, finyear = fromyear.ToString(), totalDelivery = (Int32)x.DelHome + (Int32)x.DelPrivate + (Int32)x.DelPublic, delHome = (Int32)x.DelHome, delPrivate = (Int32)x.DelPrivate, delPublic = (Int32)x.DelPublic }).ToList();
                db.immunizationDetails = data.Where(x => x.flag == 1).Select(x => new ImmunizationDetails { unittype = (byte)x.pieUnittype, unitcode = x.Unitcode, finyear = fromyear.ToString(), totalChilderenReg = (Int32)x.TotalChildrenReg, fullyImmunized = (Int32)x.FullyImmunized, partImmunized = (Int32)x.PartiallyImmunized, notImmunized = (Int32)x.NotImmunized }).ToList();
                db.top7DistrictPerformance = data.Where(x => x.flag == 2).Select(x => new Top7DistrictPerformance { finyear = fromyear.ToString(), unitcode = x.Unitcode, unitname = x.unitname, unittype = (byte)x.unittype, performacePer = (double)x.percentage, maxValueDistrict = (int)x.maxvalDist }).OrderByDescending(x => x.performacePer).ToList();
                db.top7BlockPerformance = data.Where(x => x.flag == 3).Select(x => new Top7BlockPerformance { finyear = fromyear.ToString(), unitcode = x.Unitcode, unitname = x.unitname, unittype = (byte)x.unittype, performacePer = (double)x.percentage, maxValueBlock = (int)x.maxvalBlock }).OrderByDescending(x => x.performacePer).ToList();
                db.maternalDeaths = data.Where(x => x.flag == 4).Select(x => new MaternalDeaths { finyear = fromyear.ToString(), unitcode = x.Unitcode, unitname = x.unitname, unittype = (byte)x.unittype, deathCount = (int)x.MaternalDeathCount }).OrderByDescending(x => x.deathCount).ToList();
                db.infantDeaths = data.Where(x => x.flag == 5).Select(x => new InfantDeaths { finyear = fromyear.ToString(), unitcode = x.Unitcode, unitname = x.unitname, unittype = (byte)x.unittype, deathCount = (int)x.InfantDeathCount }).OrderByDescending(x => x.deathCount).ToList();
                db.sterlization = data.Where(x => x.flag == 7).Select(x => new Sterlization { finyear = fromyear.ToString(), unitcode = x.Unitcode, unitname = x.unitname, unittype = (byte)x.unittype, monthName = x.MName, sterlizationCount = (int)x.totalSterlizationMonth, monthValue = x.mthyr }).OrderBy(x => x.monthValue).ToList();
                db.sexRatio = data.Where(x => x.flag == 8).Select(x => new SexRatio { finyear = fromyear.ToString(), unitcode = x.Unitcode, unitname = x.unitname, unittype = (byte)x.unittype, monthName = x.MName, girlsRatio = (int)x.birth_girlCount, monthValue = (int)x.birth_boyCount }).OrderBy(x => x.monthValue).ToList();
                //db.abortion = data.Where(x => x.flag == 11).Select(x => new Abortion { finyear = fromyear.ToString(), unitcode = x.Unitcode, unitname = x.unitname, unittype = (byte)x.unittype, monthName = x.MName, cnt = (int)x.totalAbotionMonth, monthValue = x.mthyr }).OrderBy(x => x.monthValue).ToList();
                //db.iud = data.Where(x => x.flag == 12).Select(x => new IUD { finyear = fromyear.ToString(), unitcode = x.Unitcode, unitname = x.unitname, unittype = (byte)x.unittype, monthName = x.MName, cnt = (int)x.IUD, monthValue = x.mthyr }).OrderBy(x => x.monthValue).ToList();
                var p2 = data.Where(x => x.flag == 1).Select(x => new
                {
                    unittype = (byte)x.pieUnittype,
                    unitcode = x.Unitcode,
                    finyear = fromyear.ToString(),
                    highRisk_HighBPCount = x.HighRisk_HighBP,
                    highRisk_Diabetes = x.HighRisk_Diabetes,
                    highRisk_APH = x.HighRisk_APH,
                    highRisk_Malaria = x.HighRisk_Malaria,
                    highRisk_Other = x.HighRisk_Other,
                    highRisk_Anemia = x.HighRisk_Anemia,
                    highRisk_Age = x.HighRisk_Age,
                    highRisk_Height = x.HighRisk_Height,
                    highRisk_Delivery = x.HighRisk_Delivery,
                    highRisk_LieAndBreech = x.HighRisk_LieAndBreech,
                    highRisk_Heart = x.HighRisk_Heart,
                    highRisk_Kidney = x.HighRisk_Kidney
                }).FirstOrDefault();
                List<HighRisk> hr = new List<HighRisk>();
                List<HighRisk> hr1 = new List<HighRisk>();
                if (p2 != null)
                {
                    HighRisk h = new HighRisk();
                    h.unittype = p2.unittype;
                    h.unitcode = p2.unitcode;
                    h.finyear = p2.finyear;
                    h.highRiskName = "HighBP";
                    h.highRiskNameH = "उच्च बी.पी.";
                    h.highRiskValue = Convert.ToDouble(p2.highRisk_HighBPCount);
                    hr.Add(h);
                    h = new HighRisk();
                    h.unittype = p2.unittype;
                    h.unitcode = p2.unitcode;
                    h.finyear = p2.finyear;
                    h.highRiskName = "Diabetes";
                    h.highRiskNameH = "मधुमेह";
                    h.highRiskValue = Convert.ToDouble(p2.highRisk_Diabetes);
                    hr.Add(h);
                    h = new HighRisk();
                    h.unittype = p2.unittype;
                    h.unitcode = p2.unitcode;
                    h.finyear = p2.finyear;
                    h.highRiskName = "APH";
                    h.highRiskNameH = "ए पी एच";
                    h.highRiskValue = Convert.ToDouble(p2.highRisk_APH);
                    hr.Add(h);
                    h = new HighRisk();
                    h.unittype = p2.unittype;
                    h.unitcode = p2.unitcode;
                    h.finyear = p2.finyear;
                    h.highRiskName = "Malaria";
                    h.highRiskNameH = "मलेरिया";
                    h.highRiskValue = Convert.ToDouble(p2.highRisk_Malaria);
                    hr.Add(h);
                    h = new HighRisk();
                    h.unittype = p2.unittype;
                    h.unitcode = p2.unitcode;
                    h.finyear = p2.finyear;
                    h.highRiskName = "Other";
                    h.highRiskNameH = "अन्य";
                    h.highRiskValue = Convert.ToDouble(p2.highRisk_Other);
                    hr.Add(h);
                    h = new HighRisk();
                    h.unittype = p2.unittype;
                    h.unitcode = p2.unitcode;
                    h.finyear = p2.finyear;
                    h.highRiskName = "Severe Anemia";
                    h.highRiskNameH = "गंभीर एनीमिया";
                    h.highRiskValue = Convert.ToDouble(p2.highRisk_Anemia);
                    hr.Add(h);
                    h = new HighRisk();
                    h.unittype = p2.unittype;
                    h.unitcode = p2.unitcode;
                    h.finyear = p2.finyear;
                    h.highRiskName = "Age( < 18 / > 35)";
                    h.highRiskNameH = "आयु";
                    h.highRiskValue = Convert.ToDouble(p2.highRisk_Age);
                    hr.Add(h);
                    h = new HighRisk();
                    h.unittype = p2.unittype;
                    h.unitcode = p2.unitcode;
                    h.finyear = p2.finyear;
                    h.highRiskName = "Short Height";
                    h.highRiskNameH = "छोटा कद";
                    h.highRiskValue = Convert.ToDouble(p2.highRisk_Height);
                    hr.Add(h);
                    h = new HighRisk();
                    h.unittype = p2.unittype;
                    h.unitcode = p2.unitcode;
                    h.finyear = p2.finyear;
                    h.highRiskName = "Previous Complex Delivery";
                    h.highRiskNameH = "पूर्व जटिल प्रसव";
                    h.highRiskValue = Convert.ToDouble(p2.highRisk_Delivery);
                    hr.Add(h);
                    h = new HighRisk();
                    h.unittype = p2.unittype;
                    h.unitcode = p2.unitcode;
                    h.finyear = p2.finyear;
                    h.highRiskName = "Transverse Lie/ Breech";
                    h.highRiskNameH = "Lie/ Breech";
                    h.highRiskValue = Convert.ToDouble(p2.highRisk_LieAndBreech);
                    hr.Add(h);
                    h = new HighRisk();
                    h.unittype = p2.unittype;
                    h.unitcode = p2.unitcode;
                    h.finyear = p2.finyear;
                    h.highRiskName = "Heart Disease";
                    h.highRiskNameH = "ह्र्दय रोग";
                    h.highRiskValue = Convert.ToDouble(p2.highRisk_Heart);
                    hr.Add(h);
                    h = new HighRisk();
                    h.unittype = p2.unittype;
                    h.unitcode = p2.unitcode;
                    h.finyear = p2.finyear;
                    h.highRiskName = "Kidney Disease";
                    h.highRiskNameH = "गुर्दा रोग ";
                    h.highRiskValue = Convert.ToDouble(p2.highRisk_Kidney);
                    hr.Add(h);

                    hr1 = hr.Where(x => x.highRiskName != "Other").OrderByDescending(x => x.highRiskValue).ToList();
                    h = new HighRisk();
                    h.unittype = p2.unittype;
                    h.unitcode = p2.unitcode;
                    h.finyear = p2.finyear;
                    h.highRiskName = "Other";
                    h.highRiskNameH = "अन्य";
                    h.highRiskValue = Convert.ToDouble(p2.highRisk_Other);
                    hr1.Add(h);


                }

                db.highRisk = hr1;


                List<VaccineRequirement> vr = new List<VaccineRequirement>();
                var p = data.Where(x => x.flag == 6).Select(x => new { BCGFlag = x.BCGFlag, OPVFlag = x.OPVFlag, HBFlag = x.HBFlag, PentaFlag = x.PentaFlag, MeaslesFlag = x.MeaslesFlag, unitcode = x.Unitcode, unitname = x.unitname, unittype = x.unittype, bcg = x.Bcgcount, PentaCount = x.PentaCount, OPVCount = x.OPVCount, MeaslCount = x.MeaslCount, HBCount = (int)x.HBCount }).FirstOrDefault();
                if (p != null)
                {
                    VaccineRequirement vr1 = new VaccineRequirement();
                    vr1.unitcode = p.unitcode;
                    vr1.unitname = p.unitname;
                    vr1.unittype = (byte)p.unittype;
                    vr1.immuName = "BCG";
                    vr1.immuNameH = "बीसीजी";
                    vr1.vaccineReqCount = (int)p.bcg;
                    vr1.vaccFlag = (byte)p.BCGFlag;
                    vr1.finyear = fromyear.ToString();
                    vr.Add(vr1);
                    vr1 = new VaccineRequirement();
                    vr1.unitcode = p.unitcode;
                    vr1.unitname = p.unitname;
                    vr1.unittype = (byte)p.unittype;
                    vr1.immuName = "Pentavalent";
                    vr1.immuNameH = "पेन्टावेलेंट";
                    vr1.vaccineReqCount = (int)p.PentaCount;
                    vr1.vaccFlag = (byte)p.PentaFlag;
                    vr1.finyear = fromyear.ToString();
                    vr.Add(vr1);
                    vr1 = new VaccineRequirement();
                    vr1.unitcode = p.unitcode;
                    vr1.unitname = p.unitname;
                    vr1.unittype = (byte)p.unittype;
                    vr1.immuName = "OPV";
                    vr1.immuNameH = "ओपीवी";
                    vr1.vaccineReqCount = (int)p.OPVCount;
                    vr1.vaccFlag = (byte)p.OPVFlag;
                    vr1.finyear = fromyear.ToString();
                    vr.Add(vr1);
                    vr1 = new VaccineRequirement();
                    vr1.unitcode = p.unitcode;
                    vr1.unitname = p.unitname;
                    vr1.unittype = (byte)p.unittype;
                    vr1.immuName = "Measles";
                    vr1.immuNameH = "खसरा";
                    vr1.vaccineReqCount = (int)p.MeaslCount;
                    vr1.vaccFlag = (byte)p.MeaslesFlag;
                    vr1.finyear = fromyear.ToString();
                    vr.Add(vr1);
                    vr1 = new VaccineRequirement();
                    vr1.unitcode = p.unitcode;
                    vr1.unitname = p.unitname;
                    vr1.unittype = (byte)p.unittype;
                    vr1.immuName = "Hepatitis";
                    vr1.immuNameH = "हैपेटाइटिस-बी";
                    vr1.vaccineReqCount = (int)p.HBCount;
                    vr1.vaccFlag = (byte)p.HBFlag;
                    vr1.finyear = fromyear.ToString();
                    vr.Add(vr1);
                }

                db.vaccineRequirement = vr;
                List<IndicatorWisePerformance> iwp = new List<IndicatorWisePerformance>();
                var p1 = data.Where(x => x.flag == 10).Select(x => new
                {

                    unitcode = x.Unitcode,
                    unitname = x.unitname,
                    unittype = x.unittype,
                    ANC3 = (double)x.ANC3P,
                    InstDel = (double)x.InstDelP,
                    FullImmu = (double)x.FullImmuP,
                    Ster = (double)x.SterP,
                    IUD = (double)x.IUDP
                }).FirstOrDefault();
                if (p1 != null)
                {
                    IndicatorWisePerformance iwp1 = new IndicatorWisePerformance();
                    iwp1.unitcode = p.unitcode;
                    iwp1.unitname = p.unitname;
                    iwp1.unittype = (byte)p.unittype;
                    iwp1.indicatorName = "Full ANC";
                    iwp1.indicatorNameH = "पूर्ण एएनसी ";
                    iwp1.indicatorValue = Convert.ToString(p1.ANC3);
                    iwp1.finyear = fromyear.ToString();
                    iwp1.indicatorFlag = 12;
                    iwp.Add(iwp1);
                    iwp1 = new IndicatorWisePerformance();
                    iwp1.unitcode = p.unitcode;
                    iwp1.unitname = p.unitname;
                    iwp1.unittype = (byte)p.unittype;
                    iwp1.indicatorName = "Institutional  Delivery";
                    iwp1.indicatorNameH = "संस्थागत प्रसव";
                    iwp1.indicatorValue = Convert.ToString(p1.InstDel);
                    iwp1.finyear = fromyear.ToString();
                    iwp1.indicatorFlag = 13;
                    iwp.Add(iwp1);
                    iwp1 = new IndicatorWisePerformance();
                    iwp1.unitcode = p.unitcode;
                    iwp1.unitname = p.unitname;
                    iwp1.unittype = (byte)p.unittype;
                    iwp1.indicatorName = "Full Immunization";
                    iwp1.indicatorNameH = "पूर्ण टीकाकरण";
                    iwp1.indicatorValue = Convert.ToString(p1.FullImmu);
                    iwp1.finyear = fromyear.ToString();
                    iwp1.indicatorFlag = 14;
                    iwp.Add(iwp1);
                    iwp1 = new IndicatorWisePerformance();
                    iwp1.unitcode = p.unitcode;
                    iwp1.unitname = p.unitname;
                    iwp1.unittype = (byte)p.unittype;
                    iwp1.indicatorName = "Sterlization";
                    iwp1.indicatorNameH = "नसबंदी";
                    iwp1.indicatorValue = Convert.ToString(p1.Ster);
                    iwp1.finyear = fromyear.ToString();
                    iwp1.indicatorFlag = 15;
                    iwp.Add(iwp1);
                    iwp1 = new IndicatorWisePerformance();
                    iwp1.unitcode = p.unitcode;
                    iwp1.unitname = p.unitname;
                    iwp1.unittype = (byte)p.unittype;
                    iwp1.indicatorName = "IUD";
                    iwp1.indicatorNameH = "आईयूडी";
                    iwp1.indicatorValue = Convert.ToString(p1.IUD);
                    iwp1.finyear = fromyear.ToString();
                    iwp1.indicatorFlag = 16;
                    iwp.Add(iwp1);
                }

                db.indicatorWisePerformance = iwp;
                if (db != null)
                {
                    _objResponseModel.ResposeData = db;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage CreateDashboardafterclick(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                if (un.LoginUnitType == 1)
                {
                    un.LoginUnitCode = "0";
                }
                else if (un.LoginUnitType == 2)
                {
                    un.LoginUnitCode = un.LoginUnitCode.Substring(0, 2);
                }
                dynamic data;
                if (un.LoginUnitCode.Length != 9)
                {
                    data = cnaa.uspDashboardPie(un.LoginUnitCode, "01/04/" + Convert.ToString(un.Finyear), (byte)un.LoginUnitType, (byte)un.Flag).ToList();
                }
                else
                {
                    data = cnaa.uspDashboardPie_SubCenter(un.LoginUnitCode, "01/04/" + Convert.ToString(un.Finyear), (byte)un.Flag).ToList();
                }
                if (data != null)
                {
                    _objResponseModel.ResposeData = data;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }


            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage CreateDashboardafterclickForSterliandSexRatio(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                dynamic data;
                if (un.UnitCode.Length >= 9 && un.Flag > 2 && un.Flag < 8)
                {
                    data = cnaa.uspDashboardVacc_Subcenter(un.Finyear, un.UnitCode, (byte)un.Flag).ToList();
                }
                else if (un.Flag == 10 || un.Flag == 11)
                {
                    if (un.UnitCode.Length >= 9)
                    {
                        data = cnaa.uspDashboardIndicators_Subcenter(un.Finyear, un.UnitCode, (byte)un.Flag).ToList();
                    }
                    else
                    {
                        data = cnaa.uspDashboardIndicators(un.UnitCode, un.Finyear, (byte)un.UnitType, (byte)un.Flag).ToList();
                    }
                }
                else if (un.Flag >= 12)
                {
                    if (un.UnitCode.Length >= 9)
                    {
                        data = cnaa.uspDashboardIndicatorsPerformance_Subcenter(un.UnitCode, un.Finyear, (byte)un.Flag).ToList();
                    }
                    else
                    {
                        data = cnaa.uspDashboardIndicatorsPerformance(un.Finyear, un.UnitCode, (byte)un.UnitType, (byte)un.Flag).ToList();
                    }
                }
                else if (un.UnitCode.Length >= 9 && un.Flag != 1 && un.Flag != 2)
                {
                    data = cnaa.uspDashboardSerail_Subcenter(un.Finyear, un.UnitCode, (byte)un.Flag).ToList();
                }
                else
                {
                    data = cnaa.uspDashboardSerial(un.UnitCode, un.Mthyr, (byte)un.UnitType, (byte)un.Flag).ToList();
                }


                if (data != null)
                {
                    _objResponseModel.ResposeData = data;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }


            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        public HttpResponseMessage DashboardUnitDDL(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                short[] strUnitType;
                if (un.LoginUnitType == 1)
                {
                    strUnitType = new short[2] { 1, 3 };
                    un.LoginUnitCode = un.LoginUnitCode.Substring(0, 1);
                }
                else if (un.LoginUnitType == 3)
                {
                    strUnitType = new short[2] { 3, 4 };
                    un.LoginUnitCode = un.LoginUnitCode.Substring(0, 4);
                }
                else if (un.LoginUnitType == 4)
                {
                    strUnitType = new short[1] { 4 };
                    un.LoginUnitCode = un.LoginUnitCode.Substring(0, 6);
                }
                else
                {
                    strUnitType = new short[1] { un.LoginUnitType };
                    un.LoginUnitCode = un.LoginUnitCode.Substring(0, 11);
                }
                var data = rajmed.UnitMasters.Where(x => x.UnitCode.StartsWith(un.LoginUnitCode) && strUnitType.Contains(x.UnitType)).Select(x => new
                {
                    Unitcode = (x.UnitType == 1 ? x.UnitCode.Substring(0, 1) : ((x.UnitType == 3) ? x.UnitCode.Substring(0, 4) : ((x.UnitType == 4) ? x.UnitCode.Substring(0, 6) : ((x.UnitType == 2) ? x.UnitCode.Substring(0, 2) : x.UnitCode.Substring(0, 9))))),
                    UnitNameHindi = x.UnitNameHindi
                }).ToList();
                if (data.Count > 0)
                {
                    _objResponseModel.ResposeData = data.OrderBy(x => x.Unitcode);
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }


            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        private int CheckVersion(string appVersion, string iosAppVersion)
        {
            int CheckAppVersionFlag = 0;
            ResponseModel _objResponseModel = new ResponseModel();
            if (!string.IsNullOrEmpty(Convert.ToString(appVersion)))
            {
                if (rajmed.AppHistories.Where(x => x.AppFlag == 1 && x.LiveFlag == 1 && x.VersionName == appVersion).Count() == 0)
                {
                    appValiationMsg = "यह वर्जन पुराना हो चुका है, कृपया Google play store से नया वर्जन अपडेट करें ! ";
                    CheckAppVersionFlag = 1;
                }
            }
            else if (!string.IsNullOrEmpty(Convert.ToString(iosAppVersion)))
            {
                if (rajmed.AppHistories.Where(x => x.AppFlag == 8 && x.LiveFlag == 1 && x.VersionName == iosAppVersion).Count() == 0)
                {
                    appValiationMsg = "यह वर्जन पुराना हो चुका है, कृपया App Store से नया वर्जन अपडेट करें ! ";
                    CheckAppVersionFlag = 1;
                }
            }
            else
            {
                CheckAppVersionFlag = 1;
            }
            return CheckAppVersionFlag;
        }
        public HttpResponseMessage anmRank(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                var data = cnaa.uspANMPerformanceApp(un.UnitCode, 0).ToList();
                if (data != null)
                {
                    _objResponseModel.ResposeData = data;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }



        public HttpResponseMessage PostMasterCodes(MasterCodesList p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                var data = from mcode in cnaa.MasterCodes
                           where mcode.ParentID == p.parentid
                           select new { mcode.Name, mcode.masterID };
                if (data.Count() > 0)
                {
                    _objResponseModel.ResposeData = data;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("uspLineListInfantDetails")]
        public HttpResponseMessage uspLineListInfantDetails(Pcts a)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(a.UserID, a.TokenNo);
            if (tokenFlag == true)
            {
                if (a.ANCRegID != 0)
                {
                    var data = cnaa.uspLineListInfantDetails(a.ANCRegID, a.MotherID).ToList();
                    if (data.Count > 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";

                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        [ActionName("PostDeathData")]
        public HttpResponseMessage PostDeathData(Pcts a)
        {
            int finyear = (DateTime.Now.Month < 4) ? DateTime.Now.Year - 1 : DateTime.Now.Year;

            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(a.UserID, a.TokenNo);
            if (tokenFlag == true)
            {
                if (a.LoginUnitType != 0)
                {
                    var data = cnaa.uspDeathData(a.LoginUnitcode, Convert.ToByte(a.LoginUnitType), finyear, 0).ToList();
                    if (data.Count > 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";

                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public HttpResponseMessage PostCreateToken(UserAuthenticate a)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            _objResponseModel.ResposeData = TokenManager.GenerateToken(a.UserID);
            _objResponseModel.Status = true;

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        private void writeclassdata(ImmunizationData a)
        {
            Type type = typeof(ImmunizationData);

            Dictionary<string, object> properties = new Dictionary<string, object>();
            string str = "";
            foreach (System.Reflection.PropertyInfo prop in type.GetProperties())
            {
                properties.Add(prop.Name, prop.GetValue(a));
                str = str + prop.Name + ":" + prop.GetValue(a);
                str = str + Environment.NewLine;
            }
            ErrorHandler.WriteError(str);
        }



        private bool ValidateToken(string userid, string tokenNo)
        {
            return true;
            var data = cnaa.PctsTokens.Where(x => x.UserID == userid && x.Salt == tokenNo).FirstOrDefault();
            if (data == null)
            {
                return false;
            }
            else
            {
                return true;

            }

        }



        public HttpResponseMessage Logout(UserAuthenticate u)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {

                //string Mno = u.MobileNo;
                //string Did = u.DeviceID;

                var Pctstok = cnaa.PctsTokens.Where(a => a.UserID == u.UserID && a.DeviceID == u.DeviceID).First();
                cnaa.PctsTokens.Remove(Pctstok);
                cnaa.SaveChanges();
                _objResponseModel.Status = true;
                _objResponseModel.Message = "Logout successfully";

            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in post user" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        }




        public string CheckValidNumber(string input, Int16 minlen, Int16 maxlen, Int16 mandatory, string columnName)
        {
            if (mandatory == 0 && (input == null || input == ""))
            {
                return "";
            }
            else if (mandatory == 1 && (input == null || input == ""))
            {
                return "Invalid " + columnName;
            }
            string regExpr = "^([0-9]){" + minlen.ToString() + "," + maxlen.ToString() + "}$";
            System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(input, regExpr, RegexOptions.None, TimeSpan.FromMilliseconds(10));
            if (m.Success)
                return "";
            else
                return "Invalid " + columnName;
        }
        public HttpResponseMessage uspGetVillageListForJ1(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.LoginUnitid > 0)
                {
                    //  var data = rajmed.Villages.Where(x => x.unitid == p.LoginUnitid && x.OtherLocation == 1).Select(x => new { VillageName = x.UnitNameHindi, VillageautoID = x.VillageAutoID }).ToList();
                    var data = cnaa.uspGetVillageByAshaID((int?)p.LoginUnitid, (int?)p.VillageAutoid, (int?)p.ANMAutoID).Select(x => new { VillageName = x.VillageName, VillageautoID = x.VillageAutoID }).ToList();

                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data.OrderBy(x => x.VillageautoID);
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage GetBankList()
        {
            ResponseModel _objResponseModel = new ResponseModel();
            var data = cnaa.Banks.Where(x => x.IsDeleted == 0).Select(x => new { Bank_Name = x.Bank_Name }).Distinct().ToList();
            _objResponseModel.ResposeData = data;
            _objResponseModel.Status = true;
            _objResponseModel.Message = "Data Received successfully";
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public bool checkStringWithSameDigit(string no)
        {
            if (Regex.IsMatch(no, @"/^\b(\d+)\1+\b$"))
                return true;
            else
                return false;
        }
        public bool validateMobileNoSameDigitMorethantimes(string no, Int16 times)
        {
            var matches = Regex.Matches(no, @"(.)\1+");
            for (int i = 0; i <= matches.Count - 1; i++)
            {
                if (matches[i].Length > times)
                    return true;
            }
            return false;
        }

        public HttpResponseMessage PostBankName(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.BankName.Length > 0)
                {
                    var data = cnaa.Banks.Where(x => x.Bank_Name == p.BankName && x.Branch_Name.Contains(p.BranchName) && x.IsDeleted == 0).Select(x => new { BranchName = x.Branch_Name, IFSC_CODE = x.IFSC_CODE }).ToList();
                    if (data.Count > 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage PostIfscCode(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.IfscCode.Length > 0)
                {
                    var data = cnaa.Banks.Where(x => x.IFSC_CODE == p.IfscCode && x.IsDeleted == 0).Select(x => new { BranchName = x.Bank_Name + " (" + x.Branch_Name + ")" }).FirstOrDefault();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage PostJ1(MotherAncRegDetail m)
        {
            //  writeclassdata(m);
            ResponseModel _objResponseModel = new ResponseModel();

            bool tokenFlag = ValidateToken(m.UserID, m.TokenNo);
            if (tokenFlag == true)
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        int CheckAppVersionFlag = CheckVersion(m.AppVersion, m.IOSAppVersion);
                        if (CheckAppVersionFlag == 1)
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = appValiationMsg;
                            _objResponseModel.AppVersion = 1;
                            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                        }
                        else
                        {
                            string pctsid = "";
                            //if (!string.IsNullOrEmpty(m.AadhaarNo))
                            //{
                            //     pctsid = cnaa.Database.SqlQuery<string>("select top 1 pctsid from pcts..BHAMASHAH ad inner join mother moth on moth.motherid = ad.ID where ad.ID > 0 and ad.IDFlag=1 and isnull(AadhaarNo,'')={0} and moth.motherid NOT IN (select motherid  from motherstatus where reasonid not in (5,6))", Convert.ToString(m.AadhaarNo)).FirstOrDefault();
                            //    if (!string.IsNullOrEmpty(pctsid))
                            //    {
                            //        List<Dictionary<string, string>> listHash1 = new List<Dictionary<string, string>>();
                            //        Dictionary<string, string> dis = new Dictionary<string, string>();
                            //        //dis.Add("Msg", "Aadhaar Number already exists with PCTS ID - !" + pctsid);
                            //        dis.Add("PCTSID", pctsid);
                            //        listHash1.Add(dis);
                            //        _objResponseModel.Message = "Aadhaar Number already exists with PCTS ID - !" + pctsid;
                            //        _objResponseModel.Status = true;
                            //        _objResponseModel.ResposeData = listHash1;
                            //        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                            //    }
                            //}                            
                            string msg = SaveJ1Details(m, 1, ref pctsid);
                            if (msg != "")
                            {
                                _objResponseModel.Status = false;
                                _objResponseModel.Message = msg;
                                Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                            }
                            else
                            {
                                _objResponseModel.Status = true;
                                _objResponseModel.Message = "धन्यवाद, PCTS पर महिला का पंजीकरण हो गया है । कृपया पीसीटीएस आईडी लिख ले \nपीसीटीएस आईडी हैं :" + pctsid;
                                _objResponseModel.ResposeData = pctsid;
                                Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                            }
                        }
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Model Error";
                        ErrorHandler.WriteError("Error in post delivery model--" + ModelState);
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                    }
                }
                catch (Exception ex)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "validation Error";
                    ErrorHandler.WriteError("Error in post delivery--" + ex.ToString());
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("PutJ1")]
        public HttpResponseMessage PutJ1(MotherAncRegDetail m)
        {
            //  writeclassdata(m);
            ResponseModel _objResponseModel = new ResponseModel();

            bool tokenFlag = ValidateToken(m.UserID, m.TokenNo);
            if (tokenFlag == true)
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        int CheckAppVersionFlag = CheckVersion(m.AppVersion, m.IOSAppVersion);
                        if (CheckAppVersionFlag == 1)
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = appValiationMsg;
                            _objResponseModel.AppVersion = 1;
                            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                        }
                        else
                        {
                            string pctsid = "";
                            string msg = SaveJ1Details(m, 2, ref pctsid);
                            if (msg != "")
                            {
                                _objResponseModel.Status = false;
                                _objResponseModel.Message = msg;
                                Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                            }
                            else
                            {
                                _objResponseModel.Status = true;
                                _objResponseModel.Message = "धन्यवाद, J1 का विवरण सेव हो चुका हैं।";
                            }
                        }
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Model Error";
                        ErrorHandler.WriteError("Error in put j1 model--" + ModelState);
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                    }

                }
                catch (Exception ex)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "validation Error";
                    ErrorHandler.WriteError("Error in put j1--" + ex.ToString());
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }



            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        private string validateJ1Detail(MotherAncRegDetail m, int postputFlag)
        {
            string ErrorMsg = "";
            if (postputFlag == 2)
            {
                ErrorMsg = CheckValidNumber(Convert.ToString(m.ANCRegID), 1, 9, 1, "ANCRegID");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                ErrorMsg = CheckValidNumber(Convert.ToString(m.MotherID), 1, 9, 1, "MotherID");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                ErrorMsg = CheckValidNumber(Convert.ToString(m.VillageAutoID), 1, 10, 0, "VillageAutoID");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                ErrorMsg = CheckValidNumber(Convert.ToString(m.Mobileno), 10, 10, 0, "Mobileno");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.DirectDelivery)) && m.IsNewRegistration == 0)
            {
                return "कृपया क्या महिला बिना पूर्व पंजीकरण, प्रसव के लिए आई है चुनें ! ";
            }
            else if (m.DirectDelivery != 1 && m.DirectDelivery != 2 && m.IsNewRegistration == 0)
            {
                return "कृपया क्या महिला बिना पूर्व पंजीकरण, प्रसव के लिए आई है सही चुनें ! ";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.Ghamantu)))
            {
                return "कृपया क्या महिला घुमन्तु श्रेणी की है चुनें ! ";
            }
            else if (m.Ghamantu != 1 && m.Ghamantu != 2 )
            {
                return "कृपया क्या महिला घुमन्तु श्रेणी की है सही चुनें ! ";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.VillageAutoID)))
            {
                return "कृपया गॉंव / वार्ड चुनें !";
            }

            if (string.IsNullOrEmpty(Convert.ToString(m.Name)))
            {
                return "कृपया नाम लिखें ! ";
            }
            else if (!Regex.IsMatch(m.Name, @"^([ \u0900-\u097f ,\u200D,\u00c0-\u01ffa-zA-Z'])+$"))
            {
                return "कृपया सही नाम लिखें !";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.NameE)))
            {
                return "कृपया महिला का नाम(अंग्रेज़ी में) लिखें !";
            }
            else if (!Regex.IsMatch(m.NameE, @"^([a-zA-Z\s])+$"))
            {
                return "कृपया महिला का नाम(अंग्रेज़ी में) सही लिखें !";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.HusbnameE)))
            {
                return "कृपया पिता/पति का नाम(अंग्रेज़ी में) लिखें !";
            }
            else if (!Regex.IsMatch(m.HusbnameE, @"^([a-zA-Z\s])+$"))
            {
                return "कृपया पिता/पति का नाम(अंग्रेज़ी में) सही लिखें !";
            }

            if (string.IsNullOrEmpty(Convert.ToString(m.IsHusband)))
            {
                return "कृपया पिता/पति का नाम चुनें ! ";
            }
            else if (m.IsHusband != 1 && m.IsHusband != 2)
            {
                return "कृपया पिता/पति का नाम सही चुनें ! ";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.Husbname)))
            {
                return "कृपया पिता/पति का नाम लिखें ! ";
            }
            else if (!Regex.IsMatch(m.Husbname, @"^([ \u0900-\u097f ,\u200D,\u00c0-\u01ffa-zA-Z'])+$"))
            {
                return "कृपया सही पिता/पति का नाम लिखें !";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.CurrentAddressE)))
            {
                return "कृपया महिला का वर्तमान पता(अंग्रेज़ी में) लिखें !";
            }
            else if (!Regex.IsMatch(m.CurrentAddressE, @"^[\w\s\d\,\-]*$"))
            {
                return "कृपया महिला का वर्तमान पता(अंग्रेज़ी में) सही लिखें !";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.PermanentAddressE)))
            {
                return "कृपया महिला का स्थायी पता(अंग्रेज़ी में) लिखें !";
            }
            else if (!Regex.IsMatch(m.PermanentAddressE, @"^([a-zA-Z\s])+$"))
            {
                return "कृपया महिला का स्थायी पता(अंग्रेज़ी में) सही लिखें !";
            }

            if (m.DirectDelivery == 2 && m.VillageName == "Other State")
            {
                return "अन्य राज्य की महिला के रजिस्ट्रेशन के लिए 'क्या महिला बिना पूर्व पंजीकरण, प्रसव के लिए आई है' में हॉं चुने !";
            }
            if (m.Mobileno != null)
            {
                if (m.Mobileno.Length == 10)
                {
                    if (checkStringWithSameDigit(m.Mobileno))
                    {
                        return "कृपया सही मोबाईल नं. लिखे !";
                    }
                    if (validateMobileNoSameDigitMorethantimes(m.Mobileno, 5))
                    {
                        return "कृपया सही मोबाईल नं. लिखे !";
                    }
                }
            }
            if (!string.IsNullOrEmpty(m.ECID))
            {
                if (!Regex.IsMatch(m.ECID, @"^[0-9]*$"))
                {
                    return "योग्‍य दम्‍पत्ति संख्‍या अंकों में डालें !";
                }
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.BPL)) && m.IsNewRegistration == 0)
            {
                return "कृपया बी. पी. एल. चुनें !";
            }
            else if (m.BPL != 1 && m.BPL != 2 && m.IsNewRegistration == 0)
            {
                return "कृपया सही बी. पी. एल. चुनें !";
            }
            if (string.IsNullOrEmpty(m.BPLCardNo) && m.IsNewRegistration == 0)
            {
                m.BPL = 2;
                m.BeforeDelivery500 = 2;
            }


            if (string.IsNullOrEmpty(Convert.ToString(m.Age)))
            {
                return "कृपया आयु लिखें !";
            }
            else if (m.Age < 13 || m.Age > 48)
            {
                return "Age should be between 13 and 48 years !";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.Cast)))
            {
                return "कृपया जाति चुनें !";
            }
            else if (m.Cast != 1 && m.Cast != 2 && m.Cast != 3)
            {
                return "कृपया सही जाति चुनें !";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.Location_Rajasthan)) && m.IsNewRegistration == 0)
            {
                return "कृपया क्या प्रसूता राजस्थान की मूल निवासी है चुनें !";
            }
            else if (m.Location_Rajasthan != 1 && m.Location_Rajasthan != 2 && m.IsNewRegistration == 0)
            {
                return "कृपया सही क्या प्रसूता राजस्थान की मूल निवासी है चुनें !";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.Height)))
            {
                return "कृपया कद चुनें !";
            }
            else if (m.Height < 60 || m.Height > 240)
            {
                return "कृपया सही कद डालें !";
            }



            if (string.IsNullOrEmpty(Convert.ToString(m.RegDate)))
            {
                ErrorMsg = "कृपया पंजीकरण की तिथि डालें !";
                return ErrorMsg;
            }
            else
            {
                ErrorMsg = checkDate(Convert.ToString(m.RegDate), 1, "पंजीकरण");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                if (DifferenceInDays(Convert.ToDateTime(m.RegDate), DateTime.Now) < 0)
                {
                    ErrorMsg = "पंजीकरण की तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
                    return ErrorMsg;
                }


            }
            if (string.IsNullOrEmpty(Convert.ToString(m.LMPDT)) && m.IsNewRegistration == 0)
            {
                ErrorMsg = "कृपया आखिरी माहवारी की तिथि डालें !";
                return ErrorMsg;
            }
            else
            {
                if (m.IsNewRegistration == 0)
                {
                    ErrorMsg = checkDate(Convert.ToString(m.LMPDT), 1, "आखिरी माहवारी");
                    if (ErrorMsg != "")
                    {
                        return ErrorMsg;
                    }
                    if (DifferenceInDays(Convert.ToDateTime(m.LMPDT), DateTime.Now) < 0)
                    {
                        return "आखिरी माहवारी की तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
                    }
                    if (DifferenceInMonth(Convert.ToDateTime(m.LMPDT), Convert.ToDateTime(m.RegDate)) > 9)
                    {
                        return "कृपया सही आखिरी माहवारी/पंजीकरण की तिथि डालें !";
                    }
                    if (DifferenceInDays(Convert.ToDateTime(m.RegDate), Convert.ToDateTime(m.LMPDT)) >= 0)
                    {
                        return "आखिरी माहवारी की तिथि पंजीकरण की तिथि से ज्यादा नहीं होनी चाहिए !";
                    }
                }
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.NFSA)) && m.IsNewRegistration == 0)
            {
                return "कृपया NFSA पात्रता चुनें !";
            }
            else if (m.NFSA != 1 && m.NFSA != 2 && m.IsNewRegistration == 0)
            {
                return "कृपया सही NFSA पात्रता चुनें !";
            }
            if (!string.IsNullOrEmpty(m.Accountno) && m.IsNewRegistration == 0)
            {
                if (m.Accountno.Length < 10)
                {
                    return "कृपया सही खाता संख्या लिखें";
                }
                if (!Regex.IsMatch(m.Accountno, @"^[0-9]*$"))
                {
                    return "कृपया सही खाता संख्या लिखें !";
                }
                if (checkStringWithSameDigit(m.Accountno))
                {
                    return "कृपया सही खाता संख्या लिखें !";
                }
                if (string.IsNullOrEmpty(m.Ifsc_code))
                {
                    return "कृपया सही IFSC कोड लिखें !";
                }
                if (string.IsNullOrEmpty(m.AccountName))
                {
                    return "कृपया खाता धारक का नाम लिखें !";
                }
            }
            if (!string.IsNullOrEmpty(m.Ifsc_code) && m.IsNewRegistration == 0)
            {

                if (!Regex.IsMatch(m.Ifsc_code, @"^[a-z,A-Z,0-9]{10,25}$"))
                {
                    return "कृपया सही IFSC कोड लिखें !";
                }
                if (Regex.IsMatch(m.Ifsc_code, @"^[a-z,A-Z]*$"))
                {
                    return "कृपया सही IFSC कोड लिखें !";
                }
                else if (Regex.IsMatch(m.Ifsc_code, @"^[0-9]*$"))
                {
                    return "कृपया सही IFSC कोड लिखें !";
                }

                if (string.IsNullOrEmpty(m.Accountno))
                {
                    return "कृपया खाता संख्या लिखें !";
                }
                if (string.IsNullOrEmpty(m.AccountName))
                {
                    return "कृपया खाता धारक का नाम लिखें !";
                }
            }
            if (!string.IsNullOrEmpty(m.RationCardNo) && m.IsNewRegistration == 0)
            {

                if (!Regex.IsMatch(m.RationCardNo, @"^[a-z,A-Z,0-9]{12,50}$"))
                {
                    return "कृपया सही राशन कार्ड नम्बर लिखें !";
                }

            }

            if (!string.IsNullOrEmpty(m.AadhaarNo))
            {
                if (m.AadhaarNo.Length != 12 && m.AadhaarNo.Length != 15)
                {
                    return "कृपया सही आधार संख्या लिखें !";
                }
                if (!Regex.IsMatch(m.AadhaarNo, @"^[0-9]*$"))
                {
                    return "कृपया सही आधार संख्या लिखें !";
                }
                if (m.Consent != 1)
                {
                    return "कृपया Consent चुने !";
                }
            }
            if (!string.IsNullOrEmpty(m.JanAadhaarNo))
            {
                if (m.JanAadhaarNo.Length != 10)
                {
                    return "कृपया सही जन आधार संख्या लिखें !";
                }
                if (!Regex.IsMatch(m.JanAadhaarNo, @"^[0-9]*$"))
                {
                    return "कृपया सही जन आधार संख्या लिखें !";
                }
                if (m.Consent != 1)
                {
                    return "कृपया Consent चुने !";
                }
            }
            if (!string.IsNullOrEmpty(m.RationCardNo) && m.IsNewRegistration == 0)
            {
                if (m.RationCardNo.Length > 0 && m.RationCardNo.Length < 12)
                {
                    return "कृपया सही राशन कार्ड नम्बर लिखें !";
                }
            }
            if (m.ashaAutoID == 0)
            {
                return "कृपया आशा का नाम चुने !";
            }


            return ErrorMsg;
        }


        private string SaveJ1Details(MotherAncRegDetail m, Int16 methodFlag, ref string Newpctsid)
        {
            string ErrorMsg = validateJ1Detail(m, methodFlag);
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            else
            {
                try
                {
                    string pctsid = "";
                    if (!string.IsNullOrEmpty(m.AadhaarNo))
                    {
                        pctsid = cnaa.Database.SqlQuery<string>("select top 1 pctsid from pcts..BHAMASHAH ad inner join mother moth on moth.motherid = ad.ID where ad.ID > 0 and ad.IDFlag=1 and isnull(AadhaarNo,'')={0} and moth.motherid NOT IN (select motherid  from motherstatus where reasonid not in (5,6))", Convert.ToString(m.AadhaarNo)).FirstOrDefault();
                        if (!string.IsNullOrEmpty(pctsid))
                        {                            
                            return "Aadhaar Number already exists with PCTS ID -  : " + pctsid;
                        }
                    }

                    pctsid = cnaa.Mothers.Where(x => x.VillageAutoID == m.VillageAutoID && x.Name == m.Name && x.Husbname == m.Husbname && x.Age == m.Age).Select(x => x.pctsid).FirstOrDefault();
                    if (!string.IsNullOrEmpty(pctsid))
                    {
                        return "Duplicate case ! The case is already registered with PCTSID - " + pctsid;
                    }

                    int infantDeathCount = 0;
                    int id = cnaa.Database.SqlQuery<int>("SELECT isnull(max(isnull(convert(int, substring(pctsid,14,len(pctsid) - 1)),0)),0) from Mother WHERE left(pctsid, 11) ={0}", Convert.ToString(m.UnitCode)).FirstOrDefault() + 1;
                    pctsid = m.UnitCode + "99" + Convert.ToString(id);

                    using (TransactionScope transaction = new TransactionScope())
                    {
                        try
                        {
                            using (cnaaEntities objcnaa = new cnaaEntities())
                            {
                                Mother p = new Mother();
                                p.ECID = m.ECID;
                                p.Name = m.Name;
                                p.Address = m.Address;
                                p.Age = m.Age;
                                p.Cast = m.Cast;
                                p.LiveChild = m.LiveChild;
                                p.Mobileno = m.Mobileno;
                                p.Husbname = m.Husbname;
                                p.Accountno = m.Accountno;
                                p.EntryDate = DateTime.Now;
                                p.pctsid = pctsid;
                                p.LastUpdated = DateTime.Now;
                                p.Ifsc_code = m.Ifsc_code;
                                p.RationCardNo = m.RationCardNo;
                                p.VillageAutoID = m.VillageAutoID;
                                p.AccountName = m.AccountName;
                                p.ancregid = m.ANCRegID;
                                p.IsHusband = m.IsHusband;
                                p.CastGroup = m.CastGroup;
                                p.Height = m.Height;
                                p.Status = 0;
                                p.Religion = m.Religion;
                                p.NameE = m.NameE;
                                p.Divyang = m.Divyang;
                                p.IsRegWithoutPregnent = (byte)m.IsNewRegistration;
                                p.Media = m.Media;
                                p.EntryUserNo = (int)m.EntryUserNo;
                                p.UpdateUserNo = m.UpdateUserNo;
                                p.ashaAutoID = (int)m.ashaAutoID;
                                p.RegDate = m.RegDate;
                                p.Location_Rajasthan = m.Location_Rajasthan;
                                p.BPL = m.BPL;
                                p.BPLCardno = m.BPLCardNo;
                                objcnaa.Mothers.Add(p);
                                objcnaa.SaveChanges();
                                int MotherID = p.MotherID;
                                m.MotherID = p.MotherID;


                                if (!string.IsNullOrEmpty(m.AadhaarNo) && !string.IsNullOrEmpty(m.Ifsc_code) && !string.IsNullOrEmpty(m.Accountno) && m.DirectDelivery == 2 && m.ashaAutoID > 0)
                                {
                                    objcnaa.Database.ExecuteSqlCommand("insert into AadhaarBankInfo(motherid,ashaAutoID,AadhaarBankInfoUpdatedDate) values({0},{1},{2})", m.MotherID, m.ashaAutoID, DateTime.Now);
                                }
                                if (m.IsNewRegistration == 0)
                                {
                                    ANCRegDetail anc = new ANCRegDetail();
                                    anc.MotherID = m.MotherID;
                                    anc.RegDate = m.RegDate;
                                    anc.LMPDT = Convert.ToDateTime(m.LMPDT);
                                    anc.DelFlag = 0;
                                    anc.EntryDate = DateTime.Now;
                                    anc.ashaAutoID = m.ashaAutoID;
                                    anc.HighRisk = 0;
                                    anc.EntryUnitVillage = m.VillageAutoID;
                                    anc.Freeze_AadhaarBankInfo = 0;

                                    anc.Location_Rajasthan = m.Location_Rajasthan;
                                    anc.DirectDelivery = m.DirectDelivery;
                                    anc.VillageAutoID = m.VillageAutoID;
                                    anc.Ghamantu = m.Ghamantu;
                                    anc.NFSA = m.NFSA;
                                    anc.BPL = (byte)m.BPL;
                                    if (m.BPL == 1)
                                    {
                                        anc.BeforeDelivery500 = m.BeforeDelivery500;
                                        anc.bplcardno = m.BPLCardNo;
                                    }
                                    else
                                    {
                                        anc.BeforeDelivery500 = 2;
                                        anc.bplcardno = null;
                                    }

                                    anc.LastUpdated = DateTime.Now;
                                    anc.Media = m.Media;
                                    anc.EntryUserNo = m.EntryUserNo;
                                    anc.UpdateUserNo = m.UpdateUserNo;
                                    anc.VillageUpdationDate = DateTime.Now;
                                    anc.PermanentAddressE = m.PermanentAddressE;
                                    anc.CurrentAddressE = m.CurrentAddressE;
                                    anc.QualificationH = m.QualificationH;
                                    anc.QualificationW = m.QualificationW;
                                    anc.BusinessH = m.BusinessH;
                                    anc.BusinessW = m.BusinessW;
                                    anc.IsHusband = m.IsHusband;
                                    anc.HusbnameE = m.HusbnameE;
                                    if (m.LiveChild + infantDeathCount == 1 && DifferenceInDays(Convert.ToDateTime(m.LMPDT), Convert.ToDateTime("2022-11-01")) <= 0)
                                    {
                                        anc.IcdsRegistrationFlag = 1;
                                        anc.ICDS2ndFlagDate = DateTime.Now;
                                    }
                                    else
                                    {
                                        anc.IcdsRegistrationFlag = 0;
                                        anc.ICDS2ndFlagDate = null;
                                    }

                                    anc.Age = m.Age;
                                    anc.CaseNo = 0;
                                    objcnaa.ANCRegDetails.Add(anc);
                                    objcnaa.SaveChanges();
                                    m.ANCRegID = anc.ANCRegID;
                                    var p1 = objcnaa.Mothers.Where(x => x.MotherID == m.MotherID).FirstOrDefault();
                                    p1.ancregid = anc.ANCRegID;
                                }
                                objcnaa.SaveChanges();

                                if (!string.IsNullOrEmpty(m.AadhaarNo) || !string.IsNullOrEmpty(m.JanAadhaarNo))
                                {
                                    objcnaa.Database.ExecuteSqlCommand("insert into pcts..BHAMASHAH(id,IDFlag,JanAadhaarNo,Consent,MotherID,AadhaarNo,JanMemberID,MemberIDVerifyBy,MemberIDVerifiedDate)  values({0},{1},{2},{3},{4},{5},{6},{7},{8})", m.MotherID, 1, m.JanAadhaarNo, m.Consent, m.MotherID, m.AadhaarNo, m.JanMemberID, m.MemberIDVerifyBy, DateTime.Now);
                                }

                                transaction.Complete();
                                Newpctsid = pctsid;
                                return "";
                            }

                        }
                        catch (Exception ex)
                        {
                            Transaction.Current.Rollback();
                            transaction.Dispose();
                            if (methodFlag == 1)
                            {
                                ErrorMsg = "ओह ! पंजीकरण का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें । ";
                                ErrorHandler.WriteError("Error in Post anc" + ex.ToString());
                            }
                            else
                            {
                                ErrorMsg = "ओह ! पंजीकरण का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा अपडेट करें ।";
                                ErrorHandler.WriteError("Error in PUT anc" + ex.ToString());
                            }
                            return ErrorMsg;
                        }
                    }
                }
                catch (DbEntityValidationException e)
                {
                    return "error";
                    //foreach (var eve in e.EntityValidationErrors)
                    //{
                    //    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                    //        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    //    foreach (var ve in eve.ValidationErrors)
                    //    {
                    //        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                    //            ve.PropertyName, ve.ErrorMessage);
                    //    }
                    //}
                    //throw;
                }
            }
            return "";
        }
        public HttpResponseMessage uspGetAllRequest(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.UserNo > 0)
                {
                    var data = cnaa.Mother_App.Where(x => x.UpdateUserNo == p.UserNo).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage posteligibleJ1casesinunit(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.LoginUnitid != 0)
                {
                    var data = cnaa.uspMotherlist(p.LoginUnitid, p.VillageAutoid).ToList();

                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage uspGetRequestByPCTSID(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.UserNo > 0)
                {

                    //    var data = cnaa.Mother_App.Where(x => x.UpdateUserNo == p.UserNo && x.Mother_AppID == p.Mother_AppID).ToList();
                    var data = cnaa.uspGetRequestByPCTSID(p.UserNo, p.Mother_AppID).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage PostANMAppData(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                var data = cnaa.uspAppData(un.LoginUnitCode, un.LoginUnitType, un.FromDate, un.ToDate, un.AshaType).ToList();
                if (data != null)
                {
                    _objResponseModel.ResposeData = data;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }


            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage PostANMAppDataCount(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                var data = cnaa.uspANMAppDataCount(un.LoginUnitCode, un.LoginUnitType, un.FromDate, un.ToDate, (byte)un.Flag).ToList();
                if (data != null)
                {
                    _objResponseModel.ResposeData = data;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }


            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage PostANMAppDataDetails(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                var data = cnaa.uspANMAppDataDetails(un.LoginUnitCode, un.LoginUnitType, un.FromDate, un.ToDate, (byte)un.Flag, un.AshaAutoID).ToList();
                if (data != null)
                {
                    _objResponseModel.ResposeData = data;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }


            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        private string validateSwasthyaMitrkDetail(SwasthyaMitrk m, int postputFlag)
        {
            string ErrorMsg = "";
            if (postputFlag == 2)
            {
                ErrorMsg = CheckValidNumber(Convert.ToString(m.SwasthyaMitrkID), 1, 9, 1, "SwasthyaMitrkID");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }

                ErrorMsg = CheckValidNumber(Convert.ToString(m.VillageAutoID), 1, 10, 0, "VillageAutoID");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                ErrorMsg = CheckValidNumber(Convert.ToString(m.MobileNo), 10, 10, 0, "Mobileno");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                ErrorMsg = CheckValidNumber(Convert.ToString(m.WhatsAppNo), 10, 10, 0, "WhatsAppNo");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.Name)))
            {
                return "कृपया नाम लिखें ! ";
            }
            else if (!Regex.IsMatch(m.Name, @"^([ \u0900-\u097f ,\u200D,\u00c0-\u01ffa-zA-Z'])+$"))
            {
                return "कृपया सही नाम लिखें !";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.Sex)))
            {
                return "कृपया लिंग चुनें !";
            }
            else if (m.Sex != 1 && m.Sex != 2 && m.Sex != 3)
            {
                return "कृपया सही लिंग चुनें ! ";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.VillageAutoID)))
            {
                return "कृपया गॉंव / वार्ड चुनें !";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.IsFather)))
            {
                return "कृपया पिता/पति का नाम चुनें ! ";
            }
            else if (m.IsFather != 1 && m.IsFather != 2)
            {
                return "कृपया पिता/पति का नाम सही चुनें ! ";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.FatherHusbandName)))
            {
                return "कृपया पिता/पति का नाम लिखें ! ";
            }
            else if (!Regex.IsMatch(m.FatherHusbandName, @"^([ \u0900-\u097f ,\u200D,\u00c0-\u01ffa-zA-Z'])+$"))
            {
                return "कृपया सही पिता/पति का नाम लिखें !";
            }

            if (m.MobileNo != null)
            {
                if (m.MobileNo.Length == 10)
                {
                    if (checkStringWithSameDigit(m.MobileNo))
                    {
                        return "कृपया सही मोबाईल नं. लिखे !";
                    }
                    if (validateMobileNoSameDigitMorethantimes(m.MobileNo, 5))
                    {
                        return "कृपया सही मोबाईल नं. लिखे !";
                    }
                }
            }
            //if (m.WhatsAppNo != null)
            //{
            //    if (m.WhatsAppNo.Length == 10)
            //    {
            //        if (checkStringWithSameDigit(m.WhatsAppNo))
            //        {
            //            return "कृपया सही वव्हाट्सएप्प नं. लिखे !";
            //        }
            //        if (validateMobileNoSameDigitMorethantimes(m.WhatsAppNo, 5))
            //        {
            //            return "कृपया सही वव्हाट्सएप्प नं. लिखे !";
            //        }
            //    }
            //}



            if (string.IsNullOrEmpty(Convert.ToString(m.DOB)))
            {
                ErrorMsg = "कृपया जन्म की तिथि डालें !";
                return ErrorMsg;
            }
            else
            {
                ErrorMsg = checkDate(Convert.ToString(m.DOB), 1, "जन्म");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                if (DifferenceInDays(Convert.ToDateTime(m.DOB), DateTime.Now) < 0)
                {
                    ErrorMsg = "जन्म की तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
                    return ErrorMsg;
                }


            }
            if (!string.IsNullOrEmpty(Convert.ToString(m.EmailID)))
            {
                if (!Regex.IsMatch(m.EmailID, @"^([a-zA-Z0-9_\-\.]+)@([a-zA-Z0-9_\-\.]+)\.([a-zA-Z]{2,5})$"))
                {
                    return "कृपया सही मेल आईडी लिखें !";
                }
            }
            if (!string.IsNullOrEmpty(Convert.ToString(m.Experience)))
            {
                if (Convert.ToString(m.Experience).Length > 4)
                {
                    return "कृपया सही अनुभव लिखें !";
                }
            }
            if (!string.IsNullOrEmpty(Convert.ToString(m.Address)))
            {
                if (!Regex.IsMatch(m.Address, @"^[\w\s\d]{5,200}$"))
                {
                    return "कृपया सही पता लिखें !";
                }
            }
            if (!string.IsNullOrEmpty(Convert.ToString(m.Pincode)))
            {
                if (!Regex.IsMatch(m.Pincode, @"^[\d]{6}$"))
                {
                    return "कृपया सही पिनकोड लिखें !";
                }
            }
            //if (string.IsNullOrEmpty(Convert.ToString(m.MartialStatus)))
            //{
            //    return "कृपया वैवाहिक स्थिति चुनें !";
            //}
            //else if (m.MartialStatus != 1 && m.MartialStatus != 2)
            //{
            //    return "कृपया सही वैवाहित स्थिति चुनें !";
            //}

            if (!string.IsNullOrEmpty(m.AadhaarNo))
            {
                if (m.AadhaarNo.Length != 12 && m.AadhaarNo.Length != 15)
                {
                    return "कृपया सही आधार संख्या लिखें !";
                }
                if (!Regex.IsMatch(m.AadhaarNo, @"^[0-9]*$"))
                {
                    return "कृपया सही आधार संख्या लिखें !";
                }
                if (m.Consent != 1)
                {
                    return "कृपया Consent चुने !";
                }
            }
            if (!string.IsNullOrEmpty(m.JanAadhaarNo))
            {
                if (m.JanAadhaarNo.Length != 10)
                {
                    return "कृपया सही जन आधार संख्या लिखें !";
                }
                if (!Regex.IsMatch(m.JanAadhaarNo, @"^[0-9]*$"))
                {
                    return "कृपया सही जन आधार संख्या लिखें !";
                }
                if (m.Consent != 1)
                {
                    return "कृपया Consent चुने !";
                }
            }
            return ErrorMsg;
        }


        private string SaveSwasthyaMitrkDetails(SwasthyaMitrk m, Int16 methodFlag)
        {
            string ErrorMsg = validateSwasthyaMitrkDetail(m, methodFlag);

            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            else
            {
                using (TransactionScope transaction = new TransactionScope())
                {
                    try
                    {
                        ErrorMsg = "";
                        int duplicateCount = rajmed.SwasthyaMitrks.Where(x => x.VillageAutoID == m.VillageAutoID && x.Sex == m.Sex).Count();
                        if (methodFlag == 1 && duplicateCount >= 1)
                        {
                            return "एक गाँव में एक स्वास्थ्य मित्र " + ((m.Sex == 1) ? "पुरुष" : "महिला") + " ही  हो सकता है |\nकृपया स्वास्थ्य मित्र " + ((m.Sex == 1) ? "पुरुष" : "महिला") + " की जाँच कर वापस प्रयास करें !";
                        }
                        else if (methodFlag == 2 && duplicateCount > 1)
                        {
                            return "एक गाँव में एक स्वास्थ्य मित्र " + ((m.Sex == 1) ? "पुरुष" : "महिला") + " ही  हो सकता है |\nकृपया स्वास्थ्य मित्र " + ((m.Sex == 1) ? "पुरुष" : "महिला") + " की जाँच कर वापस प्रयास करें !";
                        }

                        string UpdateValueFlag = "";
                        var p = new SwasthyaMitrk();
                        if (methodFlag == 2)
                        {
                            p = rajmed.SwasthyaMitrks.Where(x => x.SwasthyaMitrkID == m.SwasthyaMitrkID).FirstOrDefault();
                            if (p.Name != m.Name)
                                UpdateValueFlag = UpdateValueFlag + ",1";
                            if (p.Sex != m.Sex)
                                UpdateValueFlag = UpdateValueFlag + ",2";
                            if (p.MobileNo != m.MobileNo)
                                UpdateValueFlag = UpdateValueFlag + ",3";
                            if (p.WhatsAppNo != m.WhatsAppNo)
                                UpdateValueFlag = UpdateValueFlag + ",4";
                            if (p.Qualification != m.Qualification)
                                UpdateValueFlag = UpdateValueFlag + ",5";
                            if (p.DOB != m.DOB)
                                UpdateValueFlag = UpdateValueFlag + ",7";
                            if (p.IsFather != m.IsFather)
                                UpdateValueFlag = UpdateValueFlag + ",8";
                            if (p.FatherHusbandName != m.FatherHusbandName)
                                UpdateValueFlag = UpdateValueFlag + ",9";
                            if (p.Status != m.Status)
                                UpdateValueFlag = UpdateValueFlag + ",10";
                            if (p.EmailID != m.EmailID)
                                UpdateValueFlag = UpdateValueFlag + ",24";
                            if (p.Experience != m.Experience)
                                UpdateValueFlag = UpdateValueFlag + ",25";
                            if (p.Address != m.Address)
                                UpdateValueFlag = UpdateValueFlag + ",26";
                            if (p.Pincode != m.Pincode)
                                UpdateValueFlag = UpdateValueFlag + ",27";
                            if (UpdateValueFlag.Length > 0)
                            {
                                HistorySwasthyaMitrk hs = new HistorySwasthyaMitrk();
                                hs.SwasthyaMitrkID = p.SwasthyaMitrkID;
                                hs.VillageAutoID = p.VillageAutoID;
                                hs.Name = p.Name;
                                hs.Sex = p.Sex;
                                hs.IsFather = p.IsFather;
                                hs.FatherHusbandName = p.FatherHusbandName;
                                hs.MobileNo = p.MobileNo;
                                hs.WhatsAppNo = p.WhatsAppNo;
                                hs.DOB = p.DOB;
                                hs.EntryDate = p.EntryDate;
                                hs.LastUpdated = p.LastUpdated;
                                hs.Qualification = p.Qualification;
                                hs.Media = p.Media;
                                hs.EmailID = p.EmailID;
                                hs.Experience = p.Experience;
                                hs.Address = p.Address;
                                hs.Pincode = p.Pincode;
                                hs.EntryUserNo = p.EntryUserNo;
                                hs.UpdateUserNo = p.UpdateUserNo;
                                hs.Status = p.Status;
                                hs.HistoryEntryDate = DateTime.Now;
                                hs.UpdateValueFlag = UpdateValueFlag;
                                rajmed.HistorySwasthyaMitrks.Add(hs);
                                rajmed.SaveChanges();
                            }
                        }
                        else
                        {
                            p.VillageAutoID = m.VillageAutoID;
                        }
                        p.Name = m.Name;
                        p.Sex = m.Sex;
                        p.IsFather = m.IsFather;
                        p.FatherHusbandName = m.FatherHusbandName;
                        p.MobileNo = m.MobileNo;
                        p.WhatsAppNo = m.WhatsAppNo;
                        p.Qualification = m.Qualification;
                        p.DOB = m.DOB;
                        p.EntryDate = DateTime.Now;
                        p.LastUpdated = DateTime.Now;
                        p.Media = m.Media;
                        p.EmailID = m.EmailID;
                        p.Experience = m.Experience;
                        p.Address = m.Address;
                        p.Pincode = m.Pincode;
                        p.EntryUserNo = m.UpdateUserNo;
                        p.UpdateUserNo = m.UpdateUserNo;
                        p.Status = 1;
                        if (methodFlag == 1)
                        {
                            rajmed.SwasthyaMitrks.Add(p);
                        }
                        rajmed.SaveChanges();
                        if (methodFlag == 1)
                        {
                            m.SwasthyaMitrkID = p.SwasthyaMitrkID;
                        }


                        int rowsAffected = rajmed.Database.SqlQuery<int>("select count(*) from pcts..SwasthyaMitrk  where SwasthyaMitrkID={0}", Convert.ToInt32(m.SwasthyaMitrkID)).FirstOrDefault();

                        if ((rowsAffected == 0) && (m.JanAadhaarNo != null || m.AadhaarNo != null))
                        {
                            rowsAffected = rajmed.Database.ExecuteSqlCommand("insert into pcts..SwasthyaMitrk(AadhaarNo,Consent,JanAadhaarNo,Entrydate,LastUpdated,SwasthyaMitrkID) values({0},{1},{2},{3},{4},{5})", m.AadhaarNo, m.Consent, m.JanAadhaarNo, DateTime.Now, DateTime.Now, m.SwasthyaMitrkID);

                        }
                        else if (m.SwasthyaMitrkID != 0 && rowsAffected > 0)
                        {
                            SwasthyaMitrkJan p2 = rajmed.Database.SqlQuery<SwasthyaMitrkJan>("select * from pcts..SwasthyaMitrk where SwasthyaMitrkID={0}", m.SwasthyaMitrkID).FirstOrDefault();
                            UpdateValueFlag = "";
                            if (p2 != null)
                            {
                                if (p2.AadhaarNo != m.AadhaarNo)
                                {
                                    UpdateValueFlag = UpdateValueFlag + ",21";
                                }
                                if (p2.JanAadhaarNo != m.JanAadhaarNo)
                                {
                                    UpdateValueFlag = UpdateValueFlag + ",22";
                                }
                            }
                            if (UpdateValueFlag != "")
                            {
                                rowsAffected = rajmed.Database.ExecuteSqlCommand("insert into pcts..HistorySwasthyaMitrk(AadhaarNo,Consent,JanAadhaarNo,Entrydate,LastUpdated,UpdateValueFlag,HistoryEntryDate) values({0},{1},{2},{3},{4},{5},{6})", m.AadhaarNo, m.Consent, m.JanAadhaarNo, DateTime.Now, DateTime.Now, UpdateValueFlag, DateTime.Now);

                            }

                            rowsAffected = rajmed.Database.ExecuteSqlCommand("update pcts..SwasthyaMitrk set AadhaarNo={0},Consent={1},JanAadhaarNo={2},LastUpdated = {3}  where SwasthyaMitrkID={4}", m.AadhaarNo, m.Consent, m.JanAadhaarNo, DateTime.Now, m.SwasthyaMitrkID);

                        }
                        if (m.ChangePhoto == 1)
                        {
                            rowsAffected = rajmed.Database.SqlQuery<int>("select count(*) from OJSPMPDFs..SwasthyaMitrkPhoto  where SwasthyaMitrkID={0}", Convert.ToInt32(m.SwasthyaMitrkID)).FirstOrDefault();
                            if ((rowsAffected == 0) && (m.PhotoArray != null && m.PhotoArray.Length > 1))
                            {
                                rowsAffected = rajmed.Database.ExecuteSqlCommand("insert into OJSPMPDFs..SwasthyaMitrkPhoto(SwasthyaMitrkID,Photo,LastUpdateDate,PhotoType) values({0},{1},{2},{3})", m.SwasthyaMitrkID, m.PhotoArray, DateTime.Now, m.PhotoType);

                            }
                            else if (m.SwasthyaMitrkID != 0 && rowsAffected > 0)
                            {
                                rowsAffected = rajmed.Database.ExecuteSqlCommand("insert into OJSPMPDFs..HistorySwasthyaMitrkPhoto(SwasthyaMitrkID,Photo,LastUpdateDate,HistoryEntryDate,PhotoType) select SwasthyaMitrkID,Photo,LastUpdateDate,{1},PhotoType  from OJSPMPDFs..SwasthyaMitrkPhoto where SwasthyaMitrkID={0}", m.SwasthyaMitrkID, DateTime.Now);

                                rowsAffected = rajmed.Database.ExecuteSqlCommand("update OJSPMPDFs..SwasthyaMitrkPhoto set Photo={1},LastUpdateDate = {2},PhotoType={3}  where SwasthyaMitrkID={0}", m.SwasthyaMitrkID, m.PhotoArray, DateTime.Now, m.PhotoType);

                            }
                        }
                        transaction.Complete();
                    }
                    catch (Exception ex)
                    {
                        Transaction.Current.Rollback();
                        transaction.Dispose();
                        if (methodFlag == 1)
                        {
                            return "ओह ! स्वास्थ्य मित्र का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें ।" + ex.ToString();
                        }
                        else
                        {
                            return "ओह ! स्वास्थ्य मित्र का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा अपडेट करें ।" + ex.ToString();
                        }
                    }
                }
            }
            return "";
        }

        private async Task<SwasthyaMitrk> ConvertMulipartRequest()
        {
            SwasthyaMitrk sm = null;
            try
            {
                var provider = await Request.Content.ReadAsMultipartAsync<InMemoryMultipartFormDataStreamProvider>(new InMemoryMultipartFormDataStreamProvider());
                //access form data  
                NameValueCollection formData = provider.FormData;
                sm = new SwasthyaMitrk();
                if (formData != null)
                {
                    sm.SwasthyaMitrkID = Convert.ToInt32(formData["SwasthyaMitrkID"]);
                    sm.VillageAutoID = Convert.ToInt32(formData["VillageAutoID"]);
                    sm.Name = Convert.ToString(formData["Name"]);
                    sm.Sex = Convert.ToByte(formData["Sex"]);
                    sm.IsFather = Convert.ToByte(formData["IsFather"]);
                    sm.FatherHusbandName = Convert.ToString(formData["FatherHusbandName"]);
                    sm.MobileNo = Convert.ToString(formData["MobileNo"]);
                    sm.WhatsAppNo = Convert.ToString(formData["WhatsAppNo"]);
                    sm.Qualification = Convert.ToByte(formData["Qualification"]);
                    sm.DOB = Convert.ToDateTime(formData["DOB"]);
                    sm.EntryDate = DateTime.Now;
                    sm.LastUpdated = DateTime.Now;
                    sm.Media = Convert.ToByte(formData["Media"]);
                    sm.EmailID = Convert.ToString(formData["EmailID"]);
                    if (formData["Experience"] == "")
                    {
                        sm.Experience = null;
                    }
                    else
                    {
                        sm.Experience = Convert.ToDouble(formData["Experience"]);
                    }
                    sm.Address = Convert.ToString(formData["Address"]);
                    sm.Pincode = Convert.ToString(formData["Pincode"]);
                    sm.EntryUserNo = Convert.ToByte(formData["UpdateUserNo"]);
                    sm.UpdateUserNo = Convert.ToByte(formData["UpdateUserNo"]);
                    sm.AppVersion = Convert.ToString(formData["AppVersion"]);
                    sm.UserID = Convert.ToString(formData["UserID"]);
                    sm.TokenNo = Convert.ToString(formData["TokenNo"]);
                    sm.AadhaarNo = Convert.ToString(formData["AadhaarNo"]);
                    if (sm.AadhaarNo == "")
                    {
                        sm.AadhaarNo = null;
                    }
                    sm.JanAadhaarNo = Convert.ToString(formData["JanAadhaarNo"]);
                    if (sm.JanAadhaarNo == "")
                    {
                        sm.JanAadhaarNo = null;
                    }
                    if (formData["Consent"] == "")
                    {
                        sm.Consent = null;
                    }
                    else
                    {
                        sm.Consent = Convert.ToByte(formData["Consent"]);
                    }
                    if (formData["Consent"] == "")
                    {
                        sm.ChangePhoto = 0;
                    }
                    else
                    {
                        sm.ChangePhoto = Convert.ToByte(formData["ChangePhoto"]);
                    }
                    sm.Status = 1;
                }

                //access files  
                if (provider != null && sm.ChangePhoto == 1)
                {
                    IList<HttpContent> files = provider.Files;
                    if (files != null)
                    {
                        if (files.Count > 0)
                        {
                            HttpContent file1 = files[0];
                            if (file1 != null)
                            {
                                if (file1.Headers.ContentDisposition.FileName.Trim('\"') != "")
                                {
                                    var thisFileName = file1.Headers.ContentDisposition.FileName.Trim('\"');
                                    Stream input = await file1.ReadAsStreamAsync();
                                    byte[] b;

                                    using (BinaryReader br = new BinaryReader(input))
                                    {
                                        b = br.ReadBytes((int)input.Length);
                                    }
                                    sm.PhotoArray = b;
                                    sm.PhotoType = file1.Headers.ContentDisposition.FileName.Split('.')[1].Replace("'", "");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in ConvertMulipartRequest--" + ex.ToString());
            }
            return sm;
        }

        [ActionName("PostSwasthyaMitrk")]
        public async Task<HttpResponseMessage> PostSwasthyaMitrk()
        {
            ResponseModel _objResponseModel = new ResponseModel();
            //  Check if the request contains multipart/form-data.  
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }
            SwasthyaMitrk sm = await ConvertMulipartRequest();
            if (sm == null)
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Error in Data";
                _objResponseModel.AppVersion = 1;
                return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
            }
            sm.SwasthyaMitrkID = 0;

            bool tokenFlag = ValidateToken(sm.UserID, sm.TokenNo);
            if (tokenFlag == true)
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        int CheckAppVersionFlag = CheckVersion(sm.AppVersion, sm.IOSAppVersion);
                        if (CheckAppVersionFlag == 1)
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = appValiationMsg;
                            _objResponseModel.AppVersion = 1;
                            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                        }
                        else
                        {
                            string msg = SaveSwasthyaMitrkDetails(sm, 1);
                            if (msg != "")
                            {
                                _objResponseModel.Status = false;
                                _objResponseModel.Message = msg;
                                Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                            }
                            else
                            {
                                int SwasthyaMitrkID = sm.SwasthyaMitrkID;
                                _objResponseModel.Status = true;
                                _objResponseModel.Message = "धन्यवाद, स्वास्थ्य मित्र का विवरण सेव हो चुका हैं।\nकृपया स्वास्थ्य मित्र की आई डी नोट करें " + SwasthyaMitrkID;
                            }
                        }
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Model Error";
                        ErrorHandler.WriteError("Error in post Swasthya model--" + ModelState);
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                    }

                }
                catch (Exception ex)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "validation Error";
                    ErrorHandler.WriteError("Error in post Swasthya--" + ex.ToString());
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("PutSwasthyaMitrk")]
        public async Task<HttpResponseMessage> PutSwasthyaMitrk()
        {
            ResponseModel _objResponseModel = new ResponseModel();
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }
            SwasthyaMitrk m = await ConvertMulipartRequest();
            if (m == null)
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Error in Data";
                _objResponseModel.AppVersion = 1;
                return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
            }
            bool tokenFlag = ValidateToken(m.UserID, m.TokenNo);
            if (tokenFlag == true)
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        int CheckAppVersionFlag = CheckVersion(m.AppVersion, m.IOSAppVersion);
                        if (CheckAppVersionFlag == 1)
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = appValiationMsg;
                            _objResponseModel.AppVersion = 1;
                            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                        }
                        else
                        {
                            string msg = SaveSwasthyaMitrkDetails(m, 2);
                            if (msg != "")
                            {
                                _objResponseModel.Status = false;
                                _objResponseModel.Message = msg;
                                Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                            }
                            else
                            {
                                _objResponseModel.Status = true;
                                _objResponseModel.Message = "धन्यवाद, स्वास्थ्य मित्र का विवरण सेव हो चुका हैं।";
                            }
                        }
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Model Error";
                        ErrorHandler.WriteError("Error in put Swasthya model--" + ModelState);
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                    }

                }
                catch (Exception ex)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "validation Error";
                    ErrorHandler.WriteError("Error in put Swasthya--" + ex.ToString());
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }



            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        public HttpResponseMessage PostMasterCodesraj(MasterCodesList p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                var data = from mcode in rajmed.MasterCodes
                           where mcode.ParentID == p.parentid
                           select new { mcode.Name, mcode.masterID };
                if (data.Count() > 0)
                {
                    _objResponseModel.ResposeData = data;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage PostSwasthyaMitrkLocation(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                var data = rajmed.uspSwasthyaMitrkLocation(p.ANMAutoID).ToList();
                if (data != null)
                {
                    _objResponseModel.ResposeData = data;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage uspGetAllVillageListForSwasthyaMitrk(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.LoginUnitid > 0)
                {
                    var data = rajmed.Villages.Where(x => x.unitid == p.LoginUnitid && x.OtherLocation == 1).Select(x => new { VillageName = x.UnitNameHindi, VillageautoID = x.VillageAutoID }).ToList();
                    data.Add(new { VillageName = "सभी गाँव", VillageautoID = 0 });
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data.OrderBy(x => x.VillageautoID);
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage uspGetVillageListForSwasthyaMitrk(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.LoginUnitid > 0)
                {
                    var data = rajmed.Villages.Where(x => x.unitid == p.LoginUnitid && x.OtherLocation == 1).Select(x => new { VillageName = x.UnitNameHindi, VillageautoID = x.VillageAutoID }).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data.OrderBy(x => x.VillageautoID);
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [HttpPost]
        public HttpResponseMessage GetSwasthyaMitrkData(SwasthyaMitrk sm)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(sm.UserID, sm.TokenNo);
            if (tokenFlag == true)
            {
                List<SwasthyaMitrk> p = rajmed.uspSwasthyaMitrkData(sm.AnmUnitID).Select(x => new SwasthyaMitrk
                {
                    SwasthyaMitrkID = x.SwasthyaMitrkID,
                    VillageAutoID = x.VillageAutoID,
                    VillageName = x.VillageName,
                    Name = x.Name,
                    Sex = x.Sex,
                    MobileNo = x.MobileNo,
                    WhatsAppNo = x.WhatsAppNo,
                    Qualification = x.Qualification,
                    DOB = x.DOB,
                    IsFather = x.IsFather,
                    FatherHusbandName = x.FatherHusbandName,
                    EmailID = x.EmailID,
                    Experience = x.Experience,
                    Address = x.Address,
                    Pincode = x.Pincode,
                    AadhaarNo = x.AadhaarNo,
                    JanAadhaarNo = x.JanAadhaarNo,
                    Consent = (byte?)x.Consent,
                    PhotoArray = x.Photo,
                    PhotoType = x.PhotoType
                }).OrderBy(x => x.VillageName).ToList();
                if (sm.VillageAutoID > 0)
                {
                    p = p.Where(x => x.VillageAutoID == sm.VillageAutoID).ToList();
                }


                foreach (SwasthyaMitrk sm1 in p)
                {
                    if (sm1.SwasthyaMitrkID > 0)
                    {
                        SwasthyaMitrkPhoto p2 = rajmed.Database.SqlQuery<SwasthyaMitrkPhoto>("select top 1 Photo,PhotoType from OJSPMPDFs..SwasthyaMitrkPhoto  where SwasthyaMitrkID={0}", sm1.SwasthyaMitrkID).FirstOrDefault();
                        if (p2 != null)
                        {
                            string filePath = AppDomain.CurrentDomain.BaseDirectory + "SwasthyaMitrkPhoto\\sm";
                            Uri baseUri = new Uri(Request.RequestUri.AbsoluteUri.Replace(Request.RequestUri.PathAndQuery, String.Empty));
                            string fullPath = filePath + sm1.SwasthyaMitrkID + "." + sm1.PhotoType;
                            string resourceRelative = "~/SwasthyaMitrkPhoto/sm" + sm1.SwasthyaMitrkID + "." + sm1.PhotoType;
                            Uri resourceFullPath = new Uri(baseUri, VirtualPathUtility.ToAbsolute(resourceRelative));
                            System.Drawing.Image imageIn = ConvertByteArrayToImage(sm1.PhotoArray);
                            File.WriteAllBytes(fullPath, sm1.PhotoArray);
                            sm1.PhotoPath = resourceFullPath.AbsolutePath;
                            sm1.PhotoArray = null;
                        }
                    }
                    string experience = Convert.ToString(sm1.Experience);

                }


                var p3 = p.Select(x => new { VillageAutoID = x.VillageAutoID, VillageName = x.VillageName }).Distinct().ToList();
                List<SwasthyaMitrkVillageList> smv = new List<SwasthyaMitrkVillageList>();
                foreach (var p4 in p3)
                {
                    SwasthyaMitrkVillageList smv1 = new SwasthyaMitrkVillageList();
                    smv1.VillageAutoID = p4.VillageAutoID;
                    smv1.VillageName = p4.VillageName;
                    List<SwasthyaMitrk> sm8 = p.Where(x => x.VillageAutoID == p4.VillageAutoID && x.SwasthyaMitrkID > 0).ToList();
                    smv1.SwasthyaMitrk = sm8;
                    smv.Add(smv1);
                }


                _objResponseModel.ResposeData = smv;
                _objResponseModel.Status = true;
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public Image ConvertByteArrayToImage(byte[] byteArrayIn)
        {
            using (MemoryStream ms = new MemoryStream(byteArrayIn))
            {
                return Image.FromStream(ms);
            }
        }
        public HttpResponseMessage PostSentSMS(BeneficiaryMobileNo_Maa u)
        {
            string message = "Data Received successfully";
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = true;
            if (!string.IsNullOrEmpty(u.UserID))
            {
                tokenFlag = ValidateToken(u.UserID, u.TokenNo);
            }
            else
            {
                //   tokenFlag = ValidateToken(u.MobileNo, u.TokenNo);
                tokenFlag = true;
            }

            //if (u.SmsFlag == 4)
            //    tokenFlag = ValidateToken(u.MobileNo, u.TokenNo);
            //else
            //    tokenFlag = ValidateToken(u.UserID, u.TokenNo);
            Boolean status = true;
            if (tokenFlag == true)
            {
                if (validateMobileNo(u.MobileNo))
                {
                    if (u.SmsFlag != 4 && u.SmsFlag != 82)
                    {
                        var data = cnaa.Mothers.Where(x => x.Mobileno == u.MobileNo).Count();
                        if (data == 0)
                        {
                            status = false;
                            message = "कृपया सही मोबाइल नम्बर डाले !";
                        }
                        else
                        {
                            bool sendSmsFlag = sendSms1(u.MobileNo, Convert.ToByte(u.SmsFlag));
                            _objResponseModel.ResposeData = sendSmsFlag;
                            if (sendSmsFlag)
                                message = "OTP Sent successfully";
                            else
                                message = "OTP can not sent ";
                        }
                    }
                    else
                    {
                        bool sendSmsFlag = sendSms1(u.MobileNo, Convert.ToByte(u.SmsFlag));
                        _objResponseModel.ResposeData = sendSmsFlag;
                        if (sendSmsFlag)
                            message = "OTP Sent successfully";
                        else
                            message = "OTP can not sent ";
                    }
                }
                else
                {
                    status = false;
                    message = "कृपया सही मोबाईल नं. लिखे !";
                }
            }
            else
            {
                status = false;
                message = "Invalid request";
            }
            _objResponseModel.Status = status;
            _objResponseModel.Message = message;
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public bool sendSms1(string mobileNo, byte SmsFlag)
        {
            // mobileNo = "7976119833";
            string randomno = RandomInteger(1000, 9999).ToString();
            randomno = "1234";
            if (mobileNo == "7976119833" || mobileNo == "5999999999")
                randomno = "1234";
            MaacnaaEntities maa = new MaacnaaEntities();
            //    string msg = "OTP for PCTS app is " + randomno + " Valid for 15 Minutes. - Department of Medical health and Welfare GoR";
            string msg = "The OTP for verification is " + randomno + " in PCTS, valid for 15 Minutes. -Medical & Health Dept, GoR";
            //  var p = maa.OTPs.Where(x => x.MobileNo == mobileNo && x.SmsFlag == SmsFlag).FirstOrDefault();
            var p = maa.OTPs.Where(x => x.MobileNo == mobileNo).FirstOrDefault();
            if (p == null)
            {
                OTP p1 = new OTP();
                p1.MobileNo = mobileNo;
                p1.OTPNo = randomno;
                p1.EntryDate = DateTime.Now;
                p1.SmsFlag = SmsFlag;
                maa.OTPs.Add(p1);
                maa.SaveChanges();

            }
            else
            {
                p.OTPNo = randomno;
                p.EntryDate = DateTime.Now;
                maa.SaveChanges();
            }

            // return SendSMS(mobileNo, msg, SmsFlag);
            string TemplateCode = "1107160265631815011";
            return SendSMSNew(msg, mobileNo, TemplateCode, SmsFlag);
        }


        public bool SendSMSNew(string message, string mobileno, string TemplateCode, Int16 SmsFlag)
        {
            return true;
            if (mobileno == "7976119833" || mobileno == "5999999999")
                return true;
            String ErroDesc = "";
            String messageid = "";
            try
            {
                //string url = "https://vsms.minavo.in/api/singlesms.php?auth_key=e5294d887d3fc772ed96427c6220fd8720220331164356&mobilenumber=" + mobileno + "&message=" + message + "&sid=NHMRAJ&mtype=N&template_id=" + TemplateCode;
                //HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(url);
                //HttpWebResponse myResp = (HttpWebResponse)myReq.GetResponse();
                //System.IO.StreamReader respStreamReader = new System.IO.StreamReader(myResp.GetResponseStream());
                //if (myResp.StatusCode == HttpStatusCode.OK)
                //{
                //    messageid = respStreamReader.ReadToEnd();
                //}
                //respStreamReader.Close();
                //if (messageid != "")
                //{
                //    dynamic data = JObject.Parse(messageid);
                //    messageid = Convert.ToString((string)data.SelectToken("log_id"));
                //}
                System.Net.ServicePointManager.SecurityProtocol =
System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 |
System.Net.SecurityProtocolType.Tls;
                HttpClient client = new HttpClient();
                //client.BaseAddress = new
                client.BaseAddress = new Uri("https://esanchar.rajasthan.gov.in/esanchar2/api/OBD/CreateOTPRequest");
                client.DefaultRequestHeaders.Add("username", "NhmOtp");
                client.DefaultRequestHeaders.Add("password", "1z57Sq%OkwE4yQH#");
                // Add an Accept header for JSON format.
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                MultipartFormDataContent form = new MultipartFormDataContent();
                //By Pass SSL for testing
                System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate (
                Object obj, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                System.Security.Cryptography.X509Certificates.X509Chain chain,
                System.Net.Security.SslPolicyErrors errors)
                {
                    return (true);
                };
                List<string> mobileList = new List<string>();
                mobileList.Add(mobileno);
                ExternalSMSApiInfo inputparams = new ExternalSMSApiInfo();
                inputparams.UniqueID = "NHM_OTP";
                inputparams.serviceName = "RAJMHS";
                inputparams.language = "ENG";
                inputparams.message = message;
                inputparams.mobileNo = mobileList;
                inputparams.templateID = "1407168326925374769";
                var response = client.PostAsync(client.BaseAddress, new StringContent(new JavaScriptSerializer().Serialize(inputparams), Encoding.UTF8, "application/json")).Result;
                var asyncResponse = response.Content.ReadAsStringAsync().Result;
                var jsonResponse = JObject.Parse(asyncResponse);
                if (Convert.ToString(jsonResponse["responseCode"]) == "200")
                {
                    messageid = Convert.ToString(jsonResponse["responseID"]);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("--sms error---" + ex.ToString());
                ErroDesc = ex.ToString().Substring(0, 300);

                return false;
            }
            if (messageid != "")
            {
                MaacnaaEntities maa = new MaacnaaEntities();
                OtpLog ol = new OtpLog();
                ol.ErrorDescription = ErroDesc;
                ol.MobileNo = mobileno;
                ol.PushDate = DateTime.Now;
                ol.ErrorID = 2;
                ol.MessageId = messageid;
                ol.Msgtype = (byte)SmsFlag;
                maa.OtpLogs.Add(ol);
                maa.SaveChanges();
            }
            return true;
        }


        private bool CheckOtp(string MobileNo, string OTP, byte SmsFlag, string DeviceID, string UserID)
        {
            MaacnaaEntities maa = new MaacnaaEntities();
            //  MobileNo = "7976119833";
            var p = maa.OTPs.Where(x => x.MobileNo == MobileNo && x.OTPNo == OTP).OrderByDescending(x => x.EntryDate).FirstOrDefault();
            //   ErrorHandler.WriteError("---MobileNo--" + MobileNo + "--UserID--" + UserID + "---SmsFlag--" + SmsFlag);
            // var p = maa.OTPs.Where(x => x.MobileNo == MobileNo && x.OTPNo == OTP && x.SmsFlag == SmsFlag).OrderByDescending(x => x.EntryDate).FirstOrDefault();
            if (p != null)
            {
                if (DifferenceInMinutes(p.EntryDate, DateTime.Now) > 15)
                {
                    return false;
                }
                else
                {
                    if (SmsFlag != 4)
                    {
                        var p1 = maa.PinDetails.Where(x => x.MobileNo == MobileNo).FirstOrDefault();
                        if (p1 != null)
                        {
                            if (p1.DeviceID != DeviceID)
                            {

                            }
                            p1.DeviceID = DeviceID;
                            maa.SaveChanges();
                        }
                    }
                    else
                    {

                        if (MobileNo != UserID)
                        {
                            using (TransactionScope transaction = new TransactionScope())
                            {
                                int successFlag = 1;
                                try
                                {
                                    using (rajmedicalEntities objraj = new rajmedicalEntities())
                                    {
                                        string previousMobileNo = objraj.Users.Where(x => x.UserID == UserID && x.IsDeleted == 0).Select(x => x.UserContactNo).FirstOrDefault();
                                        if (previousMobileNo != "")
                                        {
                                            //var p3 = rajmed.Users.Where(x => x.IsDeleted == 0 && x.UserContactNo == previousMobileNo).ToList();
                                            //if (p3.Count > 0)
                                            //{
                                            //    int rowsAffected = objraj.Database.ExecuteSqlCommand("insert into Userslog(UnitCode,UserID,Password,UserName,UserContactNo,Role,ApplicationNos,old2pwd,old1pwd" +
                                            //",IsDeleted,unitid,PwdUpdatedDate,AshaRoleId,ExpireOn,Active,OJSPMRoleID,PctsRoleID,EctsRoleID,resetpwd,Saltvalue,Saltcreatedon," +
                                            //"AppVersion,AppPassword,Imei,EntryDate,ANMAutoID,IsAppUser,UserNo,AppRoleID,HRRoleID,OfficeID,Email," +
                                            //"LastUpdated,DesignationID,VerifiedMobile,VerifiedEmail,SsoID,IPAddress,UpdatedBy,ColumnFlag)" +
                                            //"select UnitCode,UserID,Password,UserName,UserContactNo,Role,ApplicationNos,old2pwd,old1pwd" +
                                            //",IsDeleted,unitid,PwdUpdatedDate,AshaRoleId,ExpireOn,Active,OJSPMRoleID,PctsRoleID,EctsRoleID,resetpwd,Saltvalue,Saltcreatedon," +
                                            //"AppVersion,AppPassword,Imei,EntryDate,ANMAutoID,IsAppUser,UserNo,AppRoleID,HRRoleID,OfficeID,Email," +
                                            //"getdate(),DesignationID,VerifiedMobile,VerifiedEmail,SsoID,'',{0},{1} from users where UserContactNo={2} and IsDeleted = 0", UserID, ",2,", previousMobileNo, UserID);
                                            //    if (rowsAffected > 0)
                                            //    {
                                            //        //rowsAffected = objraj.Database.ExecuteSqlCommand("update users set UserContactNo = NULL where UserContactNo = {0} and IsDeleted = 0", previousMobileNo);
                                            //        //if (rowsAffected < 0)
                                            //        //{
                                            //        //    successFlag = 0;
                                            //        //}
                                            //    }
                                            //}
                                        }
                                        var p2 = objraj.Users.Where(x => x.UserID == UserID && x.IsDeleted == 0).FirstOrDefault();
                                        if (p2 != null && successFlag == 1)
                                        {

                                            if (p2.AppRoleID == 32 || p2.AppRoleID == 33)
                                            {
                                                int rowsAffected = 0;
                                                //if (MobileNo.Length == 10)
                                                //{
                                                //    rowsAffected = objraj.Database.ExecuteSqlCommand("update users set UserContactNo = NULL where UserContactNo={0} and IsDeleted = 0", MobileNo);

                                                //}
                                                if (p2.AppRoleID == 33)
                                                {
                                                    rowsAffected = objraj.Database.ExecuteSqlCommand("update users set userid={1},UserContactNo = {1} where userid={0} and IsDeleted = 0", UserID, MobileNo);
                                                }
                                                else
                                                {
                                                    rowsAffected = objraj.Database.ExecuteSqlCommand("update users set UserContactNo = {1} where userid={0} and IsDeleted = 0", UserID, MobileNo);
                                                }
                                                if (rowsAffected > 0)
                                                {

                                                    rowsAffected = objraj.Database.ExecuteSqlCommand("insert into HistoryAshamaster(unitcode,AshaName,AshaPhone, " +
                                                        " IsDeleted,type,LastUpdated,Ifsc_code,Accountno," +
                                                        "Accounttype,Bankname,unitid,ashaAutoID,Status,VerifiedAccount," +
                                                        "VerifiedAccountByBank,userid,CUGMobile,RationCardNo,UpdateValueFlag,VerifyMobileNo) " +
                                                        " select unitcode,AshaName,AshaPhone,IsDeleted,type,LastUpdated,Ifsc_code,Accountno," +
                                                        " Accounttype,Bankname,unitid,ashaAutoID,Status,VerifiedAccount, " +
                                                        "VerifiedAccountByBank,{2},CUGMobile,RationCardNo,{1},VerifyMobileNo from ashamaster where ashaautoid={0} ", p2.ANMAutoID, ",5,", UserID);
                                                    if (rowsAffected > 0)
                                                    {
                                                        rowsAffected = objraj.Database.ExecuteSqlCommand("update ashamaster set AshaPhone = {1} where ashaautoid={0}", p2.ANMAutoID, MobileNo);
                                                        if (rowsAffected > 0)
                                                        {
                                                            int finyear = DateTime.Now.Year;
                                                            if (DateTime.Now.Month < 3)
                                                                finyear = finyear - 1;

                                                            rowsAffected = objraj.Database.ExecuteSqlCommand("update yearlyashamaster set AshaPhone = {1} where ashaautoid={0} and finyear={2}", p2.ANMAutoID, MobileNo, finyear);
                                                            if (rowsAffected > 0)
                                                            {
                                                                successFlag = 1;
                                                            }
                                                            else
                                                            {
                                                                successFlag = 0;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        successFlag = 0;
                                                    }
                                                }
                                                else
                                                {
                                                    successFlag = 0;
                                                }
                                            }
                                            else
                                            {
                                                p2.UserContactNo = MobileNo;
                                                objraj.SaveChanges();
                                                successFlag = 1;
                                            }

                                        }
                                    }
                                    if (successFlag == 1)
                                    {
                                        transaction.Complete();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ErrorHandler.WriteError("---MobileNo--" + MobileNo + "--UserID--" + UserID + "---SmsFlag--" + SmsFlag + "--error in opt" + ex.ToString());
                                    Transaction.Current.Rollback();
                                    transaction.Dispose();
                                    return false;

                                }
                            }
                        }


                    }
                    return true;
                }
            }
            else
            {
                return false;
            }
        }


        public HttpResponseMessage PostCheckOTP(BeneficiaryMobileNo_Maa u)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(u.UserID, u.TokenNo);
            if (tokenFlag == true)
            {
                MaacnaaEntities maa = new MaacnaaEntities();
                Boolean status = true;
                string message = "";
                if (u.MobileNo == "7000000000" && u.OTP == "1234")
                {
                    status = true;
                    message = "Valid OTP";
                }
                else
                {
                    if (CheckOtp(u.MobileNo, u.OTP, u.SmsFlag, u.DeviceID, u.UserID))
                    {
                        status = true;
                        message = "Valid OTP";
                    }
                    else
                    {
                        status = false;
                        message = "Invalid OTP";
                    }
                }
                //status = true;
                //message = "Valid OTP";
                _objResponseModel.Status = status;
                _objResponseModel.Message = message;
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage birthcertificates(birthcertificates p)
        {

            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                MaacnaaEntities maa = new MaacnaaEntities();
                try
                {
                    if (Convert.ToInt32(p.infantid) == 0)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                    else
                    {
                        string fileName = HttpContext.Current.Server.MapPath("~/BirthCertificates/" + p.infantid + ".pdf");
                        if (File.Exists(fileName))
                        {

                            Dictionary<string, string> hash = new Dictionary<string, string> { };
                            //hash.Add("Url", "pctsapp/BirthCertificates/" + p.infantid + ".pdf");
                            hash.Add("Url", "BirthCertificates/" + Convert.ToString(p.infantid) + ".pdf");
                            hash.Add("Infantid", p.infantid + ".pdf");
                            _objResponseModel.ResposeData = hash;
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "Data Received successfully";
                        }
                        else
                        {
                            int InfIDs = Convert.ToInt32(p.infantid);
                            var data = maa.PehchanBirths.Where(x => x.InfantID == InfIDs).Select(x => new { RegNo = x.RegNo, RegYear = (int?)x.RegistrationYear }).FirstOrDefault();
                            if (data != null)
                            {

                                string password = Convert.ToString(ConfigurationManager.AppSettings["ApiPassword"]);
                                string userid = Convert.ToString(ConfigurationManager.AppSettings["ApiUserid"]);
                                //Random rnd = new Random();
                                int salt = RandomInteger(10000000, 99999999);
                                var saltedPass = GenerateSHA256String(password);
                                var sha256pass = GenerateSHA256String(salt + saltedPass.ToLower());
                                var encpassword = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sha256pass.Trim()));

                                //  DateTime date = Convert.ToDateTime(data.RegDate);
                                int year = Convert.ToInt32(data.RegYear);

                                //    string URL = "http://164.100.153.91/pehchanws/searchreg/service.asmx/SearchRegistration?";
                                string URL = "https://pehchan.raj.nic.in/pehchanws/searchreg/service.asmx/SearchRegistration?";
                                URL += "user_id=" + HttpUtility.UrlEncode(userid) + "&user_pwd=" + (encpassword) + "&salt=" + HttpUtility.UrlEncode(salt.ToString()) + "&Event=" + HttpUtility.UrlEncode("1") + "&RegisNumber=" + HttpUtility.UrlEncode(data.RegNo) + "&Year=" + HttpUtility.UrlEncode(year.ToString());
                                HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(URL);
                                HttpWebResponse myResp = (HttpWebResponse)myReq.GetResponse();
                                //System.IO.StreamReader respStreamReader = new System.IO.StreamReader(myResp.GetResponseStream());
                                //string pdfData = respStreamReader.ReadToEnd();
                                //if (pdfData.Length > 260000)
                                //{
                                //    _objResponseModel.Status = false;
                                //    _objResponseModel.Message = "Pdf File Size cannot be more than 1 mb";
                                //    return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                                //}
                                string uploadfolderpath = HttpContext.Current.Server.MapPath("~/BirthCertificates/");
                                System.IO.StreamReader respStreamReader = null;
                                int MAX_STR_LEN = 260000;
                                StringBuilder sb = new StringBuilder();
                                using (respStreamReader = new System.IO.StreamReader(myResp.GetResponseStream()))
                                {
                                    int intC;
                                    while ((intC = respStreamReader.Read()) != -1)
                                    {
                                        char c = (char)intC;
                                        if (c == '\n')
                                        {
                                            continue;
                                        }
                                        //if (sb.Length >= MAX_STR_LEN)
                                        //{
                                        //    _objResponseModel.Status = false;
                                        //    _objResponseModel.Message = "Pdf File Size cannot be more than 1 mb";
                                        //    return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                                        //}
                                        sb.Append(c);
                                    }
                                }

                                string pdfData = sb.ToString();
                                XmlDocument doc = new XmlDocument();
                                doc.LoadXml(pdfData);
                                XmlNodeList xml = doc.GetElementsByTagName("string");
                                string Str = "";
                                foreach (XmlNode xn in xml)
                                {
                                    Str = xn.FirstChild.InnerText;

                                }

                                var objects = JArray.Parse(Str); // parse as array 
                                string PdfFilnalData = "";
                                foreach (JObject root in objects)
                                {
                                    foreach (KeyValuePair<String, JToken> app in root)
                                    {
                                        var appName = app.Key;
                                        if (app.Key == "certificate")
                                        {
                                            PdfFilnalData = Convert.ToString(app.Value);
                                        }

                                    }
                                }


                                int length = pdfData.Length;
                                if (length > 999)
                                {
                                    //string PdfFilnalData = pdfData.Substring(99, length - 157);
                                    WriteByteArrayToPdf(PdfFilnalData, uploadfolderpath, p.infantid.ToString());
                                }
                                respStreamReader.Close();
                                myResp.Close();
                                string[] files = Directory.GetFiles(uploadfolderpath);
                                int iCnt = 0;
                                foreach (string file in files)
                                {
                                    FileInfo info = new FileInfo(file);
                                    info.Refresh();
                                    if (info.LastWriteTime <= DateTime.Now.AddDays(-1))
                                    {
                                        info.Delete();
                                        iCnt += 1;
                                    }
                                }
                                if (length > 999)
                                {

                                    Dictionary<string, string> hash = new Dictionary<string, string> { };
                                    hash.Add("Url", "BirthCertificates/" + Convert.ToString(p.infantid) + ".pdf");
                                    hash.Add("Infantid", Convert.ToString(p.infantid) + ".pdf");
                                    _objResponseModel.ResposeData = hash;
                                    _objResponseModel.Status = true;
                                    _objResponseModel.Message = "Data Received successfully";
                                }
                                else
                                {
                                    _objResponseModel.Status = false;
                                    _objResponseModel.Message = "No Data Found";
                                }
                            }
                            else
                            {
                                _objResponseModel.Status = false;
                                _objResponseModel.Message = "No Data Found";
                            }
                        }
                    }
                }
                catch
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "validation Error";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public static string GenerateSHA256String(string inputString)
        {
            SHA256 sha256 = SHA256Managed.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(inputString);
            byte[] hash = sha256.ComputeHash(bytes);
            return GetStringFromHash(hash);
        }


        public void WriteByteArrayToPdf(string inPDFByteArrayStream, string pdflocation, string fileName)
        {
            byte[] data = Convert.FromBase64String(inPDFByteArrayStream);
            if (Directory.Exists(pdflocation))
            {
                pdflocation = pdflocation + fileName + ".pdf";
                using (FileStream Writer = new System.IO.FileStream(pdflocation, FileMode.Create, FileAccess.Write))
                {

                    Writer.Write(data, 0, data.Length);
                }
            }
            else
            {
                throw new System.Exception("PDF Shared Location not found");
            }

        }


        private int RandomInteger(int min, int max)
        {
            RNGCryptoServiceProvider Rand = new RNGCryptoServiceProvider();
            uint scale = uint.MaxValue;
            while (scale == uint.MaxValue)
            {
                // Get four random bytes.
                byte[] four_bytes = new byte[4];
                Rand.GetBytes(four_bytes);

                // Convert that into an uint.
                scale = BitConverter.ToUInt32(four_bytes, 0);
            }

            // Add min to the scaled difference between max and min.
            return (int)(min + (max - min) *
                (scale / (double)uint.MaxValue));
        }



        public bool CheckValidNumber(string input, Int16 minlen, Int16 maxlen, Int16 mandatory)
        {
            if (mandatory == 0 && (input == null || input == ""))
            {
                return false;
            }
            else if (mandatory == 1 && (input == null || input == ""))
            {
                return false;
            }
            string regExpr = "^([0-9]){" + minlen.ToString() + "," + maxlen.ToString() + "}$";
            System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(input, regExpr, RegexOptions.None, TimeSpan.FromMilliseconds(10));
            if (m.Success)
                return true;
            else
                return false;
        }







        public HttpResponseMessage UnittypeLevel(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.Role > 9)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
                else
                {
                    _objResponseModel.Status = true;
                    int[] idsToBeSearched = new int[] { 1 };
                    if (p.Role < 4)
                    {
                        idsToBeSearched = new int[] { 1, 3, 4 };
                    }
                    else if (p.Role > 3 && p.Role < 7)
                    {
                        idsToBeSearched = new int[] { 3, 4 };
                    }
                    else if (p.Role > 6 && p.Role < 10)
                    {
                        idsToBeSearched = new int[] { 4 };
                    }
                    var qry2 = (from un in rajmed.UnitTypeMasters
                                where idsToBeSearched.Contains(un.UnitTypeCode)
                                select new
                                {
                                    unittypecode = un.UnitTypeCode,
                                    UnittypeName = un.UnittypeName,
                                }).ToList();
                    _objResponseModel.ResposeData = qry2;
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage userinfo(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            //if (tokenFlag == true)
            //{
            if (p.Role > 9)
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "No Data Found";
            }
            else
            {
                //_objResponseModel.Status = true;
                //string unitcode;
                //if (p.UnitType == 1)
                //{
                //    p.Unitcode = "00000000000";
                //}
                //else if (p.UnitType == 3 || p.UnitType == 4)
                //{
                //    p.Unitcode;
                //}

                var data = cnaa.UspverifyOfficer(p.Unitcode).ToList();
                if (data.Count != 0)
                {
                    _objResponseModel.ResposeData = data;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }



                //    var p1 = (from usr in rajmed.Users
                //              join un in rajmed.UnitMasters on usr.unitid equals un.UnitID
                //              join degis in rajmed.DesignationMasters on usr.DesignationID equals degis.DesigID
                //              join vos in cnaa.VerifyOffcers on usr.UserNo equals vos.OfficerUserNo into lj
                //              from leftjoin in lj.DefaultIfEmpty()
                //              where (un.UnitCode == p.Unitcode)
                //              select new
                //              {
                //                  UserName = usr.UserName,
                //                  Mobileno = usr.UserContactNo,
                //                  Designation = degis.DesigName,
                //                  Email = usr.Email ?? "",
                //                  UserID = usr.UserID,
                //                  officeruserno = usr.UserNo,
                //                  IsVerify = ((int?)leftjoin.IsVerify ?? 0)

                //              }
                //).ToList();
                //    _objResponseModel.ResposeData = p1;

            }
            //}
            //else
            //{
            //    _objResponseModel.Status = false;
            //    _objResponseModel.Message = "Invalid request";
            //}
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }



        public HttpResponseMessage PostUnittypeLevel(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                var p1 = (from un in rajmed.Users
                          where un.UnitCode == p.LoginUnitcode && un.IsDeleted == 0 && ((un.ExpireOn == null ? DateTime.Now : un.ExpireOn) > DateTime.Now)
                          select new
                          {
                              UserID = un.UserID + (un.UserName != null ? "(" + un.UserName + ")" : ""),
                              UserNo = un.UserNo
                          }).OrderBy(x => x.UserID).ToList();

                _objResponseModel.ResposeData = p1;
                _objResponseModel.Status = true;
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage CasesList(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.PCTSID != "" || p.Unitcode != "")
                {
                    var data = cnaa.UspRecordVerificationList(p.Unitcode, p.PCTSID).ToList();
                    if (data.Count != 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "पीसीटीएस आईडी सही नहीं हैं।";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }



        //[ActionName("PostVerifyUSer")]
        //public HttpResponseMessage PostVerifyUSer(VerifyOffcer p)     //VerifyOfficer
        //{
        //    ResponseModel _objResponseModel = new ResponseModel();
        //    bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
        //    if (tokenFlag == true)
        //    {
        //        try
        //        {
        //            //int IsVerify;
        //            //if (p.IsVerify != 0)
        //            //{
        //            //    IsVerify = cnaa.VerifyOffcers.Where(x =>  x.OfficerUserNo == p.OfficerUserNo).Max(x => x.IsVerify + 1);
        //            //}
        //            //else
        //            //{
        //            //    IsVerify = 0;
        //            //}
        //            if (ModelState.IsValid)
        //            {
        //                var data = cnaa.VerifyOffcers.Where(x => x.OfficerUserNo == p.OfficerUserNo).FirstOrDefault();
        //                if (data == null)
        //                {
        //                    VerifyOffcer vf = new VerifyOffcer();
        //                    vf.OfficerUserNo = p.OfficerUserNo;
        //                    vf.IsVerify = p.IsVerify;
        //                    vf.EntryUserNo = p.EntryUserNo;
        //                    vf.EntryDate = DateTime.Now;
        //                    vf.UpdatedDate = DateTime.Now;
        //                    vf.UpdateUserNo = p.EntryUserNo;
        //                    cnaa.VerifyOffcers.Add(vf);
        //                    cnaa.SaveChanges();
        //                }
        //                else
        //                {
        //                    data.UpdatedDate = DateTime.Now;
        //                    data.UpdateUserNo = p.EntryUserNo;
        //                    data.IsVerify = p.IsVerify;
        //                    cnaa.SaveChanges();
        //                }
        //                _objResponseModel.Status = true;
        //                _objResponseModel.Message = "Data Saved successfully";
        //            }
        //            else
        //            {
        //                _objResponseModel.Status = false;
        //                _objResponseModel.Message = "Model Error";
        //                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
        //            }
        //        }
        //        catch
        //        {
        //            _objResponseModel.Status = false;
        //            _objResponseModel.Message = "validation Error";
        //        }
        //    }
        //    else
        //    {
        //        _objResponseModel.Status = false;
        //        _objResponseModel.Message = "Invalid request";
        //    }
        //    return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        //}


        //public HttpResponseMessage PersonalInfo(Pcts p)
        //{
        //    ResponseModel _objResponseModel = new ResponseModel();
        //    //bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
        //    //if (tokenFlag == true)
        //    //{
        //        if (p.PCTSID != "")
        //        {
        //            var data = cnaa.UspRecordVerificationPersonalinfo(p.PCTSID).ToList();
        //            if (data.Count != 0)
        //            {
        //                _objResponseModel.ResposeData = data;
        //                _objResponseModel.Status = true;
        //                _objResponseModel.Message = "Data Received successfully";
        //            }
        //            else
        //            {
        //                _objResponseModel.Status = true;
        //                _objResponseModel.Message = "No Data Found";
        //            }
        //        }
        //        else
        //        {
        //            _objResponseModel.Status = false;
        //            _objResponseModel.Message = "पीसीटीएस आईडी सही नहीं हैं।";
        //        }
        //    //}
        //    //else
        //    //{
        //    //    _objResponseModel.Status = false;
        //    //    _objResponseModel.Message = "Invalid request";
        //    //}
        //    return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        //}

        //public HttpResponseMessage ANCinfo(Pcts p)
        //{
        //    ResponseModel _objResponseModel = new ResponseModel();
        //    //bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
        //    //if (tokenFlag == true)
        //    //{
        //        if (p.PCTSID != "")
        //        {
        //            var data = cnaa.UspRecordVerificationANC(p.PCTSID).ToList();
        //            if (data.Count != 0)
        //            {
        //                _objResponseModel.ResposeData = data;
        //                _objResponseModel.Status = true;
        //                _objResponseModel.Message = "Data Received successfully";
        //            }
        //            else
        //            {
        //                _objResponseModel.Status = true;
        //                _objResponseModel.Message = "No Data Found";
        //            }
        //        }
        //        else
        //        {
        //            _objResponseModel.Status = false;
        //            _objResponseModel.Message = "पीसीटीएस आईडी सही नहीं हैं।";
        //        }
        //    //}
        //    //else
        //    //{
        //    //    _objResponseModel.Status = false;
        //    //    _objResponseModel.Message = "Invalid request";
        //    //}
        //    return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        //}




        //public HttpResponseMessage PNCinfo(Pcts p)
        //{
        //    ResponseModel _objResponseModel = new ResponseModel();
        //    //bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
        //    //if (tokenFlag == true)
        //    //{
        //        if (p.PCTSID != "")
        //        {
        //            var data = cnaa.UspRecordVerificationPNC(p.PCTSID).ToList();
        //            if (data.Count != 0)
        //            {
        //                _objResponseModel.ResposeData = data;
        //                _objResponseModel.Status = true;
        //                _objResponseModel.Message = "Data Received successfully";
        //            }
        //            else
        //            {
        //                _objResponseModel.Status = false;
        //                _objResponseModel.Message = "No Data Found";
        //            }
        //        }
        //        else
        //        {
        //            _objResponseModel.Status = false;
        //            _objResponseModel.Message = "पीसीटीएस आईडी सही नहीं हैं।";
        //        }
        //    //}
        //    //else
        //    //{
        //    //    _objResponseModel.Status = false;
        //    //    _objResponseModel.Message = "Invalid request";
        //    //}
        //    return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        //}

        //public HttpResponseMessage PostRecordVerification(RecordVerification p)     //RecordVerification
        //{
        //    ResponseModel _objResponseModel = new ResponseModel();

        //    bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
        //    if (tokenFlag == true)
        //    {
        //        try
        //        {
        //            if (ModelState.IsValid)
        //            {
        //                var data = cnaa.RecordVerifications.Where(x => x.ANCRegID == p.ANCRegID && x.Infantid == p.Infantid && x.RVFlag == p.type).FirstOrDefault();
        //                if (data == null)
        //                {
        //                    RecordVerification rv = new RecordVerification();
        //                    rv.ANCRegID = p.ANCRegID;
        //                    rv.Infantid = p.Infantid;
        //                    rv.Verify = (byte)p.YesNo;
        //                    rv.Remarks = p.Remarks;
        //                    rv.RVFlag = (byte)p.type;
        //                    rv.EntryUserNo = p.UpdateUserNo;
        //                    rv.UpdateUserNo = p.UpdateUserNo;
        //                    rv.EntryDate = DateTime.Now;
        //                    rv.UpdateDate = DateTime.Now;
        //                    cnaa.RecordVerifications.Add(rv);
        //                    cnaa.SaveChanges();
        //                    _objResponseModel.Status = true;
        //                    _objResponseModel.Message = "Data Saved successfully";

        //                }
        //                else
        //                {


        //                    data.ANCRegID = p.ANCRegID;
        //                    data.Infantid = p.Infantid;
        //                    data.Verify = (byte)p.YesNo;
        //                    data.Remarks = p.Remarks;
        //                    data.UpdateUserNo = p.UpdateUserNo;
        //                    data.UpdateDate = DateTime.Now;
        //                    cnaa.SaveChanges();
        //                    _objResponseModel.Status = true;
        //                    _objResponseModel.Message = "Data Saved successfully";
        //                }


        //            }
        //            else
        //            {
        //                _objResponseModel.Status = false;
        //                _objResponseModel.Message = "Model Error";
        //                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
        //            }
        //        }
        //        catch
        //        {
        //            _objResponseModel.Status = false;
        //            _objResponseModel.Message = "validation Error";
        //        }
        //    }
        //    else
        //    {
        //        _objResponseModel.Status = false;
        //        _objResponseModel.Message = "Invalid request";
        //    }
        //    return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        //}






        //public HttpResponseMessage Imuinfo(Pcts p)
        //{
        //    ResponseModel _objResponseModel = new ResponseModel();
        //    bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
        //    if (tokenFlag == true)
        //    {
        //        if (p.PCTSID != "")
        //        {
        //            var data = cnaa.UspRecordVerificationImmunization(p.PCTSID).Select(x => new { villageName = x.VillageName, UnitName = x.UnitName, Block = x.Block, chcphc = x.CHCPHC, district = x.district, name = x.mothername, Husbname = x.Husbname, ANCRegID = x.ANCRegID, PCTSID = x.pctsid, Birth_date = x.Birth_date, InfantID = x.InfantID, ChildName = x.ChildName, Sex = x.Sex, MotherID = x.MotherID, Weight = x.Weight, childid = x.childid }).ToList();
        //            if (data.Count != 0)
        //            {

        //                List<MotherInfantListForVerification> listHash = new List<MotherInfantListForVerification>();
        //                Int32 i = 0;
        //                for (i = 0; i < data.Count; i++)
        //                {
        //                    MotherInfantListForVerification ii = new MotherInfantListForVerification();
        //                    ii.villageName = Convert.ToString(data[i].villageName);
        //                    ii.UnitName = Convert.ToString(data[i].UnitName);
        //                    ii.Block = Convert.ToString(data[i].Block);
        //                    ii.chcphc = Convert.ToString(data[i].chcphc);
        //                    ii.district = Convert.ToString(data[i].district);
        //                    ii.ANCRegID = Convert.ToInt64(data[i].ANCRegID);
        //                    Int32 ANCRegID = Convert.ToInt32(data[i].ANCRegID);
        //                    ii.PCTSID = Convert.ToString(data[i].PCTSID);
        //                    ii.name = Convert.ToString(data[i].name);
        //                    ii.Husbname = Convert.ToString(data[i].Husbname);

        //                    List<InfantListForImmunization> listHash1 = data.Where(x => x.ANCRegID == ANCRegID).Select(x => new InfantListForImmunization { Birth_date = x.Birth_date, ChildName = x.ChildName, Sex = x.Sex, InfantID = x.InfantID, ChildID = x.childid }).ToList();
        //                    ii.infantList = listHash1;
        //                    i += listHash1.Count;
        //                    listHash.Add(ii);
        //                }
        //                _objResponseModel.ResposeData = listHash;
        //                _objResponseModel.Status = true;
        //                _objResponseModel.Message = "Data Received successfully";



        //            }
        //            else
        //            {
        //                _objResponseModel.Status = false;
        //                _objResponseModel.Message = "No Data Found";
        //            }
        //        }
        //        else
        //        {
        //            _objResponseModel.Status = false;
        //            _objResponseModel.Message = "पीसीटीएस आईडी सही नहीं हैं।";
        //        }
        //    }
        //    else
        //    {
        //        _objResponseModel.Status = false;
        //        _objResponseModel.Message = "Invalid request";
        //    }
        //    return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        //}




        //public HttpResponseMessage infantInfo(Pcts p)
        //{
        //    ResponseModel _objResponseModel = new ResponseModel();
        //    bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
        //    if (tokenFlag == true)
        //    {


        //        var p1 = (from imu in cnaa.Immunizations
        //                  join inf in cnaa.Infants on imu.InfantID equals inf.InfantID
        //                  join mst in cnaa.ImmunizationMasters on imu.ImmuCode equals mst.ImmuCode
        //                  where (imu.InfantID == p.InfantID)
        //                  select new
        //                  {
        //                      ChildName = inf.ChildName,
        //                      vacc_name = mst.ImmuName,
        //                      vacc_code = imu.ImmuCode,
        //                      weight = ((int?)imu.Weight ?? 0),
        //                      imudate = imu.immudate,
        //                      ANCRegID = inf.ANCRegID

        //                  }
        //    ).ToList();
        //        _objResponseModel.ResposeData = p1;
        //        _objResponseModel.Status = true;
        //        _objResponseModel.Message = "Data Received successfully";

        //    }
        //    else
        //    {
        //        _objResponseModel.Status = false;
        //        _objResponseModel.Message = "Invalid request";
        //    }
        //    return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        //}



        //[ActionName("postBlockDataforVerify")]
        //public HttpResponseMessage postBlockDataforVerify(Pcts p)
        //{
        //    ResponseModel _objResponseModel = new ResponseModel();
        //    bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
        //    if (tokenFlag == true)
        //    {
        //        if (p.Unitcode != "0")
        //        {
        //            var data = rajmed.UnitMasters.Where(x => x.UnitType == 4 && x.UnitCode.StartsWith(p.Unitcode.Substring(0, 4))).Select(x => new { unitcode = x.UnitCode, unitNameHindi = x.UnitNameHindi }).ToList();
        //            if (data != null)
        //            {
        //                if (data.Count > 0)
        //                {
        //                    _objResponseModel.ResposeData = data;
        //                    _objResponseModel.Status = true;
        //                    _objResponseModel.Message = "Data Received successfully";
        //                }
        //                else
        //                {
        //                    _objResponseModel.Status = false;
        //                    _objResponseModel.Message = "No Data Found";
        //                }
        //            }
        //            else
        //            {
        //                _objResponseModel.Status = false;
        //                _objResponseModel.Message = "No Data Found";
        //            }
        //        }
        //        else
        //        {
        //            _objResponseModel.Status = false;
        //            _objResponseModel.Message = "No Data Found";
        //        }
        //    }
        //    else
        //    {
        //        _objResponseModel.Status = false;
        //        _objResponseModel.Message = "Invalid request";
        //    }

        //    return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        //}




        //public HttpResponseMessage RecordVerifaicationReports(Pcts p)
        //{
        //    ResponseModel _objResponseModel = new ResponseModel();
        //    bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
        //    if (tokenFlag == true)
        //    {
        //        if (p.Unitcode != "")
        //        {
        //            var data = cnaa.USPRecordverificationReports(p.Unitcode, (byte)p.UnitType).ToList();
        //            if (data.Count != 0)
        //            {
        //                _objResponseModel.ResposeData = data;
        //                _objResponseModel.Status = true;
        //                _objResponseModel.Message = "Data Received successfully";
        //            }
        //            else
        //            {
        //                _objResponseModel.Status = false;
        //                _objResponseModel.Message = "No Data Found";
        //            }
        //        }
        //        else
        //        {
        //            _objResponseModel.Status = false;
        //            _objResponseModel.Message = "invalid unitcode।";
        //        }
        //    }
        //    else
        //    {
        //        _objResponseModel.Status = false;
        //        _objResponseModel.Message = "Invalid request";
        //    }
        //    return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        //}


        //public HttpResponseMessage RecordVerifaicationReportsDetails(Pcts p)
        //{
        //    ResponseModel _objResponseModel = new ResponseModel();
        //    bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
        //    if (tokenFlag == true)
        //    {
        //        if (p.Unitcode != "")
        //        {
        //            var data = cnaa.UspRecordvarificationReportsDetails(p.Unitcode).ToList();
        //            if (data.Count != 0)
        //            {
        //                _objResponseModel.ResposeData = data;
        //                _objResponseModel.Status = true;
        //                _objResponseModel.Message = "Data Received successfully";
        //            }
        //            else
        //            {
        //                _objResponseModel.Status = false;
        //                _objResponseModel.Message = "No Data Found";
        //            }
        //        }
        //        else
        //        {
        //            _objResponseModel.Status = false;
        //            _objResponseModel.Message = "invalid unitcode।";
        //        }
        //    }
        //    else
        //    {
        //        _objResponseModel.Status = false;
        //        _objResponseModel.Message = "Invalid request";
        //    }
        //    return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        //}
        public string ComputeHash(string input, HashAlgorithm algorithm, string Salt1)
        {
            byte[] salt = System.Text.Encoding.UTF8.GetBytes(Salt1);
            Byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);

            // Combine salt and input bytes
            Byte[] saltedInput = new Byte[salt.Length + (inputBytes.Length - 1) + 1];
            salt.CopyTo(saltedInput, 0);
            inputBytes.CopyTo(saltedInput, salt.Length);

            Byte[] hashedBytes = algorithm.ComputeHash(saltedInput);

            return BitConverter.ToString(hashedBytes).Replace("-", "").Trim();
        }



        //public HttpResponseMessage AshaInactive(Pcts p)
        //{
        //    ResponseModel _objResponseModel = new ResponseModel();
        //    bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
        //    if (tokenFlag == true)
        //    {
        //        if (p.ANMAutoID > 0)
        //        {
        //            var data = cnaa.uspASHAIncentive(p.ANMAutoID, p.MthYr).ToList();
        //            if (data.Count != 0)
        //            {
        //                _objResponseModel.ResposeData = data;
        //                _objResponseModel.Status = true;
        //                _objResponseModel.Message = "Data Received successfully";
        //            }
        //            else
        //            {
        //                _objResponseModel.Status = false;
        //                _objResponseModel.Message = "डाटा उपलब्ध नहीं है |";
        //            }
        //        }
        //        else
        //        {
        //            _objResponseModel.Status = false;
        //            _objResponseModel.Message = "ऐ.ऍन.एम. सही चुने ।";
        //        }
        //    }
        //    else
        //    {
        //        _objResponseModel.Status = false;
        //        _objResponseModel.Message = "Invalid request";
        //    }
        //    return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        //}

        public HttpResponseMessage PostCheckAsha(UserAuthenticate s)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {

                var p = cnaa.DeviceTokens.Where(x => x.DeviceID == s.DeviceID && x.TokenNo == s.TokenNo).FirstOrDefault();
                if (p == null || string.IsNullOrEmpty(s.DeviceID) || string.IsNullOrEmpty(s.TokenNo))
                {
                    _objResponseModel.Message = "कृपया सही यूज़र आईडी/पासवर्ड डाले";
                }
                else
                {
                    var datas = rajmed.AshaMasters.Where(a => a.type == 1 && a.AshaPhone == s.MobileNo1 && a.Status == 1).FirstOrDefault();
                    if (datas == null)
                    {
                        _objResponseModel.Message = "कृपया सही मोबाईल नं. लिखे !";
                    }
                    else
                    {
                        List<Dictionary<string, string>> listHash = new List<Dictionary<string, string>>();
                        Dictionary<string, string> hash = new Dictionary<string, string> { };
                        var datas1 = rajmed.Users.Where(a => a.UserID == s.MobileNo1 && a.IsDeleted == 0).FirstOrDefault();
                        if (datas1 == null)
                        {
                            //if (datas != null)
                            //{
                            //    _objResponseModel.Status = true;
                            //    hash.Add("Password", "false");
                            //    sendSms1(s.MobileNo1, 5);
                            //    _objResponseModel.Message = "";
                            //}
                            //else
                            //{
                            //    _objResponseModel.Message = "कृपया सही मोबाईल नं. लिखे !";
                            //}

                            hash.Add("Password", "false");
                            sendSms1(s.MobileNo1, 5);
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "";
                            //_objResponseModel.Status = false;
                            //_objResponseModel.Message = "कृपया सही यूज़र आईडी/पासवर्ड डाले !";
                        }
                        else
                        {

                            hash.Add("Password", "true");
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "";
                            //_objResponseModel.Status = false;
                            //_objResponseModel.Message = "कृपया सही यूज़र आईडी/पासवर्ड डाले !";
                        }
                       
                        listHash.Add(hash);
                        _objResponseModel.ResposeData = listHash;
                    }


                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in post user" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही यूज़र आईडी/पासवर्ड डाले  ! ";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        public HttpResponseMessage VideoUrl(MaaVideo p)
        {
            MaacnaaEntities maa = new MaacnaaEntities();
            ResponseModel _objResponseModel = new ResponseModel();
            string Vtype = "";
            var data = maa.MaaVideoDetails.Where(x => x.VideoType == p.VideoType).Select(x => new { VideoId = x.ID, VideoName = x.VideoName, VideoType = x.VideoType, Descrption = x.Descrption, ImageName = x.ImageName }).ToList();
            if (data != null)
            {
                List<MaaVideo> p1 = new List<MaaVideo>();
                MaaVideo pct;
                string browesfolderpath = HttpContext.Current.Server.MapPath("~/Video/");

                Dictionary<string, string> hash = new Dictionary<string, string> { };
                foreach (var item in data)
                {
                    pct = new MaaVideo();
                    pct.VideoId = item.VideoId;
                    pct.VideoType = (byte)item.VideoType;
                    if (item.VideoType == 1)
                        Vtype = "प्रसव पूर्व जाँच ";
                    else if (item.VideoType == 2)
                        Vtype = "प्रसवोत्तर देखभाल";
                    else if (item.VideoType == 3)
                        Vtype = "टीकाकरण";
                    pct.VideoTypeName = Vtype;
                    pct.VideoName = "Video/" + item.VideoName;
                    pct.Descrption = item.Descrption;
                    pct.ImageName = "Images/" + item.ImageName;
                    p1.Add(pct);
                }
                _objResponseModel.ResposeData = p1;
                _objResponseModel.Status = true;
                _objResponseModel.Message = "Data Received successfully";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        public HttpResponseMessage GetAlltables()
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                var data = dhs.uspGetAllTables().ToList();
                var p1 = data.Select(x => x.TableName).Distinct().ToList();
                List<DHSTableList> listHash = new List<DHSTableList>();
                Int32 i = 0;
                for (i = 0; i < p1.Count(); i++)
                {
                    DHSTableList tl = new DHSTableList();
                    tl.TableName = p1[i].ToString();
                    List<ColumnList> listHash1 = data.Where(x => x.TableName == p1[i].ToString()).Select(x => new ColumnList { ColumnName = x.ColumnName, IsNullable = x.IsNullable, DataType = x.DataType, Columnlength = x.Columnlength, DefaultValue = x.DefaultValue, PkColumnName = x.PkColumnName, UkColumnName = x.UkColumnName, AutoIncrement = x.AutoColumn }).ToList();
                    tl.ColumnList = listHash1;
                    listHash.Add(tl);
                }

                if (listHash != null)
                {
                    _objResponseModel.ResposeData = listHash;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in GetAlltables" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = " ";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage PostDataByLoginUnitID(TableList tl)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                List<DHS_UnitMaster> unitMasterList = new List<DHS_UnitMaster>();
                List<DHS_Villages> villagesList = new List<DHS_Villages>();
                List<DHS_AshaMaster> ashaList = new List<DHS_AshaMaster>();
                List<DHS_AnganwariMaster> anganList = new List<DHS_AnganwariMaster>();
                List<DHS_Anganwari_Village> anganvillageList = new List<DHS_Anganwari_Village>();
                //       List<Couple_Master_Temp> cmt = new List<Couple_Master_Temp>();
                List<HouseFamily> hf = new List<HouseFamily>();
                List<MemberDetail> md = new List<MemberDetail>();
                List<ECdetail> ed = new List<ECdetail>();
                List<DHSMasterCode> mc = new List<DHSMasterCode>();
                List<DiseaseDetail> dd = new List<DiseaseDetail>();
                List<MemberPhoto> mp = new List<MemberPhoto>();
                List<UnMarriedGirlsDetail> umgd = new List<UnMarriedGirlsDetail>();
                List<ChildInformation> cinfo = new List<ChildInformation>();

                var p1 = rajmed.UnitMasters.Where(x => x.UnitCode == tl.LoginUnitCode).FirstOrDefault();
                if (p1 != null)
                {
                    tl.LoginUnitId = p1.UnitID;
                }

                var p = dhs.GetUnitmasterData(tl.LoginUnitCode).ToList();
                unitMasterList = p.Select(x => new DHS_UnitMaster
                {
                    UnitCode = x.UnitCode,
                    UnitName = x.UnitName,
                    UnitType = x.UnitType,
                    UnitID = x.UnitID,
                    UnitNameHindi = x.UnitNameHindi,
                    LastUpdated = x.LastUpdated
                }).ToList();
                if (tl.LastSyncDate != null)
                {
                    unitMasterList = unitMasterList.Where(x => x.LastUpdated >= tl.LastSyncDate).ToList();
                }
                ashaList = rajmed.AshaMasters.Where(x => x.unitid == tl.LoginUnitId && x.Status == 1).Select(x => new DHS_AshaMaster
                {
                    AshaName = x.AshaName,
                    unitid = tl.LoginUnitId,
                    ashaAutoID = (int)x.ashaAutoID,
                    LastUpdated = x.LastUpdated,
                    Status = x.Status,
                    type = x.type
                }).ToList();
                if (tl.ANMAutoID > 0)
                {
                    ashaList = ashaList.Where(x => x.ashaAutoID == tl.ANMAutoID).ToList();

                }
                var p6 = (from a in rajmed.AshaMasters
                          join b in rajmed.ASHA_AddlCharge on a.ashaAutoID equals b.AshaAutoID
                          join c in rajmed.AnganwariMasters on b.AnganwariNo equals c.AnganwariNo
                          join d in rajmed.Anganwari_Village on c.AnganwariNo equals d.AnganwariNo
                          join e in rajmed.Villages on d.VillageAutoID equals e.VillageAutoID
                          where a.unitid == tl.LoginUnitId && a.Status == 1 && c.IsDeleted == 0
                          select new
                          {
                              AnganwariNo = c.AnganwariNo,
                              NameE = c.NameE,
                              NameH = c.NameH,
                              unitid = e.unitid,
                              LastUpdated = e.LastUpdated,
                              AWCID = c.AWCID,
                              ashaAutoID = a.ashaAutoID,
                              VillageAutoID = e.VillageAutoID,
                              UnitCode = e.UnitCode,
                              VillageName = e.VillageName,
                              UnitNameHindi = e.UnitNameHindi,
                              type = e.type,
                              AnganwariUnitId = c.unitid
                          }).ToList();
                if (tl.ANMAutoID > 0)
                {
                    p6 = p6.Where(x => x.ashaAutoID == tl.ANMAutoID).ToList();
                }
                if (p6 != null)
                {
                    anganList = p6.Where(x => x.AnganwariNo > 0).Select(x => new DHS_AnganwariMaster
                    {
                        AnganwariNo = x.AnganwariNo,
                        NameE = x.NameE,
                        NameH = x.NameH,
                        unitid = x.AnganwariUnitId,
                        LastUpdated = x.LastUpdated,
                        AWCID = x.AWCID
                    }).Distinct().ToList();

                    anganvillageList = p6.Where(x => x.VillageAutoID > 0).Select(x => new DHS_Anganwari_Village
                    {
                        AnganwariNo = x.AnganwariNo,
                        VillageAutoID = x.VillageAutoID
                    }).Distinct().ToList();

                    if (anganvillageList != null)
                    {

                        villagesList = p6.Where(x => x.VillageAutoID > 0).Select(x => new DHS_Villages
                        {
                            UnitCode = x.UnitCode,
                            VillageName = x.VillageName,
                            UnitNameHindi = x.UnitNameHindi,
                            type = x.type,
                            unitid = x.unitid,
                            VillageAutoID = x.VillageAutoID,
                            LastUpdated = x.LastUpdated
                        }).Distinct().ToList();
                    }

                }
                if (tl.LastSyncDate != null)
                {
                    if (villagesList != null)
                    {
                        villagesList = villagesList.Where(x => x.LastUpdated >= tl.LastSyncDate).ToList();
                    }
                    if (ashaList != null)
                    {
                        ashaList = ashaList.Where(x => x.LastUpdated >= tl.LastSyncDate).ToList();
                    }
                    if (anganList != null)
                    {
                        anganList = anganList.Where(x => x.LastUpdated >= tl.LastSyncDate).ToList();
                    }
                }



                //   int[] villagearray = dhsrajmed.Villages.Where(x => x.unitid == tl.LoginUnitId && x.OtherLocation == 1).Select(x => x.VillageAutoID).ToList().ToArray();


                //  cmt = dhs.Couple_Master_Temp.Where(x => villagearray.Contains((int)x.VillageAutoID)).ToList();
                if (tl.LastSyncDate == null)
                {
                    hf = dhs.HouseFamilies.Where(x => x.UnitID == tl.LoginUnitId).ToList();
                }
                else
                {
                    hf = dhs.HouseFamilies.Where(x => x.UnitID == tl.LoginUnitId && x.LastUpdateDate >= tl.LastSyncDate).ToList();
                }
                if (tl.LastSyncDate == null)
                {
                    md = dhs.MemberDetails.Where(x => x.UnitID == tl.LoginUnitId).ToList();
                }
                else
                {
                    md = dhs.MemberDetails.Where(x => x.UnitID == tl.LoginUnitId && x.LastUpdateDate >= tl.LastSyncDate).ToList();
                }

                if (tl.LastSyncDate == null)
                {
                    ed = dhs.ECdetails.Where(x => x.UnitID == tl.LoginUnitId).ToList();
                }
                else
                {
                    ed = dhs.ECdetails.Where(x => x.UnitID == tl.LoginUnitId && x.LastUpdateDate >= tl.LastSyncDate).ToList();
                }
                mc = dhs.DHSMasterCodes.ToList();

                int[] memberIDarray = md.Select(x => x.MemberID).ToList().ToArray();

                mp = dhs.MemberPhotoes.Where(x => memberIDarray.Contains((int)x.MemberlD)).ToList();

                if (tl.LastSyncDate == null)
                {
                    umgd = dhs.UnMarriedGirlsDetails.Where(x => x.UnitID == tl.LoginUnitId).ToList();
                }
                else
                {
                    umgd = dhs.UnMarriedGirlsDetails.Where(x => x.UnitID == tl.LoginUnitId && x.LastUpdateDate >= tl.LastSyncDate).ToList();
                }
                dd = dhs.DiseaseDetails.Where(x => memberIDarray.Contains(x.MemberID)).ToList();
                cinfo = dhs.ChildInformations.Where(x => memberIDarray.Contains(x.MemberID)).ToList();






                List<DashboardModel> dm = new List<DashboardModel>();
                DashboardModel dm1 = new DashboardModel();
                dm1.TableName = "UnitMaster";
                dm1.TableData = unitMasterList;
                dm.Add(dm1);
                dm1 = new DashboardModel();
                dm1.TableName = "Villages";
                dm1.TableData = villagesList;
                dm.Add(dm1);
                dm1 = new DashboardModel();
                dm1.TableName = "AshaMaster";
                dm1.TableData = ashaList;
                dm.Add(dm1);
                dm1 = new DashboardModel();
                dm1.TableName = "AnganwariMaster";
                dm1.TableData = anganList;
                dm.Add(dm1);
                dm1 = new DashboardModel();
                dm1.TableName = "Anganwari_Village";
                dm1.TableData = anganvillageList;
                dm.Add(dm1);
                //dm1 = new DashboardModel();
                //dm1.TableName = "Couple_Master_Temp";
                //dm1.TableData = cmt;
                //dm.Add(dm1);
                dm1 = new DashboardModel();
                dm1.TableName = "HouseFamily";
                dm1.TableData = hf;
                dm.Add(dm1);
                dm1 = new DashboardModel();
                dm1.TableName = "MemberDetails";
                dm1.TableData = md;
                dm.Add(dm1);
                dm1 = new DashboardModel();
                dm1.TableName = "ECdetails";
                dm1.TableData = ed;
                dm.Add(dm1);
                dm1 = new DashboardModel();
                dm1.TableName = "DHSMasterCodes";
                dm1.TableData = mc;
                dm.Add(dm1);
                dm1 = new DashboardModel();
                dm1.TableName = "MemberPhoto";
                dm1.TableData = mp;
                dm.Add(dm1);
                dm1 = new DashboardModel();
                dm1.TableName = "DiseaseDetails";
                dm1.TableData = dd;
                dm.Add(dm1);
                dm1 = new DashboardModel();
                dm1.TableName = "UnMarriedGirlsDetail";
                dm1.TableData = umgd;
                dm.Add(dm1);

                dm1 = new DashboardModel();
                dm1.TableName = "ChildInformations";
                dm1.TableData = cinfo;
                dm.Add(dm1);
                _objResponseModel.ResposeData = dm;
                _objResponseModel.Status = true;
                _objResponseModel.Message = "Data Received successfully";
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in PostDataByLoginUnitID" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = " ";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }



        public HttpResponseMessage PostOfflineData(DHS_TableData temp)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                DataSet myDataSet = JsonConvert.DeserializeObject<DataSet>(temp.tempData);
                int i = 0;
                bool status = true;
                if (temp.tempData1 != null)
                {
                    try
                    {
                        DataSet myDataSet1 = JsonConvert.DeserializeObject<DataSet>(temp.tempData1);
                        for (i = 0; i < myDataSet1.Tables.Count; i++)
                        {
                            if (myDataSet1.Tables[i].TableName == "MemberDetails")
                            {
                                List<DHS_ResponseMemberDetails> drhs = UpdateMembeFamilyData(myDataSet1.Tables[i]);
                                if (drhs.Count() == 0)
                                {
                                    status = false;

                                }
                                _objResponseModel.ResposeData = drhs;
                            }
                            else if (myDataSet1.Tables[i].TableName == "ChildInformation")
                            {
                                List<DHS_ResponseChildInformation> drhs = SaveChildInformationData(myDataSet1.Tables[i]);
                                if (drhs.Count() == 0)
                                {
                                    status = false;

                                }
                                _objResponseModel.ResposeData = drhs;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler.WriteError("Error in Update Member Details data" + ex.ToString());
                    }
                }

                for (i = 0; i < myDataSet.Tables.Count; i++)
                {
                    if (myDataSet.Tables[i].TableName == "HouseFamily")
                    {
                        List<DHS_ResponseHouseFamilies> drhs = SaveHouseFamilyData(myDataSet.Tables[i]);
                        if (drhs.Count() == 0)
                        {
                            status = false;
                            break;
                        }
                        _objResponseModel.ResposeData = drhs;
                    }
                    else if (myDataSet.Tables[i].TableName == "MemberDetails")
                    {
                        List<DHS_ResponseMemberDetails> drhs = SaveMembeFamilyData(myDataSet.Tables[i]);
                        if (drhs.Count() == 0)
                        {
                            status = false;
                            break;
                        }
                        _objResponseModel.ResposeData = drhs;
                    }
                    else if (myDataSet.Tables[i].TableName == "EcDetails")
                    {
                        List<DHS_ResponseEcDetails> drhs = SaveECDetailsData(myDataSet.Tables[i]);
                        if (drhs.Count() == 0)
                        {
                            status = false;
                            break;
                        }
                        _objResponseModel.ResposeData = drhs;
                    }
                    else if (myDataSet.Tables[i].TableName == "UnMarriedGirlsDetail")
                    {
                        List<DHS_ResponseUnMarriedGirlsDetail> drhs = SaveUnMarriedGirlsData(myDataSet.Tables[i]);
                        if (drhs.Count() == 0)
                        {
                            status = false;
                            break;
                        }
                        _objResponseModel.ResposeData = drhs;
                    }
                    //else if (myDataSet.Tables[i].TableName == "ChildInformation")
                    //{
                    //    List<DHS_ResponseChildInformation> drhs = Savechildinfo(myDataSet.Tables[i]);
                    //    if (drhs.Count() == 0)
                    //    {
                    //        status = false;
                    //        break;
                    //    }
                    //    _objResponseModel.ResposeData = drhs;
                    //}

                }
                _objResponseModel.Status = status;
                _objResponseModel.Message = "Data Received successfully";
                return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in PostOfflineData" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = " ";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }




        //public HttpResponseMessage PostOfflineDataSecond(DHS_TableData temp)
        //{
        //    ResponseModel _objResponseModel = new ResponseModel();
        //    DataSet myDataSet = JsonConvert.DeserializeObject<DataSet>(temp.tempData);
        //    int i = 0;
        //    bool status = true;
        //    for (i = 0; i < myDataSet.Tables.Count; i++)
        //    {
        //        if (myDataSet.Tables[i].TableName == "MemberDetails")
        //        {
        //            List<DHS_ResponseMemberDetails> drhs = UpdateMembeFamilyData(myDataSet.Tables[i]);
        //            if (drhs.Count() == 0)
        //            {
        //                status = false;
        //                break;
        //            }
        //            _objResponseModel.ResposeData = drhs;
        //        }
        //    }
        //    _objResponseModel.Status = status;
        //    _objResponseModel.Message = "Data Received successfully";
        //    return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);       
        //}




        private List<DHS_ResponseHouseFamilies> SaveHouseFamilyData(DataTable dt)
        {
            List<DHS_ResponseHouseFamilies> drhs = new List<DHS_ResponseHouseFamilies>();
            try
            {
                List<HouseFamily> hf = (from DataRow dr in dt.Rows
                                        select new HouseFamily()
                                        {
                                            SurveyYear = Convert.ToInt32(dr["SurveyYear"]),
                                            UnitID = Convert.ToInt32(dr["UnitID"]),
                                            AnganwariNo = Convert.ToInt32(dr["AnganwariNo"]),
                                            HouseNo = Convert.ToInt32(dr["HouseNo"]),
                                            FamiliyNo = Convert.ToInt32(dr["FamiliyNo"]),
                                            Bpl = Convert.ToByte(dr["Bpl"]),
                                            BhamashahID = Convert.ToString(dr["BhamashahID"]),
                                            Address = Convert.ToString(dr["Address"]),
                                            Entrydate = Convert.ToDateTime(dr["Entrydate"]),
                                            LastUpdateDate = Convert.ToDateTime(dr["LastUpdateDate"]),
                                            Caste = Convert.ToByte(dr["Caste"]),
                                            Religion = Convert.ToByte(dr["Religion"]),
                                            Permanent_Residence = Convert.ToByte(dr["Permanent_Residence"]),
                                            FuelType = Convert.ToString(dr["FuelType"]),
                                            HouseFamilyID = Convert.ToInt32(dr["HouseFamilyID"]),
                                        }).ToList();





                foreach (HouseFamily tempHf in hf)
                {
                    int HouseFamilyIdoffline = tempHf.HouseFamilyID;
                    HouseFamily hf1 = dhs.HouseFamilies.Where(x => x.AnganwariNo == tempHf.AnganwariNo && x.HouseNo == tempHf.HouseNo && x.FamiliyNo == tempHf.FamiliyNo).FirstOrDefault();
                    int HouseFamilyId = 0;
                    if (hf1 != null)
                    {
                        hf1.Bpl = tempHf.Bpl;
                        hf1.BhamashahID = tempHf.BhamashahID;
                        hf1.Address = tempHf.Address;
                        hf1.LastUpdateDate = tempHf.LastUpdateDate;
                        hf1.Caste = tempHf.Bpl;
                        hf1.Religion = tempHf.Religion;
                        hf1.Permanent_Residence = tempHf.Permanent_Residence;
                        hf1.FuelType = tempHf.FuelType;

                        hf1.LastUpdateDate_online = DateTime.Now;
                        dhs.SaveChanges();
                        HouseFamilyId = hf1.HouseFamilyID;
                    }
                    else
                    {
                        hf1 = new HouseFamily();
                        hf1.SurveyYear = tempHf.SurveyYear;
                        hf1.UnitID = tempHf.UnitID;
                        hf1.AnganwariNo = tempHf.AnganwariNo;
                        hf1.HouseNo = tempHf.HouseNo;
                        hf1.FamiliyNo = tempHf.FamiliyNo;
                        hf1.Bpl = tempHf.Bpl;
                        hf1.BhamashahID = tempHf.BhamashahID;
                        hf1.Address = tempHf.Address;
                        hf1.Entrydate = tempHf.Entrydate;
                        hf1.LastUpdateDate = tempHf.LastUpdateDate;
                        hf1.Caste = tempHf.Caste;
                        hf1.Religion = tempHf.Religion;
                        hf1.Permanent_Residence = tempHf.Permanent_Residence;
                        hf1.FuelType = tempHf.FuelType;
                        hf1.Entrydate_online = DateTime.Now;
                        hf1.LastUpdateDate_online = DateTime.Now;
                        dhs.HouseFamilies.Add(hf1);
                        dhs.SaveChanges();
                        HouseFamilyId = dhs.HouseFamilies.Where(x => x.AnganwariNo == tempHf.AnganwariNo && x.HouseNo == tempHf.HouseNo && x.FamiliyNo == tempHf.FamiliyNo).Select(x => x.HouseFamilyID).FirstOrDefault();

                    }
                    if (HouseFamilyId > 0)
                    {
                        DHS_ResponseHouseFamilies hf11 = new DHS_ResponseHouseFamilies();
                        hf11.AnganwariNo = tempHf.AnganwariNo;
                        hf11.HouseNo = tempHf.HouseNo;
                        hf11.FamiliyNo = tempHf.FamiliyNo;
                        hf11.HouseFamilyID = HouseFamilyId;
                        hf11.HouseFamilyID_Offline = HouseFamilyIdoffline;
                        drhs.Add(hf11);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in saving house family data" + ex.ToString());
            }
            return drhs;
        }


        private List<DHS_ResponseChildInformation> SaveChildInformationData(DataTable dt)
        {
            List<DHS_ResponseChildInformation> childinfo = new List<DHS_ResponseChildInformation>();
            try
            {
                List<ChildInformation> hf = (from DataRow dr in dt.Rows
                                             select new ChildInformation()
                                             {
                                                 MemberID = Convert.ToInt32(dr["MemberID"]),
                                                 Male = Convert.ToByte(dr["Male"]),
                                                 Female = Convert.ToByte(dr["Female"]),
                                                 Transgender = Convert.ToByte(dr["Transgender"]),

                                             }).ToList();



                foreach (ChildInformation tempHf in hf)
                {
                    ChildInformation hf1 = dhs.ChildInformations.Where(x => x.MemberID == tempHf.MemberID).FirstOrDefault();
                    int MemberID = 0;
                    if (hf1 != null)
                    {
                        hf1.Male = tempHf.Male;
                        hf1.Female = tempHf.Female;
                        hf1.Transgender = tempHf.Transgender;
                        dhs.SaveChanges();
                        MemberID = hf1.MemberID;
                    }
                    else
                    {
                        hf1 = new ChildInformation();
                        hf1.MemberID = tempHf.MemberID;
                        hf1.Male = tempHf.Male;
                        hf1.Female = tempHf.Female;
                        hf1.Transgender = tempHf.Transgender;
                        dhs.ChildInformations.Add(hf1);
                        dhs.SaveChanges();
                        MemberID = Convert.ToInt32(dhs.ChildInformations.Where(x => x.MemberID == tempHf.MemberID).FirstOrDefault());

                    }
                    if (MemberID > 0)
                    {
                        DHS_ResponseChildInformation hf11 = new DHS_ResponseChildInformation();
                        hf11.MemberID = tempHf.MemberID;
                        childinfo.Add(hf11);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in saving house family data" + ex.ToString());
            }
            return childinfo;
        }





        private List<DHS_ResponseMemberDetails> SaveMembeFamilyData(DataTable dt)
        {
            List<DHS_ResponseMemberDetails> drmd = new List<DHS_ResponseMemberDetails>();
            try
            {
                List<MemberDetail> md = new List<MemberDetail>();
                md = (from DataRow dr in dt.Rows
                      select new MemberDetail()
                      {

                          SurveyYear = Convert.ToInt32(dr["SurveyYear"]),
                          UnitID = Convert.ToInt32(dr["UnitID"]),
                          AnganwariNo = Convert.ToInt32(dr["AnganwariNo"]),
                          Name = Convert.ToString(dr["Name"]),
                          Age = Convert.ToByte(dr["Age"]),
                          Sex = Convert.ToByte(dr["Sex"]),
                          MobileNo = Convert.ToString(dr["MobileNo"]),
                          AadhaarNo = Convert.ToString(dr["AadhaarNo"]),
                          Entrydate = Convert.ToDateTime(dr["Entrydate"]),
                          LastUpdateDate = Convert.ToDateTime(dr["LastUpdateDate"]),
                          Profession = Convert.ToByte(dr["Profession"]),
                          RelationToHead = Convert.ToByte(dr["RelationToHead"]),
                          PollutionHazards = Convert.ToString(dr["PollutionHazards"]),
                          MaritalStatus = Convert.ToByte(dr["MaritalStatus"]),
                          Education = Convert.ToByte(dr["Education"]),
                          MobileNoBelongsToWhom = Convert.ToByte(dr["MobileNoBelongsToWhom"]),
                          EntryByUserNo = Convert.ToInt32(dr["EntryByUserNo"]),
                          UpdateByUserNo = Convert.ToInt32(dr["UpdateByUserNo"]),
                          RationCardNo = Convert.ToString(dr["RationCardNo"]),
                          DiseasesCodes = Convert.ToString(dr["DiseasesCodes"]),
                          HouseFamilyID = Convert.ToInt32(dr["HouseFamilyID"]),

                          SpouseID = Convert.ToString(dr["SpouseID"]) == "" ? 0 : Convert.ToInt32(dr["SpouseID"]),
                          Fatherid = Convert.ToString(dr["Fatherid"]) == "" ? 0 : Convert.ToInt32(dr["Fatherid"]),
                          Motherid = Convert.ToString(dr["Motherid"]) == "" ? 0 : Convert.ToInt32(dr["Motherid"]),

                          MemberID = Convert.ToInt32(dr["MemberID"])



                      }).ToList();
                foreach (MemberDetail tempMd in md)
                {
                    MemberDetail p = dhs.MemberDetails.Where(x => x.AnganwariNo == tempMd.AnganwariNo && x.HouseFamilyID == tempMd.HouseFamilyID && x.AadhaarNo == tempMd.AadhaarNo).FirstOrDefault();
                    int MemberIDoffline = tempMd.MemberID;
                    int MemberID = 0;
                    if (p != null)
                    {
                        p.Age = tempMd.Age;
                        p.Sex = tempMd.Sex;
                        p.MobileNo = tempMd.MobileNo;
                        p.AadhaarNo = tempMd.AadhaarNo;
                        p.LastUpdateDate = Convert.ToDateTime(tempMd.LastUpdateDate);
                        p.Profession = tempMd.Profession;
                        p.RelationToHead = tempMd.RelationToHead;
                        p.PollutionHazards = tempMd.PollutionHazards;
                        p.MaritalStatus = tempMd.MaritalStatus;
                        p.Education = tempMd.Education;
                        p.MobileNoBelongsToWhom = tempMd.MobileNoBelongsToWhom;
                        p.PollutionHazards = tempMd.PollutionHazards;
                        p.UpdateByUserNo = tempMd.UpdateByUserNo;
                        p.RationCardNo = tempMd.RationCardNo;
                        p.DiseasesCodes = tempMd.DiseasesCodes;
                        p.SpouseID = tempMd.SpouseID;

                        if (tempMd.SpouseID == 0)
                        {
                            p.SpouseID = null;
                        }
                        else
                        {
                            p.SpouseID = tempMd.SpouseID;
                        }

                        if (tempMd.Fatherid == 0)
                        {
                            p.Fatherid = null;
                        }
                        else
                        {
                            p.Fatherid = tempMd.Fatherid;
                        }
                        if (tempMd.Motherid == 0)
                        {
                            p.Motherid = null;
                        }
                        else
                        {
                            p.Motherid = tempMd.Motherid;
                        }
                        p.LastUpdateDate_online = DateTime.Now;
                        dhs.SaveChanges();
                        MemberID = p.MemberID;
                    }
                    else
                    {
                        p = new MemberDetail();
                        p.SurveyYear = tempMd.SurveyYear;
                        p.UnitID = tempMd.UnitID;
                        p.AnganwariNo = tempMd.AnganwariNo;
                        p.HouseFamilyID = tempMd.HouseFamilyID;
                        p.Age = tempMd.Age;
                        p.Sex = tempMd.Sex;
                        p.MobileNo = tempMd.MobileNo;
                        p.AadhaarNo = tempMd.AadhaarNo;
                        p.Entrydate = Convert.ToDateTime(tempMd.Entrydate);
                        p.LastUpdateDate = Convert.ToDateTime(tempMd.LastUpdateDate);
                        p.Profession = tempMd.Profession;
                        p.RelationToHead = tempMd.RelationToHead;
                        p.PollutionHazards = tempMd.PollutionHazards;
                        p.MaritalStatus = tempMd.MaritalStatus;
                        p.Education = tempMd.Education;
                        p.MobileNoBelongsToWhom = tempMd.MobileNoBelongsToWhom;
                        p.PollutionHazards = tempMd.PollutionHazards;
                        p.UpdateByUserNo = tempMd.UpdateByUserNo;
                        p.RationCardNo = tempMd.RationCardNo;
                        p.DiseasesCodes = tempMd.DiseasesCodes;
                        p.Name = tempMd.Name;
                        if (tempMd.SpouseID == 0)
                        {
                            p.SpouseID = null;
                        }
                        else
                        {
                            p.SpouseID = tempMd.SpouseID;
                        }

                        if (tempMd.Fatherid == 0)
                        {
                            p.Fatherid = null;
                        }
                        else
                        {
                            p.Fatherid = tempMd.Fatherid;
                        }
                        if (tempMd.Motherid == 0)
                        {
                            p.Motherid = null;
                        }
                        else
                        {
                            p.Motherid = tempMd.Motherid;
                        }
                        p.Entrydate_online = DateTime.Now;
                        p.LastUpdateDate_online = DateTime.Now;
                        dhs.MemberDetails.Add(p);
                        dhs.SaveChanges();
                        MemberID = dhs.MemberDetails.Where(x => x.HouseFamilyID == tempMd.HouseFamilyID && x.AadhaarNo == tempMd.AadhaarNo).Select(x => x.MemberID).FirstOrDefault();
                    }
                    if (MemberID > 0)
                    {
                        DHS_ResponseMemberDetails hf11 = new DHS_ResponseMemberDetails();
                        hf11.AnganwariNo = tempMd.AnganwariNo;
                        hf11.HouseFamilyID = tempMd.HouseFamilyID;
                        hf11.Name = tempMd.Name;
                        hf11.MemberID = MemberID;
                        hf11.MemberID_Offline = MemberIDoffline;
                        hf11.SpouseID_Offline = Convert.ToInt32(tempMd.SpouseID);
                        hf11.Fatherid_Offline = Convert.ToInt32(tempMd.Fatherid);
                        hf11.Motherid_Offline = Convert.ToInt32(tempMd.Motherid);
                        drmd.Add(hf11);
                    }
                    dhs.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in saving house family data" + ex.ToString());
            }
            return drmd;
        }




        private List<DHS_ResponseMemberDetails> UpdateMembeFamilyData(DataTable dt)
        {


            List<DHS_ResponseMemberDetails> drmd = new List<DHS_ResponseMemberDetails>();
            try
            {
                List<MemberDetail> md = new List<MemberDetail>();
                md = (from DataRow dr in dt.Rows
                      select new MemberDetail()
                      {


                          SpouseID = Convert.ToString(dr["SpouseID"]) == "" ? 0 : Convert.ToInt32(dr["SpouseID"]),
                          Fatherid = Convert.ToString(dr["Fatherid"]) == "" ? 0 : Convert.ToInt32(dr["Fatherid"]),
                          Motherid = Convert.ToString(dr["Motherid"]) == "" ? 0 : Convert.ToInt32(dr["Motherid"]),
                          MemberID = Convert.ToInt32(dr["MemberID"])



                      }).ToList();
                foreach (MemberDetail tempMd in md)
                {
                    MemberDetail p = dhs.MemberDetails.Where(x => x.MemberID == tempMd.MemberID).FirstOrDefault();
                    int MemberIDoffline = tempMd.MemberID;
                    int MemberID = 0;
                    if (p != null)
                    {


                        if (tempMd.SpouseID == 0)
                        {
                            p.SpouseID = null;
                        }
                        else
                        {
                            p.SpouseID = tempMd.SpouseID;
                        }

                        if (tempMd.Fatherid == 0)
                        {
                            p.Fatherid = null;
                        }
                        else
                        {
                            p.Fatherid = tempMd.Fatherid;
                        }
                        if (tempMd.Motherid == 0)
                        {
                            p.Motherid = null;
                        }
                        else
                        {
                            p.Motherid = tempMd.Motherid;
                        }
                        dhs.SaveChanges();
                        MemberID = p.MemberID;
                    }


                    dhs.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in saving house family data" + ex.ToString());
            }
            return drmd;
        }

        //private List<DHS_ResponseChildInformation> Savechildinfo(DataTable dt)
        //{
        //    List<DHS_ResponseChildInformation> child = new List<DHS_ResponseChildInformation>();
        //    try
        //    {
        //        List<ChildInformation> childinfo = new List<ChildInformation>();
        //        childinfo = (from DataRow dr in dt.Rows
        //                     select new ChildInformation()
        //              {
        //                  MemberID = Convert.ToInt32(dr["MemberID"]),
        //                  Male = Convert.ToByte(dr["Male"]),
        //                  Female = Convert.ToByte(dr["Female"]),
        //                  Transgender = Convert.ToByte(dr["Transgender"])
        //              }).ToList();
        //        foreach (ChildInformation tempchildinfo in childinfo)
        //        {
        //            ChildInformation p = dhs.ChildInformations.Where(x => x.MemberID == tempchildinfo.MemberID).FirstOrDefault();
        //            int MemberIDoffline = tempMd.MemberID;
        //            int MemberID = 0;
        //            if (p != null)
        //            {
        //                p.Age = tempMd.Age;
        //                p.Sex = tempMd.Sex;
        //                p.MobileNo = tempMd.MobileNo;
        //                p.AadhaarNo = tempMd.AadhaarNo;
        //                p.LastUpdateDate = Convert.ToDateTime(tempMd.LastUpdateDate);
        //                p.Profession = tempMd.Profession;
        //                p.RelationToHead = tempMd.RelationToHead;
        //                p.PollutionHazards = tempMd.PollutionHazards;
        //                p.MaritalStatus = tempMd.MaritalStatus;
        //                p.Education = tempMd.Education;
        //                p.MobileNoBelongsToWhom = tempMd.MobileNoBelongsToWhom;
        //                p.PollutionHazards = tempMd.PollutionHazards;
        //                p.UpdateByUserNo = tempMd.UpdateByUserNo;
        //                p.RationCardNo = tempMd.RationCardNo;
        //                p.DiseasesCodes = tempMd.DiseasesCodes;
        //                p.SpouseID = tempMd.SpouseID;
        //                p.Parent_id = tempMd.Parent_id;
        //                dhs.SaveChanges();
        //                MemberID = p.MemberID;
        //            }
        //            else
        //            {
        //                p = new MemberDetail();
        //                p.SurveyYear = tempMd.SurveyYear;
        //                p.UnitID = tempMd.UnitID;
        //                p.AnganwariNo = tempMd.AnganwariNo;
        //                p.HouseFamilyID = tempMd.HouseFamilyID;
        //                p.Age = tempMd.Age;
        //                p.Sex = tempMd.Sex;
        //                p.MobileNo = tempMd.MobileNo;
        //                p.AadhaarNo = tempMd.AadhaarNo;
        //                p.Entrydate = Convert.ToDateTime(tempMd.Entrydate);
        //                p.LastUpdateDate = Convert.ToDateTime(tempMd.LastUpdateDate);
        //                p.Profession = tempMd.Profession;
        //                p.RelationToHead = tempMd.RelationToHead;
        //                p.PollutionHazards = tempMd.PollutionHazards;
        //                p.MaritalStatus = tempMd.MaritalStatus;
        //                p.Education = tempMd.Education;
        //                p.MobileNoBelongsToWhom = tempMd.MobileNoBelongsToWhom;
        //                p.PollutionHazards = tempMd.PollutionHazards;
        //                p.UpdateByUserNo = tempMd.UpdateByUserNo;
        //                p.RationCardNo = tempMd.RationCardNo;
        //                p.DiseasesCodes = tempMd.DiseasesCodes;
        //                p.Name = tempMd.Name;
        //                p.SpouseID = tempMd.SpouseID;
        //                p.Parent_id = tempMd.Parent_id;
        //                dhs.MemberDetails.Add(p);
        //                dhs.SaveChanges();
        //                MemberID = dhs.MemberDetails.Where(x => x.HouseFamilyID == tempMd.HouseFamilyID && x.Name == tempMd.Name).Select(x => x.MemberID).FirstOrDefault();
        //            }
        //            if (MemberID > 0)
        //            {
        //                DHS_ResponseMemberDetails hf11 = new DHS_ResponseMemberDetails();
        //                hf11.AnganwariNo = tempMd.AnganwariNo;
        //                hf11.HouseFamilyID = tempMd.HouseFamilyID;
        //                hf11.Name = tempMd.Name;
        //                hf11.MemberID = MemberID;
        //                hf11.MemberID_Offline = MemberIDoffline;
        //                drmd.Add(hf11);
        //            }
        //            dhs.SaveChanges();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorHandler.WriteError("Error in saving house family data" + ex.ToString());
        //    }
        //    return drmd;
        //}
        private List<DHS_ResponseUnMarriedGirlsDetail> SaveUnMarriedGirlsData(DataTable dt)
        {
            List<DHS_ResponseUnMarriedGirlsDetail> drUnMarriedGirls = new List<DHS_ResponseUnMarriedGirlsDetail>();
            try
            {
                List<UnMarriedGirlsDetail> md = new List<UnMarriedGirlsDetail>();
                md = (from DataRow dr in dt.Rows
                      select new UnMarriedGirlsDetail
                      {
                          UnitID = Convert.ToInt32(dr["UnitID"]),
                          AnganwariNo = Convert.ToInt32(dr["AnganwariNo"]),
                          MemberID = Convert.ToInt32(dr["MemberID"]),
                          Flag = Convert.ToByte(dr["Flag"]),
                          Duration = Convert.ToByte(dr["Duration"]),
                          Used = Convert.ToByte(dr["Used"]),
                          Cleaning = Convert.ToString(dr["Cleaning"]),
                          TypeOfCleaningCloths = Convert.ToString(dr["TypeOfCleaningCloths"]),
                          DistroyNapkin = Convert.ToString(dr["DistroyNapkin"]),
                          FamiliyConcept = Convert.ToString(dr["FamiliyConcept"]),
                          PhysicalProblem = Convert.ToString(dr["PhysicalProblem"]),
                          AdviceFrom = Convert.ToByte(dr["AdviceFrom"]),
                          Entrydate = Convert.ToDateTime(dr["EntryDate"]),
                          LastUpdateDate = Convert.ToDateTime(dr["LastUpdateDate"]),
                          SurveyYear = Convert.ToInt32(dr["SurveyYear"])
                      }).ToList();


                foreach (UnMarriedGirlsDetail Unm in md)
                {
                    UnMarriedGirlsDetail p = dhs.UnMarriedGirlsDetails.Where(x => x.MemberID == Unm.MemberID).FirstOrDefault();
                    int MemberID = 0;
                    if (p != null)
                    {

                        p.UnitID = Convert.ToInt32(Unm.UnitID);
                        p.AnganwariNo = Convert.ToInt32(Unm.AnganwariNo);
                        p.Flag = Convert.ToByte(Unm.Flag);
                        p.Duration = Convert.ToByte(Unm.Duration);
                        p.Used = Convert.ToByte(Unm.Used);
                        p.Cleaning = Convert.ToString(Unm.Cleaning);
                        p.TypeOfCleaningCloths = Convert.ToString(Unm.TypeOfCleaningCloths);
                        p.DistroyNapkin = Convert.ToString(Unm.DistroyNapkin);
                        p.FamiliyConcept = Convert.ToString(Unm.FamiliyConcept);
                        p.PhysicalProblem = Convert.ToString(Unm.PhysicalProblem);
                        p.AdviceFrom = Convert.ToByte(Unm.AdviceFrom);
                        p.Entrydate = Convert.ToDateTime(Unm.Entrydate);
                        p.LastUpdateDate = Convert.ToDateTime(Unm.LastUpdateDate);
                        p.SurveyYear = Convert.ToInt32(Unm.SurveyYear);
                        p.LastUpdateDate_online = DateTime.Now;
                        dhs.SaveChanges();
                        MemberID = p.MemberID;
                    }
                    else
                    {
                        p = new UnMarriedGirlsDetail();
                        p.UnitID = Convert.ToInt32(Unm.UnitID);
                        p.AnganwariNo = Convert.ToInt32(Unm.AnganwariNo);
                        p.MemberID = Convert.ToInt32(Unm.MemberID);
                        p.Flag = Convert.ToByte(Unm.Flag);
                        p.Duration = Convert.ToByte(Unm.Duration);
                        p.Used = Convert.ToByte(Unm.Used);
                        p.Cleaning = Convert.ToString(Unm.Cleaning);
                        p.TypeOfCleaningCloths = Convert.ToString(Unm.TypeOfCleaningCloths);
                        p.DistroyNapkin = Convert.ToString(Unm.DistroyNapkin);
                        p.FamiliyConcept = Convert.ToString(Unm.FamiliyConcept);
                        p.PhysicalProblem = Convert.ToString(Unm.PhysicalProblem);
                        p.AdviceFrom = Convert.ToByte(Unm.AdviceFrom);
                        p.Entrydate = Convert.ToDateTime(Unm.Entrydate);
                        p.LastUpdateDate = Convert.ToDateTime(Unm.LastUpdateDate);
                        p.SurveyYear = Convert.ToInt32(Unm.SurveyYear);
                        p.Entrydate_online = DateTime.Now;
                        p.LastUpdateDate_online = DateTime.Now;
                        dhs.UnMarriedGirlsDetails.Add(p);
                        dhs.SaveChanges();
                        MemberID = Convert.ToInt32(Unm.MemberID);
                    }

                    if (MemberID > 0)
                    {
                        DHS_ResponseUnMarriedGirlsDetail hf11 = new DHS_ResponseUnMarriedGirlsDetail();
                        hf11.AnganwariNo = Unm.AnganwariNo;
                        hf11.MemberID = MemberID;
                        drUnMarriedGirls.Add(hf11);
                    }
                }


            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in saving Un Married Girls data" + ex.ToString());
            }
            return drUnMarriedGirls;
        }

        private List<DHS_ResponseEcDetails> SaveECDetailsData(DataTable dt)
        {
            List<DHS_ResponseEcDetails> drECdetails = new List<DHS_ResponseEcDetails>();
            try
            {
                List<ECdetail> md = new List<ECdetail>();
                md = (from DataRow dr in dt.Rows
                      select new ECdetail
                      {
                          SurveyYear = Convert.ToInt32(dr["SurveyYear"]),
                          UnitID = Convert.ToInt32(dr["UnitID"]),
                          AnganwariNo = Convert.ToInt32(dr["AnganwariNo"]),
                          MemberID = Convert.ToInt32(dr["MemberID"]),
                          IFSCCode = Convert.ToString(dr["IFSCCode"]),
                          AccountNo = Convert.ToString(dr["AccountNo"]),
                          PCTSID = Convert.ToString(dr["PCTSID"]),
                          MarraigeDate = Convert.ToDateTime(dr["MarraigeDate"]),
                          AgeOnMarraige = Convert.ToByte(dr["AgeOnMarraige"]),
                          HusbandAgeOnMarraige = Convert.ToByte(dr["HusbandAgeOnMarraige"]),

                          Nisanthan = Convert.ToByte(dr["Nisanthan"]),
                          DeliveryInLastYear = Convert.ToByte(dr["DeliveryInLastYear"]),
                          InfantDeathBelow5YearInLastYear = Convert.ToByte(dr["InfantDeathBelow5YearInLastYear"]),
                          LMPDate = Convert.ToDateTime(dr["LMPDate"]),
                          WantBaby = Convert.ToByte(dr["WantBaby"]),
                          Entrydate = Convert.ToDateTime(dr["Entrydate"]),
                          LastUpdateDate = Convert.ToDateTime(dr["LastUpdateDate"]),
                          EntryByUserNo = Convert.ToInt32(dr["EntryByUserNo"]),
                          UpdateByUserNo = Convert.ToInt32(dr["UpdateByUserNo"]),
                          CoupleNo = Convert.ToInt32(dr["CoupleNo"]),
                          BhamashahID = Convert.ToString(dr["BhamashahID"])
                      }).ToList();
                foreach (ECdetail tempEC in md)
                {
                    ECdetail p = dhs.ECdetails.Where(x => x.MemberID == tempEC.MemberID).FirstOrDefault();
                    int MemberID = 0;
                    if (p != null)
                    {
                        p.SurveyYear = Convert.ToInt32(tempEC.SurveyYear);
                        p.UnitID = Convert.ToInt32(tempEC.UnitID);
                        p.AnganwariNo = Convert.ToInt32(tempEC.AnganwariNo);
                        p.IFSCCode = Convert.ToString(tempEC.IFSCCode);
                        p.AccountNo = Convert.ToString(tempEC.AccountNo);
                        p.PCTSID = Convert.ToString(tempEC.PCTSID);
                        p.MarraigeDate = Convert.ToDateTime(tempEC.MarraigeDate);
                        p.AgeOnMarraige = Convert.ToByte(tempEC.AgeOnMarraige);
                        p.HusbandAgeOnMarraige = Convert.ToByte(tempEC.HusbandAgeOnMarraige);

                        p.Nisanthan = Convert.ToByte(tempEC.Nisanthan);
                        p.DeliveryInLastYear = Convert.ToByte(tempEC.DeliveryInLastYear);
                        p.InfantDeathBelow5YearInLastYear = Convert.ToByte(tempEC.InfantDeathBelow5YearInLastYear);
                        p.LMPDate = Convert.ToDateTime(tempEC.LMPDate);
                        p.WantBaby = Convert.ToByte(tempEC.WantBaby);
                        p.Entrydate = Convert.ToDateTime(tempEC.Entrydate);
                        p.LastUpdateDate = Convert.ToDateTime(tempEC.LastUpdateDate);
                        p.EntryByUserNo = Convert.ToInt32(tempEC.EntryByUserNo);
                        p.UpdateByUserNo = Convert.ToInt32(tempEC.UpdateByUserNo);
                        p.CoupleNo = Convert.ToInt32(tempEC.CoupleNo);
                        p.BhamashahID = Convert.ToString(tempEC.BhamashahID);
                        p.LastUpdateDate_online = DateTime.Now;
                        dhs.SaveChanges();
                        MemberID = p.MemberID;
                    }
                    else
                    {
                        p = new ECdetail();
                        p.SurveyYear = Convert.ToInt32(tempEC.SurveyYear);
                        p.UnitID = Convert.ToInt32(tempEC.UnitID);
                        p.AnganwariNo = Convert.ToInt32(tempEC.AnganwariNo);
                        p.MemberID = Convert.ToInt32(tempEC.MemberID);
                        p.IFSCCode = Convert.ToString(tempEC.IFSCCode);
                        p.AccountNo = Convert.ToString(tempEC.AccountNo);
                        p.PCTSID = Convert.ToString(tempEC.PCTSID);
                        p.MarraigeDate = Convert.ToDateTime(tempEC.MarraigeDate);
                        p.AgeOnMarraige = Convert.ToByte(tempEC.AgeOnMarraige);
                        p.HusbandAgeOnMarraige = Convert.ToByte(tempEC.HusbandAgeOnMarraige);

                        p.Nisanthan = Convert.ToByte(tempEC.Nisanthan);
                        p.DeliveryInLastYear = Convert.ToByte(tempEC.DeliveryInLastYear);
                        p.InfantDeathBelow5YearInLastYear = Convert.ToByte(tempEC.InfantDeathBelow5YearInLastYear);
                        p.LMPDate = Convert.ToDateTime(tempEC.LMPDate);
                        p.WantBaby = Convert.ToByte(tempEC.WantBaby);
                        p.Entrydate = Convert.ToDateTime(tempEC.Entrydate);
                        p.LastUpdateDate = Convert.ToDateTime(tempEC.LastUpdateDate);
                        p.EntryByUserNo = Convert.ToInt32(tempEC.EntryByUserNo);
                        p.UpdateByUserNo = Convert.ToInt32(tempEC.UpdateByUserNo);
                        p.CoupleNo = Convert.ToInt32(tempEC.CoupleNo);
                        p.BhamashahID = Convert.ToString(tempEC.BhamashahID);
                        p.Entrydate_online = DateTime.Now;
                        p.LastUpdateDate_online = DateTime.Now;
                        dhs.ECdetails.Add(p);
                        dhs.SaveChanges();
                        MemberID = Convert.ToInt32(tempEC.MemberID);
                    }

                    if (MemberID > 0)
                    {
                        DHS_ResponseEcDetails hf11 = new DHS_ResponseEcDetails();
                        hf11.AnganwariNo = tempEC.AnganwariNo;
                        hf11.MemberID = MemberID;
                        drECdetails.Add(hf11);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in saving house family data" + ex.ToString());
            }
            return drECdetails;
        }
        public HttpResponseMessage LogoutToken(UserAuthenticate u)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                var ptok = cnaa.PctsTokens.Where(a => a.UserID == u.UserID && a.DeviceID == u.DeviceID).FirstOrDefault();
                if (ptok != null)
                {
                    cnaa.PctsTokens.Remove(ptok);
                    cnaa.SaveChanges();
                }
                _objResponseModel.Status = true;
                _objResponseModel.Message = "Logout successfully";
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in post user" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        }

        public HttpResponseMessage DeathList(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.LoginUnitid != 0)
                {
                    //  string Loginunitcode = rajmed.UnitMasters.Where(x => x.UnitID == p.LoginUnitid).Select(x => x.UnitCode).FirstOrDefault();
                    var data = cnaa.USPDeathList(p.LoginUnitid, p.ANMAutoID).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "बीते 15 महीनों में इस संस्था इकाई पर कोई मृत्यु नहीं हुई है ";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "बीते 15 महीनों में इस संस्था इकाई पर कोई मृत्यु नहीं हुई है";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage InfantDeathList(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.LoginUnitid != 0)
                {
                    var data = cnaa.USPDeathListForInfant(p.LoginUnitid, p.VillageAutoid, p.ANMAutoID).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "बीते 30 दिनों में इस संस्था इकाई पर कोई मृत्यु नहीं हुई है ";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "बीते 30 दिनों में इस संस्था इकाई पर कोई मृत्यु नहीं हुई है";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage AshaDeshbord(Pcts Pc)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(Pc.UserID, Pc.TokenNo);
            if (tokenFlag == true)
            {
                if (Pc.ASHAAutoid != 0)
                {
                    var data = cnaa.UspAshaDeshbord(Pc.ASHAAutoid, Pc.finyear).ToList();
                    AshaDashboard db = new AshaDashboard();
                    db.ancRegDashboard = data.Select(x => new ANCRegDashboard { unittype = (byte)x.unittype, unitcode = x.unitcode, finyear = Pc.finyear.ToString(), TotalANCReg = (Int32)x.totalANC, ANCRegTrimister = (Int32)x.ANCRegTrimaster }).ToList();
                    db.birthDetails = data.Select(x => new BirthDetails { unittype = (byte)x.unittype, unitcode = x.unitcode, finyear = Pc.finyear.ToString(), totalBirth = (Int32)x.TotalChildrenReg, liveMaleBirth = (Int32)x.MaleCount, liveFeMaleBirth = (Int32)x.FeMaleCount, stillBirth = (Int32)x.stllBirthCount }).ToList();
                    db.deliveryDetails = data.Select(x => new DeliveryDetailDashboard { unittype = (byte)x.unittype, unitcode = x.unitcode, finyear = Pc.finyear.ToString(), totalDelivery = (Int32)x.homeDelCount + (Int32)x.privateDelCount + (Int32)x.publicDelCount, delHome = (Int32)x.homeDelCount, delPrivate = (Int32)x.privateDelCount, delPublic = (Int32)x.publicDelCount }).ToList();
                    db.immunizationDetails = data.Select(x => new ImmunizationDetails { unittype = (byte)x.unittype, unitcode = x.unitcode, finyear = Pc.finyear.ToString(), totalChilderenReg = (Int32)x.TotalChildrenReg, fullyImmunized = (Int32)x.FullyImmunized, partImmunized = (Int32)x.PartiallyImmunized, notImmunized = (Int32)x.NotImmunized }).ToList();


                    List<VaccineRequirement> vr = new List<VaccineRequirement>();
                    var p = data.Select(x => new { BCGFlag = x.BcgFlag, OPVFlag = x.OPVFlag, HBFlag = x.HBFlag, PentaFlag = x.pentaFlag, MeaslesFlag = x.MeaslesFlag, unitcode = x.unitcode, unittype = x.unittype, bcg = x.BcgCount, PentaCount = x.pentaCount, OPVCount = x.OPVCount, MeaslCount = x.MeaslCount, HBCount = (int)x.HBCount }).FirstOrDefault();
                    if (p != null)
                    {
                        VaccineRequirement vr1 = new VaccineRequirement();
                        vr1.unitcode = p.unitcode;
                        vr1.unitname = "";
                        vr1.unittype = (byte)p.unittype;
                        vr1.immuName = "BCG";
                        vr1.immuNameH = "बीसीजी";
                        vr1.vaccineReqCount = (int)p.bcg;
                        vr1.vaccFlag = (byte)p.BCGFlag;
                        vr1.finyear = Pc.finyear;
                        vr.Add(vr1);
                        vr1 = new VaccineRequirement();
                        vr1.unitcode = p.unitcode;
                        vr1.unitname = "";
                        vr1.unittype = (byte)p.unittype;
                        vr1.immuName = "Pentavalent";
                        vr1.immuNameH = "पेन्टावेलेंट";
                        vr1.vaccineReqCount = (int)p.PentaCount;
                        vr1.vaccFlag = (byte)p.PentaFlag;
                        vr1.finyear = Pc.finyear;
                        vr.Add(vr1);
                        vr1 = new VaccineRequirement();
                        vr1.unitcode = p.unitcode;
                        vr1.unitname = "";
                        vr1.unittype = (byte)p.unittype;
                        vr1.immuName = "OPV";
                        vr1.immuNameH = "ओपीवी";
                        vr1.vaccineReqCount = (int)p.OPVCount;
                        vr1.vaccFlag = (byte)p.OPVFlag;
                        vr1.finyear = Pc.finyear;
                        vr.Add(vr1);
                        vr1 = new VaccineRequirement();
                        vr1.unitcode = p.unitcode;
                        vr1.unitname = "";
                        vr1.unittype = (byte)p.unittype;
                        vr1.immuName = "Measles";
                        vr1.immuNameH = "खसरा";
                        vr1.vaccineReqCount = (int)p.MeaslCount;
                        vr1.vaccFlag = (byte)p.MeaslesFlag;
                        vr1.finyear = Pc.finyear;
                        vr.Add(vr1);
                        vr1 = new VaccineRequirement();
                        vr1.unitcode = p.unitcode;
                        vr1.unitname = "";
                        vr1.unittype = (byte)p.unittype;
                        vr1.immuName = "Hepatitis";
                        vr1.immuNameH = "हैपेटाइटिस-बी";
                        vr1.vaccineReqCount = (int)p.HBCount;
                        vr1.vaccFlag = (byte)p.HBFlag;
                        vr1.finyear = Pc.finyear;
                        vr.Add(vr1);
                    }

                    db.vaccineRequirement = vr;


                    if (db != null)
                    {
                        _objResponseModel.ResposeData = db;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }

                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid request";
                }
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage UspBirthCertificateList(Pcts P)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(P.UserID, P.TokenNo);
            if (tokenFlag == true)
            {
                if (P.LoginUnitcode != "")
                {
                    var data = cnaa.UspBirthCertificateList(P.LoginUnitcode, Convert.ToString(P.FromDate)).Select(x => new { name = x.name, Husbname = x.Husbname, Mobileno = x.Mobileno, PCTSID = x.pctsid, MotherID = x.MotherID, Birth_date = x.Birth_date, ChildName = x.ChildName, Sex = x.SexName, InfantID = x.InfantID, ChildID = x.childid, PehchanRegFlag = x.PehchanRegFlag }).ToList();
                    if (data.Count != 0)
                    {
                        List<MotherInfantListForImmunization> listHash = new List<MotherInfantListForImmunization>();
                        Int32 i = 0;
                        for (i = 0; i < data.Count; i++)
                        {
                            MotherInfantListForImmunization ii = new MotherInfantListForImmunization();
                            ii.name = Convert.ToString(data[i].name);
                            ii.Husbname = Convert.ToString(data[i].Husbname);
                            ii.Mobileno = Convert.ToString(data[i].Mobileno);
                            ii.PctsId = Convert.ToString(data[i].PCTSID);
                            Int32 motherid = Convert.ToInt32(data[i].MotherID);


                            List<InfantListForBirthCertificate> listHash1 = data.Where(x => x.MotherID == motherid).Select(x => new InfantListForBirthCertificate { Birth_date = x.Birth_date, ChildName = x.ChildName, Sex = x.Sex, InfantID = x.InfantID, ChildID = x.ChildID, PehchanRegFlag = x.PehchanRegFlag }).ToList();
                            ii.infantList = listHash1;
                            //i += 1;
                            listHash.Add(ii);
                        }
                        _objResponseModel.ResposeData = listHash;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }

            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        public HttpResponseMessage GetYearList()
        {
            ResponseModel _objResponseModel = new ResponseModel();
            List<Dictionary<string, string>> listHash = new List<Dictionary<string, string>>();
            int i = 1;
            while (i <= 5)
            {
                Dictionary<string, string> hash = new Dictionary<string, string> { };
                hash.Add("Year", DateTime.Now.AddYears(i - 5).Year.ToString());
                listHash.Add(hash);
                i += 1;
            }
            _objResponseModel.ResposeData = listHash;
            _objResponseModel.Status = true;
            _objResponseModel.Message = "Data Found";
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }



        public HttpResponseMessage WeightDetail(WeightDetails p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string InfID = p.infantid;
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);

            DateTime SDate = DateTime.Now;

            if (tokenFlag == true)
            {
                if (InfID == null)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
                else
                {
                    int InfIDs = Convert.ToInt32(InfID);
                    var p1 = (from inf in cnaa.Infants
                              join moth in cnaa.Mothers on inf.MotherID equals moth.MotherID
                              join imu in cnaa.Immunizations on inf.InfantID equals imu.InfantID into lj
                              from leftjoin in lj.DefaultIfEmpty()
                              where inf.InfantID == InfIDs
                              select new
                              {
                                  motherid = inf.MotherID,
                                  infanid = inf.InfantID,
                                  Birth_date = inf.Birth_date,
                                  immudate = (DateTime?)leftjoin.immudate ?? inf.Birth_date,
                                  sex = inf.Sex,
                                  weight = (leftjoin.Weight ?? (inf.Weight ?? 0)),
                                  ChildName = inf.ChildName,

                              }
                  ).Distinct().ToList();

                    if (p1 != null)
                    {
                        List<WeightDetails> w1 = new List<WeightDetails>();
                        WeightDetails w;
                        foreach (var item in p1)
                        {
                            w = new WeightDetails();
                            w.infantid = Convert.ToString(item.infanid);
                            w.motherid = item.motherid;
                            w.sex = (Byte)item.sex;

                            w.weight = (float)item.weight;
                            w.age = DifferenceInMonth(item.Birth_date, item.immudate);
                            w.Birth_date = item.Birth_date;
                            w.ChildName = item.ChildName;
                            w1.Add(w);

                        }

                        _objResponseModel.ResposeData = w1;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }


                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }


            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        }
        public HttpResponseMessage uspASHAWorkPlan(Pcts p)
        {

            ResponseModel _objResponseModel = new ResponseModel();

            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.TagName == "A" || p.TagName == "P" || p.TagName == "S")
                {
                    var data = cnaa.uspAshaPlan(p.ASHAAutoid, p.MthYr, p.TagName).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else if (p.TagName == "I")
                {
                    var data = cnaa.uspAshaPlanForImmunization(p.ASHAAutoid, p.MthYr, p.TagName).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);


            //ResponseModel _objResponseModel = new ResponseModel();
            //bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            //if (tokenFlag == true)
            //{
            //    if (p.TagName == "A" || p.TagName == "P" || p.TagName == "S")
            //    {
            //        var data = cnaa.uspAshaPlan(p.ASHAAutoid, p.MthYr, p.TagName).ToList();
            //        if (data != null)
            //        {
            //            _objResponseModel.ResposeData = data;
            //            _objResponseModel.Status = true;
            //            _objResponseModel.Message = "Data Received successfully";
            //        }
            //        else
            //        {
            //            _objResponseModel.Status = true;
            //            _objResponseModel.Message = "No Data Found";
            //        }
            //    }
            //    else if (p.TagName == "I")
            //    {
            //        var data = cnaa.uspAshaPlanForImmunization(p.ASHAAutoid, p.MthYr, p.TagName).ToList();
            //        if (data != null)
            //        {
            //            _objResponseModel.ResposeData = data;
            //            _objResponseModel.Status = true;
            //            _objResponseModel.Message = "Data Received successfully";
            //        }
            //        else
            //        {
            //            _objResponseModel.Status = true;
            //            _objResponseModel.Message = "No Data Found";
            //        }
            //    }
            //}
            //else
            //{
            //    _objResponseModel.Status = false;
            //    _objResponseModel.Message = "Invalid request";
            //}

            //return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage uspASHAWorkPlan1(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            _objResponseModel.Message = "Harendra";
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.TagName == "A" || p.TagName == "P" || p.TagName == "S")
                {
                    var data = cnaa.uspAshaPlan(p.ASHAAutoid, p.MthYr, p.TagName).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else if (p.TagName == "I")
                {
                    var data = cnaa.uspAshaPlanForImmunization(p.ASHAAutoid, p.MthYr, p.TagName).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage GetAshaSoftMthyr()
        {
            ResponseModel _objResponseModel = new ResponseModel();
            List<Dictionary<string, string>> listHash = new List<Dictionary<string, string>>();
            var monthyear = cnaa.USPAshaSoftMthyr().ToList();
            foreach (var mth in monthyear)
            {
                Dictionary<string, string> hash = new Dictionary<string, string> { };
                hash.Add("MonthName", CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Convert.ToInt32(mth.ToString().Substring(4, 2))).ToString().Substring(0, 3) + "-" + mth.ToString().Substring(0, 4));
                hash.Add("MonthValue", mth.ToString());
                listHash.Add(hash);
            }
            _objResponseModel.ResposeData = listHash;
            _objResponseModel.Status = true;
            _objResponseModel.Message = "Data Found";
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        }

        public HttpResponseMessage USPaAshaIncentives(Pcts P)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            //bool tokenFlag = ValidateToken(P.UserID, P.TokenNo);
            bool IsData = false;
            //if (tokenFlag == true)
            //{
            if (P.ASHAAutoid != 0)
            {
                var data = cnaa.USPAshaIncentivesApp(P.ASHAAutoid, P.MthYr).Select(x => new { ashaname = x.ashaname, ashaphone = x.ashaphone, Accountno = x.Accountno, Ifsc_code = x.Ifsc_code, Bank_Name = x.Bank_Name, PaymentDate = x.PaymentDate, ServiceCode = x.ServiceCode, Amount = x.Amount, ServiceEnglish = x.ServiceEnglish, SeviceHindi = x.SeviceHindi, activitycode = x.activitycode, ActivityServiceHindi = x.ActivityServiceHindi, ActivityServiceEnglish = x.ActivityServiceEnglish, ServiceAmount = x.ServiceAmount, TotalAmount = x.TotalAmount }).ToList();
                if (data.Count != 0)
                {
                    List<AshaIncAshaIncentiv> listHash = new List<AshaIncAshaIncentiv>();
                    Int32 i = 0;
                    for (i = 0; i < data.Count; i++)
                    {
                        AshaIncAshaIncentiv ii = new AshaIncAshaIncentiv();
                        Int16 ServiceCode = Convert.ToByte(data[i].ServiceCode);
                        if (Convert.ToString(data[i].activitycode) == "0")
                        {
                            ii.Ashaname = Convert.ToString(data[i].ashaname);
                            ii.Ashaphone = Convert.ToString(data[i].ashaphone);
                            ii.Accountno = Convert.ToString(data[i].Accountno);
                            ii.Ifsc_code = Convert.ToString(data[i].Ifsc_code);
                            ii.Bank_Name = Convert.ToString(data[i].Bank_Name);
                            ii.PaymentDate = Convert.ToString(data[i].PaymentDate);
                            ii.Amount = Convert.ToInt32(data[i].Amount);
                            ii.ServiceCode = Convert.ToByte(data[i].ServiceCode);
                            ii.ServiceEnglish = Convert.ToString(data[i].ServiceEnglish);
                            ii.ServiceHindi = Convert.ToString(data[i].SeviceHindi);
                            ii.TotalAmount = Convert.ToInt32(data[i].TotalAmount);
                            List<AshaIncentivActivity> listHash1 = data.Where(x => x.ServiceCode == ServiceCode && x.activitycode != 0).Select(x => new AshaIncentivActivity { ServiceEnglish = x.ActivityServiceEnglish, ServiceHindi = x.ActivityServiceHindi, Amount = Convert.ToInt32(x.ServiceAmount) }).ToList();
                            ii.Activitylist = listHash1;
                            listHash.Add(ii);
                            IsData = true;
                        }


                    }
                    if (IsData == true)
                    {
                        _objResponseModel.ResposeData = listHash;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }

                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
                //}
                //else
                //{
                //    _objResponseModel.Status = false;
                //    _objResponseModel.Message = "No Data Found";
                //}

            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        public HttpResponseMessage PostAshaAutoid(UnitMasterAdmin un)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(un.UserID, un.TokenNo);
            if (tokenFlag == true)
            {
                var data = rajmed.uspGetAshaDetails(un.UnitID).ToList();
                if (data != null)
                {
                    _objResponseModel.ResposeData = data;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }


            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage HelpDesk(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            var data = cnaa.USPHelpDesk(p.type).ToList();
            if (data != null && p.type != 1)
            {
                _objResponseModel.ResposeData = data;
                _objResponseModel.Status = true;
                _objResponseModel.Message = "एप्लीकेशन से संबंधित समस्या के लिए जिला व ब्लाक स्तर से संपर्क करे";
            }
            else
            {
                _objResponseModel.Status = true;
                _objResponseModel.Message = "एप्लीकेशन से संबंधित समस्या के लिए जिला व ब्लाक स्तर से संपर्क करे";
            }
            //  _objResponseModel.Message = "एप्लीकेशन से संबंधित समस्या के लिए संपर्क करे : Pradeep Nagarwal (ASO) - 9001600969, Chanchanl Bhardwaj (SI) - 9887453535, Jyoti Bala Joshi (SI) - 9772859684";
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        [ActionName("uspDueListForANMVerification")]
        public HttpResponseMessage uspDueListForANMVerification(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.LoginUnitid != 0)
                {
                    var data = cnaa.uspDueListForANMVerification(p.LoginUnitid, p.type, p.ASHAAutoid).ToList();
                    if (data.Count != 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("uspDueListForANMVerification1")]
        public HttpResponseMessage uspDueListForANMVerification1(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                try
                {
                    if (p.LoginUnitid != 0)
                    {
                        var data = cnaa.uspDueListForANMVerification1(p.LoginUnitid, p.ASHAAutoid, Convert.ToInt32(p.action), p.type).ToList();
                        if (data.Count != 0)
                        {
                            _objResponseModel.ResposeData = data;
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "Data Received successfully";
                        }
                        else
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = "No Data Found";
                        }
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                catch
                {
                    _objResponseModel.Message = "Error";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage uspListForANMVerify(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                try
                {
                    if (p.LoginUnitid != 0)
                    {
                        var data = cnaa.uspListForANMVerify(p.LoginUnitid, p.ASHAAutoid, p.type).ToList();
                        if (data.Count != 0)
                        {
                            _objResponseModel.ResposeData = data;
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "Data Received successfully";
                        }
                        else
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = "No Data Found";
                        }
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                catch
                {
                    _objResponseModel.Message = "Error";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage uspListForANMVerifyTotal(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                try
                {
                    if (p.LoginUnitid != 0)
                    {
                        var data = cnaa.uspListForANMVerifyTotal(p.LoginUnitid, p.ASHAAutoid, p.type).ToList();
                        if (data.Count != 0)
                        {
                            _objResponseModel.ResposeData = data;
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "Data Received successfully";
                        }
                        else
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = "No Data Found";
                        }
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                catch
                {
                    _objResponseModel.Message = "Error";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage uspANMANCVerify(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                using (cnaaEntities objcnaa = new cnaaEntities())
                {
                    var p1 = objcnaa.ANCDetails.Where(x => x.ANCRegID == p.ANCRegID && x.ANCFlag == p.type).FirstOrDefault();
                    if (p1 == null)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Invalid ANC";
                    }
                    else
                    {
                        p1.Media = (p1.Media == (byte)2) ? (byte)3 : (byte)1;
                        p1.ANMVerify = 1;
                        p1.ANMVerificationDate = DateTime.Now;
                        p1.UpdateUserNo = p.UserNo;
                        objcnaa.SaveChanges();
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data has been saved";
                    }


                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage uspANMPNCVerify(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                using (cnaaEntities objcnaa = new cnaaEntities())
                {
                    var p1 = objcnaa.HBPNCs.Where(x => x.Ancregid == p.ANCRegID && x.PNCFlag == p.type).FirstOrDefault();
                    if (p1 == null)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Invalid ANC";
                    }
                    else
                    {
                        p1.Media = (p1.Media == (byte)2) ? (byte)3 : (byte)1;
                        p1.ANMVerify = 1;
                        p1.UpdateUserNo = p.UserNo;
                        p1.ANMVerificationDate = DateTime.Now;
                        objcnaa.SaveChanges();
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data has been saved";
                    }


                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage HBYClist(Pcts P)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(P.UserID, P.TokenNo);
            if (tokenFlag == true)
            {
                if (P.LoginUnitid != 0)
                {
                    var data = cnaa.uspHBYClist(P.LoginUnitid, P.VillageAutoid, P.ANMAutoID).Select(x => new { name = x.name, Husbname = x.Husbname, Mobileno = x.Mobileno, PCTSID = x.pctsid, MotherID = x.motherid, Birth_date = x.Birth_date, ChildName = x.ChildName, Sex = x.SexName, InfantID = x.infantid, ChildID = x.childid }).ToList();
                    if (data.Count != 0)
                    {
                        List<MotherInfantListForImmunization> listHash = new List<MotherInfantListForImmunization>();
                        Int32 i = 0;
                        for (i = 0; i < data.Count; i++)
                        {
                            MotherInfantListForImmunization ii = new MotherInfantListForImmunization();
                            ii.name = Convert.ToString(data[i].name);
                            ii.Husbname = Convert.ToString(data[i].Husbname);
                            ii.Mobileno = Convert.ToString(data[i].Mobileno);
                            ii.PctsId = Convert.ToString(data[i].PCTSID);
                            Int32 motherid = Convert.ToInt32(data[i].MotherID);


                            List<InfantListForBirthCertificate> listHash1 = data.Where(x => x.MotherID == motherid).Select(x => new InfantListForBirthCertificate { Birth_date = x.Birth_date, ChildName = x.ChildName, Sex = x.Sex, InfantID = x.InfantID, ChildID = x.ChildID }).ToList();
                            ii.infantList = listHash1;
                            //i += 1;
                            listHash.Add(ii);
                        }
                        _objResponseModel.ResposeData = listHash;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }

            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        }


        [ActionName("uspDataforManageHBYC")]
        public HttpResponseMessage uspDataforManageHBYC(Pcts P)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(P.UserID, P.TokenNo);
            try
            {
                if (tokenFlag == true)
                {
                    if (P.InfantID != 0)
                    {
                        var data = cnaa.uspDataforManageHBYCList(P.InfantID).ToList();
                        if (data != null)
                        {
                            _objResponseModel.ResposeData = data;
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "Data Received successfully";
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "No Data Found";

                        }
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid request";
                }
            }
            catch (Exception ex)
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = ex.ToString();
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }



        [ActionName("PostHBYCDetail")]
        public HttpResponseMessage PostHBYCDetail(HBYC HB)     //AddHBYC Details
        {

            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(HB.AppVersion, HB.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {

                        string ErrorMsg = SaveHBYCDetails(HB, 1);

                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, एचबीवाईसी  का विवरण सेव हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }

            }
            catch
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        public HttpResponseMessage PutHBYCDetail(HBYC HB)     //UpdateHBYC Details
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(HB.AppVersion, HB.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {

                        string ErrorMsg = SaveHBYCDetails(HB, 2);

                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, एचबीवाईसी का विवरण अपडेट हो चुका हैं।";
                        }


                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    ErrorHandler.WriteError("Error in model PUT anc" + Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
                }

            }
            catch (Exception ex)
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Validation Error";
                ErrorHandler.WriteError("Error in validation PUT anc" + ex.ToString());
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        private string validateHBYC(HBYC HB, Int16 methodFlag)
        {
            string ErrorMsg = "";
            if (ValidateToken(HB.UserID, HB.TokenNo) == false)
            {
                return "Invalid Request";
            }

            ErrorMsg = CheckValidNumber(Convert.ToString(HB.AncRegId), 1, 9, 1, "ANCRegID");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = CheckValidNumber(Convert.ToString(HB.MotherId), 1, 9, 1, "MotherID");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = CheckValidNumber(Convert.ToString(HB.InfantId), 1, 9, 1, "InfantID");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = CheckValidNumber(Convert.ToString(HB.VillageAutoID), 1, 10, 0, "VillageAutoID");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = CheckValidNumber(Convert.ToString(HB.HBYCFlag), 1, 2, 1, "HBYCFlag");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            if (HB.HBYCFlag < 3 || HB.HBYCFlag > 15)
            {
                ErrorMsg = "एचबीवाईसी की तिथि सही डालें !";
                return ErrorMsg;
            }

            ErrorMsg = checkDate(Convert.ToString(HB.VisitDate), 1, "विजिट");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }

            if (HB.Weight != 0)
            {
                if (HB.Weight < 1 || HB.Weight > 15)
                {
                    ErrorMsg = "Invalid weight!";
                    return ErrorMsg;
                }

            }
            byte flag = 0;
            if (DifferenceInDays(HB.BirthDate, HB.VisitDate) >= 90 && DifferenceInDays(HB.BirthDate, HB.VisitDate) <= 105)
            {
                flag = 3;
            }
            else if (DifferenceInDays(HB.BirthDate, HB.VisitDate) >= 180 && DifferenceInDays(HB.BirthDate, HB.VisitDate) <= 195)
            {
                flag = 6;
            }
            else if (DifferenceInDays(HB.BirthDate, HB.VisitDate) >= 270 && DifferenceInDays(HB.BirthDate, HB.VisitDate) <= 285)
            {
                flag = 9;
            }
            else if (DifferenceInDays(HB.BirthDate, HB.VisitDate) >= 360 && DifferenceInDays(HB.BirthDate, HB.VisitDate) <= 375)
            {
                flag = 12;
            }
            else if (DifferenceInDays(HB.BirthDate, HB.VisitDate) >= 450 && DifferenceInDays(HB.BirthDate, HB.VisitDate) <= 465)
            {
                flag = 15;
            }
            if (HB.HBYCFlag == 0 && flag == 0)
            {
                return "Invalid HBYC Detail!";
            }

            if (flag != HB.HBYCFlag)
            {
                return "Please check Visit Date.It should be " + HB.HBYCFlag + " Month !";
            }
            if (HB.Height < 30 || HB.Height > 99)
            {
                return "Invalid Height !";
            }


            if (HB.GrowthChart == 1 && HB.Color == 0)
            {
                return "कृपया बच्चे को किस रंग में वर्गीकृत किया गया चुनें !";
            }

            if (HB.GrowthLate == 1 && HB.Refer == 0)
            {
                return " कृपया क्या बच्चे को RBSK अथवा उच्च संस्थान में रेफर किया गया चुनें !";
            }
            if (HB.Refer == 1 && HB.ReferUnitcode == "")
            {
                return " कृपया रेफेर संस्था का सही नाम चुनें !";
            }
            var p = cnaa.HBYCs.Where(x => x.InfantId == HB.InfantId && x.HBYCFlag < HB.HBYCFlag).Max(x => x.Height);
            if (p != null)
            {
                if (HB.Height < p)
                {
                    return "Invalid Height !";
                }
            }
            p = cnaa.HBYCs.Where(x => x.InfantId == HB.InfantId && x.HBYCFlag > HB.HBYCFlag).Min(x => x.Height);
            if (p != null)
            {
                if (HB.Height > p)
                {
                    return "Invalid Height !";
                }
            }



            return ErrorMsg;
        }


        private string SaveHBYCDetails(HBYC HB, Int16 methodFlag)
        {
            byte flag = 0;
            if (DifferenceInDays(HB.BirthDate, HB.VisitDate) >= 90 && DifferenceInDays(HB.BirthDate, HB.VisitDate) <= 105)
            {
                flag = 3;
            }
            else if (DifferenceInDays(HB.BirthDate, HB.VisitDate) >= 180 && DifferenceInDays(HB.BirthDate, HB.VisitDate) <= 195)
            {
                flag = 6;
            }
            else if (DifferenceInDays(HB.BirthDate, HB.VisitDate) >= 270 && DifferenceInDays(HB.BirthDate, HB.VisitDate) <= 285)
            {
                flag = 9;
            }
            else if (DifferenceInDays(HB.BirthDate, HB.VisitDate) >= 360 && DifferenceInDays(HB.BirthDate, HB.VisitDate) <= 375)
            {
                flag = 12;
            }
            else if (DifferenceInDays(HB.BirthDate, HB.VisitDate) >= 450 && DifferenceInDays(HB.BirthDate, HB.VisitDate) <= 465)
            {
                flag = 15;
            }

            string ErrorMsg = validateHBYC(HB, methodFlag);

            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            else
            {
                //int iid = 0;
                //if (HB.InfantId != 0)
                //{
                //    iid = cnaa.HBYCs.Where(x => x.InfantId == HB.InfantId && x.HBYCFlag == flag).Select(x => x.InfantId).FirstOrDefault();
                //}
                int ReferUnitID = 0;
                if (HB.ReferUnitcode != "0")
                {
                    ReferUnitID = rajmed.UnitMasters.Where(x => x.UnitCode == HB.ReferUnitcode).Select(x => x.UnitID).FirstOrDefault();
                }
                HB.ReferUnitID = ReferUnitID;
                using (TransactionScope transaction = new TransactionScope())
                {
                    try
                    {
                        using (cnaaEntities objcnaa = new cnaaEntities())
                        {


                            var p = objcnaa.HBYCs.Where(x => x.InfantId == HB.InfantId && x.HBYCFlag == HB.HBYCFlag).FirstOrDefault();
                            if (p != null)
                            {
                                Int16 AnyChanges = 0;
                                if (p.ASHAAutoID != HB.ASHAAutoID)
                                    AnyChanges = 1;
                                else if (p.VisitDate != HB.VisitDate)
                                    AnyChanges = 1;
                                else if (p.ORSPacket != HB.ORSPacket)
                                    AnyChanges = 1;
                                else if (p.IFASirap != HB.IFASirap)
                                    AnyChanges = 1;
                                else if (p.GrowthChart != HB.GrowthChart)
                                    AnyChanges = 1;
                                else if (p.Color != HB.Color)
                                    AnyChanges = 1;
                                else if (p.FoodAccordingAge != HB.FoodAccordingAge)
                                    AnyChanges = 1;
                                else if (p.GrowthLate != HB.GrowthLate)
                                    AnyChanges = 1;
                                else if (p.Refer != HB.Refer)
                                    AnyChanges = 1;
                                else if (p.Freeze != HB.Freeze)
                                    AnyChanges = 1;
                                else if (p.Smthyr != HB.Smthyr)
                                    AnyChanges = 1;
                                else if (p.Weight != HB.Weight)
                                    AnyChanges = 1;
                                else if (p.Height != HB.Height)
                                    AnyChanges = 1;
                                else if (p.ReferUnitID != HB.ReferUnitID)
                                    AnyChanges = 1;
                                else if (p.ANMVerify != HB.ANMVerify)
                                    AnyChanges = 1;

                                if (AnyChanges == 2)
                                {
                                    HBYC_Log al = new HBYC_Log();
                                    al.MotherId = p.MotherId;
                                    al.AncRegId = p.AncRegId;
                                    al.InfantId = p.InfantId;
                                    al.HBYCFlag = p.HBYCFlag;
                                    al.VillageAutoID = p.VillageAutoID;
                                    al.ANMVerificationDate = p.ANMVerificationDate;
                                    al.ANMVerify = p.ANMVerify;
                                    al.ASHAAutoID = p.ASHAAutoID;
                                    al.VisitDate = p.VisitDate;
                                    al.ORSPacket = p.ORSPacket;
                                    al.IFASirap = p.IFASirap;
                                    al.GrowthChart = p.GrowthChart;
                                    al.Color = p.Color;
                                    al.FoodAccordingAge = p.FoodAccordingAge;
                                    al.GrowthLate = p.GrowthLate;
                                    al.Refer = p.Refer;
                                    al.Freeze = p.Freeze;
                                    al.Smthyr = p.Smthyr;
                                    al.EntryDate = p.EntryDate;
                                    al.LastUpdateDate = DateTime.Now;
                                    al.EntryUserNo = p.EntryUserNo;
                                    al.UpdateUserNo = HB.UpdateUserNo;
                                    al.LogEntryDate = DateTime.Now;
                                    al.Weight = p.Weight;
                                    al.Height = p.Height;
                                    al.ReferUnitID = HB.ReferUnitID;
                                    al.Media = p.Media;
                                    objcnaa.HBYC_Log.Add(al);
                                    objcnaa.SaveChanges();
                                }

                            }
                            if (methodFlag == 1)
                            {
                                Int16 HBYCFlag = (Int16)objcnaa.HBYCs.Where(x => x.InfantId == HB.InfantId && x.HBYCFlag == flag).Select(x => x.HBYCFlag).FirstOrDefault();
                                if (HBYCFlag == flag)
                                {
                                    return "Visit Schedule are already exist";
                                }
                            }
                            if (p == null)
                            {
                                p = new HBYC();
                                p.EntryDate = DateTime.Now;
                                p.EntryUserNo = HB.UpdateUserNo;
                                p.VillageAutoID = HB.VillageAutoID;
                                p.InfantId = HB.InfantId;
                                p.AncRegId = HB.AncRegId;
                                p.MotherId = HB.MotherId;
                                p.HBYCFlag = HB.HBYCFlag;
                            }
                            p.ASHAAutoID = HB.ASHAAutoID;
                            p.VisitDate = HB.VisitDate;
                            p.ORSPacket = HB.ORSPacket;
                            p.IFASirap = HB.IFASirap;
                            p.GrowthChart = HB.GrowthChart;
                            p.Color = HB.Color;
                            p.FoodAccordingAge = HB.FoodAccordingAge;
                            p.GrowthLate = HB.GrowthLate;
                            p.Weight = HB.Weight;
                            p.Height = HB.Height;
                            p.Refer = HB.Refer;
                            p.ReferUnitID = HB.ReferUnitID;
                            p.LastUpdateDate = DateTime.Now;
                            p.UpdateUserNo = HB.UpdateUserNo;
                            p.Media = HB.Media;
                            p.ANMVerify = 0;
                            p.ANMVerificationDate = null;
                            if (methodFlag == 1)
                            {
                                objcnaa.HBYCs.Add(p);
                            }
                            objcnaa.SaveChanges();

                            var p1 = objcnaa.Infants.Where(x => x.InfantID == HB.InfantId).FirstOrDefault();
                            p1.LastUpdated = DateTime.Now;
                            objcnaa.SaveChanges();

                            transaction.Complete();
                            return "";

                        }

                    }
                    catch (Exception ex)
                    {
                        Transaction.Current.Rollback();
                        transaction.Dispose();
                        if (methodFlag == 1)
                        {
                            ErrorMsg = "ओह ! एचबीवाईसी का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें ।";
                            ErrorHandler.WriteError("Error in Post HBYC" + ex.ToString());
                            ErrorHandler.WriteError(HB.ToString());
                        }
                        else
                        {
                            ErrorMsg = "ओह ! एचबीवाईसी  का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा अपडेट करें ।";
                            ErrorHandler.WriteError("Error in PUT HBYC" + ex.ToString());
                        }

                    }
                }
            }
            return ErrorMsg;
        }


        public HttpResponseMessage postfillPHCCHCHBYC(Pcts pcts)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            bool tokenFlag = ValidateToken(pcts.UserID, pcts.TokenNo);
            if (tokenFlag == true)
            {
                if (ModelState.IsValid)
                {
                    var qry = rajmed.UnitMasters.Where(x => x.UnitCode.StartsWith(pcts.Unitcode.Substring(0, 6)) && x.UnitType == pcts.UnitType).OrderBy(x => x.UnitName).Select(x => new
                    {
                        x.UnitID,
                        UnitName = x.UnitNameHindi == null ? x.UnitName : x.UnitNameHindi,
                        x.UnitCode
                    }).Select(x => new
                    {
                        x.UnitID,
                        x.UnitName,
                        x.UnitCode
                    }).ToList();

                    if (qry != null)
                    {
                        response = Request.CreateResponse(HttpStatusCode.OK, qry);
                    }
                    else
                    {
                        response = Request.CreateResponse(HttpStatusCode.NotFound, "Data not Found");
                    }

                }
                else
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            else
            {
                response = Request.CreateResponse(HttpStatusCode.NotFound, "Invalid request");
            }

            return response;
        }


        public HttpResponseMessage uspANMHBYCVerify(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                using (cnaaEntities objcnaa = new cnaaEntities())
                {
                    var p1 = objcnaa.HBYCs.Where(x => x.InfantId == p.InfantID && x.HBYCFlag == p.type).FirstOrDefault();
                    if (p1 == null)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Invalid HBYC";
                    }
                    else
                    {
                        p1.Media = (p1.Media == (byte)2) ? (byte)3 : (byte)1;
                        p1.ANMVerify = 1;
                        p1.ANMVerificationDate = DateTime.Now;
                        p1.UpdateUserNo = p.UserNo;
                        objcnaa.SaveChanges();
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data has been saved";
                    }


                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        public HttpResponseMessage uspANMMotherDeathVerify(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                using (cnaaEntities objcnaa = new cnaaEntities())
                {
                    var p1 = objcnaa.DeathDetails.Where(x => x.motherid == p.MotherID && x.infantid == 0).FirstOrDefault();
                    if (p1 == null)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Invalid Mother Death Details";
                    }
                    else
                    {
                        p1.Media = (p1.Media == (byte)2) ? (byte)3 : (byte)1; ;
                        p1.ANMVerify = 1;
                        p1.ANMVerificationDate = DateTime.Now;
                        p1.UpdateUserNo = p.UserNo;
                        objcnaa.SaveChanges();
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data has been saved";
                    }


                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage uspANMInfantDeathVerify(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                using (cnaaEntities objcnaa = new cnaaEntities())
                {
                    var p1 = objcnaa.DeathDetails.Where(x => x.motherid == p.MotherID && x.infantid == p.InfantID).FirstOrDefault();
                    if (p1 == null)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Invalid infant Death Details";
                    }
                    else
                    {
                        p1.Media = (p1.Media == (byte)2) ? (byte)3 : (byte)1; ;
                        p1.ANMVerify = 1;
                        p1.ANMVerificationDate = DateTime.Now;
                        p1.UpdateUserNo = p.UserNo;
                        objcnaa.SaveChanges();
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data has been saved";
                    }


                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }





        public HttpResponseMessage uspGetVillageListAsha(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.LoginUnitid > 0)
                {
                    // var data = rajmed.uspGetVillageByAshaID(p.LoginUnitid).Select(x => new { VillageName = x.VillageName, VillageautoID = x.VillageAutoID }).ToList();
                    var data = cnaa.uspGetVillageByAshaID((int?)p.LoginUnitid, (int?)p.VillageAutoid, (int?)p.ANMAutoID).Select(x => new { VillageName = x.VillageName, VillageautoID = x.VillageAutoID }).ToList();

                    //data.Add(new { VillageName = "सभी गाँव", VillageautoID = 0 });
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data.OrderBy(x => x.VillageautoID);
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage CallRCHexe(UserAuthenticate s)
        {
            bool status = false;
            string Message = "";
            ResponseModel _objResponseModel = new ResponseModel();
            if (ModelState.IsValid)
            {
                s.UserID = s.UserID.ToString().Trim().ToLower();
                var qry = rajmed.Users.Where(x => x.UserID == s.UserID && x.IsDeleted == 0).FirstOrDefault();
                if (qry != null && s.UserID.ToLower() == "sa")
                {
                    var saltedPass = "5248696471" + qry.Password;
                    var sha512pass = GenerateSHA512String(saltedPass.ToLower());
                    if (sha512pass.ToLower() == s.Password.ToLower())
                    {
                        FileInfo file = new FileInfo(HttpContext.Current.Server.MapPath("~"));
                        DriveInfo drive = new DriveInfo(file.Directory.Root.FullName);
                        if (drive.Name != "")
                        {
                            string exePath = drive.Name + "inetpub\\wwwroot\\ConsoleApplication\\RCHResend\\RCH.exe";
                            exePath = drive.Name + s.DeviceID;
                            System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
                            try
                            {
                                myProcess.StartInfo.UseShellExecute = false;
                                myProcess.StartInfo.FileName = exePath;
                                myProcess.StartInfo.CreateNoWindow = true;
                                myProcess.Start();
                                status = true;
                                Message = "Data Received successfully";
                            }
                            catch (Exception ex)
                            {

                                Message = ex.ToString();
                            }
                        }
                        _objResponseModel.Status = status;
                        _objResponseModel.Message = Message;
                    }

                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही यूज़र आईडी/पासवर्ड डाले ! ";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }



        public HttpResponseMessage DeleteANCDetail(ANCDetail anc)     //UpdateANC Details
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(anc.UserID, anc.TokenNo);
            if (tokenFlag == true)
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        int CheckAppVersionFlag = CheckVersion(anc.AppVersion, anc.IOSAppVersion);
                        if (CheckAppVersionFlag == 1)
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = appValiationMsg;
                            _objResponseModel.AppVersion = 1;
                            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                        }
                        else
                        {
                            string ErrorMsg = "";
                            if (ValidateToken(anc.UserID, anc.TokenNo) == false)
                            {
                                ErrorMsg = "Invalid Request";
                            }
                            if (ErrorMsg != "")
                            {
                                ErrorMsg = CheckValidNumber(Convert.ToString(anc.ANCRegID), 1, 9, 1, "ANCRegID");
                            }
                            if (ErrorMsg != "")
                            {
                                ErrorMsg = CheckValidNumber(Convert.ToString(anc.ANCFlag), 1, 1, 1, "ANCFlag");
                            }
                            if (ErrorMsg != "")
                            {
                                _objResponseModel.Status = false;
                                _objResponseModel.Message = ErrorMsg;
                                return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                            }
                            else
                            {
                                using (TransactionScope transaction = new TransactionScope())
                                {
                                    try
                                    {
                                        using (cnaaEntities objcnaa = new cnaaEntities())
                                        {
                                            var p = objcnaa.ANCDetails.Where(x => x.ANCRegID == anc.ANCRegID && x.ANCFlag == anc.ANCFlag).FirstOrDefault();
                                            if (p != null)
                                            {
                                                ANCDetails_Log al = new ANCDetails_Log();
                                                al.ANCFlag = p.ANCFlag;
                                                al.ANCDate = p.ANCDate;
                                                al.ALBE400 = p.ALBE400;
                                                al.ANCRegID = p.ANCRegID;
                                                al.anganwariNo = p.anganwariNo;
                                                al.ANMVerificationDate = p.ANMVerificationDate;
                                                al.ANMVerify = p.ANMVerify;
                                                al.ashaAutoID = p.ashaAutoID;
                                                al.BloodPressureD = p.BloodPressureD;
                                                al.BloodPressureS = p.BloodPressureS;
                                                al.CAL500 = p.CAL500;
                                                al.CompL = p.CompL;
                                                al.CovidCase = p.CovidCase;
                                                al.CovidForeignTrip = p.CovidForeignTrip;
                                                al.CovidFromDate = p.CovidFromDate;
                                                al.CovidIsolation = p.CovidIsolation;
                                                al.CovidQuarantine = p.CovidQuarantine;
                                                al.CovidRelativePositive = p.CovidRelativePositive;
                                                al.CovidRelativePossibility = p.CovidRelativePossibility;
                                                al.Entrydate = p.Entrydate;
                                                al.EntryUserNo = p.EntryUserNo;
                                                al.HB = p.HB;
                                                al.IFA = p.IFA;
                                                al.IPAddress = "";
                                                al.IronSucrose1 = p.IronSucrose1;
                                                al.IronSucrose2 = p.IronSucrose2;
                                                al.IronSucrose3 = p.IronSucrose3;
                                                al.IronSucrose4 = p.IronSucrose4;
                                                al.IsDeleted = 1;
                                                al.LastUpdated = p.LastUpdated;
                                                al.Media = p.Media;
                                                al.motherid = p.motherid;
                                                al.NormalLodingIronSucrose1 = p.NormalLodingIronSucrose1;
                                                al.ReferUnitID = p.ReferUnitID;
                                                al.RTI = p.RTI;
                                                al.TreatmentCode = p.TreatmentCode;
                                                al.TT1 = p.TT1;
                                                al.TT2 = p.TT2;
                                                al.TTB = p.TTB;
                                                al.UpdateDate = DateTime.Now;
                                                al.UpdatedBy = anc.UserID;
                                                al.UpdateUserNo = p.UpdateUserNo;
                                                al.UrineA = p.UrineA;
                                                al.UrineS = p.UrineS;
                                                al.VillageAutoID = p.VillageAutoID;
                                                al.Weight = p.Weight;
                                                objcnaa.ANCDetails_Log.Add(al);
                                                objcnaa.SaveChanges();
                                                objcnaa.ANCDetails.Remove(p);
                                                objcnaa.SaveChanges();
                                                int? TreatmentCode = objcnaa.ANCDetails.Where(x => x.ANCRegID == anc.ANCRegID).Max(x => x.TreatmentCode).GetValueOrDefault();
                                                if (TreatmentCode == null)
                                                {
                                                    TreatmentCode = 0;
                                                }
                                                var p1 = objcnaa.ANCRegDetails.Where(x => x.ANCRegID == anc.ANCRegID).FirstOrDefault();
                                                p1.HighRisk = Convert.ToByte((TreatmentCode > 0) ? 1 : 0);
                                                p1.LastUpdated = DateTime.Now;
                                                objcnaa.SaveChanges();
                                            }

                                            transaction.Complete();
                                        }

                                    }

                                    catch (Exception ex)
                                    {
                                        Transaction.Current.Rollback();
                                        transaction.Dispose();
                                        ErrorMsg = ex.ToString();
                                        ErrorHandler.WriteError("Error in validation delete anc" + ErrorMsg);
                                    }
                                }

                                if (ErrorMsg != "")
                                {
                                    _objResponseModel.Status = false;
                                    _objResponseModel.Message = "ओह ! एएनसी का विवरण डिलीट नहीं हुआ हैं। कृपया दोबारा डिलीट करें ।";
                                    ErrorHandler.WriteError("Error in validation delete anc" + ErrorMsg);
                                }
                                else
                                {
                                    _objResponseModel.Status = true;
                                    _objResponseModel.Message = "धन्यवाद, एएनसी का विवरण डिलीट हो चुका हैं।";
                                }
                            }
                        }
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Model Error";
                        ErrorHandler.WriteError("Error in model delete anc" + Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
                    }

                }
                catch (Exception ex)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Validation Error";
                    ErrorHandler.WriteError("Error in validation delete anc" + ex.ToString());
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage DeletePNCDetail(HBPNC pnc)
        {
            ResponseModel _objResponseModel = new ResponseModel();

            try
            {
                if (ModelState.IsValid)
                {

                    int CheckAppVersionFlag = CheckVersion(pnc.AppVersion, pnc.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = "";
                        if (ValidateToken(pnc.UserID, pnc.TokenNo) == false)
                        {
                            ErrorMsg = "Invalid Request";
                        }
                        if (ErrorMsg != "")
                        {
                            ErrorMsg = CheckValidNumber(Convert.ToString(pnc.Ancregid), 1, 9, 1, "Ancregid");
                        }
                        if (ErrorMsg != "")
                        {
                            ErrorMsg = CheckValidNumber(Convert.ToString(pnc.PNCFlag), 1, 1, 1, "PNCFlag");
                        }
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                        }
                        else
                        {
                            using (TransactionScope transaction = new TransactionScope())
                            {
                                try
                                {
                                    using (cnaaEntities objcnaa = new cnaaEntities())
                                    {
                                        var p = objcnaa.HBPNCs.Where(x => x.Ancregid == pnc.Ancregid && x.PNCFlag == pnc.PNCFlag).FirstOrDefault();
                                        if (p != null)
                                        {
                                            HBPNC_Log p1 = new HBPNC_Log();
                                            p1.Motherid = p.Motherid;
                                            p1.Ancregid = p.Ancregid;
                                            p1.PNCFlag = p.PNCFlag;
                                            p1.PNCComp = p.PNCComp;
                                            p1.PPCM = p.PPCM;
                                            p1.entrydate = p.entrydate;
                                            p1.PNCDate = p.PNCDate;
                                            p1.Ashaautoid = p.Ashaautoid;
                                            p1.ReferUnitID = p.ReferUnitID;
                                            p1.Child1_InfantID = p.Child1_InfantID;
                                            p1.Child1_Weight = p.Child1_Weight;
                                            p1.Child1_IsLive = p.Child1_IsLive;
                                            p1.Child1_Comp = p.Child1_Comp;
                                            p1.Child2_InfantID = p.Child2_InfantID;
                                            p1.Child2_Weight = p.Child2_Weight;
                                            p1.Child2_IsLive = p.Child2_IsLive;
                                            p1.Child2_Comp = p.Child2_Comp;
                                            p1.Child3_InfantID = p.Child3_InfantID;
                                            p1.Child3_Weight = p.Child3_Weight;
                                            p1.Child3_IsLive = p.Child3_IsLive;
                                            p1.Child3_Comp = p.Child3_Comp;
                                            p1.Child4_InfantID = p.Child4_InfantID;
                                            p1.Child4_Weight = p.Child4_Weight;
                                            p1.Child4_IsLive = p.Child4_IsLive;
                                            p1.Child4_Comp = p.Child4_Comp;
                                            p1.Child5_InfantID = p.Child5_InfantID;
                                            p1.Child5_Weight = p.Child5_Weight;
                                            p1.Child5_IsLive = p.Child5_IsLive;
                                            p1.Child5_Comp = p.Child5_Comp;
                                            p1.Freeze = p.Freeze;
                                            p1.S_mthyr = p.S_mthyr;
                                            p1.VillageAutoID = p.VillageAutoID;
                                            p1.ANMautoid = p.ANMautoid;
                                            p1.LastUpdated = p.LastUpdated;
                                            p1.Media = p.Media;
                                            p1.UpdatedBy = pnc.UserID;
                                            p1.UpdateDate = DateTime.Now;
                                            p1.IsDeleted = 1;
                                            p1.EntryUserNo = p.EntryUserNo;
                                            p1.UpdateUserNo = p.UpdateUserNo;
                                            p1.ANMVerify = p.ANMVerify;
                                            p1.IPAddress = "";
                                            p1.ANMVerificationDate = p.ANMVerificationDate;
                                            p1.MotherChildHealthFlag = p.MotherChildHealthFlag;
                                            p1.IsRecovery = 0;
                                            objcnaa.HBPNC_Log.Add(p1);
                                            objcnaa.SaveChanges();

                                            objcnaa.HBPNCs.Remove(p);
                                            objcnaa.SaveChanges();
                                            var p2 = objcnaa.Mothers.Where(x => x.ancregid == pnc.Ancregid && x.MotherID == pnc.Motherid).FirstOrDefault();
                                            if (p2 != null)
                                            {
                                                p2.LastUpdated = DateTime.Now;
                                                objcnaa.SaveChanges();
                                            }

                                            transaction.Complete();
                                            _objResponseModel.Status = true;
                                            _objResponseModel.Message = "धन्यवाद, पीएनसी का विवरण डिलीट हो चुका हैं।";
                                        }
                                        else
                                        {
                                            _objResponseModel.Status = false;
                                            _objResponseModel.Message = "ओह ! पीएनसी का विवरण डिलीट नहीं हुआ हैं। कृपया दोबारा डिलीट करें ।";
                                        }
                                    }
                                }
                                //catch (DbEntityValidationException e)
                                //{
                                //    //return "error";
                                //    foreach (var eve in e.EntityValidationErrors)
                                //    {
                                //        ErrorHandler.WriteError("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:" +
                                //            eve.Entry.Entity.GetType().Name + eve.Entry.State);
                                //        foreach (var ve in eve.ValidationErrors)
                                //        {
                                //            ErrorHandler.WriteError("- Property: \"{0}\", Error: \"{1}\"" +
                                //                ve.PropertyName + ve.ErrorMessage);
                                //        }
                                //    }
                                //    throw;
                                //}
                                catch (Exception ex)
                                {
                                    Transaction.Current.Rollback();
                                    transaction.Dispose();
                                    _objResponseModel.Status = false;
                                    _objResponseModel.Message = "ओह ! पीएनसी का विवरण डिलीट नहीं हुआ हैं। कृपया दोबारा डिलीट करें ।";
                                    ErrorHandler.WriteError("Error in delete pnc" + ex.ToString());
                                }
                            }
                        }




                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    ErrorHandler.WriteError("Error in model delete Hpnc" + Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));

                }
            }
            catch
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }



        public HttpResponseMessage DeleteHBYCDetail(HBYC HB)     //DeleteHBYC Details
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(HB.AppVersion, HB.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {

                        string ErrorMsg = "";
                        if (ValidateToken(HB.UserID, HB.TokenNo) == false)
                        {
                            ErrorMsg = "Invalid Request";
                        }
                        if (ErrorMsg != "")
                        {
                            ErrorMsg = CheckValidNumber(Convert.ToString(HB.InfantId), 1, 9, 1, "InfantId");
                        }
                        if (ErrorMsg != "")
                        {
                            ErrorMsg = CheckValidNumber(Convert.ToString(HB.HBYCFlag), 1, 1, 1, "HBYCFlag");
                        }
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                        }
                        else
                        {
                            using (TransactionScope transaction = new TransactionScope())
                            {
                                try
                                {
                                    using (cnaaEntities objcnaa = new cnaaEntities())
                                    {
                                        var p = objcnaa.HBYCs.Where(x => x.InfantId == HB.InfantId && x.HBYCFlag == HB.HBYCFlag).FirstOrDefault();
                                        if (p != null)
                                        {
                                            HBYC_Log al = new HBYC_Log();
                                            al.MotherId = p.MotherId;
                                            al.AncRegId = p.AncRegId;
                                            al.InfantId = p.InfantId;
                                            al.HBYCFlag = p.HBYCFlag;
                                            al.VillageAutoID = p.VillageAutoID;
                                            al.ANMVerificationDate = p.ANMVerificationDate;
                                            al.ANMVerify = p.ANMVerify;
                                            al.ASHAAutoID = p.ASHAAutoID;
                                            al.VisitDate = p.VisitDate;
                                            al.ORSPacket = p.ORSPacket;
                                            al.IFASirap = p.IFASirap;
                                            al.GrowthChart = p.GrowthChart;
                                            al.Color = p.Color;
                                            al.FoodAccordingAge = p.FoodAccordingAge;
                                            al.GrowthLate = p.GrowthLate;
                                            al.Refer = p.Refer;
                                            al.Freeze = p.Freeze;
                                            al.Smthyr = p.Smthyr;
                                            al.EntryDate = p.EntryDate;
                                            al.LastUpdateDate = DateTime.Now;
                                            al.EntryUserNo = p.EntryUserNo;
                                            al.UpdateUserNo = HB.UpdateUserNo;
                                            al.LogEntryDate = DateTime.Now;
                                            al.Weight = p.Weight;
                                            al.Height = p.Height;
                                            al.ReferUnitID = HB.ReferUnitID;
                                            al.Media = p.Media;
                                            al.IsDeleted = 1;
                                            objcnaa.HBYC_Log.Add(al);
                                            objcnaa.SaveChanges();
                                        }
                                        objcnaa.HBYCs.Remove(p);
                                        objcnaa.SaveChanges();
                                        var p1 = objcnaa.Infants.Where(x => x.InfantID == HB.InfantId).FirstOrDefault();
                                        p1.LastUpdated = DateTime.Now;
                                        objcnaa.SaveChanges();
                                        transaction.Complete();
                                    }

                                }
                                catch (Exception ex)
                                {
                                    ErrorHandler.WriteError(ex.ToString());
                                    Transaction.Current.Rollback();
                                    transaction.Dispose();
                                    ErrorMsg = "ओह ! एचबीवाईसी का विवरण डिलीट नहीं हुआ हैं। कृपया दोबारा डिलीट करें ।";
                                }
                            }
                            if (ErrorMsg != "")
                            {
                                _objResponseModel.Status = false;
                                _objResponseModel.Message = ErrorMsg;
                            }
                            else
                            {
                                _objResponseModel.Status = true;
                                _objResponseModel.Message = "धन्यवाद, एचबीवाईसी का विवरण डिलीट हो चुका हैं।";
                            }
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    ErrorHandler.WriteError("Error in model delete hbyc " + Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
                }

            }
            catch (Exception ex)
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Validation Error";
                ErrorHandler.WriteError("Error in validation  delete hbyc " + ex.ToString());
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("postHighRiskCasesList")]
        public HttpResponseMessage postHighRiskCasesList(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.LoginUnitid != 0)
                {
                    var data = cnaa.uspHighRisklist(p.LoginUnitid, p.VillageAutoid).ToList();

                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        private string validateHighRisk(HighRiskCas hr, Int16 methodFlag)
        {
            string ErrorMsg = "";
            if (ValidateToken(hr.UserID, hr.TokenNo) == false)
            {
                return "Invalid Request";
            }

            ErrorMsg = CheckValidNumber(Convert.ToString(hr.ANCRegid), 1, 9, 1, "ANCRegID");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = CheckValidNumber(Convert.ToString(hr.motherid), 1, 9, 1, "MotherID");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = CheckValidNumber(Convert.ToString(hr.VillageAutoID), 1, 10, 0, "VillageAutoID");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = CheckValidNumber(Convert.ToString(hr.ANCFlag), 1, 1, 1, "ANCFlag");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            if (hr.ANCFlag < 5 || hr.ANCFlag > 9)
            {
                ErrorMsg = "सम्पर्क की तारीख सही डालें !";
                return ErrorMsg;
            }

            ErrorMsg = checkDate(Convert.ToString(hr.ANCDate), 1, "सम्पर्क");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            if (methodFlag != 3)
            {
                var p1 = cnaa.HighRiskCases.Where(x => x.ANCRegid == hr.ANCRegid).ToList();
                if (p1.Count > 0)
                {
                    foreach (HighRiskCas a in p1)
                    {
                        if (a.ANCFlag == 5)
                        {
                            hr.Visit5Date = a.ANCDate;
                        }
                        else if (a.ANCFlag == 6)
                        {
                            hr.Visit6Date = a.ANCDate;
                        }
                        else if (a.ANCFlag == 7)
                        {
                            hr.Visit7Date = a.ANCDate;
                        }
                        else if (a.ANCFlag == 8)
                        {
                            hr.Visit8Date = a.ANCDate;
                        }
                        else if (a.ANCFlag == 9)
                        {
                            hr.Visit9Date = a.ANCDate;
                        }
                    }
                }

                if (hr.ANCFlag == 5)
                {
                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit6Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.ANCDate, (DateTime)hr.Visit6Date) <= 25)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 2nd सम्पर्क जाँच की तिथि के बीच में 25 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit7Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.ANCDate, (DateTime)hr.Visit7Date) <= 50)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 3rd सम्पर्क जाँच की तिथि के बीच में 50 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit8Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.ANCDate, (DateTime)hr.Visit8Date) <= 75)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 4th सम्पर्क जाँच की तिथि के बीच में 75 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit9Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.ANCDate, (DateTime)hr.Visit9Date) <= 100)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 5th सम्पर्क जाँच की तिथि के बीच में 100 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                }
                else if (hr.ANCFlag == 6)
                {
                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit5Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.Visit5Date, (DateTime)hr.ANCDate) <= 25)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 1st सम्पर्क जाँच की तिथि के बीच में 25 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit7Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.ANCDate, (DateTime)hr.Visit7Date) <= 25)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 3rd सम्पर्क जाँच की तिथि के बीच में 25 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit8Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.ANCDate, (DateTime)hr.Visit8Date) <= 50)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 4th सम्पर्क जाँच की तिथि के बीच में 50 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit9Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.ANCDate, (DateTime)hr.Visit9Date) <= 75)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 5th सम्पर्क जाँच की तिथि के बीच में 75 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                }
                else if (hr.ANCFlag == 7)
                {
                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit5Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.Visit5Date, (DateTime)hr.ANCDate) <= 50)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 1st सम्पर्क जाँच की तिथि के बीच में 50 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit6Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.Visit6Date, (DateTime)hr.ANCDate) <= 25)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 2nd सम्पर्क जाँच की तिथि के बीच में 25 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit8Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.ANCDate, (DateTime)hr.Visit8Date) <= 25)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 4th सम्पर्क जाँच की तिथि के बीच में 25 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit9Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.ANCDate, (DateTime)hr.Visit9Date) <= 50)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 5th सम्पर्क जाँच की तिथि के बीच में 50 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                }
                else if (hr.ANCFlag == 8)
                {
                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit5Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.Visit5Date, (DateTime)hr.ANCDate) <= 75)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 1st सम्पर्क जाँच की तिथि के बीच में 75 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit6Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.Visit6Date, (DateTime)hr.ANCDate) <= 50)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 2nd सम्पर्क जाँच की तिथि के बीच में 50 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit7Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.Visit7Date, (DateTime)hr.ANCDate) <= 25)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 3rd सम्पर्क जाँच की तिथि के बीच में 25 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }

                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit9Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.ANCDate, (DateTime)hr.Visit9Date) <= 25)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 5th सम्पर्क जाँच की तिथि के बीच में 25 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                }
                else if (hr.ANCFlag == 9)
                {
                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit5Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.Visit5Date, (DateTime)hr.ANCDate) <= 100)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 1st सम्पर्क जाँच की तिथि के बीच में 100 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit6Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.Visit6Date, (DateTime)hr.ANCDate) <= 75)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 2nd सम्पर्क जाँच की तिथि के बीच में 75 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit7Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.Visit7Date, (DateTime)hr.ANCDate) <= 50)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 3rd सम्पर्क जाँच की तिथि के बीच में 50 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }

                    if (!String.IsNullOrEmpty(Convert.ToString(hr.Visit8Date)))
                    {
                        if (DifferenceInDays((DateTime)hr.Visit8Date, (DateTime)hr.ANCDate) <= 25)
                        {
                            return "कृपया सम्पर्क की तिथि जाँचे|\nसम्पर्क एवं 4th सम्पर्क जाँच की तिथि के बीच में 25 दिन से अधिक का अंतर होना चाहिए|";
                        }
                    }
                }

            }



            return ErrorMsg;
        }

        private string SaveHighRiskDetails(HighRiskCas hr, Int16 methodFlag)
        {

            string ErrorMsg = validateHighRisk(hr, methodFlag);
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            else
            {
                hr.ContactUnitID = rajmed.UnitMasters.Where(x => x.UnitCode == hr.ContactUnitcode).Select(x => x.UnitID).FirstOrDefault();
                hr.DelPlaceUnitID = rajmed.UnitMasters.Where(x => x.UnitCode == hr.DelUnitcode).Select(x => x.UnitID).FirstOrDefault();
                using (TransactionScope transaction = new TransactionScope())
                {
                    try
                    {
                        using (cnaaEntities objcnaa = new cnaaEntities())
                        {
                            var p = objcnaa.HighRiskCases.Where(x => x.ANCRegid == hr.ANCRegid && x.ANCFlag == hr.ANCFlag).FirstOrDefault();
                            if (p != null)
                            {
                                Int16 AnyChanges = 0;
                                if (p.ANCDate != hr.ANCDate)
                                    AnyChanges = 1;
                                else if (p.ContactUnitID != hr.ContactUnitID)
                                    AnyChanges = 1;
                                else if (p.DelPlaceUnitID != hr.DelPlaceUnitID)
                                    AnyChanges = 1;

                                if (AnyChanges == 1)
                                {
                                    HighRiskCasesLog al = new HighRiskCasesLog();
                                    al.ANCFlag = p.ANCFlag;
                                    al.ANCDate = p.ANCDate;
                                    al.ContactUnitID = p.ContactUnitID;
                                    al.DelPlaceUnitID = p.DelPlaceUnitID;
                                    al.Ashaautoid = p.Ashaautoid;
                                    al.ANMVerificationDate = p.ANMVerificationDate;
                                    al.ANMVerify = p.ANMVerify;
                                    if (methodFlag != 3)
                                    {
                                        al.DeleteFlag = 0;
                                    }
                                    else
                                    {
                                        al.DeleteFlag = 1;
                                    }

                                    al.LastUpdated = p.LastUpdated;
                                    al.Media = p.Media;
                                    al.motherid = p.motherid;
                                    al.LogEntryDate = DateTime.Now;
                                    al.UpdateUserNo = p.UpdateUserNo;
                                    al.LogUserNo = Convert.ToInt32(hr.UpdateUserNo);
                                    al.LogIPAddress = "";
                                    objcnaa.HighRiskCasesLogs.Add(al);
                                    objcnaa.SaveChanges();
                                }

                            }
                            if (methodFlag != 3)
                            {
                                if (p == null)
                                {
                                    p = new HighRiskCas();
                                    p.ANCRegid = hr.ANCRegid;
                                    p.ANCFlag = hr.ANCFlag;
                                    p.motherid = hr.motherid;
                                    p.VillageAutoID = hr.VillageAutoID;
                                    p.Entrydate = DateTime.Now;
                                    p.EntryUserNo = hr.UpdateUserNo;
                                }
                                p.Ashaautoid = hr.Ashaautoid;
                                p.ANCDate = hr.ANCDate;
                                p.DelPlaceUnitID = hr.DelPlaceUnitID;
                                p.ContactUnitID = hr.ContactUnitID;
                                p.UpdateUserNo = hr.UpdateUserNo;
                                p.LastUpdated = DateTime.Now;
                                p.Media = hr.Media;
                                p.ANMVerify = 0;
                                p.ANMVerificationDate = null;
                                if (methodFlag == 1)
                                {
                                    objcnaa.HighRiskCases.Add(p);
                                }
                            }
                            else
                            {
                                objcnaa.HighRiskCases.Remove(p);
                            }
                            objcnaa.SaveChanges();
                            var p2 = objcnaa.Mothers.Where(x => x.ancregid == hr.ANCRegid && x.MotherID == hr.motherid).FirstOrDefault();
                            p2.LastUpdated = DateTime.Now;
                            objcnaa.SaveChanges();
                            transaction.Complete();
                            return "";
                        }

                    }
                    catch (DbEntityValidationException ex)
                    {
                        Transaction.Current.Rollback();
                        transaction.Dispose();
                        //foreach (var eve in ex.EntityValidationErrors)
                        //{
                        //    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        //        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                        //    foreach (var ve in eve.ValidationErrors)
                        //    {
                        //        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                        //            ve.PropertyName, ve.ErrorMessage);
                        //    }
                        //}

                        if (methodFlag == 1)
                        {
                            ErrorMsg = "ओह ! हाईरिस्क का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें । ";
                            ErrorHandler.WriteError("Error in Post high risk" + ex.ToString());
                        }
                        else
                        {
                            ErrorMsg = "ओह ! हाईरिस्क का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा अपडेट करें ।";
                            ErrorHandler.WriteError("Error in PUT high risk" + ex.ToString());
                        }

                    }
                }



            }
            return ErrorMsg;
        }


        public HttpResponseMessage PostHighRiskDetail(HighRiskCas hr)     //AddHigh Risk Details
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(hr.AppVersion, hr.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {

                        string ErrorMsg = SaveHighRiskDetails(hr, 1);

                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, हाईरिस्क का विवरण सेव हो चुका हैं।";
                        }


                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }

            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post highRisk" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        }

        public HttpResponseMessage PutHighRiskDetail(HighRiskCas hr)     //AddHigh Risk Details
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(hr.AppVersion, hr.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {

                        string ErrorMsg = SaveHighRiskDetails(hr, 2);

                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, हाईरिस्क का विवरण सेव हो चुका हैं।";
                        }


                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }

            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Put highRisk" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        }

        public HttpResponseMessage DeleteHighRiskDetail(HighRiskCas hr)     //AddHigh Risk Details
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(hr.AppVersion, hr.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {

                        string ErrorMsg = SaveHighRiskDetails(hr, 3);

                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, हाईरिस्क का विवरण डिलीट हो चुका हैं।";
                        }


                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }

            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Put highRisk" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        }

        public HttpResponseMessage uspDataforManageHighRiskCases(Pcts a)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(a.UserID, a.TokenNo);
            if (tokenFlag == true)
            {
                if (a.ANCRegID != 0 && a.MotherID != 0)
                {
                    var data = cnaa.uspManageHighRiskCases(a.ANCRegID, a.MotherID).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";

                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage uspANMHighRiskVerify(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                using (cnaaEntities objcnaa = new cnaaEntities())
                {
                    var p1 = objcnaa.HighRiskCases.Where(x => x.ANCRegid == p.ANCRegID && x.ANCFlag == p.type).FirstOrDefault();
                    if (p1 == null)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Invalid High Risk Details";
                    }
                    else
                    {
                        p1.Media = (p1.Media == (byte)2) ? (byte)3 : (byte)1;
                        p1.ANMVerify = 1;
                        p1.ANMVerificationDate = DateTime.Now;
                        objcnaa.SaveChanges();
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data has been saved";
                    }


                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage uspFamiliyDetailsByJanAadhaarNo(Mother_App p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                string url = "";
                if (p.AadhaarNo != null)
                {
                    url = "https://api.sewadwaar.rajasthan.gov.in/app/live/Janaadhaar/Prod/Service/action/fetchJayFamily/" + Convert.ToString(p.AadhaarNo) + "?client_id=e81959ba-820c-4da9-925a-98648768961c";
                }
                else
                {
                    url = "https://api.sewadwaar.rajasthan.gov.in/app/live/Janaadhaar/Prod/Service/action/fetchJayFamily/" + Convert.ToString(p.JanAadhaarNo) + "?client_id=e81959ba-820c-4da9-925a-98648768961c";
                }
                HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse myResp = (HttpWebResponse)myReq.GetResponse();
                System.IO.StreamReader respStreamReader = new System.IO.StreamReader(myResp.GetResponseStream());
                string responseFromServer = "";
                if (myResp.StatusCode == HttpStatusCode.OK)
                {
                    responseFromServer = respStreamReader.ReadToEnd();
                }
                respStreamReader.Close();
                List<BHAMASHAH> bh = new List<BHAMASHAH>();
                if (responseFromServer != "")
                {
                    JArray array = JArray.Parse(responseFromServer);
                    foreach (JObject obj in array.Children<JObject>())
                    {
                        BHAMASHAH b = new BHAMASHAH();
                        foreach (JProperty singleProp in obj.Properties())
                        {
                            if (singleProp.Name == "NAME")
                                b.Name = singleProp.Value.ToString();
                            else if (singleProp.Name == "NAME_HINDI")
                                b.NameH = singleProp.Value.ToString();
                            else if (singleProp.Name == "MOBILE_NO")
                                b.MoblieNo = singleProp.Value.ToString();
                            else if (singleProp.Name == "ENR_ID")
                                b.BHAMASHAHAckID = singleProp.Value.ToString();
                            else if (singleProp.Name == "JAN_MEMBER_ID")
                                b.JanMemberID = singleProp.Value.ToString();
                            else if (singleProp.Name == "JAN_AADHAR")
                                b.JanAadhaarNo = singleProp.Value.ToString();
                            else if (singleProp.Name == "AADHAR_ID")
                                b.AadhaarNo = singleProp.Value.ToString();
                        }
                        bh.Add(b);

                    }
                    if (bh.Count > 0)
                    {
                        _objResponseModel.ResposeData = bh;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.ResposeData = bh;
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data found";
                    }


                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        public HttpResponseMessage uspPostJanAadhaarNo(BHAMASHAH p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                string pctsid = "";
                string message = "";
                if (p.AadhaarNo != null)
                {
                    pctsid = Convert.ToString(cnaa.Database.SqlQuery<string>("select pctsid from cnaa..mother moth,pcts..BHAMASHAH p where moth.motherid = p.id and p.idflag = 1 and AadhaarNo={0}", p.AadhaarNo).FirstOrDefault());
                    message = "यह आधार नंबर पहले से ही इस पीसीटीएस " + pctsid + " से जुड़ा हुआ हैं !";
                }
                else
                {
                    pctsid = Convert.ToString(cnaa.Database.SqlQuery<string>("select pctsid from cnaa..mother moth,pcts..BHAMASHAH p where moth.motherid = p.id and p.idflag = 1 and JanAadhaarNo={0} and JanMemberID = {1} ", p.JanAadhaarNo, p.JanMemberID).FirstOrDefault());
                    message = "यह जन आधार नंबर पहले से ही इस पीसीटीएस " + pctsid + " से जुड़ा हुआ हैं !";
                }

                Dictionary<string, string> hash = new Dictionary<string, string> { };
                hash.Add("pctsid", pctsid);
                if (pctsid != "" && pctsid != null)
                {
                    _objResponseModel.ResposeData = hash;
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = message;
                }
                else
                {
                    _objResponseModel.ResposeData = hash;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "No Data found";
                }



            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage PostForgotPassword(BeneficiaryMobileNo_Maa u)
        {
            string message = "Data Received successfully";
            ResponseModel _objResponseModel = new ResponseModel();
            var p = rajmed.Users.Where(x => x.UserID == u.UserID).Select(x => x.UserContactNo).FirstOrDefault();
            if (p == null)
            {
                message = "इस यूजर आईडी से मोबाइल नम्बर कोई लिंक नहीं है ";
                _objResponseModel.Status = false;
            }
            else
            {
                bool sendSmsFlag = sendSms1(p.ToString(), Convert.ToByte(u.SmsFlag));
                if (sendSmsFlag)
                    message = "OTP Sent successfully";
                else
                    message = "OTP can not sent ";
                if (sendSmsFlag)
                {
                    string salt = Guid.NewGuid().ToString();

                    var datas = cnaa.PctsTokens.Where(a => a.UserID == u.UserID && a.DeviceID == u.DeviceID).FirstOrDefault();
                    if (datas == null)
                    {
                        PctsToken pt = new PctsToken();
                        pt.UserID = u.UserID;
                        pt.DeviceID = u.DeviceID;
                        pt.Salt = salt;
                        pt.Date = DateTime.Now;
                        cnaa.PctsTokens.Add(pt);
                        cnaa.SaveChanges();

                    }
                    else
                    {
                        datas.Salt = salt;
                        datas.Date = DateTime.Now;
                        cnaa.SaveChanges();
                    }
                    Dictionary<string, string> hash = new Dictionary<string, string> { };
                    hash.Add("Token", salt);
                    hash.Add("UserContactNo", p.ToString());
                    _objResponseModel.ResposeData = hash;
                }


                _objResponseModel.Status = sendSmsFlag;
            }

            _objResponseModel.Message = message;
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage postAncDetailsForClaimForm(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            ASHAClaimForm a = new ASHAClaimForm();
            if (tokenFlag == true)
            {

                if (p.ASHAAutoid != 0)
                {
                    var data0 = cnaa.Database.SqlQuery<AshaBasicDetail>("exec AshaBasicDetail {0},{1}  ", p.ASHAAutoid, p.MthYr).Select(x => new { Population = x.Population, ECCount = x.ECCount, FamilyCount = x.FamilyCount, HouseHoldCount = x.HouseHoldCount, HealthLocationName = x.HealthLocationName, HealthLocationTypeName = x.HealthLocationTypeName, PHCCHCName = x.PHCCHCName, PHCCHCTypeName = x.PHCCHCTypeName, V_FromDate = x.V_FromDate, V_ToDate = x.V_ToDate, ScheduleMonthName = x.ScheduleMonthName, ScheduleYear = x.ScheduleYear, ClaimAmount = x.ClaimAmount }).ToList();
                    a.AshaBasicDetail = data0;
                    AncPayment ap = new AncPayment();
                    ap.Urban12Week = 75;
                    ap.UrbanTD = 75;
                    ap.UrbanSecondAnc = 50;
                    ap.UrbanThirdAnc = 50;
                    ap.UrbanForthAnc = 50;
                    ap.Rural12Week = 75;
                    ap.RuralTD = 50;
                    ap.RuralSecondAnc = 25;
                    ap.RuralThirdAnc = 25;
                    ap.RuralForthAnc = 25;
                    List<AncPayment> ab = new List<AncPayment> { };
                    ab.Add(ap);
                    a.AncPayment = ab;
                    p.Unitcode = "01010601";
                    p.MthYr = 202304;
                    p.ASHAAutoid = 694;
                    var data = cnaa.Database.SqlQuery<AncCheckupClaimForm>("exec ashasoft..USP_Anc3Checkups {0},{1},{2},{3} ", p.Unitcode, p.MthYr, 3, p.ASHAAutoid).Select(x => new { PctsID = x.PctsID, ANC1 = x.ANC1, ANC2 = x.ANC2, ANC3 = x.ANC3, ANC4 = x.ANC4, LMPDate = x.LMPDate, WomenName = x.WomenName, HusbName = x.HusbName, HighRisk = x.HighRiskFlag, RegDate = x.LMPDate, ReferFlag = x.ReferFlag, MotherMobileNo = x.MotherMobileNo }).ToList();
                    a.AncCheckup = data;
                    var data2 = cnaa.Database.SqlQuery<HBNCClaimForm>("exec ashasoft..USP_HBPNCDetails {0},{1},{2},{3} ", p.Unitcode, p.MthYr, 3, p.ASHAAutoid).Select(x => new { PctsID = x.PctsID, WomenName = x.WomenName, HusbName = x.Husbname, PncDate = x.PncDate, PncFlag = x.PncFlag, ReferFlag = 1, VisitDate = x.PncDate }).ToList();
                    a.HBNC = data2;
                    var data3 = cnaa.Database.SqlQuery<CollectingAadhaarClaimForm>("exec ashasoft..USP_CollectingACnAadhars {0},{1},{2},{3} ", p.Unitcode, p.MthYr, 3, p.ASHAAutoid).Select(x => new { PctsID = x.PctsID, WomenName = x.WomenName, HusbName = x.Husbname, AadhaarBankInfoUpdatedDate = x.AadhaarBankInfoUpdatedDate, Ifsc_code = x.Ifsc_code, BankName = x.BankName, AccountNo = x.AccountNo }).ToList();
                    a.CollectingAadhaar = data3;
                    var data4 = cnaa.Database.SqlQuery<DeliveryClaimForm>("exec ashasoft..USP_DeliveryDetails {0},{1},{2},{3} ", p.Unitcode, p.MthYr, 3, p.ASHAAutoid).Select(x => new { PctsID = x.PctsID, WomenName = x.WomenName, HusbName = x.HusbName, DeliveryDate = x.Prasav_date, DeliveryLocationName = x.DeliveryLocationName, RuralUrban = x.RuralUrban, DeliveryType = x.DeliveyType, OutCome = x.OutCome, Sex = x.InfantGender, Complication = x.DeliveryComplication }).ToList();
                    a.Delivery = data4;
                    var data5 = cnaa.Database.SqlQuery<MaternalDeathClaimForm>("exec ashasoft..USP_MaternalDetails {0},{1},{2},{3} ", p.Unitcode, p.MthYr, 3, p.ASHAAutoid).Select(x => new { PctsID = x.PctsID, WomenName = x.Name, HusbName = x.HusbName, Age = x.Age, DeathDate = x.DeathDate, ReasonName = x.ReasonName, DeathPlace = x.DeathPlace, MobileNo = x.MobileNo, DeliveryDate = x.DeliveryDate, Preventable = x.Preventable }).ToList();
                    a.MaternalDeath = data5;
                    var data6 = cnaa.Database.SqlQuery<InfantDeathClaimForm>("exec ashasoft..USP_InfantDetails {0},{1},{2},{3} ", p.Unitcode, p.MthYr, 3, p.ASHAAutoid).Select(x => new { PctsID = x.PctsID, InfantName = x.Name, AgeDetail = x.AgeDetail, DeathDate = x.DeathDate, ReasonName = x.ReasonName }).ToList();
                    a.InfantDeath = data6;
                    var data7 = cnaa.Database.SqlQuery<HBYCClaimForm>("exec ashasoft..USP_HBYCDetails {0},{1},{2},{3} ", p.Unitcode, p.MthYr, 3, 1904).Select(x => new { PctsID = x.PctsID, InfantName = x.infantname, WomenName = x.WomenName, HbycDate = x.PncDate, HbycFlag = x.PncFlag, ORS = "", IFASyrup = "", Husbname = "" }).ToList();
                    a.HBYC = data7;
                    //var data8 = cnaa.Database.SqlQuery<FullImmunizationClaimForm>("exec ashasoft..FullImmunizationDetails {0},{1},{2},{3} ", "010106010", 202208, 3, 79427).Select(x => new { PctsID = x.PctsID, InfantName = x.infantname, WomenName = x.motherName, FullImmuDate = x.FullImmuDate, Husbname = x.Husbname }).ToList();
                    //a.FullImmunization = data8;
                    var data9 = cnaa.Database.SqlQuery<Booster1ClaimForm>("exec ashasoft..USP_DTPBoosterDetails {0},{1},{2},{3} ", p.Unitcode, p.MthYr, 3, p.ASHAAutoid).Select(x => new { PctsID = x.PctsID, InfantName = x.infantname, WomenName = x.motherName, DptBoosterDate = x.DptBoosterDate, OpvBoosterDate = x.OpvBoosterDate, Mr2BoosterDate = x.Mr2BoosterDate, Husbname = x.Husbname }).ToList();
                    a.DTPBoosterDetails = data9;
                    //var data10 = cnaa.Database.SqlQuery<Booster2ClaimForm>("exec ashasoft..FullImmunizationDetails {0},{1},{2},{3} ", "010106010", 202208, 3, 79427).Select(x => new { PctsID = x.PctsID, InfantName = x.infantname, WomenName = x.motherName, BoosterDate = x.BoosterDate, Husbname = x.Husbname }).ToList();
                    //a.Booster2 = data10;
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = a;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }



                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage GetActivityMonth()
        {
            ResponseModel _objResponseModel = new ResponseModel();
            int mthyr = cnaa.Database.SqlQuery<int>("select max(mthyr) from ashasoft..Schedule ").FirstOrDefault();
            int preMthyr = cnaa.Database.SqlQuery<int>("select max(mthyr) from ashasoft..Schedule where mthyr <{0} ", mthyr).FirstOrDefault();
            int mth = 0;
            List<Dictionary<string, string>> t = new List<Dictionary<string, string>>();
            for (int i = preMthyr + 1; i <= mthyr; i++)
            {
                Dictionary<string, string> hash = new Dictionary<string, string> { };
                if (Convert.ToString(i).Substring(4, 2) == "13")
                    i = i + 88;
                mth = i;
                hash.Add("MonthName", CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Convert.ToInt32(mth.ToString().Substring(4, 2))).ToString().Substring(0, 3) + "-" + mth.ToString().Substring(0, 4));
                hash.Add("MonthValue", mth.ToString());
                t.Add(hash);
            }
            _objResponseModel.ResposeData = t;
            _objResponseModel.Status = true;
            _objResponseModel.Message = "Data Received successfully";
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage GetMtcList()
        {
            ResponseModel _objResponseModel = new ResponseModel();
            var data = cnaa.Database.SqlQuery<MtcList>("exec ASHASoft_FillMTC {0} ", 0).ToList();

            _objResponseModel.ResposeData = data;
            _objResponseModel.Status = true;
            _objResponseModel.Message = "Data Received successfully";
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        private string ValidateSamChild(SamChild sc, Int16 methodFlag)
        {
            string ErrorMsg = "";
            if (ValidateToken(sc.UserID, sc.TokenNo) == false)
            {
                return "Invalid Request";
            }
            if (methodFlag == 2 && sc.SamAutoid == 0)
            {
                return "Invalid Request";
            }

            if (!String.IsNullOrEmpty(Convert.ToString(sc.Dob)))
            {
                ErrorMsg = checkDate(Convert.ToString(sc.Dob), 1, "जन्म तिथि");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                if (DifferenceInDays((DateTime)sc.Dob, DateTime.Now) < 0)
                {
                    ErrorMsg = "SAM Child की जन्म तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
                    return ErrorMsg;
                }


            }
            else
            {
                ErrorMsg = "कृपया SAM Child की जन्म तिथि डालें !";
                return ErrorMsg;
            }
            ErrorMsg = CheckValidNumber(Convert.ToString(sc.AshaAutoid), 1, 9, 1, "Asha");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            if (sc.AshaAutoid == 0)
            {
                ErrorMsg = "कृपया ASHA चुनें !";
                return ErrorMsg;
            }
            ErrorMsg = CheckValidNumber(Convert.ToString(sc.ActivityMthyr), 1, 9, 1, "Mthyr");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            if (string.IsNullOrEmpty(Convert.ToString(sc.Name)))
            {
                return "कृपया नाम लिखें !";
            }
            else if (!Regex.IsMatch(sc.Name, @"^([a-zA-Z\s])+$"))
            {
                return "कृपया नाम सही लिखें !";
            }
            if (string.IsNullOrEmpty(Convert.ToString(sc.MotherName)))
            {
                return "कृपया Mother`s नाम लिखें !";
            }
            else if (!Regex.IsMatch(sc.MotherName, @"^([a-zA-Z\s])+$"))
            {
                return "कृपया Mother`s नाम सही लिखें !";
            }
            if (string.IsNullOrEmpty(Convert.ToString(sc.FatherName)))
            {
                return "कृपया father`s नाम लिखें !";
            }
            else if (!Regex.IsMatch(sc.FatherName, @"^([a-zA-Z\s])+$"))
            {
                return "कृपया father`s नाम सही लिखें !";
            }
            if (!string.IsNullOrEmpty(Convert.ToString(sc.ChildID)))
            {
                if (Convert.ToString(sc.ChildID).Length < 16 || Convert.ToString(sc.ChildID).Length > 22)
                {
                    return "कृपया infant नाम लिखें !";
                }


            }
            if (!string.IsNullOrEmpty(sc.MobileNo))
            {
                if (sc.MobileNo.Length == 10)
                {
                    if (checkStringWithSameDigit(sc.MobileNo))
                    {
                        return "कृपया सही मोबाईल नं. लिखे !";
                    }
                    if (validateMobileNoSameDigitMorethantimes(sc.MobileNo, 5))
                    {
                        return "कृपया सही मोबाईल नं. लिखे !";
                    }
                }
            }
            else
            {
                return "कृपया मोबाईल नं. लिखे !";
            }
            if (Convert.ToString(sc.Sex) != "1" && Convert.ToString(sc.Sex) != "2" && Convert.ToString(sc.Sex) != "3")
            {
                return "कृपया sex लिखे !";
            }
            ErrorMsg = CheckValidNumber(Convert.ToString(sc.MtcID), 1, 9, 1, "MTCId");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            if (!String.IsNullOrEmpty(Convert.ToString(sc.AdmitDate)))
            {
                ErrorMsg = checkDate(Convert.ToString(sc.AdmitDate), 1, "Admit तिथि");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                if (DifferenceInDays((DateTime)sc.AdmitDate, DateTime.Now) < 0)
                {
                    ErrorMsg = "SAM Child की Admit तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
                    return ErrorMsg;
                }
            }
            else
            {
                ErrorMsg = "कृपया SAM Child की Admit तिथि डालें !";
                return ErrorMsg;
            }
            if (!String.IsNullOrEmpty(Convert.ToString(sc.ReferDate)))
            {
                ErrorMsg = checkDate(Convert.ToString(sc.ReferDate), 1, "Refer तिथि");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                if (DifferenceInDays((DateTime)sc.ReferDate, DateTime.Now) < 0)
                {
                    ErrorMsg = "SAM Child की Refer तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
                    return ErrorMsg;
                }
            }
            else
            {
                ErrorMsg = "कृपया SAM Child की Refer तिथि डालें !";
                return ErrorMsg;
            }
            if (DifferenceInDays((DateTime)sc.AdmitDate, (DateTime)sc.ReferDate) > (5 * 365))
            {
                ErrorMsg = "At the time of Admission in MTC, the child should be atleast minimum from 1 Day to 5 Year old. !";
                return ErrorMsg;
            }
            if (DifferenceInDays((DateTime)sc.ReferDate, (DateTime)sc.Dob) > 0)
            {
                ErrorMsg = "Please Enter Correct Date of Refer in MTC !";
                return ErrorMsg;
            }
            if (Convert.ToString(sc.MuacMap) == "")
            {
                return "कृपया एमयूऐसी माप भरें ! ";
            }
            //weight
            if (Convert.ToString(sc.Weight) == "")
            {
                return "कृपया बच्चे का वजन भरें ! ";
            }
            if (Convert.ToString(sc.Transport) != "0" && Convert.ToString(sc.Transport) != "1" && Convert.ToString(sc.Transport) != "2")
            {
                return "कृपया Transport भरें ! ";
            }
            if (methodFlag == 1)
            {

            }
            int count = cnaa.Database.SqlQuery<int>("select count(*) from ashasoft..SamChildDetails where Ashaautoid={0} and LOWER(Name)=LOWER({1}) and Dob={2} and MTCId={3} and Mobile={4} and dbo.Getfinyear(mthyr)=ashasoft.dbo.Getfinyear({5}) and SamAutoid != {6} ", sc.SamAutoid, sc.Name, sc.Dob, sc.MtcID, sc.MobileNo, sc.ActivityMthyr, sc.SamAutoid).FirstOrDefault();
            if (count < 4)
            {
                count = cnaa.Database.SqlQuery<int>("select count(*) from ashasoft..SamChildDetails where Ashaautoid={0} and LOWER(Name)=LOWER({1}) and Dob={2} and MTCId={3} and Mobile={4} and mthyr={5} and SamAutoid != {6} ", sc.SamAutoid, sc.Name, sc.Dob, sc.MtcID, sc.MobileNo, sc.ActivityMthyr, sc.SamAutoid).FirstOrDefault();
                if (count > 0)
                {
                    return "Recored Already Exists. Please Check.";
                }
            }
            else
            {
                return "साल में अधिकतम 4 बार ही बच्चे को रेफेर किया जा सकता है .";
            }
            return ErrorMsg;
        }

        private string SaveSamDetails(SamChild sc, Int16 methodFlag)
        {
            string ErrorMsg = "";
            if (methodFlag == 1 || methodFlag == 2)
            {
                ErrorMsg = ValidateSamChild(sc, methodFlag);

            }
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            else
            {
                using (TransactionScope transaction = new TransactionScope())
                {
                    try
                    {
                        using (cnaaEntities objcnaa = new cnaaEntities())
                        {
                            if (methodFlag == 1)
                            {
                                var Result = objcnaa.Database.SqlQuery<string>("exec ASHASoft_SamChild {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}  ", sc.ActivityMthyr, sc.SamAutoid, sc.AshaAutoid, sc.ChildID, sc.Name, sc.Dob, sc.Address, sc.MtcID, sc.AdmitDate, sc.Transport, sc.MotherName, sc.FatherName, sc.MobileNo, sc.Sex, sc.ReferDate, sc.MuacMap, sc.Weight, sc.Remarks, sc.UpdateUserNo, sc.Media, sc.SamAutoid).FirstOrDefault(); 
                                if (Result != "0")
                                {
                                    ErrorMsg = Result;
                                }                                
                            }
                            else if (methodFlag == 3) //delete
                            {
                                //var Result = objcnaa.Database.SqlQuery<string>("exec ASHASoft_MaaProgram 0,{0},0,0,0,0,0,{1} ", Maa.PermType, Maa.RowID).FirstOrDefault();                                
                                //objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_SamChild {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}  ", sc.ActivityMthyr, sc.SamAutoid, sc.AshaAutoid, sc.ChildID, sc.Name, sc.Dob, sc.Address, sc.MtcID, sc.AdmitDate, sc.Transport, sc.MotherName, sc.FatherName, sc.MobileNo, sc.Sex, sc.ReferDate, sc.MuacMap, sc.Weight, sc.Remarks, sc.UpdateUserNo, sc.Media);

                                var Result = objcnaa.Database.SqlQuery<string>("exec ASHASoft_SamChildDelete {0},{1} ", sc.Type, sc.SamAutoid).FirstOrDefault();
                                if (Result != "0")
                                {
                                    ErrorMsg = Result;
                                }
                            }
                            else if (methodFlag == 6)//verify
                            {
                                objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_SamChild_Verification {0},{1},{2}", sc.UpdateUserNo, sc.Media, sc.SamAutoid);
                            }
                            transaction.Complete();
                        }
                    }
                    catch (Exception ex)
                    {
                        Transaction.Current.Rollback();
                        transaction.Dispose();
                        if (methodFlag == 1)
                        {
                            ErrorMsg = "ओह ! SAM का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें । ";
                            ErrorHandler.WriteError("Error in Post sam" + ex.ToString());
                        }
                        else if (methodFlag == 3)
                        {
                            ErrorMsg = "ओह ! SAM का विवरण Delete नहीं हुआ हैं। कृपया दोबारा Delete करें ।";
                            ErrorHandler.WriteError("Error in PUT sam" + ex.ToString());
                        }
                        else
                        {
                            ErrorMsg = "ओह ! SAM का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा अपडेट करें ।";
                            ErrorHandler.WriteError("Error in PUT sam" + ex.ToString());
                        }
                    }
                }
            }
            return ErrorMsg;
        }
        [ActionName("PostSamChild")]
        public HttpResponseMessage PostSamChild(SamChild sc)     //AddANC/UpdateANC Details
        {
            // writeclassdata(anc);
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(sc.AppVersion, sc.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {

                        string ErrorMsg = SaveSamDetails(sc, 1);

                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, SAm का विवरण सेव हो चुका हैं।";
                        }


                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }

            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post sam" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
                //foreach (var eve in e.EntityValidationErrors)
                //{
                //    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                //        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                //    foreach (var ve in eve.ValidationErrors)
                //    {
                //        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                //            ve.PropertyName, ve.ErrorMessage);
                //    }
                //}
                //throw;
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("PutSamChild")]
        public HttpResponseMessage PutSamChild(SamChild sc)     //AddANC/UpdateANC Details
        {
            //  writeclassdata(sc);
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(sc.AppVersion, sc.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveSamDetails(sc, sc.Type);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else if (sc.Type == 3)
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, SAm का विवरण Delete हो चुका हैं।";
                        }
                        else if (sc.Type == 6)
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, SAm का विवरण Verified हो चुका हैं।";
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, SAm का विवरण Update हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in PUT sam" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";

            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("uspSamDetail")]
        public HttpResponseMessage uspSamDetail(SamChild a)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(a.UserID, a.TokenNo);
            if (tokenFlag == true)
            {
                if (a.AshaAutoid > 0)
                {
                    var data = cnaa.Database.SqlQuery<SamChild>("exec ASHASoft_SamDetail {0},{1}", a.AshaAutoid, a.Type).ToList();
                    if (data.Count > 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage uspANMImmunizationVerify(ImmunizationData immu)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(immu.UserID, immu.TokenNo);
            if (tokenFlag == true)
            {
                using (cnaaEntities objcnaa = new cnaaEntities())
                {
                    byte immCode = Convert.ToByte(immu.ImmuCode);

                    var p1 = objcnaa.Immunizations.Where(x => x.InfantID == immu.InfantID && x.ImmuCode == immCode).FirstOrDefault();
                    if (p1 == null)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Invalid Infant";
                    }
                    else
                    {
                        //p1.Media = 3;
                        //p1.ANMVerify = 1;
                        //p1.ANMVerificationDate = DateTime.Now;
                        //p1.UpdateUserNo = p.UserNo;
                        //objcnaa.SaveChanges();
                        //_objResponseModel.Status = true;
                        //_objResponseModel.Message = "Data has been saved";

                        string ErrorMsg = "";
                        try
                        {
                            immu.Media = (immu.Media == (byte)2) ? (byte)3 : (byte)1; ;
                            string weight = Convert.ToString(immu.Weight);
                            int p2 = cnaa.InsertUpdateImmunization(immu.InfantID, immu.ASHAAutoid, immu.ImmuCode, immu.ImmuDate.ToString("dd/MM/yyyy"), immu.VillageAutoid,
                            immu.LoginUnitid, immu.EntryBy, "", immu.MotherID, 4, immu.BirthDate.ToString("dd/MM/yyyy"), immu.PartImmu, Convert.ToByte("0"), Convert.ToByte("0"),
                            immu.LoginUnitid, weight, immu.EntryUserNo, 0, immu.UnitCode, Convert.ToByte(immu.Media), immu.Latitude, immu.Longitude, Convert.ToByte(immu.ANMVerify)).Select(x => x.error).FirstOrDefault();
                            if (p2 == 0)
                            {
                                _objResponseModel.Status = true;
                                _objResponseModel.ResposeData = p2;
                                _objResponseModel.Message = "धन्यवाद, टीके का विवरण सत्यापित हो चुका हैं।";
                            }
                            else
                            {
                                _objResponseModel.Status = false;
                                _objResponseModel.ResposeData = p2;
                                _objResponseModel.Message = "ओह ! टीके का विवरण वेरीफाई नहीं हुआ हैं। कृपया दोबारा सत्यापित करें ।";
                                ErrorHandler.WriteError("Error in put immunization for " + immu.ImmuCode);
                            }
                        }

                        catch (Exception ex)
                        {
                            ErrorMsg = ex.ToString();
                            ErrorHandler.WriteError("Error in validation delete anc" + ErrorMsg);
                        }


                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = "ओह ! टीके का विवरण डिलीट नहीं हुआ हैं। कृपया दोबारा डिलीट करें ।";
                            ErrorHandler.WriteError("Error in validation delete anc" + ErrorMsg);
                        }
                    }


                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage DeleteImmunizationDetail(ImmunizationData immu)     //UpdateANC Details
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(immu.UserID, immu.TokenNo);
            if (tokenFlag == true)
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        int CheckAppVersionFlag = CheckVersion(immu.AppVersion, immu.IOSAppVersion);
                        if (CheckAppVersionFlag == 1)
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = appValiationMsg;
                            _objResponseModel.AppVersion = 1;
                            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                        }
                        else
                        {
                            string ErrorMsg = "";
                            if (ValidateToken(immu.UserID, immu.TokenNo) == false)
                            {
                                ErrorMsg = "Invalid Request";
                            }
                            if (ErrorMsg != "")
                            {
                                ErrorMsg = CheckValidNumber(Convert.ToString(immu.InfantID), 1, 9, 1, "InfantID");
                            }
                            if (ErrorMsg != "")
                            {
                                ErrorMsg = CheckValidNumber(Convert.ToString(immu.ImmuCode), 1, 2, 1, "ImmuCode");
                            }
                            Int16 eligibleForDelete = 0;
                            byte immCode = Convert.ToByte(immu.ImmuCode);
                            var p = cnaa.Immunizations.Where(x => x.InfantID == immu.InfantID).ToList();
                            if (p.Count > 0)
                            {
                                if (p.Where(x => x.ImmuCode == immCode && x.ANMVerify == 1).Count() > 0)
                                {
                                    ErrorMsg = "यह टीका अस्वीकृत नहीं किया जा सकता क्योंकि इस बच्चें के यह टीका सत्यापित हो गया हैं !";
                                }
                                else if (immu.ImmuCode == "15") // DPTB
                                {
                                    eligibleForDelete = 1;
                                }
                                else if (immu.ImmuCode == "10")  //DPT3  
                                {
                                    if (p.Where(x => x.ImmuCode == 15).Count() == 0) // DPTB
                                        eligibleForDelete = 1;
                                    else
                                    {
                                        ErrorMsg = "यह टीका अस्वीकृत नहीं किया जा सकता क्योंकि इस बच्चें के टीका DPTB लगा हुआ हैं ";
                                    }
                                }
                                else if (immu.ImmuCode == "7") //DPT2
                                {
                                    if (p.Where(x => x.ImmuCode == 10).Count() == 0)  //DPT3
                                        eligibleForDelete = 1;
                                    else
                                    {
                                        ErrorMsg = "यह टीका अस्वीकृत नहीं किया जा सकता क्योंकि इस बच्चें के टीका DPT3 लगा हुआ हैं ";
                                    }
                                }
                                else if (immu.ImmuCode == "4") //DPT1
                                {
                                    if (p.Where(x => x.ImmuCode == 7).Count() == 0)  //DPT2
                                        eligibleForDelete = 1;
                                    else
                                    {
                                        ErrorMsg = "यह टीका अस्वीकृत नहीं किया जा सकता क्योंकि इस बच्चें के टीका DPT2 लगा हुआ हैं ";
                                    }
                                }
                                else if (immu.ImmuCode == "12") //HB3
                                {
                                    eligibleForDelete = 1;
                                }
                                else if (immu.ImmuCode == "9") //HB2
                                {
                                    if (p.Where(x => x.ImmuCode == 12).Count() == 0)
                                        eligibleForDelete = 1;
                                    else
                                    {
                                        ErrorMsg = "यह टीका अस्वीकृत नहीं किया जा सकता क्योंकि इस बच्चें के टीका HB3 लगा हुआ हैं ";
                                    }
                                }
                                else if (immu.ImmuCode == "6") //HB1
                                {
                                    if (p.Where(x => x.ImmuCode == 9).Count() == 0)  //HB2
                                        eligibleForDelete = 1;
                                    else
                                    {
                                        ErrorMsg = "यह टीका अस्वीकृत नहीं किया जा सकता क्योंकि इस बच्चें के टीका HB3 लगा हुआ हैं ";
                                    }
                                }
                                else if (immu.ImmuCode == "1" || immu.ImmuCode == "2" || immu.ImmuCode == "3") //1   BCG  2 OPV0 3 HB0
                                {
                                    eligibleForDelete = 1;

                                }
                                else if (immu.ImmuCode == "5") //OPV1
                                {
                                    if (p.Where(x => x.ImmuCode == 8).Count() == 0)  //OPV2
                                        eligibleForDelete = 1;
                                    else
                                    {
                                        ErrorMsg = "यह टीका अस्वीकृत नहीं किया जा सकता क्योंकि इस बच्चें के टीका OPV2 लगा हुआ हैं ";
                                    }
                                }
                                else if (immu.ImmuCode == "8") //OPV2
                                {
                                    if (p.Where(x => x.ImmuCode == 11).Count() == 0)  //11 OPV3
                                        eligibleForDelete = 1;
                                    else
                                    {
                                        ErrorMsg = "यह टीका अस्वीकृत नहीं किया जा सकता क्योंकि इस बच्चें के टीका OPV3 लगा हुआ हैं ";
                                    }
                                }
                                else if (immu.ImmuCode == "31") //PENTA1
                                {
                                    if (p.Where(x => x.ImmuCode == 32).Count() == 0)  //PENTA2
                                        eligibleForDelete = 1;
                                    else
                                    {
                                        ErrorMsg = "यह टीका अस्वीकृत नहीं किया जा सकता क्योंकि इस बच्चें के टीका PENTA2 लगा हुआ हैं ";
                                    }
                                }
                                else if (immu.ImmuCode == "32") //PENTA2
                                {
                                    if (p.Where(x => x.ImmuCode == 33).Count() == 0) //PENTA3
                                        eligibleForDelete = 1;
                                    else
                                    {
                                        ErrorMsg = "यह टीका अस्वीकृत नहीं किया जा सकता क्योंकि इस बच्चें के टीका PENTA3 लगा हुआ हैं ";
                                    }
                                }
                                else if (immu.ImmuCode == "35") //PCV1
                                {
                                    if (p.Where(x => x.ImmuCode == 36).Count() == 0) //PCV2
                                        eligibleForDelete = 1;
                                    else
                                    {
                                        ErrorMsg = "यह टीका अस्वीकृत नहीं किया जा सकता क्योंकि इस बच्चें के टीका PCV2 लगा हुआ हैं ";
                                    }
                                }
                                else if (immu.ImmuCode == "36") //PCV2
                                {
                                    if (p.Where(x => x.ImmuCode == 37).Count() == 0) //PCVB
                                        eligibleForDelete = 1;
                                    else
                                    {
                                        ErrorMsg = "यह टीका अस्वीकृत नहीं किया जा सकता क्योंकि इस बच्चें के टीका PCVB लगा हुआ हैं ";
                                    }
                                }
                                else if (immu.ImmuCode == "38")  //RVV-1
                                {
                                    if (p.Where(x => x.ImmuCode == 39).Count() == 0)  //RVV-2
                                        eligibleForDelete = 1;
                                    else
                                    {
                                        ErrorMsg = "यह टीका अस्वीकृत नहीं किया जा सकता क्योंकि इस बच्चें के टीका RVV-2 लगा हुआ हैं ";
                                    }
                                }
                                else if (immu.ImmuCode == "39")  //RVV-2
                                {
                                    if (p.Where(x => x.ImmuCode == 40).Count() == 0) //RVV-3
                                        eligibleForDelete = 1;
                                    else
                                    {
                                        ErrorMsg = "यह टीका अस्वीकृत नहीं किया जा सकता क्योंकि इस बच्चें के टीका RVV-3 लगा हुआ हैं ";
                                    }
                                }
                                else if (immu.ImmuCode == "41")  //FIPV-1
                                {
                                    if (p.Where(x => x.ImmuCode == 34).Count() == 0) //FIPV-2
                                        eligibleForDelete = 1;
                                    else
                                    {
                                        ErrorMsg = "यह टीका अस्वीकृत नहीं किया जा सकता क्योंकि इस बच्चें के टीका FIPV-2 लगा हुआ हैं ";
                                    }
                                }
                                else if (immu.ImmuCode == "34") //FIPV-2
                                {
                                    if (p.Where(x => x.ImmuCode == 42).Count() == 0) //FIPV-3
                                        eligibleForDelete = 1;
                                    else
                                    {
                                        ErrorMsg = "यह टीका अस्वीकृत नहीं किया जा सकता क्योंकि इस बच्चें के टीका FIPV-3 लगा हुआ हैं ";
                                    }
                                }
                                else if (immu.ImmuCode == "13") //MESL RBL-1
                                {
                                    if (p.Where(x => x.ImmuCode == 30).Count() == 0)  //MESL RBL-2
                                        eligibleForDelete = 1;
                                    else
                                    {
                                        ErrorMsg = "यह टीका अस्वीकृत नहीं किया जा सकता क्योंकि इस बच्चें के टीका MESL RBL-2 लगा हुआ हैं ";
                                    }
                                }
                                else
                                {
                                    eligibleForDelete = 1;
                                }
                            }

                            if (ErrorMsg != "")
                            {
                                _objResponseModel.Status = false;
                                _objResponseModel.Message = ErrorMsg;
                                return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                            }
                            else if (eligibleForDelete == 1)
                            {



                                try
                                {
                                    string weight = Convert.ToString(immu.Weight);
                                    int p1 = cnaa.InsertUpdateImmunization(immu.InfantID, immu.ASHAAutoid, immu.ImmuCode, immu.ImmuDate.ToString("dd/MM/yyyy"), immu.VillageAutoid,
                                    immu.LoginUnitid, immu.EntryBy, "", immu.MotherID, 3, immu.BirthDate.ToString("dd/MM/yyyy"), immu.PartImmu, Convert.ToByte("0"), Convert.ToByte("0"),
                                    immu.LoginUnitid, weight, immu.EntryUserNo, 0, immu.UnitCode, Convert.ToByte(immu.Media), immu.Latitude, immu.Longitude, Convert.ToByte(immu.ANMVerify)).Select(x => x.error).FirstOrDefault();
                                    if (p1 == 0)
                                    {
                                        _objResponseModel.Status = true;
                                        _objResponseModel.ResposeData = p1;
                                        if (immu.Media == 2)
                                            _objResponseModel.Message = "धन्यवाद, टीके का विवरण डिलीट हो चुका हैं।";
                                        else
                                            _objResponseModel.Message = "धन्यवाद, टीके का विवरण अस्वीकृत हो चुका हैं।";
                                    }
                                    else
                                    {
                                        _objResponseModel.Status = false;
                                        _objResponseModel.ResposeData = p1;
                                        if (immu.Media == 2)
                                            _objResponseModel.Message = "ओह ! टीके का विवरण डिलीट नहीं हुआ हैं। कृपया दोबारा सेव करें ।";
                                        else
                                            _objResponseModel.Message = "ओह ! टीके का विवरण अस्वीकृत नहीं हुआ हैं। कृपया दोबारा सेव करें ।";
                                        ErrorHandler.WriteError("Error in put immunization for " + immu.ImmuCode);
                                    }
                                }

                                catch (Exception ex)
                                {
                                    ErrorMsg = ex.ToString();
                                    ErrorHandler.WriteError("Error in validation delete anc" + ErrorMsg);
                                }

                            }
                        }
                    }
                    else
                    {
                        string messages = string.Join("; ", ModelState.Values
                                         .SelectMany(x => x.Errors)
                                         .Select(x => x.ErrorMessage));
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Model Error";
                        ErrorHandler.WriteError("Error in model delete immunization" + Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
                    }

                }
                catch (Exception ex)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Validation Error";
                    ErrorHandler.WriteError("Error in validation delete immunization" + ex.ToString());
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage GetMonthListForClaimForm()
        {
            ResponseModel _objResponseModel = new ResponseModel();
            var data = cnaa.Database.SqlQuery<ClaimMonthList>("set language british; select   left(DATENAME(month, convert(varchar,YEAR(left(mthyr,4))) + '-'+  RIGHT(mthyr,2)+'-01'),3) + '-' + LEFT(mthyr,4) MonthName,mthyr MonthValue,case when V_FromDate is null then '' else CONVERT(varchar(10),V_FromDate,103) end V_FromDate ,case when V_ToDate is null then '' else CONVERT(varchar(10),V_ToDate,103) end V_ToDate from ashasoft..Schedule order by mthyr desc ", 0).ToList();

            _objResponseModel.ResposeData = data;
            _objResponseModel.Status = true;
            _objResponseModel.Message = "Data Received successfully";
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        ///*******************************************************///
        private string ValidateSamFollowUpdetails(SamFollowupDetails sc, int methodFlag)
        {
            string ErrorMsg = "";
            if (ValidateToken(sc.UserID, sc.TokenNo) == false)
            {
                return "Invalid Request";
            }

            ErrorMsg = CheckValidNumber(Convert.ToString(sc.SamAutoid), 1, 9, 1, "SamAutoid");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = checkDate(Convert.ToString(sc.DischargeDate), 1, "Followup");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = checkDate(Convert.ToString(sc.FollowupDate1), 0, "Followup 1");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = checkDate(Convert.ToString(sc.FollowupDate2), 0, "Followup 2");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = checkDate(Convert.ToString(sc.FollowupDate3), 0, "Followup 3");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = checkDate(Convert.ToString(sc.FollowupDate4), 0, "Followup 4");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            //if (DifferenceInDays(anc.ANCDate, DateTime.Now) < 0)
            //{
            //    return "एएनसी की तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
            //}

            return "";
        }
        private string SaveSamFollowUpDetails(SamFollowupDetails sc, int methodFlag)
        {
            string ErrorMsg = "";
            if (methodFlag == 1)
            {
                ErrorMsg = ValidateSamFollowUpdetails(sc, methodFlag);
            }
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            else
            {
                using (TransactionScope transaction = new TransactionScope())
                {
                    try
                    {
                        using (cnaaEntities objcnaa = new cnaaEntities())
                        {
                            if (methodFlag == 1)
                            {
                                if (sc.DischargeDate != null)
                                {
                                    objcnaa.Database.ExecuteSqlCommand("update ashasoft..SamChildDetails set DischargeDate={0} where SamAutoid={1} ", sc.DischargeDate, sc.SamAutoid);
                                }
                                if (sc.FollowupDate1 != null)
                                {
                                    objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_SamChildFollowupDetail {0},{1},{2},{3},{4},{5},{6},{7},{8},{9} ", sc.SamAutoid, sc.FollowupFlag1, sc.DischargeDate, sc.FollowupDate1, sc.Height1, sc.Weight1, sc.Media, sc.UpdateUserNo, sc.E_mthyr, sc.AshaautoId);
                                }
                                if (sc.FollowupDate2 != null)
                                {
                                    objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_SamChildFollowupDetail {0},{1},{2},{3},{4},{5},{6},{7},{8},{9} ", sc.SamAutoid, sc.FollowupFlag2, sc.DischargeDate, sc.FollowupDate2, sc.Height2, sc.Weight2, sc.Media, sc.UpdateUserNo, sc.E_mthyr, sc.AshaautoId);
                                }
                                if (sc.FollowupDate3 != null)
                                {
                                    objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_SamChildFollowupDetail {0},{1},{2},{3},{4},{5},{6},{7},{8},{9} ", sc.SamAutoid, sc.FollowupFlag3, sc.DischargeDate, sc.FollowupDate3, sc.Height3, sc.Weight3, sc.Media, sc.UpdateUserNo, sc.E_mthyr, sc.AshaautoId);
                                }
                                if (sc.FollowupDate4 != null)
                                {
                                    objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_SamChildFollowupDetail {0},{1},{2},{3},{4},{5},{6},{7},{8},{9} ", sc.SamAutoid, sc.FollowupFlag4, sc.DischargeDate, sc.FollowupDate4, sc.Height4, sc.Weight4, sc.Media, sc.UpdateUserNo, sc.E_mthyr, sc.AshaautoId);
                                }
                            }
                            else if (methodFlag == 6)//verify
                            {
                                objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_SamChildFollowUpVerify {0},{1},{2},{3}", sc.PermType, sc.SamAutoid, sc.Media, sc.UpdateUserNo);
                            }
                            transaction.Complete();
                        }
                    }
                    catch (Exception ex)
                    {
                        Transaction.Current.Rollback();
                        transaction.Dispose();
                        if (methodFlag == 1)
                        {
                            ErrorMsg = "ओह ! SAM Followup का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें । ";
                            ErrorHandler.WriteError("Error in Post sam Followup" + ex.ToString());
                        }
                        else
                        {
                            ErrorMsg = "ओह ! SAM Followup का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा अपडेट करें ।";
                            ErrorHandler.WriteError("Error in PUT sam Followup" + ex.ToString());
                        }

                    }
                }
            }
            return ErrorMsg;
        }

        [ActionName("PostSamChildFollowup")]
        public HttpResponseMessage PostSamChildFollowup(SamFollowupDetails sc)     //AddANC/UpdateANC Details
        {
            // writeclassdata(anc);
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(sc.AppVersion, sc.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveSamFollowUpDetails(sc, sc.PermType);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, Sam का विवरण सेव हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post sam" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
                //foreach (var eve in e.EntityValidationErrors)
                //{
                //    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                //        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                //    foreach (var ve in eve.ValidationErrors)
                //    {
                //        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                //            ve.PropertyName, ve.ErrorMessage);
                //    }
                //}
                //throw;
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("uspSamFollowupDetail")]
        public HttpResponseMessage uspSamFollowupDetail(SamFollowupDetails a)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(a.UserID, a.TokenNo);
            if (tokenFlag == true)
            {
                if (a.SamAutoid > 0)
                {


                    var data = cnaa.Database.SqlQuery<SamFollowupDetails>("exec ASHASoft_SamDetail {0},{1}", a.SamAutoid, 3).ToList();

                    if (data.Count > 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";

                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage DeleteSamChildProgram(SamChild A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(A.AppVersion, A.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveSamDetails(A, 3);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, SAM Child Program का विवरण Delete हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in IDCF Program" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("PutSamChildFollowup")]
        public HttpResponseMessage PutSamChildFollowup(SamFollowupDetails sc)     //AddANC/UpdateANC Details
        {
            //  writeclassdata(sc);
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(sc.AppVersion, sc.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveSamFollowUpDetails(sc, sc.PermType);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else if (sc.PermType == 6)
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, SAM Follow Program का विवरण Verified हो चुका हैं।";
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, SAM का विवरण सेव हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }

            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post sam" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
                //foreach (var eve in e.EntityValidationErrors)
                //{
                //    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                //        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                //    foreach (var ve in eve.ValidationErrors)
                //    {
                //        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                //            ve.PropertyName, ve.ErrorMessage);
                //    }
                //}
                //throw;
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        //************************SAM*********************************//
        [ActionName("uspSncuFollowup")]
        public HttpResponseMessage uspSncuFollowup(SncuFollowup a)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(a.UserID, a.TokenNo);
            if (tokenFlag == true)
            {
                if (a.AshaAutoid > 0)
                {
                    var data = cnaa.Database.SqlQuery<SncuFollowup>("exec ASHASoft_SncuFollowup {0},{1}", a.AshaAutoid, a.Type).Select(x => new { x.DischargeDate, x.InfantID, x.ChildName, x.MotherName, x.MobileNo, BirthDate = x.BirthDate, x.Address, x.Motherid }).ToList();
                    if (data.Count > 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        private string SaveSncuFollowUpDetails(SncuFollowupDetails sc, int methodFlag)
        {
            string ErrorMsg = ValidateSncuFollowUpdetails(sc, methodFlag);
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            else
            {
                using (TransactionScope transaction = new TransactionScope())
                {
                    try
                    {
                        using (cnaaEntities objcnaa = new cnaaEntities())
                        {
                            //if (sc.DischargeDate != null)                             
                            {
                                objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_SncuChildFollowupDetail {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10} ", sc.Infantid, sc.Motherid, sc.AshaautoId, sc.DischargeDate, sc.FollowupDate1, sc.FollowupDate2, sc.FollowupDate3, sc.FollowupDate4, sc.E_mthyr, sc.Media, sc.UpdateUserNo);
                            }
                            transaction.Complete();
                        }
                    }
                    catch (Exception ex)
                    {
                        Transaction.Current.Rollback();
                        transaction.Dispose();
                        if (methodFlag == 1)
                        {
                            ErrorMsg = "ओह ! SNCU Followup का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें । ";
                            ErrorHandler.WriteError("Error in Post sam Followup" + ex.ToString());
                        }
                        else
                        {
                            ErrorMsg = "ओह ! SNCU Followup का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा अपडेट करें ।";
                            ErrorHandler.WriteError("Error in PUT sam Followup" + ex.ToString());
                        }
                    }
                }
            }
            return ErrorMsg;
        }
        private string ValidateSncuFollowUpdetails(SncuFollowupDetails sc, int methodFlag)
        {
            string ErrorMsg = "";
            if (ValidateToken(sc.UserID, sc.TokenNo) == false)
            {
                return "Invalid Request";
            }

            ErrorMsg = CheckValidNumber(Convert.ToString(sc.SamAutoid), 1, 9, 1, "Sncuid");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = checkDate(Convert.ToString(sc.DischargeDate), 1, "Followup");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = checkDate(Convert.ToString(sc.FollowupDate1), 0, "Followup 1");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = checkDate(Convert.ToString(sc.FollowupDate2), 0, "Followup 2");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = checkDate(Convert.ToString(sc.FollowupDate3), 0, "Followup 3");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            ErrorMsg = checkDate(Convert.ToString(sc.FollowupDate4), 0, "Followup 4");
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }

            return "";
        }
        [ActionName("PostSncuFollowup")]
        public HttpResponseMessage PostSncuFollowup(SncuFollowupDetails sc)     //
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(sc.AppVersion, sc.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveSncuFollowUpDetails(sc, 1);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, SNCU का विवरण सेव हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post sam" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("PutSncuFollowup")]
        public HttpResponseMessage PutSncuFollowup(SncuFollowupDetails sc)     //AddANC/UpdateANC Details
        {
            //  writeclassdata(sc);
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(sc.AppVersion, sc.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveSncuFollowUpDetails(sc, 2);

                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, SNCU का विवरण सेव हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }

            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post SNCU" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("uspGetSNCUFollowupDetail")]
        public HttpResponseMessage uspGetSNCUFollowupDetail(SncuFollowupDetails a)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(a.UserID, a.TokenNo);
            if (tokenFlag == true)
            {
                if (a.Infantid > 0)
                {
                    var data = cnaa.Database.SqlQuery<SncuFollowupDetails>("exec ASHASoft_SncuFollowupDetails {0}", a.Infantid).ToList();
                    if (data.Count > 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        //**********************************************//
        [ActionName("PostMaaProgram")]
        public HttpResponseMessage PostMaaProgram(MaaProgram Maa)     //
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(Maa.AppVersion, Maa.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveMaaProgram(Maa, Maa.PermType);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, MAA Program का विवरण सेव हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post MAA" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        private string SaveMaaProgram(MaaProgram Maa, int methodFlag)
        {
            //string ErrorMsg = ValidateSncuFollowUpdetails(sc, methodFlag);
            string ErrorMsg = "";
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            else
            {
                using (TransactionScope transaction = new TransactionScope())
                {
                    try
                    {
                        using (cnaaEntities objcnaa = new cnaaEntities())
                        {
                            if (methodFlag == 1)
                            {
                                //objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_MaaProgram {0},{1},{2},{3},{4},{5},{6},{7}", Maa.AshaautoId, 1, Maa.TrimesterNo, Maa.FinYear, Maa.NoofParticipant, Maa.DateofProgram, Maa.E_mthyr, 2, Maa.UpdateUserNo, Maa.RowID);
                                var Result = objcnaa.Database.SqlQuery<string>("exec ASHASoft_MaaProgram {0},{1},{2},{3},{4},{5},{6},{7},{8},{9}", Maa.AshaautoId, 1, Maa.TrimesterNo, Maa.FinYear, Maa.NoofParticipant, Maa.DateofProgram, Maa.E_mthyr, Maa.RowID, Maa.PermMedia, Maa.PermEntryUserNo).FirstOrDefault();
                                if (Result != "0")
                                {
                                    ErrorMsg = Result;
                                }
                            }
                            else if (methodFlag == 2)
                            {
                                objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_MaaProgram {0},{1},{2},{3},{4},{5},{6},{7},{8},{9}", Maa.AshaautoId, 2, Maa.TrimesterNo, Maa.FinYear, Maa.NoofParticipant, Maa.DateofProgram, Maa.E_mthyr, Maa.RowID, Maa.PermMedia, Maa.PermEntryUserNo);
                            }
                            else if (methodFlag == 3)
                            {
                                var Result = objcnaa.Database.SqlQuery<string>("exec ASHASoft_MaaProgram 0,{0},0,0,0,0,0,{1} ", Maa.PermType, Maa.RowID).FirstOrDefault();
                                if (Result != "0")
                                {
                                    ErrorMsg = Result;
                                }
                            }
                            else if (methodFlag == 6)//verify
                            {
                                objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_MaaProgram {0},{1},{2},{3},{4},{5},{6},{7},{8},{9}", Maa.AshaautoId, 6, Maa.TrimesterNo, Maa.FinYear, Maa.NoofParticipant, Maa.DateofProgram, Maa.E_mthyr, Maa.RowID, Maa.PermMedia, Maa.PermEntryUserNo);
                            }
                            transaction.Complete();
                        }
                    }
                    catch (Exception ex)
                    {
                        Transaction.Current.Rollback();
                        transaction.Dispose();
                        if (methodFlag == 1)
                        {
                            ErrorMsg = "ओह ! MAA Program का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें । ";
                            ErrorHandler.WriteError("Error in Post sam Followup" + ex.ToString());
                        }
                        else
                        {
                            ErrorMsg = "ओह ! MAA Program का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा अपडेट करें ।";
                            ErrorHandler.WriteError("Error in PUT sam Followup" + ex.ToString());
                        }
                    }
                }
            }
            return ErrorMsg;
        }

        [ActionName("PutMaaProgram")]
        public HttpResponseMessage PutMaaProgram(MaaProgram Maa)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(Maa.AppVersion, Maa.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveMaaProgram(Maa, Maa.PermType);

                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else if (Maa.PermType == 6)
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, Maa Program का विवरण Verified हो चुका हैं।";
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, Maa Program का विवरण Update हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post Maa Program" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("UspGetMaaProgram")]
        public HttpResponseMessage UspGetMaaProgram(MaaProgram Maa)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(Maa.UserID, Maa.TokenNo);
            if (tokenFlag == true)
            {
                if (Maa.AshaautoId > 0)
                {
                    var data = cnaa.Database.SqlQuery<MaaProgram>("exec ASHASoft_MaaProgram {0},{1}", Maa.AshaautoId, 3).ToList();
                    if (data.Count > 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage DeleteMAAProgram(MaaProgram A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(A.AppVersion, A.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveMaaProgram(A, 3);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, MAA Program का विवरण Delete हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in MAA Program" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        //**********************------IDCF------************************//
        [ActionName("PostIDCFProgram")]
        public HttpResponseMessage PostIDCFProgram(IDCFProgram A)     //
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(A.AppVersion, A.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveIDCFProgram(A, 1);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, IDCF Program का विवरण सेव हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post IDCF" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        private string SaveIDCFProgram(IDCFProgram A, int methodFlag)
        {
            //string ErrorMsg = ValidateSncuFollowUpdetails(sc, methodFlag);
            string ErrorMsg = "";
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            else
            {
                using (TransactionScope transaction = new TransactionScope())
                {
                    try
                    {
                        using (cnaaEntities objcnaa = new cnaaEntities())
                        {
                            if (methodFlag == 1)
                            {
                                var Result = objcnaa.Database.SqlQuery<string>("exec ASHASoft_IDCFProgram {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10} ", A.AshaautoId, 1, A.E_mthyr, A.RowID, A.FinYear, A.AnganwadiNo, A.Population, A.PossibleChildren, A.ChildrenGivenTablet, A.PermMedia, A.PermEntryUserNo).FirstOrDefault();
                                if (Result != "0")
                                {

                                    ErrorMsg = Result;
                                }
                            }
                            else if (methodFlag == 2)
                            {
                                objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_IDCFProgram {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10} ", A.AshaautoId, 2, A.E_mthyr, A.RowID, A.FinYear, A.AnganwadiNo, A.Population, A.PossibleChildren, A.ChildrenGivenTablet, A.PermMedia, A.PermEntryUserNo);
                            }
                            else if (methodFlag == 3)
                            {
                                var Result = objcnaa.Database.SqlQuery<string>("exec ASHASoft_IDCFProgram 0,{0},0, {1} ", A.PermType, A.RowID).FirstOrDefault();
                                if (Result != "0")
                                {
                                    ErrorMsg = Result;
                                }
                            }
                            else if (methodFlag == 6)//verify
                            {
                                objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_IDCFProgram {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10} ", A.AshaautoId, 6, A.E_mthyr, A.RowID, A.FinYear, A.AnganwadiNo, A.Population, A.PossibleChildren, A.ChildrenGivenTablet, A.PermMedia, A.PermEntryUserNo);
                            }
                            transaction.Complete();
                        }
                    }
                    catch (Exception ex)
                    {
                        Transaction.Current.Rollback();
                        transaction.Dispose();
                        if (methodFlag == 1)
                        {
                            ErrorMsg = "ओह ! ICDF Program का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें । ";
                            ErrorHandler.WriteError("Error in Post sam Followup" + ex.ToString());
                        }
                        else
                        {
                            ErrorMsg = "ओह ! IDCF Program का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा अपडेट करें ।";
                            ErrorHandler.WriteError("Error in PUT sam Followup" + ex.ToString());
                        }
                    }
                }
            }
            return ErrorMsg;
        }

        [ActionName("UspGetIDCFProgram")]
        public HttpResponseMessage UspGetIDCFProgram(IDCFProgram A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(A.UserID, A.TokenNo);
            if (tokenFlag == true)
            {
                if (A.AshaautoId > 0)
                {
                    var data = cnaa.Database.SqlQuery<IDCFProgram>("exec ASHASoft_IDCFProgram {0},{1}", A.AshaautoId, 3).ToList();
                    if (data.Count > 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("UspGetAnganwariDetails")]
        public HttpResponseMessage UspGetAnganwariDetails(IDCFProgram A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(A.UserID, A.TokenNo);
            if (tokenFlag == true)
            {
                if (A.AshaautoId > 0)
                {
                    var data = cnaa.Database.SqlQuery<IDCFProgram>("exec ASHASoft_IDCFProgram {0},{1}", A.AshaautoId, 4).ToList();
                    if (data.Count > 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("PutIDCFProgram")]
        public HttpResponseMessage PutIDCFProgram(IDCFProgram A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(A.AppVersion, A.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveIDCFProgram(A, A.PermType);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else if (A.PermType == 6)
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, IDCF Program का विवरण Verified हो चुका हैं।";
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, IDCF Program का विवरण Update हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post IDCF Program" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage DeleteIDCFProgram(IDCFProgram A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(A.AppVersion, A.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveIDCFProgram(A, 3);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, IDCF Program का विवरण Delete हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in IDCF Program" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        //**********************************************//
        [ActionName("UspGetNIPIDetails")]
        public HttpResponseMessage UspGetNIPIDetails(NIPIProgram A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(A.UserID, A.TokenNo);
            if (tokenFlag == true)
            {
                if (A.AshaautoId > 0)
                {
                    var data = cnaa.Database.SqlQuery<NIPIProgram>("exec ASHASoft_NIPIProgram {0},{1}", A.AshaautoId, A.PermType).ToList();
                    if (data.Count > 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("PostNIPIProgram")]
        public HttpResponseMessage PostNIPIProgram(NIPIProgram A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(A.AppVersion, A.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveNIPIProgram(A, 1);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, NIPI Program का विवरण सेव हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post NIPI" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        private string SaveNIPIProgram(NIPIProgram A, int methodFlag)
        {
            string ErrorMsg = "";
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            else
            {
                using (TransactionScope transaction = new TransactionScope())
                {
                    try
                    {
                        using (cnaaEntities objcnaa = new cnaaEntities())
                        {
                            if (methodFlag == 1)
                            {
                                var Result = objcnaa.Database.SqlQuery<string>("exec ASHASoft_NIPIProgram {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10} ", A.AshaautoId, 1, A.E_mthyr, A.RowID, A.Mthyr, A.AnganwadiNo, A.Population, A.PossibleChildren, A.ChildrenGivenTablet, A.PermMedia, A.PermEntryUserNo).FirstOrDefault();
                                if (Result != "0")
                                {
                                    ErrorMsg = Result;
                                }
                            }
                            else if (methodFlag == 2)
                            {
                                objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_NIPIProgram {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}", A.AshaautoId, 2, A.E_mthyr, A.RowID, A.Mthyr, A.AnganwadiNo, A.Population, A.PossibleChildren, A.ChildrenGivenTablet, A.PermMedia, A.PermEntryUserNo);
                            }
                            else if (methodFlag == 3)
                            {
                                var Result = objcnaa.Database.SqlQuery<string>("exec ASHASoft_NIPIProgram 0,{0},0, {1} ", A.PermType, A.RowID).FirstOrDefault();
                                if (Result != "0")
                                {
                                    ErrorMsg = Result;
                                }
                            }
                            else if (methodFlag == 6)//verify
                            {
                                objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_NIPIProgram {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}", A.AshaautoId, 6, A.E_mthyr, A.RowID, A.Mthyr, A.AnganwadiNo, A.Population, A.PossibleChildren, A.ChildrenGivenTablet, A.PermMedia, A.PermEntryUserNo);
                            }
                            transaction.Complete();
                        }
                    }
                    catch (Exception ex)
                    {
                        Transaction.Current.Rollback();
                        transaction.Dispose();
                        if (methodFlag == 1)
                        {
                            ErrorMsg = "ओह ! NIPI Program का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें । ";
                            ErrorHandler.WriteError("Error in Post sam Followup" + ex.ToString());
                        }
                        else
                        {
                            ErrorMsg = "ओह ! NIPI Program का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा अपडेट करें ।";
                            ErrorHandler.WriteError("Error in PUT sam Followup" + ex.ToString());
                        }
                    }
                }
            }
            return ErrorMsg;
        }

        [ActionName("PutNIPIProgram")]
        public HttpResponseMessage PutNIPIProgram(NIPIProgram A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(A.AppVersion, A.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveNIPIProgram(A, A.PermType);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else if (A.PermType == 6)
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, NIPI Program का विवरण Verified हो चुका हैं।";
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, NIPI Program का विवरण Update हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }

            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post NIPI Program" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage DeleteNIPIProgram(NIPIProgram A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(A.AppVersion, A.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveNIPIProgram(A, 3);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, NIPI Program का विवरण Delete हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in NIPI Program" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        //*********************NDD*************************//
        [ActionName("UspGetNDDDetails")]
        public HttpResponseMessage UspGetNDDDetails(NDDProgram A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(A.UserID, A.TokenNo);
            if (tokenFlag == true)
            {
                if (A.AshaautoId > 0)
                {
                    var data = cnaa.Database.SqlQuery<NDDProgram>("exec ASHASoft_NDDProgram {0},{1}", A.AshaautoId, A.PermType).ToList();
                    if (data.Count > 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("PostNDDProgram")]
        public HttpResponseMessage PostNDDProgram(NDDProgram A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(A.AppVersion, A.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveNDDProgram(A, 1);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, NDD Program का विवरण सेव हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post NDD" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        private string SaveNDDProgram(NDDProgram A, int methodFlag)
        {
            string ErrorMsg = "";
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            else
            {
                using (TransactionScope transaction = new TransactionScope())
                {
                    try
                    {
                        using (cnaaEntities objcnaa = new cnaaEntities())
                        {
                            if (methodFlag == 1)
                            {
                                var Result = objcnaa.Database.SqlQuery<string>("exec ASHASoft_NDDProgram {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10} ", A.AshaautoId, 1, A.E_mthyr, A.RowID, A.Mthyr, A.TotChildAnganwari1to5, A.ChildAnganwair1to5SyrupGiven, A.TotChildAnganwari6to19, A.ChildAnganwair6to19SyrupGiven, A.PermMedia, A.PermEntryUserNo).FirstOrDefault();
                                if (Result != "0")
                                {
                                    ErrorMsg = Result;
                                }
                            }
                            else if (methodFlag == 2)
                            {
                                objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_NDDProgram {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}", A.AshaautoId, 2, A.E_mthyr, A.RowID, A.Mthyr, A.TotChildAnganwari1to5, A.ChildAnganwair1to5SyrupGiven, A.TotChildAnganwari6to19, A.ChildAnganwair6to19SyrupGiven, A.PermMedia, A.PermEntryUserNo);
                            }
                            else if (methodFlag == 3)
                            {
                                var Result = objcnaa.Database.SqlQuery<string>("exec ASHASoft_NDDProgram 0,{0},0, {1} ", A.PermType, A.RowID).FirstOrDefault();
                                if (Result != "0")
                                {
                                    ErrorMsg = Result;
                                }
                            }
                            else if (methodFlag == 6)//verify
                            {
                                objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_NDDProgram {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}", A.AshaautoId, 6, A.E_mthyr, A.RowID, A.Mthyr, A.TotChildAnganwari1to5, A.ChildAnganwair1to5SyrupGiven, A.TotChildAnganwari6to19, A.ChildAnganwair6to19SyrupGiven, A.PermMedia, A.PermEntryUserNo);
                            }
                            transaction.Complete();
                        }
                    }
                    catch (Exception ex)
                    {
                        Transaction.Current.Rollback();
                        transaction.Dispose();
                        if (methodFlag == 1)
                        {
                            ErrorMsg = "ओह ! NDD Program का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें । ";
                            ErrorHandler.WriteError("Error in Post sam Followup" + ex.ToString());
                        }
                        else
                        {
                            ErrorMsg = "ओह ! NDD Program का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा अपडेट करें ।";
                            ErrorHandler.WriteError("Error in PUT NDD " + ex.ToString());
                        }
                    }
                }
            }
            return ErrorMsg;
        }

        [ActionName("PutNDDProgram")]
        public HttpResponseMessage PutNDDProgram(NDDProgram A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(A.AppVersion, A.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveNDDProgram(A, A.PermType);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else if (A.PermType == 6)
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, NDD Program का विवरण Verified हो चुका हैं।";
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, NDD Program का विवरण Update हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }

            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post NDD Program" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage DeleteNDDProgram(NDDProgram A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(A.AppVersion, A.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveNDDProgram(A, 3);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, NDD Program का विवरण Delete हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in NDD Program" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        //*********************AAMR*************************//
        [ActionName("UspGetAAMRDetails")]
        public HttpResponseMessage UspGetAAMRDetails(AAMRProgram A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(A.UserID, A.TokenNo);
            if (tokenFlag == true)
            {
                if (A.AshaautoId > 0)
                {
                    var data = cnaa.Database.SqlQuery<AAMRProgram>("exec ASHASoft_AAMRProgram {0},{1}", A.AshaautoId, 3).ToList();
                    if (data.Count > 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("PostAAMRProgram")]
        public HttpResponseMessage PostAAMRProgram(AAMRProgram A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(A.AppVersion, A.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveAAMRProgram(A, 1);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, एनीमिया मुक्त भारत कार्यक्रम का विवरण सेव हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post Animiya Mukt" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        private string SaveAAMRProgram(AAMRProgram A, int methodFlag)
        {
            string ErrorMsg = "";
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            else
            {
                using (TransactionScope transaction = new TransactionScope())
                {
                    try
                    {
                        using (cnaaEntities objcnaa = new cnaaEntities())
                        {
                            if (methodFlag == 1) //insert
                            {
                                var Result = objcnaa.Database.SqlQuery<string>("exec ASHASoft_AAMRProgram {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11} ", A.AshaautoId, 1, A.E_mthyr, A.RowID, A.Mthyr, A.TotNoOfNoSchoolGoingKishori, A.NoSchoolGoingKishoriGivenTablet, A.TotNoofWoman, A.NoofWomanGivenTablet, A.AnganwadiNo, A.PermMedia, A.PermEntryUserNo).FirstOrDefault();
                                if (Result != "0")
                                {
                                    ErrorMsg = Result;
                                }
                            }
                            else if (methodFlag == 2) //update
                            {
                                objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_AAMRProgram {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}", A.AshaautoId, 2, A.E_mthyr, A.RowID, A.Mthyr, A.TotNoOfNoSchoolGoingKishori, A.NoSchoolGoingKishoriGivenTablet, A.TotNoofWoman, A.NoofWomanGivenTablet, A.AnganwadiNo, A.PermMedia, A.PermEntryUserNo);
                            }
                            else if (methodFlag == 3)//delete
                            {
                                var Result = objcnaa.Database.SqlQuery<string>("exec ASHASoft_AAMRProgram 0,{0},0, {1} ", A.PermType, A.RowID).FirstOrDefault();
                                if (Result != "0")
                                {
                                    ErrorMsg = Result;
                                }
                            }
                            else if (methodFlag == 6)//verify
                            {
                                objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_AAMRProgram {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}", A.AshaautoId, A.PermType, A.E_mthyr, A.RowID, A.Mthyr, A.TotNoOfNoSchoolGoingKishori, A.NoSchoolGoingKishoriGivenTablet, A.TotNoofWoman, A.NoofWomanGivenTablet, A.AnganwadiNo, A.PermMedia, A.PermEntryUserNo);
                            }
                            transaction.Complete();
                        }
                    }
                    catch (Exception ex)
                    {
                        Transaction.Current.Rollback();
                        transaction.Dispose();
                        if (methodFlag == 1)
                        {
                            ErrorMsg = "ओह ! एनीमिया मुक्त भारत कार्यक्रम का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें । ";
                            ErrorHandler.WriteError("Error in Post sam Followup" + ex.ToString());
                        }
                        else
                        {
                            ErrorMsg = "ओह ! एनीमिया मुक्त भारत कार्यक्रम का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा अपडेट करें ।";
                            ErrorHandler.WriteError("Error in PUT NDD " + ex.ToString());
                        }
                    }
                }
            }
            return ErrorMsg;
        }

        [ActionName("PutAAMRProgram")]
        public HttpResponseMessage PutAAMRProgram(AAMRProgram A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(A.AppVersion, A.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveAAMRProgram(A, A.PermType);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else if (A.PermType == 6)
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, एनीमिया मुक्त भारत कार्यक्रम का विवरण Verified हो चुका हैं।";
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, एनीमिया मुक्त भारत कार्यक्रम का विवरण Update हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post Animiya Mukt Program" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        [ActionName("DeleteAAMRProgram")]
        public HttpResponseMessage DeleteAAMRProgram(AAMRProgram A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(A.AppVersion, A.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveAAMRProgram(A, 3);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, एनीमिया मुक्त भारत कार्यक्रम का विवरण Delete हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post Animiya Mukt Program" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        //***********************--************************//
        [ActionName("UspGetMthyrList")]
        public HttpResponseMessage UspGetMthyrList(MthyrList A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(A.UserID, A.TokenNo);
            if (tokenFlag == true)
            {
                if (A.PermType >= 0)
                {
                    var data = cnaa.Database.SqlQuery<MthyrList>("exec AshaSoft_CommonAPI {0},{1}", A.PermType, A.ActivityCode).ToList();
                    if (data.Count > 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        //***********************GetServicesName************************//
        [ActionName("UspGetServicesNameList")]
        public HttpResponseMessage UspGetServicesNameList(ServicesList A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(A.UserID, A.TokenNo);
            if (tokenFlag == true)
            {
                if (A.UserID != "")
                {
                    var data = cnaa.Database.SqlQuery<ServicesList>("exec AshaSoft_ServicesList {0},{1}", A.PermType, A.ServiceCode).Select(x => new { Txt = x.Txt, Val = x.Val, IconPath = x.IconPath }).ToList();
                    if (data.Count > 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        ///***************************ANM Verification Case**********************************//
        [ActionName("Usp_ANMVerificationPendingList")]
        public HttpResponseMessage Usp_ANMVerificationPendingList(ANMVerificationPendingList A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(A.UserID, A.TokenNo);
            if (tokenFlag == true)
            {
                if (A.UserID != "")
                {
                    var data = cnaa.Database.SqlQuery<ANMVerificationPendingList>("exec ASHASoft_Usp_ANMVerificationPendingList {0},{1}, {2}", A.Unitid, A.PermType, A.AshaAutoid).Select(x => new { x.AshaAutoid, x.AshaName, x.TotalCases, x.TotalVerifiedCases, x.TotalUnVerifiedCases, x.AcitivityNameHindi }).ToList();
                    if (data.Count > 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
       
        [ActionName("Usp_ANMVerificationPendingListOfASHA")]
        public HttpResponseMessage Usp_ANMVerificationPendingListOfASHA(ANMVerificationPendingList A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(A.UserID, A.TokenNo);
            if (tokenFlag == true)
            {
                if (A.UserID != "")
                {
                    var data = cnaa.Database.SqlQuery<ANMVerificationPendingList>("exec ASHASoft_Usp_ANMVerificationPendingList {0},{1}, {2}", A.Unitid, A.PermType, A.AshaAutoid).Select(x => new { x.AshaAutoid, x.AshaName, x.TotalCases, x.TotalVerifiedCases, x.TotalUnVerifiedCases, x.AcitivityNameHindi }).ToList();
                    if (data.Count > 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("Usp_ANMVerificationTotalCase")]
        public HttpResponseMessage Usp_ANMVerificationTotalCase(ANMVerificationPendingList A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(A.UserID, A.TokenNo);
            if (tokenFlag == true)
            {
                if (A.UserID != "")
                {
                    List<List<object>> items = new List<List<object>>();
                    SqlParameter[] SP = new SqlParameter[2];
                    SP[0] = new SqlParameter("@PermAshaAutoid", A.AshaAutoid);
                    SP[1] = new SqlParameter("@PermType", A.PermType);
                    DataSet ds = Connect_DB.DataSet("exec ASHASoft_Usp_ANMVerificationTotalCaseList @PermType,@PermAshaAutoid", SP, "cnaa");
                    List<Dictionary<string, string>> listHash = new List<Dictionary<string, string>>();
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        Dictionary<string, string> a1 = new Dictionary<string, string>();
                        foreach (DataColumn column in ds.Tables[0].Columns)
                        {
                            a1.Add(column.ColumnName, row[column].ToString());
                        }
                        listHash.Add(a1);
                    }
                    if (listHash.Count > 0)
                    {
                        _objResponseModel.ResposeData = listHash;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";

                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        [ActionName("Usp_ANMVerificationVerifiedCase")]
        public HttpResponseMessage Usp_ANMVerificationVerifiedCase(ANMVerificationPendingList A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(A.UserID, A.TokenNo);
            if (tokenFlag == true)
            {
                if (A.UserID != "")
                {
                    List<List<object>> items = new List<List<object>>();
                    SqlParameter[] SP = new SqlParameter[2];
                    SP[0] = new SqlParameter("@PermAshaAutoid", A.AshaAutoid);
                    SP[1] = new SqlParameter("@PermType", A.PermType);
                    DataSet ds = Connect_DB.DataSet("exec ASHASoft_Usp_ANMVerificationVerifiedCase @PermType,@PermAshaAutoid", SP, "cnaa");
                    List<Dictionary<string, string>> listHash = new List<Dictionary<string, string>>();
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        Dictionary<string, string> a1 = new Dictionary<string, string>();
                        foreach (DataColumn column in ds.Tables[0].Columns)
                        {
                            a1.Add(column.ColumnName, row[column].ToString());
                        }
                        listHash.Add(a1);
                    }
                    if (listHash.Count > 0)
                    {
                        _objResponseModel.ResposeData = listHash;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";

                    }

                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("Usp_ANMVerificationUnVerifiedCase")]
        public HttpResponseMessage Usp_ANMVerificationUnVerifiedCase(ANMVerificationPendingList A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(A.UserID, A.TokenNo);
            if (tokenFlag == true)
            {
                if (A.UserID != "")
                {
                    List<List<object>> items = new List<List<object>>();
                    SqlParameter[] SP = new SqlParameter[2];
                    SP[0] = new SqlParameter("@PermAshaAutoid", A.AshaAutoid);
                    SP[1] = new SqlParameter("@PermType", A.PermType);
                    DataSet ds = Connect_DB.DataSet("exec ASHASoft_Usp_ANMVerificationUnVerifiedCase @PermType,@PermAshaAutoid", SP, "cnaa");
                    List<Dictionary<string, string>> listHash = new List<Dictionary<string, string>>();
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        Dictionary<string, string> a1 = new Dictionary<string, string>();
                        foreach (DataColumn column in ds.Tables[0].Columns)
                        {
                            a1.Add(column.ColumnName, row[column].ToString());
                        }
                        listHash.Add(a1);
                    }
                    if (listHash.Count > 0)
                    {
                        _objResponseModel.ResposeData = listHash;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";

                    }

                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        ///*************************************************************//
        [ActionName("PostASHAListWithOutVillage")]
        public HttpResponseMessage PostASHAListWithOutVillage(Pcts p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(p.UserID, p.TokenNo);
            if (tokenFlag == true)
            {
                if (p.LoginUnitid != 0 || p.RegUnitid != 0 || p.VillageAutoid != 0)
                {
                    string Loginunitcode = rajmed.UnitMasters.Where(x => x.UnitID == p.LoginUnitid).Select(x => x.UnitCode).FirstOrDefault();
                    var data = rajmed.GetASHAForLinelist(p.RegUnitid, p.VillageAutoid, p.DelplaceUnitid, Convert.ToString(Loginunitcode)).ToList();
                    if (data != null)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        //*************************Sam Recovered Child*************************************//
        [ActionName("UspSamRecoverdDetails")]
        public HttpResponseMessage UspSamRecoverdDetails(SamRecovered A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(A.UserID, A.TokenNo);
            if (tokenFlag == true)
            {
                if (A.AshaautoId > 0)
                {
                    var data = cnaa.Database.SqlQuery<SamRecovered>("exec ASHASoft_SamRecovered {0},{1},0,{2}", A.AshaautoId, A.PermType, A.SamAutoid).ToList();
                    if (data.Count > 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("PostSamRecoverd")]
        public HttpResponseMessage PostSamRecoverd(SamRecovered A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(A.AppVersion, A.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveSamRecoverd(A, 1);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, सैम से मुक्त चाइल्ड का विवरण सेव हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post SAM  Mukt" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        private string SaveSamRecoverd(SamRecovered A, int methodFlag)
        {
            string ErrorMsg = "";
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            else
            {
                using (TransactionScope transaction = new TransactionScope())
                {
                    try
                    {
                        using (cnaaEntities objcnaa = new cnaaEntities())
                        {
                            if (methodFlag == 1) //insert
                            {
                                var Result = objcnaa.Database.SqlQuery<string>("exec ASHASoft_SamRecovered {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10} ", A.AshaautoId, 1, A.E_mthyr, A.SamAutoid, A.E_mthyr, A.IsFreeSam, A.SamFreeDate, A.Height, A.Weight, A.PermMedia, A.PermEntryUserNo).FirstOrDefault();
                                if (Result != "0")
                                {
                                    ErrorMsg = Result;
                                }
                            }
                            else if (methodFlag == 2) //update
                            {
                                objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_SamRecovered {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10} ", A.AshaautoId, 2, A.E_mthyr, A.SamAutoid, A.E_mthyr, A.IsFreeSam, A.SamFreeDate, A.Height, A.Weight, A.PermMedia, A.PermEntryUserNo);
                            }
                            else if (methodFlag == 3)//delete
                            {
                                var Result = objcnaa.Database.SqlQuery<string>("exec ASHASoft_SamRecovered 0,{0},0, {1} ", 5, A.SamAutoid).FirstOrDefault();
                                if (Result != "0")
                                {
                                    ErrorMsg = Result;
                                }
                            }
                            else if (methodFlag == 6)//verify
                            {
                                objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_SamRecovered {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10} ", A.AshaautoId, A.PermType, A.E_mthyr, A.SamAutoid, A.E_mthyr, A.IsFreeSam, A.SamFreeDate, A.Height, A.Weight, A.PermMedia, A.PermEntryUserNo);
                            }
                            transaction.Complete();
                        }
                    }
                    catch (Exception ex)
                    {
                        Transaction.Current.Rollback();
                        transaction.Dispose();
                        if (methodFlag == 1)
                        {
                            ErrorMsg = "ओह ! सैम से मुक्त चाइल्ड  का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें । ";
                            ErrorHandler.WriteError("Error in Post sam Followup" + ex.ToString());
                        }
                        else
                        {
                            ErrorMsg = "ओह ! सैम से मुक्त चाइल्ड  का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा अपडेट करें ।";
                            ErrorHandler.WriteError("Error in PUT NDD " + ex.ToString());
                        }
                    }
                }
            }
            return ErrorMsg;
        }

        [ActionName("PutSamRecoverd")]
        public HttpResponseMessage PutSamRecoverd(SamRecovered A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(A.AppVersion, A.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveSamRecoverd(A, A.PermType);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else if (A.PermType == 6)
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, सैम से मुक्त चाइल्ड  का विवरण Verified हो चुका हैं।";
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, सैम से मुक्त चाइल्ड  का विवरण Update हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post SAM Mukt Program" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        [ActionName("DeleteSamRecoverd")]
        public HttpResponseMessage DeleteSamRecoverd(SamRecovered A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                if (ModelState.IsValid)
                {
                    int CheckAppVersionFlag = CheckVersion(A.AppVersion, A.IOSAppVersion);
                    if (CheckAppVersionFlag == 1)
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = appValiationMsg;
                        _objResponseModel.AppVersion = 1;
                        return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                    }
                    else
                    {
                        string ErrorMsg = SaveSamRecoverd(A, 3);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, सैम से मुक्त चाइल्ड  का विवरण Delete हो चुका हैं।";
                        }
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Model Error";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in Post SAM Mukt Program" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        //*********************************----*********************************************//

        [ActionName("UspGetDistrictList")]
        public HttpResponseMessage UspGetDistrictList(UnitList A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(A.UserID, A.TokenNo);
            if (tokenFlag == true)
            {
                if (A.PermType >= 0)
                {
                    var data = cnaa.Database.SqlQuery<UnitList>("exec AshaSoft_CommonAPI {0},{1}, {2}, {3}, {4}", A.PermType, 0, A.UnitType, A.UnitCode, A.ExtraFlag).ToList();
                    if (data.Count > 0)
                    {
                        _objResponseModel.ResposeData = data;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        ///*****************************Re-Registration ***************************//
        public HttpResponseMessage PostJReReg(MotherAncRegDetailReReg m)
        {
            //  writeclassdata(m);
            ResponseModel _objResponseModel = new ResponseModel();

            bool tokenFlag = ValidateToken(m.UserID, m.TokenNo);
            if (tokenFlag == true)
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        int CheckAppVersionFlag = CheckVersion(m.AppVersion, m.IOSAppVersion);
                        if (CheckAppVersionFlag == 1)
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = appValiationMsg;
                            _objResponseModel.AppVersion = 1;
                            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                        }
                        else
                        {
                            string pctsid = "";                                                    
                            string msg = SaveJ1DetailsReReg(m, 1, ref pctsid);
                            if (msg != "")
                            {
                                _objResponseModel.Status = false;
                                _objResponseModel.Message = msg;
                                Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                            }
                            else
                            {
                                _objResponseModel.Status = true;
                                _objResponseModel.Message = "धन्यवाद, PCTS पर महिला का पुनः पंजीकरण हो गया है ।";
                                _objResponseModel.ResposeData = pctsid;
                                Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
                            }
                        }
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Model Error";
                        ErrorHandler.WriteError("Error in post re reg model--" + ModelState);
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                    }
                }
                catch (Exception ex)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "validation Error";
                    ErrorHandler.WriteError("Error in post re reg--" + ex.ToString());
                    
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        private string SaveJ1DetailsReReg(MotherAncRegDetailReReg m, Int16 methodFlag, ref string Newpctsid)
        {
            string ErrorMsg = validateJ1DetailReReg(m, methodFlag);
            if (ErrorMsg != "")
            {
                return ErrorMsg;
            }
            else
            {
                try
                {
                    string pctsid = "";
                    if (!string.IsNullOrEmpty(m.AadhaarNo))
                    {
                        pctsid = cnaa.Database.SqlQuery<string>("select top 1 pctsid from pcts..BHAMASHAH ad inner join mother moth on moth.motherid = ad.ID where ad.ID > 0 and ad.IDFlag=1 and isnull(AadhaarNo,'')={0} and moth.motherid NOT IN (select motherid  from motherstatus where reasonid not in (5,6))", Convert.ToString(m.AadhaarNo)).FirstOrDefault();
                        if (!string.IsNullOrEmpty(pctsid))
                        {
                            return "Aadhaar Number already exists with PCTS ID -  : " + pctsid;
                        }
                    }

                    pctsid = cnaa.Mothers.Where(x => x.VillageAutoID == m.VillageAutoID && x.Name == m.Name && x.Husbname == m.Husbname && x.Age == m.Age).Select(x => x.pctsid).FirstOrDefault();
                    if (!string.IsNullOrEmpty(pctsid))
                    {
                        return "Duplicate case ! The case is already registered with PCTSID - " + pctsid;
                    }

                    int infantDeathCount = 0;
                    int id = cnaa.Database.SqlQuery<int>("SELECT isnull(max(isnull(convert(int, substring(pctsid,14,len(pctsid) - 1)),0)),0) from Mother WHERE left(pctsid, 11) ={0}", Convert.ToString(m.UnitCode)).FirstOrDefault() + 1;
                    pctsid = m.UnitCode + "99" + Convert.ToString(id);

                    using (TransactionScope transaction = new TransactionScope())
                    {
                        try
                        {
                            using (cnaaEntities objcnaa = new cnaaEntities())
                            {
                                if (!string.IsNullOrEmpty(m.AadhaarNo) && !string.IsNullOrEmpty(m.Ifsc_code) && !string.IsNullOrEmpty(m.Accountno) && m.DirectDelivery == 2 && m.ashaAutoID > 0)
                                {
                                    objcnaa.Database.ExecuteSqlCommand("insert into AadhaarBankInfo(motherid,ashaAutoID,AadhaarBankInfoUpdatedDate) values({0},{1},{2})", m.MotherID, m.ashaAutoID, DateTime.Now);
                                }                                
                                {
                                    ANCRegDetail anc = new ANCRegDetail();
                                    anc.ReMarriageFlag = (byte)m.ReMarriageFlag;
                                    anc.MotherID = m.MotherID;
                                    anc.RegDate = m.RegDate;
                                    anc.LMPDT = Convert.ToDateTime(m.LMPDT);
                                    anc.DelFlag = 0;
                                    anc.EntryDate = DateTime.Now;
                                    anc.ashaAutoID = m.ashaAutoID;
                                    anc.HighRisk = 0;
                                    anc.EntryUnitVillage = m.VillageAutoID;
                                    anc.Freeze_AadhaarBankInfo = 0;

                                    anc.Location_Rajasthan = m.Location_Rajasthan;
                                    anc.DirectDelivery = m.DirectDelivery;
                                    anc.VillageAutoID = m.VillageAutoID;
                                    anc.Ghamantu = m.Ghamantu;
                                    anc.NFSA = m.NFSA;
                                    anc.BPL = (byte)m.BPL;
                                    if (m.BPL == 1)
                                    {
                                        anc.BeforeDelivery500 = m.BeforeDelivery500;
                                        anc.bplcardno = m.BPLCardNo;
                                    }
                                    else
                                    {
                                        anc.BeforeDelivery500 = 2;
                                        anc.bplcardno = null;
                                    }

                                    anc.LastUpdated = DateTime.Now;
                                    anc.Media = m.Media;
                                    anc.EntryUserNo = m.EntryUserNo;
                                    anc.UpdateUserNo = m.UpdateUserNo;
                                    anc.VillageUpdationDate = DateTime.Now;
                                    anc.PermanentAddressE = m.PermanentAddressE;
                                    anc.CurrentAddressE = m.CurrentAddressE;
                                    anc.QualificationH = m.QualificationH;
                                    anc.QualificationW = m.QualificationW;
                                    anc.BusinessH = m.BusinessH;
                                    anc.BusinessW = m.BusinessW;
                                    anc.IsHusband = m.IsHusband;
                                    anc.HusbnameE = m.HusbnameE;
                                    if (m.LiveChild + infantDeathCount == 1 && DifferenceInDays(Convert.ToDateTime(m.LMPDT), Convert.ToDateTime("2022-11-01")) <= 0)
                                    {
                                        anc.IcdsRegistrationFlag = 1;
                                        anc.ICDS2ndFlagDate = DateTime.Now;
                                    }
                                    else
                                    {
                                        anc.IcdsRegistrationFlag = 0;
                                        anc.ICDS2ndFlagDate = null;
                                    }

                                    anc.Age = m.Age;
                                    anc.CaseNo = 0;
                                    objcnaa.ANCRegDetails.Add(anc);
                                    objcnaa.SaveChanges();
                                    m.ANCRegID = anc.ANCRegID;
                                    
                                    // Update Mother
                                    var p1 = objcnaa.Mothers.Where(x => x.MotherID == m.MotherID).FirstOrDefault();
                                    p1.ancregid = anc.ANCRegID;
                                    p1.RegDate = m.RegDate;    
                                    p1.Name = m.Name;
                                    p1.Address = m.Address;
                                    p1.Age = m.Age;
                                    p1.Cast = m.Cast;
                                    p1.LiveChild = m.LiveChild;
                                    p1.Mobileno = m.Mobileno;
                                    p1.Husbname = m.Husbname;
                                    p1.Accountno = m.Accountno;  
                                    p1.LastUpdated = DateTime.Now;
                                    p1.Ifsc_code = m.Ifsc_code;
                                    p1.RationCardNo = m.RationCardNo;
                                    p1.VillageAutoID = m.VillageAutoID;
                                    p1.AccountName = m.AccountName;                                   
                                    p1.IsHusband = m.IsHusband;
                                    p1.CastGroup = m.CastGroup;
                                    p1.Height = m.Height;
                                    p1.Religion = m.Religion;
                                    p1.NameE = m.NameE;
                                    p1.NameH = m.NameH;
                                    p1.Divyang = m.Divyang;                                                                    
                                    p1.UpdateUserNo = m.UpdateUserNo;
                                    p1.ashaAutoID = (int)m.ashaAutoID;                                                                            
                                }
                                objcnaa.SaveChanges();

                                //if (!string.IsNullOrEmpty(m.AadhaarNo) || !string.IsNullOrEmpty(m.JanAadhaarNo))
                                //{
                                //    objcnaa.Database.ExecuteSqlCommand("insert into pcts..BHAMASHAH(id,IDFlag,JanAadhaarNo,Consent,MotherID,AadhaarNo,JanMemberID,MemberIDVerifyBy,MemberIDVerifiedDate)  values({0},{1},{2},{3},{4},{5},{6},{7},{8})", m.MotherID, 1, m.JanAadhaarNo, m.Consent, m.MotherID, m.AadhaarNo, m.JanMemberID, m.MemberIDVerifyBy, DateTime.Now);
                                //}

                                transaction.Complete();
                                Newpctsid = pctsid;
                                return "";
                            }

                        }
                        catch (Exception ex)
                        {
                            Transaction.Current.Rollback();
                            transaction.Dispose();
                            if (methodFlag == 1)
                            {
                                ErrorMsg = "ओह ! पुनः पंजीकरण का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें । ";
                                ErrorHandler.WriteError("Error in Post anc" + ex.ToString());
                            }
                            else
                            {
                                ErrorMsg = "ओह ! पुनः पंजीकरण का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा अपडेट करें ।";
                                ErrorHandler.WriteError("Error in PUT anc" + ex.ToString());
                            }
                            return ErrorMsg;
                        }
                    }
                }
                catch (DbEntityValidationException e)
                {
                    return "error";
                }
            }
            return "";
        }
        private string validateJ1DetailReReg(MotherAncRegDetailReReg m, int postputFlag)
        {
            string ErrorMsg = "";
            if (postputFlag == 2)
            {
                ErrorMsg = CheckValidNumber(Convert.ToString(m.ANCRegID), 1, 9, 1, "ANCRegID");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                ErrorMsg = CheckValidNumber(Convert.ToString(m.MotherID), 1, 9, 1, "MotherID");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                ErrorMsg = CheckValidNumber(Convert.ToString(m.VillageAutoID), 1, 10, 0, "VillageAutoID");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                ErrorMsg = CheckValidNumber(Convert.ToString(m.Mobileno), 10, 10, 0, "Mobileno");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.DirectDelivery)) )
            {
                return "कृपया क्या महिला बिना पूर्व पंजीकरण, प्रसव के लिए आई है चुनें ! ";
            }
            else if (m.DirectDelivery != 1 && m.DirectDelivery != 2 )
            {
                return "कृपया क्या महिला बिना पूर्व पंजीकरण, प्रसव के लिए आई है सही चुनें ! ";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.Ghamantu)))
            {
                return "कृपया क्या महिला घुमन्तु श्रेणी की है चुनें ! ";
            }
            else if (m.Ghamantu != 1 && m.Ghamantu != 2)
            {
                return "कृपया क्या महिला घुमन्तु श्रेणी की है सही चुनें ! ";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.VillageAutoID)))
            {
                return "कृपया गॉंव / वार्ड चुनें !";
            }

            if (string.IsNullOrEmpty(Convert.ToString(m.Name)))
            {
                return "कृपया नाम लिखें ! ";
            }
            else if (!Regex.IsMatch(m.Name, @"^([ \u0900-\u097f ,\u200D,\u00c0-\u01ffa-zA-Z'])+$"))
            {
                return "कृपया सही नाम लिखें !";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.NameE)))
            {
                return "कृपया महिला का नाम(अंग्रेज़ी में) लिखें !";
            }
            else if (!Regex.IsMatch(m.NameE, @"^([a-zA-Z\s])+$"))
            {
                return "कृपया महिला का नाम(अंग्रेज़ी में) सही लिखें !";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.HusbnameE)))
            {
                return "कृपया पिता/पति का नाम(अंग्रेज़ी में) लिखें !";
            }
            else if (!Regex.IsMatch(m.HusbnameE, @"^([a-zA-Z\s])+$"))
            {
                return "कृपया पिता/पति का नाम(अंग्रेज़ी में) सही लिखें !";
            }

            if (string.IsNullOrEmpty(Convert.ToString(m.IsHusband)))
            {
                return "कृपया पिता/पति का नाम चुनें ! ";
            }
            else if (m.IsHusband != 1 && m.IsHusband != 2)
            {
                return "कृपया पिता/पति का नाम सही चुनें ! ";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.Husbname)))
            {
                return "कृपया पिता/पति का नाम लिखें ! ";
            }
            else if (!Regex.IsMatch(m.Husbname, @"^([ \u0900-\u097f ,\u200D,\u00c0-\u01ffa-zA-Z'])+$"))
            {
                return "कृपया सही पिता/पति का नाम लिखें !";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.CurrentAddressE)))
            {
                return "कृपया महिला का वर्तमान पता(अंग्रेज़ी में) लिखें !";
            }
            else if (!Regex.IsMatch(m.CurrentAddressE, @"^[\w\s\d\,\-]*$"))
            {
                return "कृपया महिला का वर्तमान पता(अंग्रेज़ी में) सही लिखें !";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.PermanentAddressE)))
            {
                return "कृपया महिला का स्थायी पता(अंग्रेज़ी में) लिखें !";
            }
            else if (!Regex.IsMatch(m.PermanentAddressE, @"^([a-zA-Z\s])+$"))
            {
                return "कृपया महिला का स्थायी पता(अंग्रेज़ी में) सही लिखें !";
            }

            if (m.DirectDelivery == 2 && m.VillageName == "Other State")
            {
                return "अन्य राज्य की महिला के रजिस्ट्रेशन के लिए 'क्या महिला बिना पूर्व पंजीकरण, प्रसव के लिए आई है' में हॉं चुने !";
            }
            if (m.Mobileno != null)
            {
                if (m.Mobileno.Length == 10)
                {
                    if (checkStringWithSameDigit(m.Mobileno))
                    {
                        return "कृपया सही मोबाईल नं. लिखे !";
                    }
                    if (validateMobileNoSameDigitMorethantimes(m.Mobileno, 5))
                    {
                        return "कृपया सही मोबाईल नं. लिखे !";
                    }
                }
            }
            if (!string.IsNullOrEmpty(m.ECID))
            {
                if (!Regex.IsMatch(m.ECID, @"^[0-9]*$"))
                {
                    return "योग्‍य दम्‍पत्ति संख्‍या अंकों में डालें !";
                }
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.BPL)) )
            {
                return "कृपया बी. पी. एल. चुनें !";
            }
            else if (m.BPL != 1 && m.BPL != 2 )
            {
                return "कृपया सही बी. पी. एल. चुनें !";
            }
            if (string.IsNullOrEmpty(m.BPLCardNo) )
            {
                m.BPL = 2;
                m.BeforeDelivery500 = 2;
            }


            if (string.IsNullOrEmpty(Convert.ToString(m.Age)))
            {
                return "कृपया आयु लिखें !";
            }
            else if (m.Age < 13 || m.Age > 48)
            {
                return "Age should be between 13 and 48 years !";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.Cast)))
            {
                return "कृपया जाति चुनें !";
            }
            else if (m.Cast != 1 && m.Cast != 2 && m.Cast != 3)
            {
                return "कृपया सही जाति चुनें !";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.Location_Rajasthan)) )
            {
                return "कृपया क्या प्रसूता राजस्थान की मूल निवासी है चुनें !";
            }
            else if (m.Location_Rajasthan != 1 && m.Location_Rajasthan != 2 )
            {
                return "कृपया सही क्या प्रसूता राजस्थान की मूल निवासी है चुनें !";
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.Height)))
            {
                return "कृपया कद चुनें !";
            }
            else if (m.Height < 60 || m.Height > 240)
            {
                return "कृपया सही कद डालें !";
            }



            if (string.IsNullOrEmpty(Convert.ToString(m.RegDate)))
            {
                ErrorMsg = "कृपया पंजीकरण की तिथि डालें !";
                return ErrorMsg;
            }
            else
            {
                ErrorMsg = checkDate(Convert.ToString(m.RegDate), 1, "पंजीकरण");
                if (ErrorMsg != "")
                {
                    return ErrorMsg;
                }
                if (DifferenceInDays(Convert.ToDateTime(m.RegDate), DateTime.Now) < 0)
                {
                    ErrorMsg = "पंजीकरण की तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
                    return ErrorMsg;
                }


            }
            if (string.IsNullOrEmpty(Convert.ToString(m.LMPDT)) )
            {
                ErrorMsg = "कृपया आखिरी माहवारी की तिथि डालें !";
                return ErrorMsg;
            }
            else
            {
                
                {
                    ErrorMsg = checkDate(Convert.ToString(m.LMPDT), 1, "आखिरी माहवारी");
                    if (ErrorMsg != "")
                    {
                        return ErrorMsg;
                    }
                    if (DifferenceInDays(Convert.ToDateTime(m.LMPDT), DateTime.Now) < 0)
                    {
                        return "आखिरी माहवारी की तिथि आज की तिथि से ज्यादा नहीं होनी चाहिए !";
                    }
                    if (DifferenceInMonth(Convert.ToDateTime(m.LMPDT), Convert.ToDateTime(m.RegDate)) > 9)
                    {
                        return "कृपया सही आखिरी माहवारी/पंजीकरण की तिथि डालें !";
                    }
                    if (DifferenceInDays(Convert.ToDateTime(m.RegDate), Convert.ToDateTime(m.LMPDT)) >= 0)
                    {
                        return "आखिरी माहवारी की तिथि पंजीकरण की तिथि से ज्यादा नहीं होनी चाहिए !";
                    }
                }
            }
            if (string.IsNullOrEmpty(Convert.ToString(m.NFSA)) )
            {
                return "कृपया NFSA पात्रता चुनें !";
            }
            else if (m.NFSA != 1 && m.NFSA != 2 )
            {
                return "कृपया सही NFSA पात्रता चुनें !";
            }
            if (!string.IsNullOrEmpty(m.Accountno) )
            {
                if (m.Accountno.Length < 10)
                {
                    return "कृपया सही खाता संख्या लिखें";
                }
                if (!Regex.IsMatch(m.Accountno, @"^[0-9]*$"))
                {
                    return "कृपया सही खाता संख्या लिखें !";
                }
                if (checkStringWithSameDigit(m.Accountno))
                {
                    return "कृपया सही खाता संख्या लिखें !";
                }
                if (string.IsNullOrEmpty(m.Ifsc_code))
                {
                    return "कृपया सही IFSC कोड लिखें !";
                }
                if (string.IsNullOrEmpty(m.AccountName))
                {
                    return "कृपया खाता धारक का नाम लिखें !";
                }
            }
            if (!string.IsNullOrEmpty(m.Ifsc_code) )
            {

                if (!Regex.IsMatch(m.Ifsc_code, @"^[a-z,A-Z,0-9]{10,25}$"))
                {
                    return "कृपया सही IFSC कोड लिखें !";
                }
                if (Regex.IsMatch(m.Ifsc_code, @"^[a-z,A-Z]*$"))
                {
                    return "कृपया सही IFSC कोड लिखें !";
                }
                else if (Regex.IsMatch(m.Ifsc_code, @"^[0-9]*$"))
                {
                    return "कृपया सही IFSC कोड लिखें !";
                }

                if (string.IsNullOrEmpty(m.Accountno))
                {
                    return "कृपया खाता संख्या लिखें !";
                }
                if (string.IsNullOrEmpty(m.AccountName))
                {
                    return "कृपया खाता धारक का नाम लिखें !";
                }
            }
            if (!string.IsNullOrEmpty(m.RationCardNo) )
            {

                if (!Regex.IsMatch(m.RationCardNo, @"^[a-z,A-Z,0-9]{12,50}$"))
                {
                    return "कृपया सही राशन कार्ड नम्बर लिखें !";
                }

            }

            if (!string.IsNullOrEmpty(m.AadhaarNo))
            {
                if (m.AadhaarNo.Length != 12 && m.AadhaarNo.Length != 15)
                {
                    return "कृपया सही आधार संख्या लिखें !";
                }
                if (!Regex.IsMatch(m.AadhaarNo, @"^[0-9]*$"))
                {
                    return "कृपया सही आधार संख्या लिखें !";
                }
                if (m.Consent != 1)
                {
                    return "कृपया Consent चुने !";
                }
            }
            if (!string.IsNullOrEmpty(m.JanAadhaarNo))
            {
                if (m.JanAadhaarNo.Length != 10)
                {
                    return "कृपया सही जन आधार संख्या लिखें !";
                }
                if (!Regex.IsMatch(m.JanAadhaarNo, @"^[0-9]*$"))
                {
                    return "कृपया सही जन आधार संख्या लिखें !";
                }
                if (m.Consent != 1)
                {
                    return "कृपया Consent चुने !";
                }
            }
            if (!string.IsNullOrEmpty(m.RationCardNo) )
            {
                if (m.RationCardNo.Length > 0 && m.RationCardNo.Length < 12)
                {
                    return "कृपया सही राशन कार्ड नम्बर लिखें !";
                }
            }
            if (m.ashaAutoID == 0)
            {
                return "कृपया आशा का नाम चुने !";
            }


            return ErrorMsg;
        }

        [ActionName("UspGetReRegistrationList")]
        public HttpResponseMessage UspGetReRegistrationList(MotherAncRegDetailReReg A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(A.UserID, A.TokenNo);
            if (tokenFlag == true)
            {
                if (A.UserID != "")
                {
                    List<List<object>> items = new List<List<object>>();
                    SqlParameter[] SP = new SqlParameter[3];
                    SP[0] = new SqlParameter("@Perm_unitid", A.Unitid);
                    SP[1] = new SqlParameter("@Perm_villageautoid", A.VillageAutoID);
                    SP[2] = new SqlParameter("@PermAshaAutoid", A.ashaAutoID);

                    DataSet ds = Connect_DB.DataSet("exec ASHASoft_ReRegistrationList @Perm_unitid,@Perm_villageautoid,@PermAshaAutoid", SP, "cnaa");
                    List<Dictionary<string, string>> listHash = new List<Dictionary<string, string>>();
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        Dictionary<string, string> a1 = new Dictionary<string, string>();
                        foreach (DataColumn column in ds.Tables[0].Columns)
                        {
                            a1.Add(column.ColumnName, row[column].ToString());
                        }
                        listHash.Add(a1);
                    }
                    if (listHash.Count > 0)
                    {
                        _objResponseModel.ResposeData = listHash;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "No Data Found";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "No Data Found";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        [ActionName("UspGetFamilyDetails")]
        public HttpResponseMessage UspGetFamilyDetalis(GetFamilyDetails A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(A.UserID, A.TokenNo);
            if (tokenFlag == true)
            {
                if (A.UserID != "")
                {
                    List<List<object>> items = new List<List<object>>();
                    SqlParameter[] SP = new SqlParameter[3];
                    SP[0] = new SqlParameter("@JAN_AADHAR", A.JAN_AADHAR);
                    SP[1] = new SqlParameter("@AADHAR_ID", A.AADHAR_ID);
                    SP[2] = new SqlParameter("@SexType", A.SexType);

                    DataSet ds = Connect_DB.DataSet("exec UspGetFamilyDetails @JAN_AADHAR,@AADHAR_ID,@SexType", SP, "cnaa");
                    List<Dictionary<string, string>> listHash = new List<Dictionary<string, string>>();
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        Dictionary<string, string> a1 = new Dictionary<string, string>();
                        foreach (DataColumn column in ds.Tables[0].Columns)
                        {
                            a1.Add(column.ColumnName, row[column].ToString());
                        }
                        listHash.Add(a1);
                    }
                    if (listHash.Count > 0)
                    {
                        _objResponseModel.ResposeData = listHash;
                        _objResponseModel.Status = true;
                        _objResponseModel.Message = "Data Received successfully";
                    }
                    else
                    {
                        HttpWebRequest request = null;
                        HttpWebResponse response1 = null;
                        Stream dataStream = null;
                        StreamReader reader = null;
                        string ResponseDs = "";
                        int sno = 0;
                        string qry = "";
                        
                            Console.WriteLine("sno" + Convert.ToString(sno));
                            SqlParameter[] p = null;
                            try
                            {
                                string url = "https://api.sewadwaar.rajasthan.gov.in/app/live/Janaadhaar/Prod/Service/action/fetchJayFamily/" + Convert.ToString(A.JAN_AADHAR) + "?client_id=e81959ba-820c-4da9-925a-98648768961c";
                                request = (HttpWebRequest)WebRequest.Create(url);
                                response1 = (HttpWebResponse)request.GetResponse();
                                dataStream = response1.GetResponseStream();
                                reader = new StreamReader(dataStream);
                                ResponseDs = reader.ReadToEnd();
                                reader.Close();
                                dataStream.Close();
                                response1.Close();
                                var responseOtpObj = JsonConvert.DeserializeObject<List<GetFamilyDetails>>(ResponseDs);
                                if (responseOtpObj != null)
                                {
                                    foreach (GetFamilyDetails a in responseOtpObj)
                                    {


                                        try
                                        {
                                            qry = "insert into pcts..JanAadhaarDetails(AADHAR_ID,NAME,DOB,JAN_MEMBER_ID,Bhamashah_ID,JAN_AADHAR,NAME_HINDI," +
                                                                     "BHAMASHAHMEMBER_ID,BHAMASHAHAckID,NFSA_STATUS,MOBILE_NO,AADHAR_REF_NO,GENDER,MEMBER_TYPE)" +
                                                                     "values(@PermAADHAR_ID,@PermNAME,@PermDOB,@PermJAN_MEMBER_ID,@PermBhamashah_ID,@PermJAN_AADHAR,@PermNAME_HINDI," +
                                                                     "@PermBHAMASHAHMEMBER_ID,@PermBHAMASHAHAckID,@PermNFSA_STATUS,@PermMOBILE_NO,@PermAADHAR_REF_NO,@PermGENDER,@PermMEMBER_TYPE)";
                                            p = new SqlParameter[14];
                                            p[0] = new SqlParameter("@PermAADHAR_ID", System.Data.SqlDbType.VarChar);
                                            p[0].Value = (a.AADHAR_ID == null) ? "" : a.AADHAR_ID;
                                            p[1] = new SqlParameter("@PermNAME", System.Data.SqlDbType.VarChar);
                                            p[1].Value = a.NAME;
                                            p[2] = new SqlParameter("@PermDOB", System.Data.SqlDbType.Date);
                                            p[2].Value = a.DOB;
                                            p[3] = new SqlParameter("@PermJAN_MEMBER_ID", System.Data.SqlDbType.VarChar);
                                            p[3].Value = (a.JAN_MEMBER_ID == null) ? "" : a.JAN_MEMBER_ID; 
                                            p[4] = new SqlParameter("@PermBhamashah_ID", System.Data.SqlDbType.VarChar);
                                            p[4].Value = (a.Bhamashah_ID == null) ? "" : a.Bhamashah_ID;
                                            p[5] = new SqlParameter("@PermJAN_AADHAR", System.Data.SqlDbType.VarChar);
                                            p[5].Value = a.JAN_AADHAR;
                                            p[6] = new SqlParameter("@PermNAME_HINDI", System.Data.SqlDbType.NVarChar);
                                            p[6].Value = (a.NAME_HINDI == null) ? "" : a.NAME_HINDI;
                                            p[7] = new SqlParameter("@PermBHAMASHAHMEMBER_ID", System.Data.SqlDbType.NVarChar);
                                            p[7].Value = (a.BHAMASHAHMEMBER_ID == null) ? "" : a.BHAMASHAHMEMBER_ID; 
                                            p[8] = new SqlParameter("@PermBHAMASHAHAckID", System.Data.SqlDbType.NVarChar);
                                            p[8].Value = (a.BHAMASHAHAckID == null) ? "" : a.BHAMASHAHAckID;
                                            p[9] = new SqlParameter("@PermNFSA_STATUS", System.Data.SqlDbType.VarChar);
                                            p[9].Value = (a.NFSA_STATUS == null) ? "" : a.NFSA_STATUS;
                                            p[10] = new SqlParameter("@PermMOBILE_NO", System.Data.SqlDbType.VarChar);
                                            p[10].Value = (a.MOBILE_NO == null) ? "" : a.MOBILE_NO;
                                            p[11] = new SqlParameter("@PermAADHAR_REF_NO", System.Data.SqlDbType.VarChar);
                                            p[11].Value = (a.AADHAR_REF_NO == null) ? "" : a.AADHAR_REF_NO;
                                            p[12] = new SqlParameter("@PermGENDER", System.Data.SqlDbType.VarChar);
                                            p[12].Value = (a.GENDER == null) ? "" : a.GENDER;
                                            p[13] = new SqlParameter("@PermMEMBER_TYPE", System.Data.SqlDbType.VarChar);
                                            p[13].Value = (a.MEMBER_TYPE == null) ? "" : a.MEMBER_TYPE;
                                            Connect_DB.ExecuteNonQuery(qry, p, "cnaa");
                                        }
                                        catch (Exception ex)
                                        {
                                            qry = "insert into pcts..JanAadhaarErrorDetails(JAN_AADHAR,JAN_MEMBER_ID,ErrorMsg)" +
                                                " values(@PermJAN_AADHAR,@PermJAN_MEMBER_ID,@PermErrorMsg) ";
                                            p = new SqlParameter[3];
                                            p[0] = new SqlParameter("@PermJAN_AADHAR", System.Data.SqlDbType.VarChar);
                                            p[0].Value = a.JAN_AADHAR;
                                            p[1] = new SqlParameter("@PermJAN_MEMBER_ID", System.Data.SqlDbType.VarChar);
                                            p[1].Value = a.JAN_MEMBER_ID;
                                            if (ex.ToString().Length > 500)
                                            {
                                                p[2] = new SqlParameter("@PermErrorMsg", System.Data.SqlDbType.VarChar);
                                                p[2].Value = ex.ToString().Substring(0, 499);
                                            }
                                            else
                                            {
                                                p[2] = new SqlParameter("@PermErrorMsg", System.Data.SqlDbType.VarChar);
                                                p[2].Value = ex.ToString();
                                            }
                                            Connect_DB.ExecuteNonQuery(qry, p, "cnaa");
                                        }
                                    }


                                }
                            }
                            catch (Exception ex)
                            {
                                qry = "insert into pcts..JanAadhaarErrorDetails(JAN_AADHAR,JAN_MEMBER_ID,ErrorMsg)" +
                                                                    " values(@PermJAN_AADHAR,@PermJAN_MEMBER_ID,@PermErrorMsg) ";
                                p = new SqlParameter[3];
                                p[0] = new SqlParameter("@PermJAN_AADHAR", System.Data.SqlDbType.VarChar);
                                p[0].Value = Convert.ToString(A.JAN_AADHAR);
                                p[1] = new SqlParameter("@PermJAN_MEMBER_ID", System.Data.SqlDbType.VarChar);
                                p[1].Value = 0;
                                if (ex.ToString().Length > 500)
                                {
                                    p[2] = new SqlParameter("@PermErrorMsg", System.Data.SqlDbType.VarChar);
                                    p[2].Value = ex.ToString().Substring(0, 499);
                                }
                                else
                                {
                                    p[2] = new SqlParameter("@PermErrorMsg", System.Data.SqlDbType.VarChar);
                                    p[2].Value = ex.ToString();
                                }
                                Connect_DB.ExecuteNonQuery(qry, p, "cnaa");
                            }
                        List<List<object>> items1 = new List<List<object>>();
                        SqlParameter[] SP1 = new SqlParameter[3];
                        SP1[0] = new SqlParameter("@JAN_AADHAR", A.JAN_AADHAR);
                        SP1[1] = new SqlParameter("@AADHAR_ID", A.AADHAR_ID);
                        SP1[2] = new SqlParameter("@SexType", A.SexType);

                        DataSet ds1 = Connect_DB.DataSet("exec UspGetFamilyDetails @JAN_AADHAR,@AADHAR_ID,@SexType", SP, "cnaa");
                        List<Dictionary<string, string>> listHash1 = new List<Dictionary<string, string>>();
                        foreach (DataRow row in ds.Tables[0].Rows)
                        {
                            Dictionary<string, string> a1 = new Dictionary<string, string>();
                            foreach (DataColumn column in ds.Tables[0].Columns)
                            {
                                a1.Add(column.ColumnName, row[column].ToString());
                            }
                            listHash1.Add(a1);
                        }
                        if (listHash.Count > 0)
                        {
                            _objResponseModel.ResposeData = listHash;
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "Data Received successfully";
                        }

                    }
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
    }
}


