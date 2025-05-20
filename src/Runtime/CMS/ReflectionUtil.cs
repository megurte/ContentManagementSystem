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
    
    /// <summary>
    /// Use this to create new instance of entity component definition
    /// </summary>
    /// <param name="self"></param>
    /// <typeparam name="T"></typeparam>
    public static T DeepCopy<T>(this T self) where T : class
    {
        if (self == null)
            return null;

        var type = self.GetType();
        var copy = Activator.CreateInstance(type);

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var value = field.GetValue(self);

            if (value is ICloneable cloneable)
                field.SetValue(copy, cloneable.Clone());
            else
                field.SetValue(copy, value);
        }

        return (T)copy;
    }
}