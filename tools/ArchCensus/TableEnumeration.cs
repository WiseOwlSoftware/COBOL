// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WiseOwl.Cobol.Tools.ArchCensus;

/// <summary>
/// ⛔ THE TABLE-ENUMERATION RULE (kb/Work PB2744; DESIGN-architecture-review §8.7 "The test-only judgment", item (a)): a
/// member that only tests use, and that exposes a data table's rows or its size, is the drift tests' observation surface
/// over that data and KEEPS, so <see cref="Reachability"/> reports it <c>table-enumeration</c> rather than
/// <c>test-only</c>, and no Delete wave is proposed against it. Two shapes qualify, judged from the declaration alone:
/// <list type="number">
/// <item><b>the rows</b>: a STATIC read-only field or get-only property whose type is a collection (an array or an
/// <see cref="System.Collections.IEnumerable"/>, never a string) — <c>DiagnosticCatalog.All</c>,
/// <c>ExceptionCatalog.Level3Rows</c>, <c>Table19.StandardModes</c>;</item>
/// <item><b>the size</b>: a get-only integral property named <c>*Count</c> whose whole getter is the
/// <c>Count</c>, <c>Length</c> or <c>*Count</c> of a field or property of its own type — <c>ZonedSign.TableCount</c>
/// (<c>Tables.Length</c>), <c>CollationTable.MappingCount</c> (<c>_singles.Count</c>),
/// <c>GraphemeBreaker.RangeCount</c> (<c>s_table.Value.RangeCount</c>).</item>
/// </list>
/// An instance collection (<c>PagedFile.FreePages</c>), a counter field read back (<c>FrameIndex.WindowCount</c>), a
/// count computed by a loop (<c>CollationKey.LevelCount</c>) and a method with arguments are not a table's enumeration:
/// they stay <c>test-only</c> for §8.7's items (b) and (c) to judge. <see cref="RuleFixtures"/> pins each arm.
/// </summary>
internal static class TableEnumeration
{
    /// <summary>The member names a size's getter may end in besides <c>*Count</c>.</summary>
    private static readonly HashSet<string> SizeNames = new(StringComparer.Ordinal) { "Count", "Length", "LongLength" };

    /// <summary>True when <paramref name="member"/> is a table's rows or its size (the class summary's two shapes).</summary>
    public static bool Is(ISymbol member) => member switch
    {
        IFieldSymbol field => field.IsStatic && field.IsReadOnly && IsCollection(field.Type),
        IPropertySymbol { IsIndexer: false, SetMethod: null } property =>
            (property.IsStatic && IsCollection(property.Type)) || IsTableSize(property),
        _ => false,
    };

    private static bool IsCollection(ITypeSymbol type) =>
        type.SpecialType != SpecialType.System_String
        && (type is IArrayTypeSymbol
            || type.SpecialType == SpecialType.System_Collections_IEnumerable
            || type.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_Collections_IEnumerable));

    private static bool IsTableSize(IPropertySymbol property) =>
        property.Name.EndsWith("Count", StringComparison.Ordinal)
        && property.Type.SpecialType is SpecialType.System_Int32 or SpecialType.System_Int64
        && property.DeclaringSyntaxReferences.Length == 1
        && property.DeclaringSyntaxReferences[0].GetSyntax() is PropertyDeclarationSyntax declaration
        && GetterExpression(declaration) is { } expression
        && IsSizeOfOwnMember(expression, property.ContainingType);

    /// <summary>The getter's whole body when it is one expression (<c>=&gt; e</c>, <c>get =&gt; e;</c> or
    /// <c>get { return e; }</c>), else null.</summary>
    private static ExpressionSyntax? GetterExpression(PropertyDeclarationSyntax declaration)
    {
        if (declaration.ExpressionBody is { } arrow)
        {
            return arrow.Expression;
        }

        AccessorDeclarationSyntax? get = declaration.AccessorList?.Accessors.SingleOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
        return get switch
        {
            { ExpressionBody: { } body } => body.Expression,
            { Body.Statements: [ReturnStatementSyntax { Expression: { } returned }] } => returned,
            _ => null,
        };
    }

    /// <summary>True when <paramref name="expression"/> is <c>a.….Size</c> or <c>a.….Count()</c>, its size name one of
    /// <see cref="SizeNames"/> or a <c>*Count</c>, and its leftmost name <c>a</c> (or <c>this.a</c>) a field or property
    /// of <paramref name="owner"/>.</summary>
    private static bool IsSizeOfOwnMember(ExpressionSyntax expression, INamedTypeSymbol owner)
    {
        if (expression is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0, Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Count" } called })
        {
            expression = called;
        }

        if (expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: var size } access
            || !(SizeNames.Contains(size) || size.EndsWith("Count", StringComparison.Ordinal)))
        {
            return false;
        }

        ExpressionSyntax root = access.Expression;
        while (root is MemberAccessExpressionSyntax { Expression: not ThisExpressionSyntax } inner)
        {
            root = inner.Expression;
        }

        string? name = root switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: var member } => member.Identifier.ValueText,
            _ => null,
        };
        return name is not null && owner.GetMembers(name).Any(m => m is IFieldSymbol or IPropertySymbol);
    }
}
