// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using WiseOwl.Cobol.Compiler.Binding.Model;
using WiseOwl.Cobol.Compiler.CodeGen;
using WiseOwl.Cobol.Compiler.CodeGen.DataDivision;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE GROUP IMAGE CODEC IS THREE JOBS IN THREE TYPES (kb/Work PB2337; docs/rearchitecture/DESIGN-architecture-review.md
/// §8.3, census R0-0042).
/// <para>The extract split one 1,400-line type into the run-time image a record struct carries (<c>GroupImageCodec</c>:
/// <c>AsImage</c>/<c>FromImage</c>, the variable-length group's current image, carrier and record layout), the
/// compile-time INITIAL image a byte-addressed area starts at (<c>ImageInitializer</c>), and the USAGE BIT faces
/// (<c>BitImageCodec</c>). What keeps the seams true, without a list of member names that a rename would outlive:</para>
/// <list type="bullet">
/// <item>the run-time codec reads no VALUE clause: it declares no member, and takes no constructor parameter, of the
/// seed's types (<see cref="ValueInitializer"/>, <see cref="SeedRecipe"/>, <see cref="Subscripts"/>), so a second
/// seeder cannot grow back inside it;</item>
/// <item>the initializer emits no member of the record struct: no method of it takes a <see cref="CodeWriter"/>;</item>
/// <item>the bit codec holds no state, and the stored implicit bit filler (<see cref="BitFiller"/>, kb/Work PB3564) is
/// read and written by it alone among the three, so the two faces and the run packing cannot each place it.</item>
/// </list>
/// </summary>
public sealed class GroupImageCodecBoundaryDriftTests
{
    private const BindingFlags Everything =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    /// <summary>True when <paramref name="t"/> is, or is built from (a type argument or an element), <paramref name="target"/>.</summary>
    private static bool Mentions(Type t, Type target) =>
        t == target
        || (t.HasElementType && Mentions(t.GetElementType()!, target))
        || (t.IsGenericType && t.GetGenericArguments().Any(a => Mentions(a, target)));

    /// <summary>Every type a declared member of <paramref name="owner"/> names: field and property types, method and
    /// constructor parameters and returns, and the same for its nested types (the record structs it declares).</summary>
    private static IEnumerable<(string Member, Type Type)> DeclaredTypes(Type owner)
    {
        foreach (var member in owner.GetMembers(Everything))
        {
            var types = member switch
            {
                FieldInfo f => [f.FieldType],
                PropertyInfo p => [p.PropertyType],
                MethodInfo m => m.GetParameters().Select(x => x.ParameterType).Append(m.ReturnType).ToArray(),
                ConstructorInfo c => c.GetParameters().Select(x => x.ParameterType).ToArray(),
                _ => Array.Empty<Type>(),
            };
            foreach (var t in types) yield return ($"{owner.Name}.{member.Name}", t);
        }
        foreach (var nested in owner.GetNestedTypes(Everything))
            foreach (var d in DeclaredTypes(nested)) yield return d;
    }

    private static List<string> Offenders(Type owner, params Type[] forbidden) =>
        DeclaredTypes(owner)
            .SelectMany(d => forbidden.Where(f => Mentions(d.Type, f)).Select(f => $"{d.Member} mentions {f.Name}"))
            .Distinct().ToList();

    [Fact]
    public void TheRunTimeCodec_ReadsNoValueClause()
    {
        Assert.True(DeclaredTypes(typeof(GroupImageCodec)).Count() >= 20,
            "the scan found almost nothing on GroupImageCodec — the reflection no longer reaches its members");
        var offenders = Offenders(typeof(GroupImageCodec), typeof(ValueInitializer), typeof(SeedRecipe), typeof(Subscripts));
        Assert.True(offenders.Count == 0,
            "GroupImageCodec names a type of the compile-time seed — the INITIAL image is ImageInitializer's, and the "
            + "run-time codec composes only what a record struct holds now:\n  " + string.Join("\n  ", offenders));
    }

    [Fact]
    public void TheInitializer_EmitsNoMember()
    {
        var offenders = Offenders(typeof(ImageInitializer), typeof(CodeWriter));
        Assert.True(offenders.Count == 0,
            "ImageInitializer takes a CodeWriter — it composes initial-value EXPRESSIONS; the members a record struct "
            + "carries are emitted by GroupImageCodec and BitImageCodec:\n  " + string.Join("\n  ", offenders));
    }

    [Fact]
    public void TheBitCodec_IsStatelessAndAloneOwnsTheStoredBitFiller()
    {
        Assert.True(typeof(BitImageCodec) is { IsAbstract: true, IsSealed: true },
            "BitImageCodec is no longer a static class — its faces are functions of the description alone");
        Assert.Contains(DeclaredTypes(typeof(BitImageCodec)), d => Mentions(d.Type, typeof(BitFiller)));
        var offenders = Offenders(typeof(GroupImageCodec), typeof(BitFiller))
            .Concat(Offenders(typeof(ImageInitializer), typeof(BitFiller))).ToList();
        Assert.True(offenders.Count == 0,
            "a stored bit filler is handled outside BitImageCodec — the faces and the run packing read and write it "
            + "there alone (kb/Work PB3564):\n  " + string.Join("\n  ", offenders));
    }
}
