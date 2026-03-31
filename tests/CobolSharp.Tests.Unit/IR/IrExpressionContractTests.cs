// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
//
// M001 Stage 4: Structural and integration tests verifying that the IR layer
// is fully decoupled from Semantics.Bound. These tests are the regression guard
// ensuring that BoundExpression never leaks back into the IR or CIL emitter.
using System.Reflection;
using CobolSharp.Compiler.CodeGen;
using CobolSharp.Compiler.IR;
using Xunit;

namespace CobolSharp.Tests.Unit.IR;

/// <summary>
/// Reflection-based structural tests: verify that no IR instruction type carries
/// BoundExpression properties and no CilEmitter method evaluates bound trees.
/// </summary>
public class IrExpressionContractTests
{
    private static readonly Assembly CompilerAssembly =
        typeof(IrInstruction).Assembly;

    private static readonly Type BoundExpressionType =
        CompilerAssembly.GetType("CobolSharp.Compiler.Semantics.Bound.BoundExpression")!;

    // ── Structural: IR types must not reference BoundExpression ──

    [Fact]
    public void No_IrInstruction_subclass_has_BoundExpression_property()
    {
        var irInstructionType = typeof(IrInstruction);
        var violations = new List<string>();

        foreach (var type in CompilerAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && irInstructionType.IsAssignableFrom(t)))
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (ReferencesBoundExpression(prop.PropertyType))
                    violations.Add($"{type.Name}.{prop.Name} : {prop.PropertyType.Name}");
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void No_IrLocation_subclass_has_BoundExpression_property()
    {
        var irLocationType = typeof(IrLocation);
        var violations = new List<string>();

        foreach (var type in CompilerAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && irLocationType.IsAssignableFrom(t)))
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (ReferencesBoundExpression(prop.PropertyType))
                    violations.Add($"{type.Name}.{prop.Name} : {prop.PropertyType.Name}");
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void No_IrExpression_subclass_has_BoundExpression_property()
    {
        var irExpressionType = typeof(IrExpression);
        var violations = new List<string>();

        foreach (var type in CompilerAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && irExpressionType.IsAssignableFrom(t)))
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (ReferencesBoundExpression(prop.PropertyType))
                    violations.Add($"{type.Name}.{prop.Name} : {prop.PropertyType.Name}");
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void No_ResolvedLocations_dictionary_in_any_IR_instruction()
    {
        var irInstructionType = typeof(IrInstruction);
        var violations = new List<string>();

        foreach (var type in CompilerAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && irInstructionType.IsAssignableFrom(t)))
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.Name.Contains("ResolvedLocations") || prop.Name.Contains("Locations"))
                {
                    if (ReferencesBoundExpression(prop.PropertyType))
                        violations.Add($"{type.Name}.{prop.Name}");
                }
            }
        }

        Assert.Empty(violations);
    }

    // ── Structural: CilEmitter must not have bound-tree evaluation methods ──

    [Fact]
    public void CilEmitter_has_no_EmitExpression_method()
    {
        var emitterType = CompilerAssembly.GetType("CobolSharp.Compiler.CodeGen.CilEmitter")!;
        var methods = emitterType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.Name == "EmitExpression")
            .ToList();

        Assert.Empty(methods);
    }

    [Fact]
    public void CilEmitter_has_no_EmitIntrinsicCall_method()
    {
        var emitterType = CompilerAssembly.GetType("CobolSharp.Compiler.CodeGen.CilEmitter")!;
        var methods = emitterType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.Name == "EmitIntrinsicCall")
            .ToList();

        Assert.Empty(methods);
    }

    [Fact]
    public void CilExpressionEmitter_has_EmitIrExpression_method()
    {
        // M003: EmitIrExpression moved from CilEmitter to CilExpressionEmitter
        var emitterType = CompilerAssembly.GetType("CobolSharp.Compiler.CodeGen.Emission.CilExpressionEmitter")!;
        var methods = emitterType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.Name == "EmitIrExpression")
            .ToList();

        Assert.Single(methods);
    }

    // ── Structural: Binder must not have PreResolveExpressionLocations ──

    [Fact]
    public void Binder_has_no_PreResolveExpressionLocations()
    {
        var binderType = typeof(CobolSharp.Compiler.CodeGen.Binder);
        var methods = binderType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.Name == "PreResolveExpressionLocations")
            .ToList();

        Assert.Empty(methods);
    }

    [Fact]
    public void ExpressionLowerer_has_LowerExpression_method()
    {
        var type = typeof(CobolSharp.Compiler.CodeGen.Lowering.ExpressionLowerer);
        var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance
            | BindingFlags.Public)
            .Where(m => m.Name == "LowerExpression")
            .ToList();

        Assert.Single(methods);
        Assert.Equal(typeof(IrExpression), methods[0].ReturnType);
    }

    // ── Structural: IR enums exist in IR namespace, not Bound ──

    [Fact]
    public void InspectTallyKind_exists_in_IR_namespace()
    {
        Assert.Equal("CobolSharp.Compiler.IR", typeof(InspectTallyKind).Namespace);
    }

    [Fact]
    public void InspectReplaceKind_exists_in_IR_namespace()
    {
        Assert.Equal("CobolSharp.Compiler.IR", typeof(InspectReplaceKind).Namespace);
    }

    [Fact]
    public void ClassConditionKind_exists_in_IR_namespace()
    {
        Assert.Equal("CobolSharp.Compiler.IR", typeof(ClassConditionKind).Namespace);
    }

    [Fact]
    public void IrCompareOp_exists_in_IR_namespace()
    {
        Assert.Equal("CobolSharp.Compiler.IR", typeof(IrCompareOp).Namespace);
    }

    // ── Helper ──

    private static bool ReferencesBoundExpression(Type type)
    {
        if (type == BoundExpressionType || type.IsSubclassOf(BoundExpressionType))
            return true;

        // Check generic type arguments (e.g., IReadOnlyList<BoundExpression>,
        // IReadOnlyDictionary<BoundExpression, IrLocation>)
        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
            {
                if (ReferencesBoundExpression(arg))
                    return true;
            }
        }

        return false;
    }
}
