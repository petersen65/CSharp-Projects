﻿//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IOT.DataModel
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    internal partial class InternetOfThingsContext : DbContext
    {
        public InternetOfThingsContext()
            : base("name=InternetOfThingsContext")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<Thing> Things { get; set; }
        public DbSet<Partition> Partitions { get; set; }
        internal DbSet<CommandTopic> CommandTopics { get; set; }
    }
}