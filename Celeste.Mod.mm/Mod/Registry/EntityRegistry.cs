using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Celeste.Mod.Registry;

public static class EntityRegistry {
    private static readonly Dictionary<string, HashSet<Type>> SidToTypes = new();
    private static readonly Dictionary<Type, HashSet<string>> TypeToSids = new();
    
    private static readonly HashSet<Type> EmptyTypeSet = new();
    private static readonly HashSet<string> EmptyStringSet = new();

    /// <summary>
    /// Gets a set of all known C# types associated with the given entity sid.
    /// Might not necessarily be exhaustive, for example if entities from that sid have not been instantiated yet.
    /// </summary>
    public static IReadOnlySet<Type> GetKnownTypesFromSid(string sid) => SidToTypes.GetValueOrDefault(sid) ?? EmptyTypeSet;
    
    /// <summary>
    /// Gets a set of all known sids associated with the given C# type.
    /// Might not necessarily be exhaustive, for example if entities from that type have not been instantiated yet.
    /// </summary>
    public static IReadOnlySet<string> GetKnownSidsFromType(Type type) => TypeToSids.GetValueOrDefault(type) ?? EmptyStringSet;
    
    internal static void RegisterSidToTypeConnection(string sid, Type type) {
        ref var sidToTypeEntry = ref CollectionsMarshal.GetValueRefOrAddDefault(SidToTypes, sid, out _);
        sidToTypeEntry ??= new(1);
        sidToTypeEntry.Add(type);
        
        ref var typeToSidsEntry = ref CollectionsMarshal.GetValueRefOrAddDefault(TypeToSids, type, out _);
        typeToSidsEntry ??= new(1);
        typeToSidsEntry.Add(sid);
    }
}