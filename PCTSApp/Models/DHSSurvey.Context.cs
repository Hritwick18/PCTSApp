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
    
    public partial class DHSurveyEntities : DbContext
    {
        public DHSurveyEntities()
            : base("name=DHSurveyEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<ChildInformation> ChildInformations { get; set; }
        public DbSet<DHSMasterCode> DHSMasterCodes { get; set; }
        public DbSet<DiseaseDetail> DiseaseDetails { get; set; }
        public DbSet<ECdetail> ECdetails { get; set; }
        public DbSet<HouseFamily> HouseFamilies { get; set; }
        public DbSet<MemberDetail> MemberDetails { get; set; }
        public DbSet<MemberPhoto> MemberPhotoes { get; set; }
        public DbSet<UnitMaster> UnitMasters { get; set; }
        public DbSet<UnMarriedGirlsDetail> UnMarriedGirlsDetails { get; set; }
    
        public virtual ObjectResult<GetUnitmasterData_Result> GetUnitmasterData(string perm_Loginunitcode)
        {
            var perm_LoginunitcodeParameter = perm_Loginunitcode != null ?
                new ObjectParameter("Perm_Loginunitcode", perm_Loginunitcode) :
                new ObjectParameter("Perm_Loginunitcode", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<GetUnitmasterData_Result>("GetUnitmasterData", perm_LoginunitcodeParameter);
        }
    
        public virtual ObjectResult<uspGetAllTables_Result> uspGetAllTables()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<uspGetAllTables_Result>("uspGetAllTables");
        }
    }
}
