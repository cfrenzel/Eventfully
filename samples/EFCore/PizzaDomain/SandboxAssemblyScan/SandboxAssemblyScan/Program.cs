using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SandboxAssemblyScan
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }

        public static class AssemblyScanning
        {
            private static bool IsOpenGeneric(this Type type)
            {
                return type.GetTypeInfo().IsGenericTypeDefinition || type.GetTypeInfo().ContainsGenericParameters;
            }
            
            private static void ConnectImplementationsToTypesClosing(Type handlerInterface, IEnumerable<Assembly> assembliesToScan)
            {
                var concretions = new List<Type>();
                var interfaces = new List<Type>();
                foreach (var type in assembliesToScan.SelectMany(a => a.DefinedTypes).Where(t => !t.IsOpenGeneric()).Where(configuration.TypeEvaluator))
                {
                    var interfaceTypes = type.FindInterfacesThatClose(handlerInterface).ToArray();
                    if (!interfaceTypes.Any()) continue;

                    if (type.IsConcrete())
                    {
                        concretions.Add(type);
                    }

                    foreach (var interfaceType in interfaceTypes)
                    {
                        interfaces.Fill(interfaceType);
                    }
                }

                foreach (var @interface in interfaces)
                {
                    var exactMatches = concretions.Where(x => x.CanBeCastTo(@interface)).ToList();
                    if (addIfAlreadyExists)
                    {
                        foreach (var type in exactMatches)
                        {
                            services.AddTransient(@interface, type);
                        }
                    }
                    else
                    {
                        if (exactMatches.Count > 1)
                        {
                            exactMatches.RemoveAll(m => !IsMatchingWithInterface(m, @interface));
                        }

                        foreach (var type in exactMatches)
                        {
                            services.TryAddTransient(@interface, type);
                        }
                    }

                    if (!@interface.IsOpenGeneric())
                    {
                        AddConcretionsThatCouldBeClosed(@interface, concretions, services);
                    }
                }
            }

           
        }
    }
}