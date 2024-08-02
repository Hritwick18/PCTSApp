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
using Newtonsoft.Json;
using System.Data;

namespace PCTSApp.Controllers
{
    public class DHSController : ApiController
    {
        private DHSurveyEntities dhs = new DHSurveyEntities();
        private rajmedicalEntities dhsrajmed = new rajmedicalEntities();
        public HttpResponseMessage GetAlltables()
        {
            ResponseModel _objResponseModel = new ResponseModel();
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

            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }
        public HttpResponseMessage PostDataByLoginUnitID(TableList tl)
        {
            ResponseModel _objResponseModel = new ResponseModel();
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

            var p1 = dhsrajmed.UnitMasters.Where(x => x.UnitCode == tl.LoginUnitCode).FirstOrDefault();
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
            ashaList = dhsrajmed.AshaMasters.Where(x => x.unitid == tl.LoginUnitId && x.Status==1).Select(x => new DHS_AshaMaster
            {
                AshaName = x.AshaName,
                unitid = tl.LoginUnitId,
                ashaAutoID = (int)x.ashaAutoID,
                LastUpdated = x.LastUpdated,
                Status = x.Status,
                type = x.type
            }).ToList();
            int AnganwariNo = 0;
            if (tl.ANMAutoID > 0)
            {
                ashaList = ashaList.Where(x => x.ashaAutoID == tl.ANMAutoID).ToList();
                var p2 = dhsrajmed.ASHA_AddlCharge.Where(x => x.AshaAutoID == tl.ANMAutoID && x.MainAnganwari == 1).FirstOrDefault();
                if (p2 != null)
                {
                    AnganwariNo = p2.AnganwariNo;
                }
            }

         
           

            villagesList = dhsrajmed.Villages.Where(x => x.unitid == tl.LoginUnitId && x.OtherLocation == 1).Select(x => new DHS_Villages
            {
                UnitCode = x.UnitCode,
                VillageName = x.VillageName,
                UnitNameHindi = x.UnitNameHindi,
                type = x.type,
                unitid = x.unitid,
                VillageAutoID = x.VillageAutoID,
                LastUpdated = x.LastUpdated
            }).ToList();
            if (tl.LastSyncDate != null)
            {
                villagesList = villagesList.Where(x => x.LastUpdated >= tl.LastSyncDate).ToList();
            }
            
            if (tl.LastSyncDate != null)
            {
                ashaList = ashaList.Where(x => x.LastUpdated >= tl.LastSyncDate).ToList();
            }
            anganList = dhsrajmed.AnganwariMasters.Where(x => x.unitid == tl.LoginUnitId).Select(x => new DHS_AnganwariMaster
            {
                AnganwariNo = x.AnganwariNo,
                NameE = x.NameE,
                NameH = x.NameH,
                unitid = tl.LoginUnitId,
                LastUpdated = x.LastUpdated,
                AWCID = x.AWCID
            }).ToList();
            if (tl.LastSyncDate != null)
            {
                anganList = anganList.Where(x => x.LastUpdated >= tl.LastSyncDate).ToList();
            }

            if (tl.ANMAutoID > 0)
            {
                anganList = anganList.Where(x => x.AnganwariNo == AnganwariNo).ToList();
            }

            anganvillageList = (from a in dhsrajmed.AnganwariMasters
                                join av in dhsrajmed.Anganwari_Village on a.AnganwariNo equals av.AnganwariNo
                                where a.unitid == tl.LoginUnitId
                                select new DHS_Anganwari_Village
                                {
                                    AnganwariNo = av.AnganwariNo,
                                    VillageAutoID = av.VillageAutoID
                                }
                          ).ToList();
            if (tl.ANMAutoID > 0)
            {
                anganvillageList = anganvillageList.Where(x => x.AnganwariNo == AnganwariNo).ToList();

                villagesList = (from a in anganvillageList
                                join av in villagesList on a.VillageAutoID equals av.VillageAutoID
                                select new DHS_Villages
                                {
                                    UnitCode = av.UnitCode,
                                    VillageName = av.VillageName,
                                    UnitNameHindi = av.UnitNameHindi,
                                    type = av.type,
                                    unitid = av.unitid,
                                    VillageAutoID = av.VillageAutoID,
                                    LastUpdated =av.LastUpdated
                                }
                          ).ToList();

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
            return Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        }


        public HttpResponseMessage PostOfflineData(DHS_TableData temp)
        {
            ResponseModel _objResponseModel = new ResponseModel();
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
                    MemberDetail p = dhs.MemberDetails.Where(x => x.AnganwariNo == tempMd.AnganwariNo && x.HouseFamilyID == tempMd.HouseFamilyID && x.AadhaarNo==tempMd.AadhaarNo).FirstOrDefault();
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

                        if (tempMd.Fatherid == 0){
                            p.Fatherid = null;
                        }
                        else{
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
                        MemberID = dhs.MemberDetails.Where(x => x.HouseFamilyID == tempMd.HouseFamilyID &&  x.AadhaarNo == tempMd.AadhaarNo).Select(x => x.MemberID).FirstOrDefault();
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
    }
 
  




}