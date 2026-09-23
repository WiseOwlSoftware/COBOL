// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Binding.Procedure;
using CobolNet.CodeGen;
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB971 — the *-ARG-OMITTED rule is ONE rule written three times, keyed to the kind of the activated
/// element that owns the formal (ISO §14.9.4.4 GR12 program, §8.4.3.2.4 GR8 function, §14.9.23.4 GR10 method).
/// Three tables have to agree for a reference to raise the RIGHT name under the RIGHT flag, and before PB971 they
/// did not: the binder queried EC-PROGRAM-ARG-OMITTED only for a CALL (so no referencing statement enabled it), a
/// function's carrier raised the PROGRAM name, and a method had no raise site at all. These facts pin them:
/// <list type="bullet">
/// <item>every <see cref="SourceElementKind"/> maps to an <see cref="ActivatedElementKind"/> (a sixth source-element
/// kind is a failure here, not a silently unchecked element);</item>
/// <item>the guard raises exactly <see cref="OmittedFormal.ConditionName"/> for its kind, armed by exactly the flag
/// <see cref="EcEmitter"/> sets for that name — the name the binder's per-statement query enables;</item>
/// <item>a PRESENT argument never raises, and checking off never raises (§14.6.13.1.1).</item>
/// </list>
/// </summary>
public sealed class OmittedFormalGuardDriftTests
{
    private static readonly Dictionary<string, string?> Gates =
        EcEmitter.FatalAmbientGates.ToDictionary(g => g.Ec, g => g.Flag, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void EverySourceElementKind_HasAnActivatedElementKind()
    {
        foreach (SourceElementKind k in Enum.GetValues<SourceElementKind>())
            _ = EcBinder.ElementKindOf(k);   // throws for an unmapped kind
        Assert.Equal(ActivatedElementKind.Method, EcBinder.ElementKindOf(SourceElementKind.MethodDefinition));
        Assert.Equal(ActivatedElementKind.Function, EcBinder.ElementKindOf(SourceElementKind.FunctionDefinition));
        Assert.Equal(ActivatedElementKind.Program, EcBinder.ElementKindOf(SourceElementKind.Program));
    }

    [Theory]
    [InlineData(ActivatedElementKind.Program, "EC-PROGRAM-ARG-OMITTED")]
    [InlineData(ActivatedElementKind.Function, "EC-FUNCTION-ARG-OMITTED")]
    [InlineData(ActivatedElementKind.Method, "EC-OO-ARG-OMITTED")]
    public void Guard_RaisesItsKindsName_UnderTheEmittersFlag(ActivatedElementKind kind, string expected)
    {
        string name = OmittedFormal.ConditionName(kind);
        Assert.Equal(expected, name);
        Assert.True(ExceptionCatalog.TryGet(name, out var info) && info.Fatality is not EcFatality.Nonfatal,
            $"{name} is Table 13 Fatal");
        Assert.True(Gates.TryGetValue(name, out string? flag) && flag is not null,
            $"EcEmitter has no flagged gate for {name} — a TURN could never arm the guard's raise");

        var exc = RunUnit.Current.Exceptions;
        var saved = exc.PushAllCheckingOff();
        try
        {
            string storage = "ABCD";
            // Checking off: the guard hands the storage back untouched and records nothing.
            Assert.Equal("ABCD", OmittedFormal.Ref(ref storage, omitted: true, kind, "X"));

            typeof(ExceptionEngine).GetProperty(flag!, BindingFlags.Public | BindingFlags.Instance)!.SetValue(exc, true);
            // A PRESENT argument never raises, checking on or off.
            Assert.Equal("ABCD", OmittedFormal.Ref(ref storage, omitted: false, kind, "X"));
            Assert.Same(exc, OmittedFormal.Carrier(exc, omitted: false, kind, "X"));

            var fatal = Assert.Throws<CobolFatalException>(() => OmittedFormal.Ref(ref storage, omitted: true, kind, "X"));
            Assert.Equal(name, fatal.EcName);
            fatal = Assert.Throws<CobolFatalException>(() => OmittedFormal.Carrier(exc, omitted: true, kind, "X"));
            Assert.Equal(name, fatal.EcName);
        }
        finally
        {
            exc.RestoreChecking(saved);
        }
    }
}
