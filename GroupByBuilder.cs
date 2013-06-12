using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Map
{
    class GroupByBuilder
    {
        private GroupBy GroupByProp { get; set; }
        private Type ViewModel { get; set; }
        private Boolean LinqToSql { get; set; }
        private Expression Right { get; set; }
        ParameterExpression ParameterExpression { get; set; }

        public GroupByBuilder(String groupByProperty, Type viewModel, Boolean linqToSql, Expression right)
        {
            Right = Expression.Call(typeof(Queryable), "AsQueryable", new[] { viewModel }, right);
            this.ViewModel = viewModel;
            ParameterExpression = Expression.Parameter(this.ViewModel, this.ViewModel.Name);
            LinqToSql = linqToSql;
            GroupByProp = this.BuildGroupBy(groupByProperty);
        }

        public class GroupBy
        {
            public PropertyInfo Property { get; set; }
            public Expression PropertyExpression { get; set; }
        }

        private GroupBy BuildGroupBy(String groupByProperty)
        {
            var groupBy = new GroupBy();
            Expression propEx = null;
            var outModel = ViewModel;
            groupBy.Property = ExpressionHelpers.ParseProperty(LinqToSql, ParameterExpression, ref propEx, ref  outModel, groupByProperty);
            groupBy.PropertyExpression = propEx;
            return groupBy;
        }

        public Expression BuildGroupByClause()
        {
            Expression ex = Expression.Lambda(GroupByProp.PropertyExpression, new ParameterExpression[] { ParameterExpression });
            return Expression.Call(typeof(Queryable), "GroupBy", new Type[] { ViewModel, GroupByProp.Property.PropertyType }, Right, ex);
        }


    }

}
