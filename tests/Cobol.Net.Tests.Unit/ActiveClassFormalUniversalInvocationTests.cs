// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ AN ACTIVE-CLASS FORMAL HANDED AN ARGUMENT BY A UNIVERSAL INVOCATION IS A CONFORMANCE VIOLATION OF THE INVOCATION, NOT OF
/// THE ARGUMENT (kb/Work PB2985, the formal half of <see cref="ActiveClassReturningUniversalSenderTests"/>). ISO §14.8.2.3.2
/// rule 4 admits an ACTIVE-CLASS formal only when "the method to be activated shall be invoked with the predefined object
/// references SELF or SUPER, or with an object reference described with the ACTIVE-CLASS phrase" (4 a)) or "with that
/// object-class-name or with an object reference described with that object-class-name and the ONLY phrase" (4 b)); a
/// universal invoking reference is none of them. §14.9.23.4 GR7 c) makes the violation EC-OO-UNIVERSAL when checking is
/// enabled in both the activated method and the activating runtime element. Before PB2985 the run-time lane asked only
/// §9.3.6 3 d) 4./5. (the argument's VALUE, <see cref="ActivationRelations.Matches"/>) and never rule 4.
/// </summary>
public sealed class ActiveClassFormalUniversalInvocationTests
{
    private static ActivationDescription Described(ObjectReferenceKind kind, string? name = null, bool factory = false,
        bool only = false) =>
        new()
        {
            Shape = ActivationShape.ObjectReference, Category = ActivationCategory.Object, ObjectKind = kind,
            ObjectName = kind is ObjectReferenceKind.ActiveClass ? null : name, Factory = factory, Only = only,
            ObjectClasses = name is null ? null : [name], ObjectInterfaces = [],
        };

    private static readonly ActivationDescription ActiveFormal = Described(ObjectReferenceKind.ActiveClass, "P");

    [Theory]
    [InlineData(ObjectReferenceKind.Universal, null, false, false)]       // the argument of a universal invoke
    [InlineData(ObjectReferenceKind.ActiveClass, "P", false, false)]      // rule 4 a)'s argument — the INVOCATION still fails
    [InlineData(ObjectReferenceKind.ObjectClass, "P", false, true)]       // rule 4 b)'s argument — likewise
    public void AnObjectReferenceArgument_IsAViolation_WhateverItsDescription(
        ObjectReferenceKind kind, string? name, bool factory, bool only)
    {
        string? why = ActivationRelations.ActiveClassFormalViolation(Described(kind, name, factory, only), ActiveFormal);
        Assert.NotNull(why);
        Assert.Contains("§14.8.2.3.2 rule 4", why);
    }

    [Fact]
    public void TheFactoryFormal_IsTheSameViolation()
    {
        Assert.NotNull(ActivationRelations.ActiveClassFormalViolation(
            Described(ObjectReferenceKind.Universal), Described(ObjectReferenceKind.ActiveClass, "P", factory: true)));
    }

    [Fact]
    public void ARelationOfOtherPairs_IsSilent()
    {
        // Not ACTIVE-CLASS: §14.8.2.3.2 rules 1-3, ParameterViolation's (and §9.3.6 3 d) 1.-3.'s match).
        Assert.Null(ActivationRelations.ActiveClassFormalViolation(
            Described(ObjectReferenceKind.Universal), Described(ObjectReferenceKind.Universal)));
        Assert.Null(ActivationRelations.ActiveClassFormalViolation(
            Described(ObjectReferenceKind.ObjectClass, "P"), Described(ObjectReferenceKind.ObjectClass, "P")));
        // An OMITTED argument has no description to conform (§9.3.6 3 b)); an argument of another shape never matched.
        Assert.Null(ActivationRelations.ActiveClassFormalViolation(
            new ActivationDescription { Shape = ActivationShape.Omitted }, ActiveFormal));
        Assert.Null(ActivationRelations.ActiveClassFormalViolation(
            new ActivationDescription { Shape = ActivationShape.Elementary, Category = ActivationCategory.Alphanumeric },
            ActiveFormal));
    }

    [Fact]
    public void TheCheck_IsRaisedOnlyWhenCheckingIsEnabledInBothTheMethodAndTheActivator()
    {
        var argument = Described(ObjectReferenceKind.Universal);
        RunUnit.Run(_ =>
        {
            ExceptionState.Clear();
            ExceptionState.OoUniversalChecking = true;
            // The activated method's half is off: no exception condition exists (§14.9.23.4 GR7 c) "both") and the
            // invocation proceeds.
            CobolObject.CheckActiveClassFormal(false, "INVOKE 'M'", argument, ActiveFormal);

            ExceptionState.OoUniversalChecking = false;
            CobolObject.CheckActiveClassFormal(true, "INVOKE 'M'", argument, ActiveFormal);

            ExceptionState.OoUniversalChecking = true;
            var raised = Assert.Throws<CobolFatalException>(
                () => CobolObject.CheckActiveClassFormal(true, "INVOKE 'M'", argument, ActiveFormal));
            Assert.Equal("EC-OO-UNIVERSAL", raised.EcName);
        });
    }

    /// <summary>The emitted <c>__CobolInvoke</c> case asks the check for each ACTIVE-CLASS formal, as it does for an
    /// ACTIVE-CLASS returning item (one invocation, both halves), and nothing else spells the call.</summary>
    [Fact]
    public void TheEmittedCase_AsksTheCheckForEachActiveClassFormal()
    {
        string emitter = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "OoEmitter.cs"));
        Assert.Contains("RuntimeApi.ObjCheckActiveClassFormal(", emitter);
        Assert.Contains("RuntimeApi.ObjCheckActiveClassReturning(", emitter);
    }
}
