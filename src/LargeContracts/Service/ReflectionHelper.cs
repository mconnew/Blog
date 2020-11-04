using System;
using System.Collections.Generic;
using System.Reflection;

namespace ServiceHelper
{
    internal static class ReflectionHelper
    {
        private static Dictionary<string, Type> s_getTypeCache = new Dictionary<string, Type>();
        private static Dictionary<string, Dictionary<string, MemberInfo>> s_getMemberCache = new Dictionary<string, Dictionary<string, MemberInfo>>();
        private static Dictionary<string, Dictionary<string, MethodInfo>> s_getMethodCache = new Dictionary<string, Dictionary<string, MethodInfo>>();

        internal static Type GetType(string typeName)
        {
            Type result;
            if (s_getTypeCache.TryGetValue(typeName, out result))
            {
                return result;
            }

            result = Type.GetType(typeName);
            s_getTypeCache.Add(typeName, result);
            return result;
        }

        internal static MemberInfo GetMember(string typeName, string memberInfoName)
        {
            Dictionary<string, MemberInfo> membersCache;
            MemberInfo result;
            if (!s_getMemberCache.TryGetValue(typeName, out membersCache))
            {
                var type = GetType(typeName);
                var members = type.GetMembers();
                membersCache = new Dictionary<string, MemberInfo>();
                foreach (var member in members)
                {
                    membersCache[member.Name] = member;
                }
                s_getMemberCache[typeName] = membersCache;
            }

            membersCache.TryGetValue(memberInfoName, out result);
            return result;
        }

        internal static MethodInfo GetMethod(string typeName, string methodInfoName)
        {
            Dictionary<string, MethodInfo> methodsCache;
            MethodInfo result;
            if (!s_getMethodCache.TryGetValue(typeName, out methodsCache))
            {
                var type = GetType(typeName);
                var methods = type.GetMethods();
                methodsCache = new Dictionary<string, MethodInfo>();
                foreach (var method in methods)
                {
                    methodsCache[method.Name] = method;
                }
                s_getMethodCache[typeName] = methodsCache;
            }

            methodsCache.TryGetValue(methodInfoName, out result);
            return result;
        }
    }
}
