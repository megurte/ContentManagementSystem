using System;
using System.Linq;
using System.Reflection;

public static class ReflectionUtil
{
    public static Type[] FindAllSubclasses<T>()
    {
        Type baseType = typeof(T);
        Assembly assembly = Assembly.GetAssembly(baseType);

        Type[] types = assembly.GetTypes();
        Type[] subclasses = types.Where(type => type.IsSubclassOf(baseType) && !type.IsAbstract).ToArray();

        return subclasses;
    }
    
    public static Type[] FindAllSubclassesIncludingInterfaces<T>()
    {
        var baseType = typeof(T);
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(asm => asm.GetTypes())
            .Where(t => baseType.IsAssignableFrom(t) && t != baseType)
            .ToArray();
    }
}