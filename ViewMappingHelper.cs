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
        Type Model { get; set; }
        public PropertyInfo PropInfo { get; set; }
        private ViewMappingAttribute _attr = null;

        public ViewMappingHelper(PropertyInfo info, Type model)
        {
            Model = model;
            PropInfo = info;
        }

        public ViewMappingHelper(ViewMappingAttribute attribute)
        {
            _attr = attribute;
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
                    if (Model.GetProperty(PropInfo.Name) != null)
                        _modelPropertyName = PropInfo.Name;
                    else
                        for (int i = 1; i < PropInfo.Name.Length; i++)
                        {
                            var infoName = PropInfo.Name;
                            infoName = infoName.Insert(i, ".");
                            if (ReflectionHelper.TryGetEvalPropertyInfo(Model, infoName) != null)
                                _modelPropertyName = infoName;

                        }
                }
                return _modelPropertyName;
            }
        }
    }
}
