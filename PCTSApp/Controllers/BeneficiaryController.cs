using System;
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
using Newtonsoft.Json.Linq;
using System.Web;
using System.Configuration;
using System.IO;
using PCTSApp.SMSApi;
using System.Xml;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Web.Script.Serialization;

namespace PCTSApp.Controllers
{

    public class BeneficiaryController : ApiController
    {

        private rajmedicalEntities rajmed = new rajmedicalEntities();
        private MaacnaaEntities cnaa = new MaacnaaEntities();
        private cnaaEntities PctsAppcnaa = new cnaaEntities();
        private string appValiationMsg = "यह वर्जन पुराना हो चुका है, कृपया Google play store से नया वर्जन अपडेट करें ! ";

        public HttpResponseMessage PostMobileNo(BeneficiaryMobileNo_Maa u)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            Boolean status = true;
            int CheckAppVersionFlag = 0;
            if (CheckAppVersionFlag == 1)
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = appValiationMsg;
                _objResponseModel.AppVersion = CheckAppVersionFlag;
                return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
            }
            string message = "Data Received successfully";
            string Mno = Decrypt(u.MobileNo);
            string Did = Decrypt(u.DeviceID);
            var data = PctsAppcnaa.Mothers.Where(x => x.Mobileno == Mno).Count();
            if (data == 0)
            {
                status = false;
                message = "कृपया सही मोबाइल नम्बर डाले !";
            }
            else
            {
                if (cnaa.PinDetails.Where(x => x.MobileNo == Mno && x.DeviceID == Did).Count() > 0)
                {
                    _objResponseModel.ResposeData = "1";
                }
                else
                {
                    _objResponseModel.ResposeData = "0";
                }
            }
            _objResponseModel.Status = status;
            _objResponseModel.Message = message;
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public string Encrypt(string data)
        {
            RijndaelManaged rijndaelCipher = new RijndaelManaged();
            rijndaelCipher.Mode = CipherMode.CBC; //remember this parameter
            rijndaelCipher.Padding = PaddingMode.PKCS7; //remember this parameter
            rijndaelCipher.KeySize = 0x80;
            rijndaelCipher.BlockSize = 0x80;
            string FilePath = Convert.ToString(ConfigurationManager.AppSettings["EncrDecr"]);
            byte[] pwdBytes = Encoding.ASCII.GetBytes(FilePath);
            //string FilePath = HttpContext.Current.Server.MapPath("~/Maa.key");
            //byte[] pwdBytes = GetFileBytes(FilePath);
            byte[] keyBytes = new byte[0x10];
            int len = pwdBytes.Length;
            if (len > keyBytes.Length)
            {
                len = keyBytes.Length;
            }
            Array.Copy(pwdBytes, keyBytes, len);
            rijndaelCipher.Key = keyBytes;
            rijndaelCipher.IV = keyBytes;
            ICryptoTransform transform = rijndaelCipher.CreateEncryptor();
            if (data != null)
            {
                byte[] plainText = Encoding.UTF8.GetBytes(data);
                return Convert.ToBase64String
                 (transform.TransformFinalBlock(plainText, 0, plainText.Length));
            }
            else
            {
                return data;
            }


        }

        public string Decrypt(string data)
        {
            RijndaelManaged rijndaelCipher = new RijndaelManaged();
            rijndaelCipher.Mode = CipherMode.CBC;
            rijndaelCipher.Padding = PaddingMode.PKCS7;

            rijndaelCipher.KeySize = 0x80;
            rijndaelCipher.BlockSize = 0x80;
            byte[] encryptedData = Convert.FromBase64String(data);
            string FilePath = Convert.ToString(ConfigurationManager.AppSettings["EncrDecr"]);
            byte[] pwdBytes = Encoding.ASCII.GetBytes(FilePath);
            //string FilePath = HttpContext.Current.Server.MapPath("~/Maa.key");
            //byte[] pwdBytes = GetFileBytes(FilePath);
            byte[] keyBytes = new byte[0x10];
            int len = pwdBytes.Length;

            if (len > keyBytes.Length)
            {
                len = keyBytes.Length;
            }
            Array.Copy(pwdBytes, keyBytes, len);
            rijndaelCipher.Key = keyBytes;
            rijndaelCipher.IV = keyBytes;
            byte[] plainText = rijndaelCipher.CreateDecryptor().TransformFinalBlock
            (encryptedData, 0, encryptedData.Length);
            return Encoding.UTF8.GetString(plainText);
        }

        public byte[] GetFileBytes(String filePath)
        {
            byte[] buffer;
            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            try
            {
                int length = (int)fileStream.Length;
                buffer = new byte[length];
                int count;
                int sum = 0;
                while ((count = fileStream.Read(buffer, sum, length - sum))
                > 0)
                    sum += count;
            }
            finally
            {
                fileStream.Close();
            }
            return buffer;
        }

        private bool validateMobileNo(string mobileNo)
        {
            if (mobileNo.Length != 10)
            {
                return false;
            }
            else if (!Regex.IsMatch(mobileNo, @"^[0-9]*$"))
            {
                return false;
            }

            return true;
        }





