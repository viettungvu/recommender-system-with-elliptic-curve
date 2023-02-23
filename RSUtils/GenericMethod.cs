using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace RSUtils
{
    public static class GenericMethod
    {
        public static IEnumerable<T> Add<T>(this IEnumerable<T> list, T value)
        {
            try
            {
                if(list != null && value != null)
                {
                    list.Concat(new T[] { value });
                }
                return list;
            }
            catch(Exception)
            {
                throw;
            }
        }

        public static string GetDisplayName(this Enum e) => ((IEnumerable<MemberInfo>)e.GetType().GetMember(e.ToString())).First<MemberInfo>().GetCustomAttribute<DisplayAttribute>().GetName();
    }
}
