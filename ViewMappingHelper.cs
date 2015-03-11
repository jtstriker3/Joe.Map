using Joe.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Map
{
    public class ViewMappingHelper
    {
        public readonly string _key = "6a14c3fb-cc65-4030-b675-02f45c2a9986-Property_Cache";

        Type Model { get; set; }
        public PropertyInfo PropInfo { get; set; }
        private ViewMappingAttribute _attr = null;
        private IEnumerable<ViewFilterAttribute> ViewFilters { get; set; }
        //private static IDictionary<String, String> _propInfoCache = new Dictionary<String, String>();

        public ViewMappingHelper(PropertyInfo info, Type model)
        {
            Model = model;
            PropInfo = info;
            if (info.PropertyType.IsGenericType)
                ViewFilters = info.PropertyType.GetGenericArguments().First().GetCustomAttributes(typeof(ViewFilterAttribute), true).Cast<ViewFilterAttribute>();
            else
                ViewFilters = new List<ViewFilterAttribute>();
        }

        public ViewMappingHelper(ViewMappingAttribute attribute)
        {
            _attr = attribute;
            ViewFilters = new List<ViewFilterAttribute>();
        }

        public ViewMappingAttribute ViewMapping
        {
            get
            {
                if (_attr == null)
                {
                    //Get All Valid Entries for This Possible View
                    var caList = this.PropInfo.GetCustomAttributes(typeof(ViewMappingAttribute), true).Cast<ViewMappingAttribute>().Where(vm =>
                        vm.Type != null
                        && vm.Type.FullName == (Model != null ? Model.FullName : String.Empty));
                    var defaultAttribute = this.PropInfo.GetCustomAttributes(typeof(ViewMappingAttribute), true).Cast<ViewMappingAttribute>().Where(vm =>
                      vm.Type == null);

                    if (caList.Count() > 1 || defaultAttribute.Count() > 1)
                        throw new Exception("There can be only One attribute per Type and One default attribute.");

                    ViewMappingAttribute attr = null;
                    if (caList.Count() == 1)
                        attr = (ViewMappingAttribute)caList.SingleOrDefault();
                    else if (defaultAttribute.Count() == 1)
                        attr = defaultAttribute.Single();
                    else if (caList.Count() == 0 && Model != null && GetModelPropertyName != null)
                        attr = new ViewMappingAttribute();
                    else if (IsEntityKey() && Model == null)
                        attr = new ViewMappingAttribute();
                    if (attr != null)
                        if (String.IsNullOrEmpty(attr.ColumnPropertyName) && GetModelPropertyName != null)
                            attr.ColumnPropertyName = GetModelPropertyName;

                    if (attr != null)
                        attr.Key = IsEntityKey() || attr.Key;
                    _attr = attr;
                }

                return _attr;
            }

        }

        public Boolean HasGroupBy
        {
            get { return !String.IsNullOrEmpty(ViewMapping.GroupBy); }
        }

        public Boolean HasCompare
        {
            get
            {
                return !String.IsNullOrEmpty(ViewMapping.ToBoolean);
            }
        }

        public Boolean HasLinqFunction
        {
            get
            {
                return !String.IsNullOrEmpty(ViewMapping.LinqFunction);
            }
        }

        public Boolean HasTransform
        {
            get
            {
                return HasCompare || HasGroupBy || HasLinqFunction;
            }
        }

        public Boolean HasOfType
        {
            get
            {
                return _attr.OfType != null;
            }
        }

        public Boolean HasMapFunction
        {
            get
            {
                return !String.IsNullOrWhiteSpace(this.ViewMapping.MapFunction);
            }
        }

        public Boolean HasModelWhere
        {
            get
            {
                return this.ViewMapping.ModelWhere != null || this.ViewFilters.Where(vf => vf.ModelWhere != null).Count() > 0;
            }
        }

        public Boolean HasWhere
        {
            get
            {
                return this.ViewMapping.Where != null || this.ViewFilters.Where(vf => vf.Where != null).Count() > 0;
            }
        }

        public Boolean CanSelect
        {
            get
            {
                return !(this.HasLinqFunction || this.HasWhere);
            }
        }

        public Type GetOfType()
        {
            return _attr.OfType;
        }

        public String GetLinqFunction()
        {
            return ViewMapping.LinqFunction.Replace("()", string.Empty);
        }

        public String GetCompareTrue()
        {
            var first = ViewMapping.ToBoolean.Split(':').First();
            switch (first.ToLower())
            {
                case "null":
                    return null;
                case "!null":
                    return "$NotNull";
                default:
                    return first;

            }
        }

        public String GetCompareFalse()
        {

            var last = ViewMapping.ToBoolean.Split(':').Last();
            switch (last.ToLower())
            {
                case "null":
                    return null;
                case "!null":
                    return "NotNull";
                default:
                    return last;

            }
        }

        public String GetModelWhere()
        {
            var viewFitlerString = String.Join(":and:", this.ViewFilters.Select(vf => vf.ModelWhere));

            if (viewFitlerString == String.Empty)
                viewFitlerString = null;

            if (viewFitlerString != null && this.ViewMapping.ModelWhere != null)
                return this.ViewMapping.ModelWhere + ":and:" + viewFitlerString;
            else if (viewFitlerString != null && this.ViewMapping.ModelWhere == null)
                return viewFitlerString;

            return this.ViewMapping.ModelWhere;
        }

        public String GetWhere()
        {
            var viewFitlerString = String.Join(":and:", this.ViewFilters.Select(vf => vf.Where));

            if (viewFitlerString == String.Empty)
                viewFitlerString = null;

            if (viewFitlerString != null && this.ViewMapping.Where != null)
                return this.ViewMapping.Where + ":and:" + viewFitlerString;
            else if (viewFitlerString != null && this.ViewMapping.Where == null)
                return viewFitlerString;

            return this.ViewMapping.Where;
        }

        private Boolean IsEntityKey()
        {
            if (Model != null)
                return PropInfo.Name.ToLower() == "id" || PropInfo.Name.ToLower() == Model.Name.ToLower() + "id";
            return PropInfo.Name.ToLower() == "id";
        }

        private String _modelPropertyName = null;

        private String GetModelPropertyName
        {
            get
            {

                if (_modelPropertyName == null && Model != null)
                {
                    //var key = Model.FullName.GetHashCode() + "_" + Model.Name + PropInfo.Name;

                    Delegate getPropInfo = (Func<Type, String, String>)((Type model, String propertyName) =>
                    {
                        if (model.GetProperty(propertyName) != null)
                            return propertyName;
                        else
                            for (int i = 1; i < propertyName.Length; i++)
                            {
                                var infoName = propertyName;
                                infoName = infoName.Insert(i, ".");
                                if (ReflectionHelper.TryGetEvalPropertyInfo(model, infoName) != null)
                                    return infoName;
                            }

                        return null;
                    });

                    _modelPropertyName = (String)Joe.Caching.Cache.Instance.GetOrAdd(_key, TimeSpan.MaxValue, getPropInfo, Model, PropInfo.Name);


                }
                return _modelPropertyName;
            }
        }
    }
}
