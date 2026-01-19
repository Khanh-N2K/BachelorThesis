using System;
using System.Collections.Generic;
using System.Linq;

namespace N2K
{
    public static class SubclassTypeCache
    {
        private static readonly Dictionary<Type, Type[]> _cache = new();

        public static Type[] GetDerivedTypes(Type baseType)
        {
            if (baseType == null)
                return Array.Empty<Type>();

            if (_cache.TryGetValue(baseType, out var cached))
                return cached;

            // 🔥 Scan ONCE per base type
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t =>
                    !t.IsAbstract &&
                    !t.IsGenericType &&
                    baseType.IsAssignableFrom(t) &&
                    t != baseType)
                .ToArray();

            _cache[baseType] = types;
            return types;
        }
    }
}
