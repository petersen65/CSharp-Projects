//------------------------------------------------------------------------------
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
    using System.Collections.Generic;
    
    public partial class CommandTopic
    {
        public int Id { get; internal set; }
        public int RelativeId { get; internal set; }
        public int MaximumSubscription { get; internal set; }
        public int CurrentSubscription { get; internal set; }
        public int PartitionId { get; internal set; }
    
        public virtual Partition Partition { get; internal set; }
    }
}