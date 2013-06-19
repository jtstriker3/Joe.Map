using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Map
{
    public class DeleteAttribute : Attribute
    {
        public Boolean Cascade { get; set; }
        public Boolean Admin { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ViewMappingAttribute : Attribute
    {
        public String ColumnPropertyName { get; set; }
        public Boolean Admin { get; set; }
        public Boolean ReadOnly { get; set; }
        public Boolean OrderBy { get; set; }
        public Boolean Descending { get; set; }
        public Boolean Key { get; set; }
        public int OrderBySequence { get; set; }
        public Boolean WriteOnly { get; set; }
        public Boolean Include { get; set; }
        /// <summary>
        /// 0 based Depth of Recursion. Only Needed for Recursive Views
        /// Recusive Views only work on Models already loaded into memory
        /// </summary>
        public int MaxDepth { get; set; }
        /// <summary>
        /// Use if you have multiple Model Mappings for a single view
        /// </summary>
        public String Type { get; set; }
        public String Where { get; set; }
        /// <summary>
        /// Set to true if you are using Code First and do not have a view representing the Many To Many relationship table.
        /// </summary>
        public Boolean UseParentListForRelationships { get; set; }
        /// <summary>
        /// Use to Group the List By a Specific Property
        /// Remember to set your Property To a IEnumerable&lt;IGrouping&lt;TKey, TViewModel&gt;&gt;
        /// </summary>
        public String GroupBy { get; set; }
        public Boolean CreateNew { get; set; }
        /// <summary>
        /// Linq Supported Function to call on the last Property e.g. Sum
        /// </summary>
        public String LinqFunction { get; set; }
        /// <summary>
        /// Convert to True of false use "True:False" for flag values
        /// </summary>
        public String ToBoolean { get; set; }
        public Type OfType { get; set; }

        public ViewMappingAttribute()
        {
            CreateNew = true;
            MaxDepth = 10;
        }

        public ViewMappingAttribute(String propertyName)
            : this()
        {
            ColumnPropertyName = propertyName;
        }

    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = true)]
    public class ViewFilterAttribute : Attribute
    {
        public String Where { get; set; }
    }
}
