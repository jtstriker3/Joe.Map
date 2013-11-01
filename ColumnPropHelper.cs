using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Map
{
    public class ColumnPropHelper
    {
        private String ColumnProperty { get; set; }

        public ColumnPropHelper(String columnProp)
        {
            ColumnProperty = columnProp ?? String.Empty;
        }

        public Boolean IsSwitch
        {
            get
            {
                return ColumnProperty.ToLower().StartsWith("switch:");
            }
        }

        public String SwitchProperty
        {
            get
            {
                var switchProp = ColumnProperty.Remove(0, 7);
                return switchProp.Remove(switchProp.IndexOf(':'), switchProp.Length - switchProp.IndexOf(':'));
            }
        }

        private String CasesStr
        {
            get
            {
                return ColumnProperty.Replace(SwitchProperty + ":", String.Empty).Replace("switch:", String.Empty);
            }
        }

        public IEnumerable<Case> Cases
        {
            get
            {
                var cases = CasesStr.Split(':');
                for (int i = 0; i < cases.Length; i += 3)
                {
                    yield return new Case(cases[i + 1], cases[i + 2]);
                }
            }
        }

        public class Case
        {
            public String Constant { get; private set; }
            public String PropertyString { get; private set; }

            public Case(String constant, String propertyString)
            {
                Constant = constant;
                PropertyString = propertyString;
            }
        }

        public String GetSwitchProperty(Object model)
        {
            var value = Reflection.ReflectionHelper.GetEvalProperty(model, this.SwitchProperty);
            foreach (Case c in this.Cases)
            {
                if (Convert.ChangeType(c.Constant, value.GetType()) == value)
                    return c.PropertyString;
            }
            return null;
        }
    }
}
