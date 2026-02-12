using System;
using NUnit.Framework;

namespace Runtime
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class FilterTagsAttribute : PropertyAttribute
    {
        public Type[] TagTypes { get; }

        public FilterTagsAttribute(params Type[] tagTypes)
        {
            TagTypes = tagTypes;
        }
    }
}