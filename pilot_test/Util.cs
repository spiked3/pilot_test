using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace pilot_test
{
    public static class Util
    {
        public static IEnumerable<string> GetMemberNames(object target, bool dynamicOnly = false)
        {
            var tList = new List<string>();
            if (!dynamicOnly)
            {
                tList.AddRange(target.GetType().GetProperties().Select(it => it.Name));
            }

            var tTarget = target as IDynamicMetaObjectProvider;
            if (tTarget != null)
            {
                tList.AddRange(tTarget.GetMetaObject(Expression.Constant(tTarget)).GetDynamicMemberNames());
            }
            return tList;
        }
    }
}