        private string Usertrail(string Mobileno, string DeviceID, Int16 Apicode, DateTime SDate)
        {
            try
            {
                var data = cnaa.UserTrailLogs.Where(x => x.MobileNo == Mobileno && x.ApiCode == Apicode).FirstOrDefault();
                if (data == null)
                {
                    UserTrailLog us = new UserTrailLog();
                    us.MobileNo = Mobileno;
                    us.DeviceID = DeviceID;
                    us.EntryDate = SDate;
                    us.ResponseTime = DateTime.Now;
                    us.ApiCode = Apicode;
                    cnaa.UserTrailLogs.Add(us);
                    cnaa.SaveChanges();
                }
                else
                {
                    data.ResponseTime = DateTime.Now;
                    cnaa.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in post user" + ex.ToString());
                return "error";
            }
            return "";
        }


        private bool CheckHit(Int16 ApiCode, string MobileNo)
        {
            var data = cnaa.UserTrailLogs.Where(x => x.MobileNo == MobileNo && x.ApiCode == ApiCode).OrderByDescending(x => x.ResponseTime).FirstOrDefault();
            if (data != null)
            {
                if (DifferenceInSec((DateTime)data.ResponseTime, DateTime.Now) <= 5)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }




        public HttpResponseMessage PostPin(BeneficiaryMobileNo_Maa u)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            Boolean status = true;
            string message = "Data Received successfully";
            string Mno = Decrypt(u.MobileNo);
            string Did = Decrypt(u.DeviceID);
            //string pn = Decrypt(u.Pin);
            DateTime SDate = DateTime.Now;
            if (validateMobileNo(Mno) == true)
            {
                var data = cnaa.PinDetails.Where(x => x.MobileNo == Mno).FirstOrDefault();
                if (DifferenceInSec(data.LastUpdatedDate, DateTime.Now) < 5)
                {
                    status = false;
                    message = "Invalid hit";
                }
                else
                {
                    if (data == null)
                    {
                        message = "0";
                        status = true;
                    }
                    else
                    {
                        if (Did.Length < 1 || Did.Length > 35)
                        {
                            status = false;
                            message = "Please try again !";
                        }
                        else
                        {
                            string HashPin = GenerateSHA512String(data.Pin + Mno).ToLower();
                            if (data.DeviceID != Did)
                            {
                                status = false;
                                message = "1";
                            }
                            else if (u.Pin != HashPin)
                            {
                                Int16 Trail = (Int16)cnaa.PinDetails.Where(x => x.MobileNo == Mno).Select(x => x.trail).DefaultIfEmpty().Max();
                                if (Trail >= 5)
                                {
                                    status = false;
                                    message = "यूजर आईडी ब्लॉक कर दिया गया है,कृपया पिन बदले!";
                                }
                                else
                                {
                                    //UserTrailLog us = new UserTrailLog();
                                    //us.MobileNo = Mno;
                                    //us.DeviceID = Did;
                                    //us.EntryDate = DateTime.Now;
                                    //cnaa.UserTrailLogs.Add(us);
                                    //cnaa.SaveChanges();
                                    Usertrail(Mno, Did, 1, SDate);
                                    data.trail = (byte)(Trail + 1);
                                    cnaa.SaveChanges();
                                    status = false;
                                    message = "कृपया सही पिन डाले !";
                                }

                            }
                            else
                            {


                                Usertrail(Mno, Did, 1, SDate);
                                data.trail = 0;
                                data.LastUpdatedDate = DateTime.Now;
                                cnaa.SaveChanges();
                                string salt = Guid.NewGuid().ToString();

                                var datas = cnaa.MaaTokens.Where(a => a.Mobileno == Mno && a.DeviceID == Did).FirstOrDefault();
                                if (datas == null)
                                {
                                    MaaToken mt = new MaaToken();
                                    mt.Mobileno = Mno;
                                    mt.DeviceID = Did;
                                    mt.Salt = salt;
                                    mt.Date = DateTime.Now;
                                    cnaa.MaaTokens.Add(mt);
                                    cnaa.SaveChanges();

                                }
                                else
                                {
                                    datas.Salt = salt;
                                    datas.Date = DateTime.Now;
                                    cnaa.SaveChanges();
                                }

                                var data1 = cnaa.uspGetMotherDetailsByMobileno(Mno, Did).Select(x => new { TokenNo = x.TokenNo, x.Name, x.Husbname, x.infantid, x.Mobileno, x.MotherID, x.pctsid, x.UnitNameHindi, x.UnitCode, x.VillageNameHindi, x.DistrictName, x.totinf }).FirstOrDefault();
                                if (data1 == null)
                                {
                                    status = false;
                                }
                                else
                                {
                                    status = true;
                                    _objResponseModel.ResposeData = data1;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                status = false;
                message = "कृपया सही मोबाईल नं. डालें";
            }
            _objResponseModel.Status = status;
            _objResponseModel.Message = message;
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage Logout(BeneficiaryMobileNo_Maa u)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            try
            {
                string Mno = Decrypt(u.MobileNo);
                string Did = Decrypt(u.DeviceID);

                //string Mno = u.MobileNo;
                //string Did = u.DeviceID;
                if (validateMobileNo(Mno) == true)
                {
                    var Mtok = cnaa.MaaTokens.Where(a => a.Mobileno == Mno && a.DeviceID == Did).First();
                    cnaa.MaaTokens.Remove(Mtok);
                    cnaa.SaveChanges();
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Logout successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "कृपया सही मोबाईल नं. डालें";
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in post user" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        }


        //private bool ValidateToken(string userid, string tokenNo)
        //{
        //    userid = userid + DateTime.Now.Year + DateTime.Now.Month.ToString("00") + DateTime.Now.Day.ToString("00");
        //    string tokenUserid = TokenManager.ValidateToken(tokenNo);
        //    if (userid.Equals(tokenUserid))
        //        return true;
        //    return false;
        //}


        private bool ValidateToken(string userid, string tokenNo)
        {
            var data = cnaa.MaaTokens.Where(x => x.Mobileno == userid && x.Salt == tokenNo).FirstOrDefault();
            if (data == null)
            {
                return false;
            }
            else
            {
                //if (DifferenceInMinutes((DateTime)data.Date, DateTime.Now) > 20)
                //{
                //    return false;
                //}
                //else
                //{
                //    return true;
                //}
                return true;

            }

        }


        public HttpResponseMessage SubmitPin(BeneficiaryMobileNo_Maa u)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            Boolean status = true;
            string message = "Data Received successfully";

            string Mno = Decrypt(u.MobileNo);
            string Did = Decrypt(u.DeviceID);
            string Otps = Decrypt(u.OTP);
            DateTime SDate = DateTime.Now;
            try
            {
                if (validateMobileNo(Mno) == true)
                {
                    if (chkotp(Mno, Otps, Did) == true)
                    {

                        var data = cnaa.PinDetails.Where(x => x.MobileNo == Mno).FirstOrDefault();
                        if (data == null)
                        {
                            PinDetail pi = new PinDetail();
                            pi.MobileNo = Mno;
                            pi.Pin = u.Pin;
                            pi.DeviceID = Did;
                            pi.SentDate = DateTime.Now;
                            pi.LastUpdatedDate = DateTime.Now;
                            cnaa.PinDetails.Add(pi);
                            cnaa.SaveChanges();
                            //u.TokenNo = TokenManager.GenerateToken(Mno);


                            string salt = Guid.NewGuid().ToString();

                            var datas = cnaa.MaaTokens.Where(a => a.Mobileno == Mno && a.DeviceID == Did).FirstOrDefault();
                            if (datas == null)
                            {
                                MaaToken mt = new MaaToken();
                                mt.Mobileno = Mno;
                                mt.DeviceID = Did;
                                mt.Salt = salt;
                                mt.Date = DateTime.Now;
                                cnaa.MaaTokens.Add(mt);
                                cnaa.SaveChanges();

                            }
                            else
                            {
                                datas.Salt = salt;
                                datas.Date = DateTime.Now;
                                cnaa.SaveChanges();
                            }




                            var data1 = cnaa.uspGetMotherDetailsByMobileno(Mno, Did).Select(x => new { TokenNo = x.TokenNo, x.Name, x.Husbname, x.infantid, x.Mobileno, x.MotherID, x.pctsid, x.UnitNameHindi, x.UnitCode, x.VillageNameHindi, x.DistrictName, x.totinf }).FirstOrDefault();
                            message = "1";
                            if (data1 == null)
                            {
                                status = false;
                            }
                            else
                            {

                                status = true;
                                _objResponseModel.ResposeData = data1;

                            }
                        }
                        else
                        {

                            if (DifferenceInSec(data.LastUpdatedDate, DateTime.Now) < 5)
                            {
                                status = false;
                                message = "Invalid hit";
                            }
                            else
                            {
                                data.Pin = u.Pin;
                                data.SentDate = DateTime.Now;
                                data.LastUpdatedDate = DateTime.Now;
                                data.trail = 0;
                                cnaa.SaveChanges();
                                //u.TokenNo = TokenManager.GenerateToken(Mno);



                                string salt = Guid.NewGuid().ToString();

                                var datas = cnaa.MaaTokens.Where(a => a.Mobileno == Mno && a.DeviceID == Did).FirstOrDefault();
                                if (datas == null)
                                {
                                    MaaToken mt = new MaaToken();
                                    mt.Mobileno = Mno;
                                    mt.DeviceID = Did;
                                    mt.Salt = salt;
                                    mt.Date = DateTime.Now;
                                    cnaa.MaaTokens.Add(mt);
                                    cnaa.SaveChanges();

                                }
                                else
                                {
                                    datas.Salt = salt;
                                    datas.Date = DateTime.Now;
                                    cnaa.SaveChanges();
                                }


                                var data1 = cnaa.uspGetMotherDetailsByMobileno(Mno, Did).Select(x => new { TokenNo = x.TokenNo, x.Name, x.Husbname, x.infantid, x.Mobileno, x.MotherID, x.pctsid, x.UnitNameHindi, x.UnitCode, x.VillageNameHindi, x.DistrictName, x.totinf }).FirstOrDefault();
                                message = "2";
                                if (data1 == null)
                                {
                                    status = false;
                                }
                                else
                                {
                                    status = true;
                                    _objResponseModel.ResposeData = data1;
                                }
                            }

                            //status = false;
                        }

                    }
                    else
                    {

                        status = false;
                        message = "कृपया OTP सही डाले";
                    }


                }
                else
                {
                    status = false;
                    message = "कृपया सही मोबाईल नं./पिन डालें";
                }
                Usertrail(Mno, Did, 2, SDate);
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in post user" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }


            _objResponseModel.Status = status;
            _objResponseModel.Message = message;
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        [HttpPost]
        public HttpResponseMessage ChangePin(BeneficiaryMobileNo_Maa u)
        {
            ResponseModel _objResponseModel = new ResponseModel();

            Boolean status = true;
            string message = "";
            string Mno = Decrypt(u.MobileNo);
            string Did = Decrypt(u.DeviceID);
            bool tokenFlag = ValidateToken(Mno, u.TokenNo);
            //string pn = Decrypt(u.Pin);
            //string oldpn = Decrypt(u.OldPin);
            try
            {
                DateTime SDate = DateTime.Now;
                if (tokenFlag == true)
                {
                    if (validateMobileNo(Mno) == true)
                    {
                        if (CheckHit(3, Mno) == false)
                        {
                            status = false;
                            message = "Invalid Hit";
                        }
                        else
                        {
                            var data = cnaa.PinDetails.Where(x => x.MobileNo == Mno).FirstOrDefault();
                            if (data.DeviceID != Did)
                            {
                                status = false;
                                message = "Please try again";
                            }
                            else if (u.Pin.Trim() == u.OldPin.Trim())
                            {
                                status = false;
                                message = "पुरानी पिन और नई पिन समान नहीं हो सकती, कृपया सही पिन डालें";
                            }
                            else
                            {

                                data.Pin = u.Pin;
                                data.SentDate = DateTime.Now;
                                cnaa.SaveChanges();
                                status = true;
                                message = "आपकी पिन बदल दी गयी है";
                            }

                        }
                    }
                    else
                    {
                        status = false;
                        message = "कृपया सही मोबाईल नं./पिन डालें";
                    }
                }
                else
                {
                    status = false;
                    message = "Invalid request";
                }
                Usertrail(Mno, Did, 3, SDate);
            }
            catch (Exception ex)
            {
                ErrorHandler.WriteError("Error in post user" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }

            _objResponseModel.Status = status;
            _objResponseModel.Message = message;
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        [HttpPost]
        public HttpResponseMessage GetBeneficiaryByMobileNo(BeneficiaryMobileNo_Maa u)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string Mno = Decrypt(u.MobileNo);

            bool tokenFlag = ValidateToken(Mno, u.TokenNo);
            Boolean status = false;
            string message = "";
            DateTime SDate = DateTime.Now;

            if (tokenFlag == true)
            {
                message = "Data Received successfully";
                var p = cnaa.PinDetails.Where(x => x.MobileNo == Mno).OrderByDescending(x => x.LastUpdatedDate).FirstOrDefault();
                var Did = p.DeviceID;

                if (validateMobileNo(Mno) == true)
                {
                    var data = cnaa.uspGetMotherDetailsByMobileno(Mno, Did).ToList();
                    if (data != null)
                    {
                        status = true;
                    }
                    else
                    {
                        message = "No Data found";
                    }
                    _objResponseModel.ResposeData = data;
                }
                else
                {
                    status = false;
                    message = "कृपया सही मोबाईल नं. डालें";
                }
            }
            else
            {
                status = false;
                message = "Invalid request";
            }
            Usertrail(Mno, "", 4, SDate);

            _objResponseModel.Status = status;
            _objResponseModel.Message = message;
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }



        public HttpResponseMessage PostANMASHADetailsByPCTSID(Pcts_Maa u)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string Mno = Decrypt(u.MobileNo);
            string PID = Decrypt(u.PCTSID);
            bool tokenFlag = ValidateToken(Mno, u.TokenNo);
            DateTime SDate = DateTime.Now;
            if (validateMobileNo(Mno) == true)
            {
                if (CheckHit(5, Mno) == false)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid Hit";
                }
                else
                {
                    if (tokenFlag == true)
                    {
                        var data = cnaa.uspANMASHADetailsByPCTSID(PID).ToList();
                        if (data.Count > 0)
                        {
                            _objResponseModel.ResposeData = data;
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "Data Received successfully";
                        }
                        else
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = "आपकी पीसीटीएस आईडी का विवरण उपलब्ध नहीं है !";
                        }
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Invalid request";
                    }
                    Usertrail(Mno, "", 5, SDate);
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही मोबाईल नं./पिन डालें";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage PostNearestFacility(Pcts_Maa s)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string Mno = Decrypt(s.MobileNo);
            string UCode = Decrypt(s.unitcode);

            //string Mno = s.MobileNo;
            //string UCode = s.unitcode;
            bool tokenFlag = ValidateToken(Mno, s.TokenNo);
            DateTime SDate = DateTime.Now;
            if (validateMobileNo(Mno) == true)
            {
                if (CheckHit(6, Mno) == false)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid Hit";
                }
                else
                {
                    if (tokenFlag == true)
                    {
                        var data = cnaa.uspNearestFacility(UCode).ToList();
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

                    Usertrail(Mno, "", 6, SDate);
                }

            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही मोबाईल नं./पिन डालें";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage PostMamtaCard(Pcts_Maa u)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string Mno = Decrypt(u.MobileNo);
            string PID = Decrypt(u.PCTSID);
            //string Mno=u.MobileNo ;
            //string PID = u.PCTSID;
            DateTime SDate = DateTime.Now;
            bool tokenFlag = ValidateToken(Mno, u.TokenNo);
            if (validateMobileNo(Mno) == true)
            {
                if (CheckHit(7, Mno) == false)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid Hit";
                }
                else
                {
                    if (tokenFlag == true)
                    {
                        var data = cnaa.uspMamtaCard(PID).Select(x => new { name = x.Name, pctsid = x.pctsid, Husbname = x.Husbname, IsHusband = x.IsHusband, AadhaarNo = Encrypt(x.AadhaarNo), BHAMASHAHID = Encrypt(x.BHAMASHAHID), Mobileno = Encrypt(x.Mobileno), RationCardNo = Encrypt(x.RationCardNo), bplcardno = Encrypt(x.bplcardno), Accountno = Encrypt(x.Accountno), Ifsc_code = Encrypt(x.Ifsc_code), Bank_Name = Encrypt(x.Bank_Name), Branch_Name = x.Branch_Name, Address = x.Address, Age = x.Age, UnitNameHindi = x.UnitNameHindi, ECID = x.ECID, RegDate = x.RegDate, LMPDT = x.LMPDT, edd = x.edd, LiveChild = x.LiveChild, sansthaflagPresent = x.sansthaflagPresent, ChildName = x.ChildName, Prasav_date = x.Prasav_date, Weight = x.Weight, Sex = x.Sex, BloodGroup = x.BloodGroup, sansthaflagPrevious = x.sansthaflagPrevious, JSYAmount = x.JSYAmount, JSYRealizationDate = x.JSYRealizationDate, ChildId = Encrypt(x.ChildId) }).FirstOrDefault();

                        if (data != null)
                        {
                            _objResponseModel.ResposeData = data;
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "Data Received successfully";
                        }
                        else
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = "आपकी पीसीटीएस आईडी का विवरण उपलब्ध नहीं है !";
                        }
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Invalid request";
                    }
                    Usertrail(Mno, "", 7, SDate);
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही मोबाईल नं./पिन डालें";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage PostANCSchedule(Pcts_Maa s)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string Mno = Decrypt(s.MobileNo);
            string PID = Decrypt(s.PCTSID);
            //string Mno = s.MobileNo;
            //string PID = s.PCTSID;
            bool tokenFlag = ValidateToken(Mno, s.TokenNo);
            DateTime SDate = DateTime.Now;
            if (validateMobileNo(Mno) == true)
            {
                if (CheckHit(8, Mno) == false)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid Hit";
                }
                else
                {
                    if (tokenFlag == true)
                    {
                        var data = cnaa.uspANCSchedule(PID).ToList();

                        if (data.Count > 0)
                        {
                            _objResponseModel.ResposeData = data;
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "Data Received successfully";
                        }
                        else
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = "आपकी पीसीटीएस आईडी का विवरण उपलब्ध नहीं है ! ";
                        }
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Invalid request";
                    }
                    Usertrail(Mno, "", 8, SDate);


                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही मोबाईल नं./पिन डालें";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage PostPNCSchedule(Pcts_Maa s)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string Mno = Decrypt(s.MobileNo);
            string PID = Decrypt(s.PCTSID);
            bool tokenFlag = ValidateToken(Mno, s.TokenNo);
            DateTime SDate = DateTime.Now;
            if (validateMobileNo(Mno) == true)
            {
                if (CheckHit(9, Mno) == false)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid Hit";
                }
                else
                {
                    if (tokenFlag == true)
                    {
                        var data = cnaa.uspPNCSchedule(PID).ToList();
                        if (data != null)
                        {
                            _objResponseModel.ResposeData = data;
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "Data Received successfully";
                        }
                        else
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = "आपकी पीसीटीएस आईडी का विवरण उपलब्ध नहीं है ";
                        }
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Invalid request";
                    }
                    Usertrail(Mno, "", 9, SDate);
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही मोबाईल नं./पिन डालें";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage uspInfantlistByPCTSID(Pcts_Maa p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string Mno = Decrypt(p.MobileNo);
            string PID = Decrypt(p.PCTSID);
            bool tokenFlag = ValidateToken(Mno, p.TokenNo);
            DateTime SDate = DateTime.Now;
            if (validateMobileNo(Mno) == true)
            {
                if (CheckHit(10, Mno) == false)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid Hit";
                }
                else
                {
                    if (tokenFlag == true)
                    {
                        if (PID.Length > 13)
                        {
                            var data = cnaa.uspInfantlistForImmunizationByPCTSID(PID).Select(x => new { name = x.name, Husbname = x.Husbname, Mobileno = x.Mobileno, Birth_date = x.Birth_date, ChildName = x.ChildName, Sex = x.Sex, MotherID = x.MotherID, InfantID = x.infantid, ChildID = x.childid, PehchanRegFlag = x.PehchanRegFlag }).ToList();
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
                    Usertrail(Mno, "", 10, SDate);
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही मोबाईल नं./पिन डालें";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage PostImmunizationSchedule(Pcts_Maa s)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string Mno = Decrypt(s.MobileNo);
            string InfID = Decrypt(s.InfantID);
            string ImuFlg = Decrypt(s.ImmuFlag);
            bool tokenFlag = ValidateToken(Mno, s.TokenNo);
            DateTime SDate = DateTime.Now;
            //   if (validateMobileNo(Mno) == true)
            //{
            //  if (CheckHit(11, Mno) == false)
            //  {
            //      _objResponseModel.Status = false;
            //      _objResponseModel.Message = "Invalid Hit";
            //  }
            //  else
            //  {
            if (tokenFlag == true)
            {
                var data = cnaa.uspImmunizationSchedule(Convert.ToInt32(InfID), Convert.ToByte(ImuFlg)).ToList();
                if (data != null)
                {
                    _objResponseModel.ResposeData = data;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "आपकी पीसीटीएस आईडी का विवरण उपलब्ध नहीं है ";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            Usertrail(Mno, "", 11, SDate);
            //  }
            //}
            //   else
            //   {
            //       _objResponseModel.Status = false;
            //       _objResponseModel.Message = "कृपया सही मोबाईल नं./पिन डालें";
            //   }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        }

        public HttpResponseMessage PostImmunizationScheduleDueDate(Pcts_Maa s)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string Mno = Decrypt(s.MobileNo);
            bool tokenFlag = ValidateToken(Mno, s.TokenNo);
            string InfID = Decrypt(s.InfantID);
            string ImuFlg = Decrypt(s.ImmuFlag);
            DateTime SDate = DateTime.Now;
            //   if (validateMobileNo(Mno) == true)
            //{
            //if (CheckHit(12, Mno) == false)
            //{
            //    _objResponseModel.Status = false;
            //    _objResponseModel.Message = "Invalid Hit";
            //}
            //else
            //{
            if (tokenFlag == true)
            {
                var data = cnaa.MaaImmunizationSchedule(Convert.ToInt32(InfID), Convert.ToByte(ImuFlg)).ToList();
                if (data != null)
                {
                    _objResponseModel.ResposeData = data;
                    _objResponseModel.Status = true;
                    _objResponseModel.Message = "Data Received successfully";
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "आपकी पीसीटीएस आईडी का विवरण उपलब्ध नहीं है ";
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "Invalid request";
            }
            Usertrail(Mno, "", 12, SDate);
            //}
            //}
            //   else
            //   {
            //       _objResponseModel.Status = false;
            //       _objResponseModel.Message = "कृपया सही मोबाईल नं./पिन डालें";
            //   }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        public HttpResponseMessage PostSentSMS(BeneficiaryMobileNo_Maa u)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            Boolean status = true;
            string message = "Data Received successfully";
            string Mno = Decrypt(u.MobileNo);
            DateTime SDate = DateTime.Now;
            if (validateMobileNo(Mno) == true)
            {
                if (CheckHit(13, Mno) == false)
                {
                    status = false;
                    message = "Invalid Hit";
                }
                else
                {
                    var data = PctsAppcnaa.Mothers.Where(x => x.Mobileno == Mno).Count();
                    if (data == 0)
                    {
                        status = false;
                        message = "कृपया सही मोबाइल नम्बर डाले !";
                    }
                    else
                    {
                        var p = cnaa.OTPs.Where(x => x.MobileNo == Mno && x.SmsFlag == 0).OrderByDescending(x => x.EntryDate).FirstOrDefault();
                        if (p != null)
                        {
                            if (DifferenceInMinutes(p.EntryDate, DateTime.Now) <= 15)
                            {
                                status = true;
                                message = "प्राप्त हुआ OTP 15 मिनट तक मान्य है ";
                            }
                            else
                            {
                                bool sendSmsFlag = sendSms1(Mno, u.SmsFlag);
                                _objResponseModel.ResposeData = sendSmsFlag;
                            }
                        }
                        else
                        {
                            bool sendSmsFlag = sendSms1(Mno, u.SmsFlag);
                            _objResponseModel.ResposeData = sendSmsFlag;
                        }
                    }
                    Usertrail(Mno, "", 13, SDate);
                }
            }
            else
            {
                status = false;
                message = "कृपया सही मोबाईल नं. डालें";
            }
            _objResponseModel.Status = status;
            _objResponseModel.Message = message;
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public int DifferenceInMinutes(DateTime d1, DateTime d2)
        {
            return Convert.ToInt32((d2 - d1).TotalMinutes);
        }
        public int DifferenceInSec(DateTime d1, DateTime d2)
        {
            return Convert.ToInt32((d2 - d1).TotalSeconds);
        }
        private bool sendSms1(string mobileNo, byte SmsFlag)
        {
            string randomno = RandomInteger(1000, 9999).ToString();
            //randomno = "1234";
            string msg;
            if (SmsFlag == 1)
            {
                //             msg = "OTP for Birth Certificate is " + randomno + " Valid for 15 Minutes. - Department of Medical health and Welfare GoR";
                msg = "The OTP for verification is " + randomno + " in Maa app, valid for 15 Minutes. -Medical & Health Dept, GoR";
            }
            else
            {
                //   msg = "OTP for Maa app is " + randomno + " Valid for 15 Minutes. - Department of Medical health and Welfare GoR";
                msg = "The OTP for verification is " + randomno + " in Maa app, valid for 15 Minutes. -Medical & Health Dept, GoR";
            }

            var p = cnaa.OTPs.Where(x => x.MobileNo == mobileNo && x.SmsFlag == SmsFlag).FirstOrDefault();
            if (p == null)
            {
                OTP p1 = new OTP();
                p1.MobileNo = mobileNo;
                p1.OTPNo = randomno;
                p1.EntryDate = DateTime.Now;
                p1.SmsFlag = SmsFlag;
                cnaa.OTPs.Add(p1);
                cnaa.SaveChanges();
            }
            else
            {
                p.OTPNo = randomno;
                p.EntryDate = DateTime.Now;
                cnaa.SaveChanges();
            }
            return SendSMS(mobileNo, msg, SmsFlag);
        }

        //,string DeviceID,string motherid

        private bool SendSMS(string mobileNo, string msg, Int16 SmsFlag)
        {
            String userId = Convert.ToString(ConfigurationManager.AppSettings["SmsUserid"]);
            String password = Convert.ToString(ConfigurationManager.AppSettings["SmsPassword"]);
            //String senderid = "PCTSRJ";
            String messageid = "";
            String ErroDesc = "";
            //String appid = "rshsalt1";
            String mobile = mobileNo;
            try
            {
                //SendSMSSoapClient sm = new SendSMSSoapClient();
                //string Salt = Convert.ToString(RandomInteger(10000000, 99999999));
                //string SMSInnerPassword = Convert.ToString(ConfigurationManager.AppSettings["SMSInnerPassword"]);
                //string Password = Convert.ToString(ConfigurationManager.AppSettings["SMSPassword"]);
                //messageid = sm.SendSMS(appid, userId, Password, "1", senderid, mobileNo, msg, ComputeHash(GenerateSHA512String(SMSInnerPassword).ToLower(), new SHA512CryptoServiceProvider(), Salt), Salt).Trim();
                //string url = "https://vsms.minavo.in/api/singlesms.php?auth_key=e5294d887d3fc772ed96427c6220fd8720220331164356&mobilenumber=" + mobileNo + "&message=" + msg + "&sid=NHMRAJ&mtype=N&template_id=" + "1107160265631815011";
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
                mobileList.Add(mobileNo);
                ExternalSMSApiInfo inputparams = new ExternalSMSApiInfo();
                inputparams.UniqueID = "NHM_OTP";
                inputparams.serviceName = "RAJMHS";
                inputparams.language = "ENG";
                inputparams.message = msg;
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
                ErroDesc = ex.ToString().Substring(0, 300);
                return false;
            }

            // messageid = "rshasalt-2-20021020202";
            OtpLog ol = new OtpLog();
            ol.ErrorDescription = ErroDesc;
            ol.MobileNo = mobile;
            ol.PushDate = DateTime.Now;
            ol.ErrorID = 2;
            ol.MessageId = messageid;
            if (SmsFlag == 1)
            {
                ol.Msgtype = 84;
            }
            else
            {
                ol.Msgtype = 81;
            }
            cnaa.OtpLogs.Add(ol);
            cnaa.SaveChanges();

            return true;
        }

        public HttpResponseMessage PostCheckOTP(BeneficiaryMobileNo_Maa u)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            Boolean status = true;
            string message = "Data Received successfully";
            string Mno = Decrypt(u.MobileNo);
            string Otps = Decrypt(u.OTP);
            string Did = Decrypt(u.DeviceID);
            DateTime SDate = DateTime.Now;
            if (validateMobileNo(Mno) == true)
            {

                var p = cnaa.OTPs.Where(x => x.MobileNo == Mno && x.OTPNo == Otps).OrderByDescending(x => x.EntryDate).FirstOrDefault();
                if (p != null)
                {
                    if (DifferenceInMinutes(p.EntryDate, DateTime.Now) > 15)
                    {
                        status = false;
                        message = "Invalid OTP";
                    }
                    else
                    {
                        var p1 = cnaa.PinDetails.Where(x => x.MobileNo == Mno).FirstOrDefault();
                        if (p1 != null)
                        {
                            if (p1.DeviceID != Did)
                            {

                            }
                            p1.DeviceID = Did;
                            cnaa.SaveChanges();
                        }

                        status = true;
                        message = "Valid OTP";


                    }
                }
                else
                {
                    status = false;
                    message = "Invalid OTP";
                }
                Usertrail(Mno, Did, 14, SDate);
            }
            else
            {
                status = false;
                message = "कृपया सही मोबाईल नं. डालें";
            }
            _objResponseModel.Status = status;
            _objResponseModel.Message = message;
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        private bool chkotp(string mobileno, string otp, string DeviceID)
        {
            string Mno = mobileno;
            string Otps = otp;
            string Did = DeviceID;

            var p = cnaa.OTPs.Where(x => x.MobileNo == Mno && x.OTPNo == Otps).OrderByDescending(x => x.EntryDate).FirstOrDefault();
            if (p != null)
            {
                if (DifferenceInMinutes(p.EntryDate, DateTime.Now) > 15)
                {
                    return false;
                }
                else
                {
                    var p1 = cnaa.PinDetails.Where(x => x.MobileNo == Mno).FirstOrDefault();
                    if (p1 != null)
                    {
                        if (p1.DeviceID != Did)
                        {

                        }
                        p1.DeviceID = Did;
                        cnaa.SaveChanges();
                    }

                    return true;


                }
            }
            else
            {
                return false;
            }
            //_objResponseModel.Status = status;
            //_objResponseModel.Message = message;
            //return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        public HttpResponseMessage PostPCTSID(Pcts_Maa p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string Mno = Decrypt(p.MobileNo);
            string PID = Decrypt(p.PCTSID);
            bool tokenFlag = ValidateToken(Mno, p.TokenNo);
            DateTime SDate = DateTime.Now;
            if (validateMobileNo(Mno) == true)
            {

                if (CheckHit(15, Mno) == false)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid Hit";
                }
                else
                {
                    if (tokenFlag == true)
                    {
                        if (PID.Length < 14)
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = "कृपया सही पीसीटीएस आईडी डाले ";
                        }
                        else
                        {
                            var p1 = PctsAppcnaa.Mothers.Where(x => x.pctsid == PID).Select(x => new { ancregid = x.ancregid, MotherID = x.MotherID, Status = x.Status }).ToList();
                            if (p1 != null && p1.Count > 0)
                            {

                                var data = cnaa.uspWomendetail(PID).ToList();
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
                    Usertrail(Mno, "", 15, SDate);
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही मोबाईल नं. डालें";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage uspInfantlistByPCTSIDForWomanDetails(Pcts_Maa p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string Mno = Decrypt(p.MobileNo);
            string PID = Decrypt(p.PCTSID);
            //string PID = p.PCTSID;
            //string Mno = p.MobileNo;
            bool tokenFlag = ValidateToken(Mno, p.TokenNo);
            DateTime SDate = DateTime.Now;
            if (validateMobileNo(Mno) == true)
            {

                if (CheckHit(16, Mno) == false)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid Hit";
                }
                else
                {
                    if (tokenFlag == true)
                    {
                        if (PID.Length > 13)
                        {
                            var data = PctsAppcnaa.uspInfantlistForWomanDetails(PID);
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
                    Usertrail(Mno, "", 16, SDate);
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही मोबाईल नं. डालें";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        [ActionName("uspDataforManageANC")]
        public HttpResponseMessage uspDataforManageANC(Pcts_Maa a)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string Mno = Decrypt(a.MobileNo);
            string ARId = Decrypt(a.ANCRegID);
            bool tokenFlag = ValidateToken(Mno, a.TokenNo);
            DateTime SDate = DateTime.Now;
            if (validateMobileNo(Mno) == true)
            {
                if (CheckHit(17, Mno) == false)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid Hit";
                }
                else
                {
                    if (tokenFlag == true)
                    {
                        if (Convert.ToInt32(ARId) != 0)
                        {
                            var data = PctsAppcnaa.uspDataforManageANC(Convert.ToInt32(ARId)).ToList();
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
                    Usertrail(Mno, "", 17, SDate);

                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही मोबाईल नं. डालें";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage uspDataforPNCWomanDetails(Pcts_Maa p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string Mno = Decrypt(p.MobileNo);
            string ARId = Decrypt(p.ANCRegID);
            DateTime SDate = DateTime.Now;
            bool tokenFlag = ValidateToken(Mno, p.TokenNo);
            if (validateMobileNo(Mno) == true)
            {
                if (CheckHit(18, Mno) == false)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid Hit";
                }
                else
                {
                    if (tokenFlag == true)
                    {
                        if (Convert.ToInt32(ARId) != 0)
                        {
                            var data = PctsAppcnaa.uspDataforPNCWomanDetails(Convert.ToInt32(ARId)).ToList();
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
                    Usertrail(Mno, "", 18, SDate);
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही मोबाईल नं. डालें";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

        }

        private int CheckVersion(string appVersion)
        {
            int CheckAppVersionFlag = 0;
            ResponseModel _objResponseModel = new ResponseModel();
            if (!string.IsNullOrEmpty(Convert.ToString(appVersion)))
            {
                string version = rajmed.AppHistories.Where(x => x.AppFlag == 1).Max(x => x.VersionName);
                if (version != null)
                {
                    if (Convert.ToString(appVersion).ToString() != Convert.ToString(version))
                    {
                        CheckAppVersionFlag = 1;
                    }
                }
            }
            else
            {
                CheckAppVersionFlag = 1;
            }
            return CheckAppVersionFlag;
        }
        [ActionName("PostComplainDetail")]
        public HttpResponseMessage PostComplainDetail(ComplainSuggestion cs)     //AddDelivery Details
        {

            ResponseModel _objResponseModel = new ResponseModel();
            //string Mno = Decrypt(cs.MobileNo);
            //string PID = Decrypt(cs.PCTSID);
            DateTime SDate = DateTime.Now;
            if (validateMobileNo(cs.MobileNo) == true)
            {
                bool tokenFlag = ValidateToken(cs.MobileNo, cs.TokenNo);
                if (tokenFlag == true)
                {
                    try
                    {
                        var p1 = PctsAppcnaa.Mothers.Where(x => x.pctsid == cs.PCTSID).Select(x => new { MotherID = x.MotherID }).FirstOrDefault();
                        if (p1 == null)
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = "पीसीटीएस आईडी सही नहीं हैं।";
                        }
                        else
                        {
                            cs.motherid = p1.MotherID;
                            if (ModelState.IsValid)
                            {
                                var p = cnaa.ComplainSuggestions.Where(x => x.ComplainID == cs.ComplainID).FirstOrDefault();
                                if (p == null)
                                {
                                    ComplainSuggestion cs1 = new ComplainSuggestion();
                                    cs1.motherid = cs.motherid;
                                    cs1.ComplainSuggestionDate = cs.ComplainSuggestionDate;
                                    cs1.EntryDate = DateTime.Now;
                                    cs1.LastUpdateDate = DateTime.Now;
                                    cs1.Description = cs.Description;
                                    cnaa.ComplainSuggestions.Add(cs1);
                                    cnaa.SaveChanges();

                                    var p2 = cnaa.uspGetMobilenoForSendComplain(cs.motherid).FirstOrDefault();
                                    if (p2 != null)
                                    {
                                        //SendSMS(p2, cs.Description,2);
                                    }

                                }
                                else
                                {
                                    p.ComplainSuggestionDate = cs.ComplainSuggestionDate;
                                    p.LastUpdateDate = DateTime.Now;
                                    p.Description = cs.Description;
                                    cnaa.SaveChanges();
                                }
                                _objResponseModel.Status = true;
                                _objResponseModel.Message = "धन्यवाद, शिकायत सेव हो चुकी हैं।";
                            }
                            else
                            {
                                _objResponseModel.Status = false;
                                _objResponseModel.Message = "Model Error";
                                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                            }
                        }


                    }
                    catch
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Invalid request";
                    }
                }
                else
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid request";
                }

                Usertrail(cs.MobileNo, "", 19, SDate);
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही मोबाईल नं. डालें";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        [ActionName("ComplainDetails")]
        public HttpResponseMessage ComplainDetails(ComplainSuggestion cs)     //AddDelivery Details
        {

            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(cs.MobileNo, cs.TokenNo);
            if (validateMobileNo(cs.MobileNo) == true)
            {
                //string Mno = Decrypt(cs.MobileNo);
                //string PID = Decrypt(cs.PCTSID);
                DateTime SDate = DateTime.Now;
                if (CheckHit(20, cs.MobileNo) == false)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid Hit";
                }
                else
                {
                    if (tokenFlag == true)
                    {
                        try
                        {
                            var p1 = PctsAppcnaa.Mothers.Where(x => x.pctsid == cs.PCTSID).Select(x => new { MotherID = x.MotherID }).FirstOrDefault();
                            if (p1 == null)
                            {
                                _objResponseModel.Status = false;
                                _objResponseModel.Message = "पीसीटीएस आईडी सही नहीं हैं।";
                            }
                            else
                            {
                                cs.motherid = p1.MotherID;
                                if (ModelState.IsValid)
                                {
                                    var p = cnaa.ComplainSuggestions.Where(x => x.motherid == cs.motherid).OrderByDescending(x => x.LastUpdateDate).ToList();
                                    if (p != null)
                                    {
                                        _objResponseModel.ResposeData = p;
                                        _objResponseModel.Status = true;
                                        _objResponseModel.Message = "Data Received successfully";
                                    }
                                    else
                                    {
                                        _objResponseModel.Status = false;
                                        _objResponseModel.Message = "No complain";
                                    }


                                }
                                else
                                {
                                    _objResponseModel.Status = false;
                                    _objResponseModel.Message = "Model Error";
                                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
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
                    Usertrail(cs.MobileNo, "", 20, SDate);

                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही मोबाईल नं. डालें";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        public HttpResponseMessage uspInfantDataByInfantID(Pcts_Maa p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string Mno = Decrypt(p.MobileNo);
            bool tokenFlag = ValidateToken(Mno, p.TokenNo);
            DateTime SDate = DateTime.Now;
            string InfID = Decrypt(p.InfantID);
            if (validateMobileNo(Mno) == true)
            {
                if (CheckHit(21, Mno) == false)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid Hit";
                }
                else
                {
                    if (tokenFlag == true)
                    {
                        if (Convert.ToInt32(InfID) > 0)
                        {
                            var data = cnaa.uspInfantDetailsByInfantID(Convert.ToInt32(InfID)).ToList();
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
                    Usertrail(Mno, "", 21, SDate);
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही मोबाईल नं. डालें";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public int DifferenceInMonth(DateTime d1, DateTime d2)
        {
            int monthsApart = 12 * (d1.Year - d2.Year) + d1.Month - d2.Month;
            return Math.Abs(monthsApart);
        }

        public HttpResponseMessage VideoUrl(MaaVideo p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string Vtype = "";
            var data = cnaa.MaaVideoDetails.Where(x => x.VideoType == p.VideoType).Select(x => new { VideoId = x.ID, VideoName = x.VideoName, VideoType = x.VideoType, Descrption = x.Descrption, ImageName = x.ImageName }).ToList();
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
                    pct.VideoName = "/Video/" + item.VideoName;
                    pct.Descrption = item.Descrption;
                    pct.ImageName = "/Images/" + item.ImageName;
                    p1.Add(pct);
                }
                _objResponseModel.ResposeData = p1;
                _objResponseModel.Status = true;
                _objResponseModel.Message = "Data Received successfully";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage uspInfantlistBirthCertificatesByPCTSID(Pcts_Maa p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string Mno = Decrypt(p.MobileNo);
            string PID = Decrypt(p.PCTSID);
            bool tokenFlag = ValidateToken(Mno, p.TokenNo);
            DateTime SDate = DateTime.Now;
            if (validateMobileNo(Mno) == true)
            {

                if (tokenFlag == true)
                {
                    if (PID.Length > 13)
                    {
                        var data = cnaa.uspInfantlistForBirthCertificatByPCTSID(PID).Select(x => new { name = x.name, Husbname = x.Husbname, Mobileno = x.Mobileno, Birth_date = x.Birth_date, ChildName = x.ChildName, Sex = x.Sex, MotherID = x.MotherID, InfantID = x.infantid, ChildID = x.childid, PehchanRegFlag = x.PehchanRegFlag }).ToList();
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
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही मोबाईल नं./पिन डालें";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }

        public HttpResponseMessage birthcertificates(birthcertificates p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string Mno = Decrypt(p.MobileNo);
            string InfID = Decrypt(p.infantid);
            bool tokenFlag = ValidateToken(Mno, p.TokenNo);
            DateTime SDate = DateTime.Now;
            if (validateMobileNo(Mno) == true)
            {
                if (CheckHit(22, Mno) == false)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid Hit";
                }
                else
                {
                    if (tokenFlag == true)
                    {
                        int? InfIDs = Convert.ToInt32(InfID);
                        try
                        {
                            if (InfIDs == null)
                            {
                                _objResponseModel.Status = false;
                                _objResponseModel.Message = "No Data Found";
                            }
                            else
                            {
                                string fileName = HttpContext.Current.Server.MapPath("~/BirthCertificates/" + InfIDs + ".pdf");
                                if (File.Exists(fileName))
                                {

                                    Dictionary<string, string> hash = new Dictionary<string, string> { };
                                    hash.Add("Url", "/BirthCertificates/" + InfIDs + ".pdf");
                                    hash.Add("Infantid", InfIDs + ".pdf");
                                    _objResponseModel.ResposeData = hash;
                                    _objResponseModel.Status = true;
                                    _objResponseModel.Message = "Data Received successfully";
                                }
                                else
                                {
                                    var data = cnaa.PehchanBirths.Where(x => x.InfantID == InfIDs).Select(x => new { RegNo = x.RegNo, RegYear = (int?)x.RegistrationYear }).FirstOrDefault();
                                    if (data != null)
                                    {

                                        string password = Convert.ToString(ConfigurationManager.AppSettings["ApiPassword"]);
                                        string userid = Convert.ToString(ConfigurationManager.AppSettings["ApiUserid"]);
                                        //   Random rnd = new Random();
                                        int salt = RandomInteger(10000000, 99999999);
                                        var saltedPass = GenerateSHA256String(password);
                                        var sha256pass = GenerateSHA256String(salt + saltedPass.ToLower());
                                        var encpassword = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sha256pass.Trim()));

                                        // DateTime date = Convert.ToDateTime(data.RegDate);
                                        int year = Convert.ToInt32(data.RegYear);

                                        string URL = "http://164.100.153.91/pehchanws/searchreg/service.asmx/SearchRegistration?";
                                        URL += "user_id=" + HttpUtility.UrlEncode(userid) + "&user_pwd=" + (encpassword) + "&salt=" + HttpUtility.UrlEncode(salt.ToString()) + "&Event=" + HttpUtility.UrlEncode("1") + "&RegisNumber=" + HttpUtility.UrlEncode(data.RegNo) + "&Year=" + HttpUtility.UrlEncode(year.ToString());
                                        HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(URL);
                                        HttpWebResponse myResp = (HttpWebResponse)myReq.GetResponse();

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
                                            WriteByteArrayToPdf(PdfFilnalData, uploadfolderpath, InfIDs.ToString());
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
                                            hash.Add("Url", "/BirthCertificates/" + InfIDs + ".pdf");
                                            hash.Add("Infantid", InfIDs + ".pdf");
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
                    Usertrail(Mno, "", 22, SDate);
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही मोबाईल नं. डालें";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
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



        public static string GenerateSHA256String(string inputString)
        {
            SHA256 sha256 = SHA256Managed.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(inputString);
            byte[] hash = sha256.ComputeHash(bytes);
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



        public HttpResponseMessage WeightDetail(WeightDetails p)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string Mno = Decrypt(p.MobileNo);
            string InfID = Decrypt(p.infantid);
            bool tokenFlag = ValidateToken(Mno, p.TokenNo);
            if (validateMobileNo(Mno) == true)
            {
                DateTime SDate = DateTime.Now;
                if (CheckHit(23, Mno) == false)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid Hit";
                }
                else
                {
                    if (tokenFlag == true)
                    {
                        if (Convert.ToString(InfID) == "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = "No Data Found";
                        }
                        else
                        {
                            int InfIDs = Convert.ToInt32(InfID);
                            var p1 = (from inf in PctsAppcnaa.Infants
                                      join moth in PctsAppcnaa.Mothers on inf.MotherID equals moth.MotherID
                                      join imu in PctsAppcnaa.Immunizations on inf.InfantID equals imu.InfantID into lj
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
                    Usertrail(Mno, "", 23, SDate);
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही मोबाईल नं. डालें";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);

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
        public HttpResponseMessage PostCreateToken(BeneficiaryMobileNo_Maa a)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            _objResponseModel.ResposeData = TokenManager.GenerateToken(a.MobileNo);
            _objResponseModel.Status = true;

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
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
        public HttpResponseMessage PostAdvanceSearch(Pcts_Maa u)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            string Mno = Decrypt(u.MobileNo);
            string PID = Decrypt(u.PCTSID);
            //string Mno = u.MobileNo;
            //string PID = u.PCTSID;
            DateTime SDate = DateTime.Now;
            bool tokenFlag = ValidateToken(Mno, u.TokenNo);
            if (validateMobileNo(Mno) == true)
            {
                if (CheckHit(24, Mno) == false)
                {
                    _objResponseModel.Status = false;
                    _objResponseModel.Message = "Invalid Hit";
                }
                else
                {
                    if (tokenFlag == true)
                    {
                        var data = cnaa.UspMaaAdvanceSearch(PID).Select(x => new { ANCRegID = x.ANCRegID, Schemefalg = x.Schemefalg, InstallmentNo = Encrypt(Convert.ToString(x.InstallmentNo)), PaymentMode = Encrypt(Convert.ToString(x.PaymentMode)), AccountNo = Encrypt(x.AccountNo), InfantID = x.InfantID, Amount = Encrypt(Convert.ToString(x.Amount)), PaymentStatus = x.PaymentStatus, ChequeDate = x.ChequeDate, ChequeNo = Encrypt(x.ChequeNo), ChequeAmount = Encrypt(Convert.ToString(x.ChequeAmount)), Bank_Name = Encrypt(x.Bank_Name), IFSC_CODE = Encrypt(x.IFSC_CODE), PaymentDate = x.PaymentDate, RealizationDate = x.RealizationDate }).ToList();
                        if (data.Count != 0)
                        {
                            _objResponseModel.ResposeData = data;
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "Data Received successfully";
                        }
                        else
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = "आपकी पीसीटीएस आईडी का विवरण उपलब्ध नहीं है !";
                        }
                    }
                    else
                    {
                        _objResponseModel.Status = false;
                        _objResponseModel.Message = "Invalid request";
                    }
                    Usertrail(Mno, "", 24, SDate);
                }
            }
            else
            {
                _objResponseModel.Status = false;
                _objResponseModel.Message = "कृपया सही मोबाईल नं. डालें";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
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
        public static string GenerateSHA512String(string inputString)
        {
            SHA512 sha512 = SHA512Managed.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(inputString);
            byte[] hash = sha512.ComputeHash(bytes);
            return GetStringFromHash(hash);
        }

    }
  


}



