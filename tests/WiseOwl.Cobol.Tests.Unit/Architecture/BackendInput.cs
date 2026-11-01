// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Runtime.CompilerServices;

namespace WiseOwl.Cobol.Tests.Unit.Architecture;

/// <summary>
/// The BACKEND INPUT measured (DESIGN-backend-abstraction §1, §6; kb/Work PB4266): every type a code-generation backend
/// can reach from the one value <c>ICodeGenBackend.Emit</c> receives, <c>BoundCompilation</c>, inside the layers that ARE
/// the neutral model (<c>compiler.model</c> and <c>compiler.bound</c>, as <c>docs/rearchitecture/architecture-edges.json</c>
/// places their namespaces). A backend reaches a type by holding a value (a field of a value it holds), by calling a
/// member (what a method or property returns: <c>HostSurface.TypeOf</c>'s <c>HostType</c>) and by naming a type of the
/// layers outright (a static function, a visitor interface it implements, a constant), so the production walk starts from
/// every type of those layers, never from <c>BoundCompilation</c> alone (kb/Work PB4266 review R1-1). From each type it
/// follows every field's type (instance and static) and every member's return type, through arrays, by-refs and generic
/// arguments, includes every subtype of a reached type, and stops at a type outside the layers, recording the member that
/// reached it (<see cref="Exits"/>): the walk's exits are the seam's leaks into the binder and the parse tree (§1.3 L6),
/// which the edges file's tolerated rows ratchet.
/// </summary>
/// <param name="Types">Every type of the backend input.</param>
/// <param name="StringMembers">Every member of those types that carries or returns text: a field (instance or static)
/// whose type holds <see cref="string"/> (exactly, or as an element or generic argument), and a method or computed
/// property that returns such a type. A property is keyed by its name and a method by its name with <c>()</c>; an
/// auto-property is seen as its backing field, under the property's name.</param>
/// <param name="Exits">Each member whose type leaves the neutral layers, as <c>member : target type</c>.</param>
internal sealed record BackendInput(
    IReadOnlyList<Type> Types,
    IReadOnlyList<BackendMember> StringMembers,
    IReadOnlyList<BackendExit> Exits)
{
    /// <summary>Walk from <paramref name="roots"/>: <paramref name="inModel"/> says whether a type is part of the neutral
    /// model (descended into), and <paramref name="universe"/> holds the types searched for subtypes. A compiler-generated
    /// type (a closure, an iterator) is never part of it.</summary>
    public static BackendInput Measure(IEnumerable<Type> roots, Func<Type, bool> inModel, IReadOnlyCollection<Type> universe)
    {
        var seen = new HashSet<Type>();
        var order = new List<Type>();
        var queue = new Queue<Type>();
        var members = new List<BackendMember>();
        var exits = new List<BackendExit>();

        void Reach(Type type, string? via, bool viaField)
        {
            foreach (Type leaf in Leaves(type))
            {
                if (leaf.IsGenericParameter || leaf.IsPrimitive || leaf == typeof(string) || leaf == typeof(void) || IsCompilerGenerated(leaf))
                {
                    continue;
                }

                if (!inModel(leaf))
                {
                    if (via is not null)
                    {
                        exits.Add(new BackendExit(via, leaf, viaField));
                    }

                    continue;
                }

                if (seen.Add(leaf))
                {
                    order.Add(leaf);
                    queue.Enqueue(leaf);
                }
            }
        }

        foreach (Type root in roots)
        {
            Reach(root, null, viaField: false);
        }

        while (queue.Count > 0)
        {
            Type type = queue.Dequeue();
            if (!type.IsSealed)
            {
                foreach (Type sub in universe)
                {
                    if (sub != type && type.IsAssignableFrom(sub) && !IsCompilerGenerated(sub))
                    {
                        Reach(sub, null, viaField: false);
                    }
                }
            }

            if (type.BaseType is { } baseType)
            {
                Reach(baseType, null, viaField: false);
            }

            const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (FieldInfo field in type.GetFields(Declared | BindingFlags.Instance | BindingFlags.Static))
            {
                if (IsCompilerGenerated(field) && !IsBackingField(field))
                {
                    continue;
                }

                string key = KeyOf(type, PropertyNameOf(field));
                if (HoldsString(field.FieldType))
                {
                    members.Add(new BackendMember(key, type, IsMethod: false));
                }

                Reach(field.FieldType, key, viaField: true);
            }

            foreach (MethodInfo method in type.GetMethods(Declared | BindingFlags.Instance | BindingFlags.Static))
            {
                if (method.Name.StartsWith('<') || IsCompilerGenerated(method) || method.Name is nameof(ToString) or "PrintMembers")
                {
                    continue;
                }

                // What a member RETURNS is reached as a field's type is: a backend that calls a model function holds its
                // result (HostSurface.TypeOf's HostType), so a type reached only that way is on the input too.
                Reach(method.ReturnType, KeyOf(type, method.IsSpecialName && method.Name.StartsWith("get_", StringComparison.Ordinal)
                    ? method.Name[4..] : method.Name + "()"), viaField: false);
                if (!HoldsString(method.ReturnType))
                {
                    continue;
                }

                if (method.IsSpecialName)
                {
                    // A property getter: an auto-property is its backing field, already seen; a computed one is a member.
                    if (!method.Name.StartsWith("get_", StringComparison.Ordinal)
                        || type.GetField($"<{method.Name[4..]}>k__BackingField", Declared | BindingFlags.Instance | BindingFlags.Static) is not null)
                    {
                        continue;
                    }

                    members.Add(new BackendMember(KeyOf(type, method.Name[4..]), type, IsMethod: false));
                    continue;
                }

                members.Add(new BackendMember(KeyOf(type, method.Name + "()"), type, IsMethod: true));
            }
        }

        return new BackendInput(order,
            members.DistinctBy(m => m.Key).OrderBy(m => m.Key, StringComparer.Ordinal).ToList(),
            exits.DistinctBy(e => (e.Member, e.Target)).ToList());
    }

    /// <summary>The key a contract row names a member by: the declaring type's full name without the
    /// <c>WiseOwl.Cobol.Binding.</c> prefix both neutral layers share (<c>Model.DataItem.CobolName</c>,
    /// <c>Bound.BoundRelational.Op</c>, a nested type joined with <c>+</c>), then the member.</summary>
    public static string KeyOf(Type type, string member)
    {
        const string Prefix = "WiseOwl.Cobol.Binding.";
        string name = type.FullName ?? type.Name;
        return (name.StartsWith(Prefix, StringComparison.Ordinal) ? name[Prefix.Length..] : name) + "." + member;
    }

    /// <summary>The types a field's type stands for: an array's, by-ref's or pointer's element, a generic type's
    /// arguments (and its definition, which a product generic is), else the type itself.</summary>
    public static IEnumerable<Type> Leaves(Type type)
    {
        if (type.HasElementType)
        {
            foreach (Type leaf in Leaves(type.GetElementType()!))
            {
                yield return leaf;
            }

            yield break;
        }

        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            yield return type.GetGenericTypeDefinition();
            foreach (Type argument in type.GetGenericArguments())
            {
                foreach (Type leaf in Leaves(argument))
                {
                    yield return leaf;
                }
            }

            yield break;
        }

        yield return type;
    }

    private static bool HoldsString(Type type) => Leaves(type).Contains(typeof(string));

    private static bool IsBackingField(FieldInfo field) => field.Name.EndsWith(">k__BackingField", StringComparison.Ordinal);

    private static string PropertyNameOf(FieldInfo field) =>
        field.Name is ['<', .. var rest] && rest.IndexOf(">k__BackingField", StringComparison.Ordinal) is > 0 and var end
            ? rest[..end]
            : field.Name;

    private static bool IsCompilerGenerated(MemberInfo member) =>
        member.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);
}

/// <summary>A text-carrying member of the backend input.</summary>
/// <param name="Key">The member's contract key (<see cref="BackendInput.KeyOf"/>).</param>
/// <param name="Declaring">The type that declares it.</param>
/// <param name="IsMethod">Whether it is a method (a render member's shape) rather than a field or property.</param>
internal sealed record BackendMember(string Key, Type Declaring, bool IsMethod)
{
    /// <summary>The member's own name: the key's last segment, without a method's <c>()</c>.</summary>
    public string Name => Key[(Key.LastIndexOf('.') + 1)..].TrimEnd('(', ')');
}

/// <summary>A member of the backend input whose type lies outside the neutral layers: a field's type, or what a method or
/// property returns (<paramref name="IsField"/> false).</summary>
/// <param name="Member">The member's contract key (<see cref="BackendInput.KeyOf"/>).</param>
/// <param name="Target">The type outside the layers.</param>
/// <param name="IsField">Whether the member is a field (an auto-property's backing field included).</param>
internal sealed record BackendExit(string Member, Type Target, bool IsField);
