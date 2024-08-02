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
    
    public partial class OfflineCnaaEntities : DbContext
    {
        public OfflineCnaaEntities()
            : base("name=OfflineCnaaEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<ANCDetails_Offline> ANCDetails_Offline { get; set; }
        public DbSet<DeliveryDetails_Offline> DeliveryDetails_Offline { get; set; }
        public DbSet<HBPNC_Offline> HBPNC_Offline { get; set; }
        public DbSet<ImmunizationMaster_Offline> ImmunizationMaster_Offline { get; set; }
        public DbSet<Infant_Offline> Infant_Offline { get; set; }
        public DbSet<InfantImmu_Offline> InfantImmu_Offline { get; set; }
        public DbSet<MasterCodes_Offline> MasterCodes_Offline { get; set; }
        public DbSet<UnitMaster_Offline> UnitMaster_Offline { get; set; }
        public DbSet<Villages_Offline> Villages_Offline { get; set; }
        public DbSet<Immunization_Offline> Immunization_Offline { get; set; }
        public DbSet<ANCRegDetail_Offline> ANCRegDetail_Offline { get; set; }
        public DbSet<Mother_Offline> Mother_Offline { get; set; }
        public DbSet<AshaMaster_Offline> AshaMaster_Offline { get; set; }
        public DbSet<AnganwariMaster_Offline> AnganwariMaster_Offline { get; set; }
    
        public virtual ObjectResult<uspGetAllTables_Result> uspGetAllTables()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<uspGetAllTables_Result>("uspGetAllTables");
        }
    
        public virtual ObjectResult<uspGetANCData_Result> uspGetANCData(Nullable<int> perm_unitid, Nullable<System.DateTime> perm_lastSyncDate)
        {
            var perm_unitidParameter = perm_unitid.HasValue ?
                new ObjectParameter("Perm_unitid", perm_unitid) :
                new ObjectParameter("Perm_unitid", typeof(int));
    
            var perm_lastSyncDateParameter = perm_lastSyncDate.HasValue ?
                new ObjectParameter("Perm_lastSyncDate", perm_lastSyncDate) :
                new ObjectParameter("Perm_lastSyncDate", typeof(System.DateTime));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<uspGetANCData_Result>("uspGetANCData", perm_unitidParameter, perm_lastSyncDateParameter);
        }
    
        public virtual ObjectResult<uspGetHBPNCData_Result> uspGetHBPNCData(Nullable<int> perm_unitid, Nullable<System.DateTime> perm_lastSyncDate)
        {
            var perm_unitidParameter = perm_unitid.HasValue ?
                new ObjectParameter("Perm_unitid", perm_unitid) :
                new ObjectParameter("Perm_unitid", typeof(int));
    
            var perm_lastSyncDateParameter = perm_lastSyncDate.HasValue ?
                new ObjectParameter("Perm_lastSyncDate", perm_lastSyncDate) :
                new ObjectParameter("Perm_lastSyncDate", typeof(System.DateTime));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<uspGetHBPNCData_Result>("uspGetHBPNCData", perm_unitidParameter, perm_lastSyncDateParameter);
        }
    
        public virtual ObjectResult<uspGetInfantData_Result> uspGetInfantData(Nullable<int> perm_unitid, Nullable<int> perm_year1, Nullable<int> perm_year2, Nullable<System.DateTime> perm_lastSyncDate)
        {
            var perm_unitidParameter = perm_unitid.HasValue ?
                new ObjectParameter("Perm_unitid", perm_unitid) :
                new ObjectParameter("Perm_unitid", typeof(int));
    
            var perm_year1Parameter = perm_year1.HasValue ?
                new ObjectParameter("Perm_year1", perm_year1) :
                new ObjectParameter("Perm_year1", typeof(int));
    
            var perm_year2Parameter = perm_year2.HasValue ?
                new ObjectParameter("Perm_year2", perm_year2) :
                new ObjectParameter("Perm_year2", typeof(int));
    
            var perm_lastSyncDateParameter = perm_lastSyncDate.HasValue ?
                new ObjectParameter("Perm_lastSyncDate", perm_lastSyncDate) :
                new ObjectParameter("Perm_lastSyncDate", typeof(System.DateTime));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<uspGetInfantData_Result>("uspGetInfantData", perm_unitidParameter, perm_year1Parameter, perm_year2Parameter, perm_lastSyncDateParameter);
        }
    }
}