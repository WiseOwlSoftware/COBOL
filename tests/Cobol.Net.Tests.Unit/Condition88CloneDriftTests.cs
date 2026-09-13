// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections;
using System.Reflection;
using CobolNet.Binding.Model;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DRIFT CHECK THAT KEEPS <see cref="Condition88.CopyOnto"/> EXHAUSTIVE (kb/Work PB555).
/// <para>A level-88 condition-name is COPIED onto a different conditional variable in two places — a TYPEDEF
/// clone (ISO §13.18.58.4 GR1, "<i>the condition-names are part of the type</i>") and a SAME AS copy
/// (§13.18.49.4 GR2a) — and the copier used to be a hand-written field list at the call site. It copied the
/// VALUE set and nothing else, so a clone silently lost the <c>IN alphabet-name-1</c> phrase: §14.7.8 rule 2's
/// collating sequence reverted to the program default, and every THRU range the clone tested could answer
/// differently from the template's. That is a SILENT WRONG ANSWER produced by a field being ADDED to the model
/// and not to the copier — the failure this class exists to make loud.</para>
/// <para><b>How it fails.</b> The test populates EVERY writable instance member of <see cref="Condition88"/>
/// reflectively with a distinctive value, copies it, and demands the copy carry each one. Add a field to
/// <see cref="Condition88"/> and forget <c>CopyOnto</c> and this test names the field. <see cref="Condition88.Parent"/>
/// is the ONE exclusion, and it is excluded BY NAME rather than by a general rule: the whole point of the copy
/// is that the clone tests the clone's storage.</para>
/// </summary>
public sealed class Condition88CloneDriftTests
{
    private static DataItem Item(string name) => new()
    {
        Level = 1,
        CobolName = name,
        CsName = name,
    };

    /// <summary>Every writable instance member of <see cref="Condition88"/> — settable properties plus
    /// collection-valued read-only ones, which are "written" by adding to them. <see cref="Condition88.Parent"/>
    /// is excluded by name (see the class remarks).</summary>
    private static IEnumerable<PropertyInfo> CarriedMembers() =>
        typeof(Condition88).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name != nameof(Condition88.Parent))
            .Where(p => p.CanWrite || typeof(IEnumerable).IsAssignableFrom(p.PropertyType) && p.PropertyType != typeof(string));

    [Fact]
    public void CopyOnto_CarriesEveryConstituentOfFormat3()
    {
        var template = Item("TEMPLATE");
        var clone = Item("CLONE");

        // Every Format-3 operand-bearing constituent, each with a value that could not appear by accident.
        var src = new Condition88
        {
            Name = "CN-DRIFT",
            Parent = template,
            Alphabet = "DRIFT-ALPHABET",
            FalseValue = "\"DRIFT-FALSE\"",
        };
        src.Values.Add(("\"LO\"", "\"HI\""));
        src.Values.Add(("\"SINGLE\"", null));

        var copy = src.CopyOnto(clone);

        Assert.Same(clone, copy.Parent);
        Assert.Equal(src.Name, copy.Name);
        Assert.Equal(src.Alphabet, copy.Alphabet);
        Assert.Equal(src.FalseValue, copy.FalseValue);
        Assert.Equal(src.Values, copy.Values);
        Assert.NotSame(src.Values, copy.Values);   // the clone owns its list — a shared one is a later aliasing bug
    }

    /// <summary>The drift half: NO writable member of <see cref="Condition88"/> may be left at its default by
    /// <see cref="Condition88.CopyOnto"/> when the source has it set. Reflective, so a NEW field fails here
    /// without anyone remembering to extend the assertion list above.</summary>
    [Fact]
    public void CopyOnto_LeavesNoWritableMemberBehind()
    {
        var src = new Condition88 { Name = "CN-REFLECT", Parent = Item("TEMPLATE") };
        var populated = new List<string>();

        foreach (var p in CarriedMembers())
        {
            if (p.CanWrite && p.PropertyType == typeof(string))
            {
                p.SetValue(src, "REFLECT-" + p.Name);
                populated.Add(p.Name);
            }
            else if (p.GetValue(src) is IList list && p.PropertyType.IsGenericType)
            {
                // The one collection today is List<(string, string?)>; add one element through the ValueTuple
                // constructor so the check does not depend on the element type's spelling.
                var elem = p.PropertyType.GetGenericArguments()[0];
                list.Add(Activator.CreateInstance(elem, "REFLECT-LOW", "REFLECT-HIGH"));
                populated.Add(p.Name);
            }
            else if (p.CanWrite)
            {
                Assert.Fail($"Condition88.{p.Name} is a writable member of a type this drift test does not know "
                    + $"how to populate ({p.PropertyType}). Extend this test AND Condition88.CopyOnto — a member "
                    + "the copier drops is the silent wrong answer kb/Work PB555 swept.");
            }
        }

        Assert.NotEmpty(populated);
        var copy = src.CopyOnto(Item("CLONE"));

        foreach (var p in CarriedMembers())
        {
            object? want = p.GetValue(src), got = p.GetValue(copy);
            if (want is IList wl)
                Assert.True(got is IList gl && gl.Count == wl.Count,
                    $"Condition88.CopyOnto dropped the collection '{p.Name}' — add it to CopyOnto.");
            else
                Assert.True(Equals(want, got),
                    $"Condition88.CopyOnto dropped '{p.Name}' (expected {want}, got {got}) — add it to CopyOnto. "
                    + "A copier that silently stops being exhaustive is how a TYPEDEF clone lost its "
                    + "IN alphabet-name-1 phrase and re-answered every THRU range (kb/Work PB555).");
        }
    }
}
