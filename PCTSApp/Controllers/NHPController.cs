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
    public class NHPController : ApiController
    {
        private rajmedicalEntities rajmed = new rajmedicalEntities();
        private cnaaEntities cnaa = new cnaaEntities();
        private DHSurveyEntities dhs = new DHSurveyEntities();
        private string appValiationMsg = "यह वर्जन पुराना हो चुका है, कृपया Google play store से नया वर्जन अपडेट करें ! ";


        //*********************ANTRA*************************//
        [ActionName("UspGetDotsDetails")]
        public HttpResponseMessage UspGetDotsDetails(DOTS A)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            bool tokenFlag = ValidateToken(A.UserID, A.TokenNo);
            if (tokenFlag == true)
            {
                if (A.UserID != "")
                {
                    List<List<object>> items = new List<List<object>>();
                    SqlParameter[] SP = new SqlParameter[2];
                    SP[0] = new SqlParameter("@Permtype", A.PermType);
                    SP[1] = new SqlParameter("@AshaautoId", A.AshaautoId);
                    DataSet ds = Connect_DB.DataSet("exec ASHASoft_DOTS @Permtype,@AshaautoId", SP, "cnaa");
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


        [ActionName("PostDotsDetails")]
        public HttpResponseMessage PostDotsDetails(DOTS A)
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
                        string ErrorMsg = SaveDotsDetails(A, 1);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, टीबी के उपचार का विवरण सेव हो चुका हैं।";
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
                ErrorHandler.WriteError("Error in Post TB " + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        private string SaveDotsDetails(DOTS A, int methodFlag)
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
                                var Result = objcnaa.Database.SqlQuery<string>("exec ASHASoft_DOTS {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16}"
                                , A.PermType, A.AshaautoId, A.PermMedia, A.PermEntryUserNo, A.DOTsPID, A.NameofPatient, A.Address, A.Age, A.sex, A.MobileNo, A.RegistrationDate, A.DateofStartingtheTreatment,
                                A.DateofCompletionofTreatment_IP, A.DateofCompletionofTreatment, A.Category, A.NikshayID, A.FamilyID).FirstOrDefault();
                                if (Result != "0")
                                {
                                    ErrorMsg = Result;
                                }
                            }
                            else if (methodFlag == 2) //update
                            {
                                //objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_ANTRA_Program {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28}"
                                //, A.PctsId, A.PermType, A.AshaautoId, A.E_mthyr, A.Mthyr, A.PermMedia, A.PermEntryUserNo, A.Latitude, A.Longitude, A.MotherID, A.WomanID, A.Unitid, A.WomanName, A.HusbandName, A.Children,
                                //A.Age, A.LastDeliveryDate, A.AadhaarNo, A.BhamashahID, A.Address, A.MobileNo, A.DhatriWoman, A.InjectionDate, A.DoseNO, A.InjectionType, A.WhenInjected,
                                //A.InjectionUnitID, A.InjectionID, A.AbortionDeliveryType);
                            }
                            else if (methodFlag == 3)//delete
                            {
                                //var Result = objcnaa.Database.SqlQuery<string>("exec ASHASoft_ANTRA_Program '0',{0},'0','0','0','0','0','0','0','0',{1},'0','0','0','0','0','','0','0','0','0','0','','0','0','0','0',{2} ", A.PermType, A.WomanID, A.InjectionID).FirstOrDefault();
                                //if (Result != "0")
                                //{
                                //    ErrorMsg = Result;
                                //}
                            }
                            else if (methodFlag == 6)//verify
                            {
                                //objcnaa.Database.ExecuteSqlCommand("exec ASHASoft_ANTRA_Program '0',{0},'0','0','0',{1},{2},'0','0','0',{3},'0','0','0','0','0','','0','0','0','0','0','','0','0','0','0',{4} ", A.PermType, A.PermMedia, A.PermEntryUserNo, A.WomanID, A.InjectionID);
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
                            ErrorMsg = "ओह ! टीबी के उपचार का विवरण सेव नहीं हुआ हैं। कृपया दोबारा सेव करें । ";
                            ErrorHandler.WriteError("Error in Post sam Followup" + ex.ToString());
                        }
                        else
                        {
                            ErrorMsg = "ओह ! टीबी के उपचार का विवरण अपडेट नहीं हुआ हैं। कृपया दोबारा अपडेट करें ।";
                            ErrorHandler.WriteError("Error in PUT NDD " + ex.ToString());
                        }
                    }
                }
            }
            return ErrorMsg;
        }

        [ActionName("PutDotsDetails")]
        public HttpResponseMessage PutDotsDetails(DOTS A)
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
                        string ErrorMsg = SaveDotsDetails(A, A.PermType);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else if (A.PermType == 6)
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, टीबी के उपचार का विवरण Verified हो चुका हैं।";
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, टीबी के उपचार का विवरण Update हो चुका हैं।";
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
                ErrorHandler.WriteError("Error in Post ANTRA Program" + ex.ToString());
                _objResponseModel.Status = false;
                _objResponseModel.Message = "validation Error";
            }
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        [ActionName("DeleteDotsDetails")]
        public HttpResponseMessage DeleteDotsDetails(DOTS A)
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
                        string ErrorMsg = SaveDotsDetails(A, 3);
                        if (ErrorMsg != "")
                        {
                            _objResponseModel.Status = false;
                            _objResponseModel.Message = ErrorMsg;
                        }
                        else
                        {
                            _objResponseModel.Status = true;
                            _objResponseModel.Message = "धन्यवाद, टीबी के उपचार का विवरण Delete हो चुका हैं।";
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
    }
}
