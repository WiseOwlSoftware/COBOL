// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace WiseOwl.Cobol.Binding.Repository;

/// <summary>
/// ⛔ WHAT AN EXPANSION MEANS, AS A NUMBER (ISO §9.3.12: two classes "created by expanding the same parameterized class with the same
/// actual parameters are the same class instance"; DESIGN-external-repository §10.5; kb/Work PB2104). The key is a SHA-256, in lower-case
/// hexadecimal, over the canonical encoding (§6.5) of everything that decides what the expansion IS: the definition's identity, the
/// skeleton (its source, formals with their kinds, <see cref="CompileState"/> and resolutions), the record of the class or interface each
/// resolution names, and the identity and record of each actual. Bytes, module version ids, file names and the compiler's version are not
/// in it, so a rebuild by a later compiler with nothing else changed keeps its key, and two expansions with one key are the same class
/// text over the same published surfaces of everything it names.
/// <para>A named class's record enters in its SURFACE form: <c>Expansion</c> removed and an expansion's kind read as a class's, so the
/// record a group computes for its own class equals the record read back from the assembly that published it, and an actual that is
/// itself an expansion contributes what it is and not how it was placed. A word that only names (a unit's, a formal's, a data item's)
/// is dropped from every record: no comparator reads one (§6.3), and the CLR names that code is generated against stay in.</para>
/// </summary>
internal static class ExpansionKey
{
    /// <summary>What a class or interface named by a resolution or an actual contributes: its identity, and its record when the
    /// repository or the group holds one (the standard class BASE and an unanswered identity hold none).</summary>
    public readonly record struct Named(string Identity, RepositoryRecord? Record);

    /// <summary>The key of the expansion of <paramref name="definitionIdentity"/> made from <paramref name="skeleton"/> with
    /// <paramref name="resolutions"/> (what the skeleton's own specifiers name, in the skeleton's order) and <paramref name="actuals"/>,
    /// and the labeled digest of each component the key is made of (<c>label=digest</c>): the expansion publishes them beside its key, so
    /// a later compilation that finds the expansion stale can say which component changed (REPO-9, §10.5).</summary>
    public static (string Key, IReadOnlyList<string> Parts) Of(string definitionIdentity, SkeletonRecord skeleton,
        IReadOnlyList<Named> resolutions, IReadOnlyList<Named> actuals)
    {
        var parts = new List<string>
        {
            Part("definition", definitionIdentity),
            Part("skeleton source", skeleton.Source, skeleton.Formals, skeleton.FormalIsInterface),
            Part("compile state", skeleton.State),
        };
        for (int i = 0; i < resolutions.Count; i++) parts.Add(Part($"resolution {skeleton.Resolutions[i].Word}", skeleton.Resolutions[i], Surface(resolutions[i])));
        foreach (var actual in actuals) parts.Add(Part($"actual {actual.Identity}", Surface(actual)));
        return (Digest(string.Join("|", parts)), parts);
    }

    /// <summary>The label of the first component in which <paramref name="mine"/> and <paramref name="theirs"/> differ, or "its key"
    /// when the two lists are not of one shape (a different number of resolutions or actuals).</summary>
    public static string FirstDifference(IReadOnlyList<string> mine, IReadOnlyList<string> theirs)
    {
        for (int i = 0; i < Math.Min(mine.Count, theirs.Count); i++)
            if (mine[i] != theirs[i]) return mine[i][..mine[i].IndexOf('=', StringComparison.Ordinal)];
        return "its key";
    }

    private static string Part(string label, params object?[] components)
    {
        var text = new StringBuilder();
        foreach (object? component in components) Canonical.Write(text, component);
        return $"{label}={Digest(text.ToString())}";
    }

    private static string Digest(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    private static object Surface(Named n) => (n.Identity, n.Record is null ? null : Normalized(n.Record));

    /// <summary>The surface form of a named class's or interface's record: no expansion identity, a class's kind.</summary>
    internal static RepositoryRecord Normalized(RepositoryRecord record) => record with
    {
        Expansion = null,
        Kind = record.Kind is Runtime.Repository.RepositoryUnitKind.Expansion
            ? (record.Class?.FactoryType is null ? Runtime.Repository.RepositoryUnitKind.Interface : Runtime.Repository.RepositoryUnitKind.Class)
            : record.Kind,
    };

    /// <summary>The canonical text of a record graph: positional records in the order their members are declared, lists in order, strings
    /// quoted with their length, no member named <c>Name</c> (a word that only names).</summary>
    private static class Canonical
    {
        private static readonly Dictionary<Type, PropertyInfo[]> Members = [];

        public static void Write(StringBuilder text, object? value)
        {
            switch (value)
            {
                case null: text.Append('~'); break;
                case string s: text.Append('"').Append(s.Length).Append(':').Append(s).Append('"'); break;
                case bool b: text.Append(b ? 'T' : 'F'); break;
                case char c: text.Append('\'').Append((int)c).Append('\''); break;
                case Enum e: text.Append(e.GetType().Name).Append('=').Append(Convert.ToInt64(e, System.Globalization.CultureInfo.InvariantCulture)); break;
                case int or long or short or byte: text.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)); break;
                case IEnumerable list:
                    text.Append('[');
                    foreach (object? item in list) { Write(text, item); text.Append(','); }
                    text.Append(']');
                    break;
                default:
                    var type = value.GetType();
                    text.Append('{').Append(type.Name);
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTuple<,>))
                        foreach (var field in type.GetFields()) { text.Append(';'); Write(text, field.GetValue(value)); }
                    else
                        foreach (var member in MembersOf(type)) { text.Append(';').Append(member.Name).Append('='); Write(text, member.GetValue(value)); }
                    text.Append('}');
                    break;
            }
        }

        private static PropertyInfo[] MembersOf(Type type)
        {
            lock (Members)
            {
                if (!Members.TryGetValue(type, out var members))
                    Members[type] = members = [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.Name != "Name" && p.GetIndexParameters().Length == 0 && p.GetMethod is not null)];
                return members;
            }
        }
    }
}
