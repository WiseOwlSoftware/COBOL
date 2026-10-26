// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Exceptions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ AN ACTIVE-CLASS RETURNING ITEM DELIVERED THROUGH A UNIVERSAL INVOCATION IS CHECKED AGAINST THE INVOCATION'S DESCRIPTION,
/// NOT AGAINST THE OBJECT RETURNED (kb/Work PB2985). ISO §14.8.3.3 rule 2 b) 4. describes the sending operand of an
/// ACTIVE-CLASS item's SET by how the method was invoked — "any other object reference": "the sending operand has the same
/// description as that object reference" — and a universal INVOKE is invoked with a universal reference, so the sender is
/// universal and §14.9.39.3 SR10 / SR12 / SR14 (closed lists of senders) admit it only into a universal receiving item. Before
/// PB2985 the run time admitted every ACTIVE-CLASS item and the delivery's class narrowing was the only check, so a typed
/// receiving item took the object without the EC-OO-UNIVERSAL §14.9.23.4 GR7 c) sets. The relation is one
/// (<see cref="ActivationRelations.ActiveClassReturningViolation"/>), asked by two kinds of activated method: every emitted
/// <c>__CobolInvoke</c> case whose returning item is ACTIVE-CLASS, and the standard class BASE's New and FactoryObject.
/// </summary>
public sealed class ActiveClassReturningUniversalSenderTests
{
    private static ActivationDescription Described(ObjectReferenceKind kind, string? name = null, bool factory = false,
        bool only = false, string[]? classes = null, string[]? interfaces = null) =>
        new()
        {
            Shape = ActivationShape.ObjectReference, Category = ActivationCategory.Object, ObjectKind = kind,
            ObjectName = kind is ObjectReferenceKind.ActiveClass ? null : name, Factory = factory, Only = only,
            ObjectClasses = classes ?? (name is null ? null : [name]), ObjectInterfaces = interfaces ?? [],
        };

    private static readonly ActivationDescription ActiveInP = Described(ObjectReferenceKind.ActiveClass, "P");

    [Fact]
    public void AUniversalReceivingItem_Conforms()
    {
        Assert.Null(ActivationRelations.ActiveClassReturningViolation(Described(ObjectReferenceKind.Universal), ActiveInP));
    }

    [Theory]
    [InlineData(ObjectReferenceKind.ObjectClass, "P", "SR12")]       // the receiving item's class is the sender's own class
    [InlineData(ObjectReferenceKind.Interface, "I", "SR10")]
    [InlineData(ObjectReferenceKind.ActiveClass, "P", "SR14")]
    public void ATypedReceivingItem_DoesNotConform_WhateverTheClasses(ObjectReferenceKind kind, string name, string rule)
    {
        var receiving = kind is ObjectReferenceKind.Interface
            ? Described(kind, name, classes: null, interfaces: ["I"])
            : Described(kind, name);
        string? why = ActivationRelations.ActiveClassReturningViolation(receiving, ActiveInP);
        Assert.NotNull(why);
        Assert.Contains("§14.8.3.3 rule 2 b) 4.", why);
        Assert.Contains(rule, why);
    }

    [Fact]
    public void TheFactoryPhrases_AreNotWhatDecides()
    {
        // The sender is universal, so FACTORY presence (rule 2's last paragraph asks it of a class-name or ACTIVE-CLASS
        // sender only) changes neither verdict.
        var factoryActive = Described(ObjectReferenceKind.ActiveClass, "P", factory: true);
        Assert.Null(ActivationRelations.ActiveClassReturningViolation(
            Described(ObjectReferenceKind.Universal), factoryActive));
        Assert.NotNull(ActivationRelations.ActiveClassReturningViolation(
            Described(ObjectReferenceKind.ObjectClass, "P", factory: true), factoryActive));
    }

    [Fact]
    public void ARelationOfOtherPairs_IsSilent()
    {
        // Not ACTIVE-CLASS: rule 1 (the pair's own SET), ReturningViolation's.
        Assert.Null(ActivationRelations.ActiveClassReturningViolation(
            Described(ObjectReferenceKind.ObjectClass, "P"), Described(ObjectReferenceKind.ObjectClass, "P")));
        // Not an object reference on the receiving side: ReturningViolation reports the pair.
        Assert.Null(ActivationRelations.ActiveClassReturningViolation(
            new ActivationDescription { Shape = ActivationShape.Elementary, Category = ActivationCategory.Alphanumeric }, ActiveInP));
    }

    [Fact]
    public void TheCheck_IsRaisedOnlyWhenCheckingIsEnabledInBothTheMethodAndTheActivator()
    {
        var typed = Described(ObjectReferenceKind.ObjectClass, "P");
        RunUnit.Run(_ =>
        {
            ExceptionState.Clear();
            ExceptionState.OoUniversalChecking = true;
            // The activated method's half is off: no exception condition exists (§14.9.23.4 GR7 c) "both"), the
            // invocation proceeds, and the delivery's class narrowing is the floor.
            CobolObject.CheckActiveClassReturning(false, "INVOKE 'M'", typed, ActiveInP);

            ExceptionState.OoUniversalChecking = false;
            CobolObject.CheckActiveClassReturning(true, "INVOKE 'M'", typed, ActiveInP);

            ExceptionState.OoUniversalChecking = true;
            var raised = Assert.Throws<CobolFatalException>(
                () => CobolObject.CheckActiveClassReturning(true, "INVOKE 'M'", typed, ActiveInP));
            Assert.Equal("EC-OO-UNIVERSAL", raised.EcName);
            CobolObject.CheckActiveClassReturning(true, "INVOKE 'M'", Described(ObjectReferenceKind.Universal), ActiveInP);
        });
    }

    /// <summary>Every standard-class method that stores a RETURNING box asks the check first: a third such method would
    /// otherwise deliver its ACTIVE-CLASS item without it (the emitted cases are asked by
    /// <c>OoEmitter.EmitCobolInvokeCase</c> for each ACTIVE-CLASS returning item).</summary>
    [Fact]
    public void EveryStandardMethodThatStoresAReturningBox_AsksTheCheckFirst()
    {
        string source = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Runtime", "Control", "BASE.cs"));
        int stores = 0;
        for (int at = source.IndexOf("returning!.Value =", StringComparison.Ordinal); at >= 0;
             at = source.IndexOf("returning!.Value =", at + 1, StringComparison.Ordinal))
        {
            stores++;
            int check = source.LastIndexOf("CheckActiveClassReturning(", at, StringComparison.Ordinal);
            int arm = source.LastIndexOf("StandardMethodCrossing.Matches(", at, StringComparison.Ordinal);
            Assert.True(check > arm && arm >= 0,
                "a BASE method stores its RETURNING box without CheckActiveClassReturning after its match (§14.8.3.3 rule 2 b) 4.; kb/Work PB2985)");
        }
        Assert.Equal(2, stores);   // FactoryObject and New (§16.2)

        string emitter = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Verbs", "OoEmitter.cs"));
        Assert.Contains("RuntimeApi.ObjCheckActiveClassReturning(", emitter);
    }
}
