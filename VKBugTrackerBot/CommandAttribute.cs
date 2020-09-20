using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace VKBugTrackerBot
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public String Command { get; }
        public Boolean AdminOnly { get; }

        public CommandAttribute(String cmd, Boolean adminOnly = false)
        {
            Command = cmd;
            AdminOnly = adminOnly;
        }

        public static IEnumerable<MethodInfo> FindMethods<TContext>(String command, Boolean hasAdmin = false, Assembly assembly = null)
            => FindMethods(typeof(CommandsModule<TContext>), command, hasAdmin, assembly);
        public static IEnumerable<MethodInfo> FindMethods(Type type, String command, Boolean hasAdmin = false, Assembly assembly = null)
        {
            if (assembly == null) assembly = Assembly.GetEntryAssembly();
            var methods = assembly.GetTypes()
                .Where(t => t.IsClass && t.BaseType == type)
                .Select(c => c.GetMethods()
                    .Where(m => m.GetCustomAttribute<CommandAttribute>()?.Command == command
                        && (!hasAdmin || m.GetCustomAttribute<CommandAttribute>().AdminOnly)))
                .SelectMany(e => e);
            return methods;
        }

        public static Boolean? HasAdminPermissionsRequired(MethodInfo method)
        {
            return method.GetCustomAttribute<CommandAttribute>()?.AdminOnly;
        }

        public static ConstructorInfo GetConstructorInfo(MethodInfo method)
        {
            // // Debug code:
            // var dt = method.DeclaringType;
            // var ctors = dt.GetConstructors();
            // var new_ctors = new List<ConstructorInfo>();
            // foreach (var ctor in ctors)
            // {
            //     var ps = ctor.GetParameters();
            //     if (ps.Count() != 1) continue;
            //     var pst = ps[0].ParameterType;
            //     var gas = dt.BaseType.GetGenericArguments();
            //     var ga = gas.FirstOrDefault();
            //     if (pst != ga) continue;
            //     new_ctors.Add(ctor);
            // }
            // return new_ctors.FirstOrDefault();
            return method.DeclaringType.GetConstructors().Where(c => c.GetParameters().Count() == 1 &&
                c.GetParameters()[0].ParameterType == method.DeclaringType.BaseType.GetGenericArguments().FirstOrDefault()).FirstOrDefault();
        }
    }
}
