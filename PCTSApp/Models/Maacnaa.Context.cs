﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Objects;
    using System.Data.Objects.DataClasses;
    using System.Linq;
    
    public partial class MaacnaaEntities : DbContext
    {
        public MaacnaaEntities()
            : base("name=MaacnaaEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<ComplainSuggestion> ComplainSuggestions { get; set; }
        public DbSet<PehchanBirth> PehchanBirths { get; set; }
        public DbSet<SMS_Log> SMS_Log { get; set; }
        public DbSet<SMSDetails2020> SMSDetails2020 { get; set; }
        public DbSet<OTP> OTPs { get; set; }
        public DbSet<OtpLog> OtpLogs { get; set; }
        public DbSet<PinDetail> PinDetails { get; set; }
        public DbSet<UserTrailLog> UserTrailLogs { get; set; }
        public DbSet<MaaVideoDetail> MaaVideoDetails { get; set; }
        public DbSet<MaaToken> MaaTokens { get; set; }
    
        public virtual ObjectResult<uspANMASHADetailsByPCTSID_Result> uspANMASHADetailsByPCTSID(string perm_PCTSID)
        {
            var perm_PCTSIDParameter = perm_PCTSID != null ?
                new ObjectParameter("Perm_PCTSID", perm_PCTSID) :
                new ObjectParameter("Perm_PCTSID", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<uspANMASHADetailsByPCTSID_Result>("uspANMASHADetailsByPCTSID", perm_PCTSIDParameter);
        }
    
        public virtual ObjectResult<string> uspGetMobilenoForSendComplain(Nullable<int> perm_motherid)
        {
            var perm_motheridParameter = perm_motherid.HasValue ?
                new ObjectParameter("Perm_motherid", perm_motherid) :
                new ObjectParameter("Perm_motherid", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<string>("uspGetMobilenoForSendComplain", perm_motheridParameter);
        }
    
        public virtual ObjectResult<uspImmunizationSchedule_Result> uspImmunizationSchedule(Nullable<int> perm_infantid, Nullable<byte> perm_flag)
        {
            var perm_infantidParameter = perm_infantid.HasValue ?
                new ObjectParameter("Perm_infantid", perm_infantid) :
                new ObjectParameter("Perm_infantid", typeof(int));
    
            var perm_flagParameter = perm_flag.HasValue ?
                new ObjectParameter("Perm_flag", perm_flag) :
                new ObjectParameter("Perm_flag", typeof(byte));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<uspImmunizationSchedule_Result>("uspImmunizationSchedule", perm_infantidParameter, perm_flagParameter);
        }
    
        public virtual ObjectResult<uspInfantDetailsByInfantID_Result> uspInfantDetailsByInfantID(Nullable<int> perm_infantid)
        {
            var perm_infantidParameter = perm_infantid.HasValue ?
                new ObjectParameter("Perm_infantid", perm_infantid) :
                new ObjectParameter("Perm_infantid", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<uspInfantDetailsByInfantID_Result>("uspInfantDetailsByInfantID", perm_infantidParameter);
        }
    
        public virtual ObjectResult<uspPNCSchedule_Result> uspPNCSchedule(string perm_pctsid)
        {
            var perm_pctsidParameter = perm_pctsid != null ?
                new ObjectParameter("Perm_pctsid", perm_pctsid) :
                new ObjectParameter("Perm_pctsid", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<uspPNCSchedule_Result>("uspPNCSchedule", perm_pctsidParameter);
        }
    
        public virtual ObjectResult<uspWomendetail_Result> uspWomendetail(string perm_pctsid)
        {
            var perm_pctsidParameter = perm_pctsid != null ?
                new ObjectParameter("Perm_pctsid", perm_pctsid) :
                new ObjectParameter("Perm_pctsid", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<uspWomendetail_Result>("uspWomendetail", perm_pctsidParameter);
        }
    
        public virtual ObjectResult<MaaImmunizationSchedule_Result> MaaImmunizationSchedule(Nullable<int> perm_infantid, Nullable<byte> perm_flag)
        {
            var perm_infantidParameter = perm_infantid.HasValue ?
                new ObjectParameter("Perm_infantid", perm_infantid) :
                new ObjectParameter("Perm_infantid", typeof(int));
    
            var perm_flagParameter = perm_flag.HasValue ?
                new ObjectParameter("Perm_flag", perm_flag) :
                new ObjectParameter("Perm_flag", typeof(byte));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<MaaImmunizationSchedule_Result>("MaaImmunizationSchedule", perm_infantidParameter, perm_flagParameter);
        }
    
        public virtual ObjectResult<uspInfantlistForImmunizationByPCTSID_Result> uspInfantlistForImmunizationByPCTSID(string perm_pctsid)
        {
            var perm_pctsidParameter = perm_pctsid != null ?
                new ObjectParameter("Perm_pctsid", perm_pctsid) :
                new ObjectParameter("Perm_pctsid", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<uspInfantlistForImmunizationByPCTSID_Result>("uspInfantlistForImmunizationByPCTSID", perm_pctsidParameter);
        }
    
        public virtual ObjectResult<uspGetMotherDetailsByMobileno_Result> uspGetMotherDetailsByMobileno(string perm_mobileno, string deviceID)
        {
            var perm_mobilenoParameter = perm_mobileno != null ?
                new ObjectParameter("Perm_mobileno", perm_mobileno) :
                new ObjectParameter("Perm_mobileno", typeof(string));
    
            var deviceIDParameter = deviceID != null ?
                new ObjectParameter("DeviceID", deviceID) :
                new ObjectParameter("DeviceID", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<uspGetMotherDetailsByMobileno_Result>("uspGetMotherDetailsByMobileno", perm_mobilenoParameter, deviceIDParameter);
        }
    
        public virtual ObjectResult<uspANCSchedule_Result> uspANCSchedule(string perm_pctsid)
        {
            var perm_pctsidParameter = perm_pctsid != null ?
                new ObjectParameter("Perm_pctsid", perm_pctsid) :
                new ObjectParameter("Perm_pctsid", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<uspANCSchedule_Result>("uspANCSchedule", perm_pctsidParameter);
        }
    
        public virtual ObjectResult<UspMaaAdvanceSearch_Result> UspMaaAdvanceSearch(string pctsid)
        {
            var pctsidParameter = pctsid != null ?
                new ObjectParameter("pctsid", pctsid) :
                new ObjectParameter("pctsid", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<UspMaaAdvanceSearch_Result>("UspMaaAdvanceSearch", pctsidParameter);
        }
    
        public virtual ObjectResult<uspNearestFacility_Result> uspNearestFacility(string perm_unitcode)
        {
            var perm_unitcodeParameter = perm_unitcode != null ?
                new ObjectParameter("Perm_unitcode", perm_unitcode) :
                new ObjectParameter("Perm_unitcode", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<uspNearestFacility_Result>("uspNearestFacility", perm_unitcodeParameter);
        }
    
        public virtual ObjectResult<uspMamtaCard_Result> uspMamtaCard(string perm_pctsID)
        {
            var perm_pctsIDParameter = perm_pctsID != null ?
                new ObjectParameter("Perm_pctsID", perm_pctsID) :
                new ObjectParameter("Perm_pctsID", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<uspMamtaCard_Result>("uspMamtaCard", perm_pctsIDParameter);
        }
    
        public virtual ObjectResult<uspInfantlistForBirthCertificatByPCTSID_Result> uspInfantlistForBirthCertificatByPCTSID(string perm_pctsid)
        {
            var perm_pctsidParameter = perm_pctsid != null ?
                new ObjectParameter("Perm_pctsid", perm_pctsid) :
                new ObjectParameter("Perm_pctsid", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<uspInfantlistForBirthCertificatByPCTSID_Result>("uspInfantlistForBirthCertificatByPCTSID", perm_pctsidParameter);
        }
    }
}