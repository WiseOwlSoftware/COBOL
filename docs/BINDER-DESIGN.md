# CobolSharp Binder / Semantic Model Design

## Overview

The binder sits between the parse tree (ANTLR4) and downstream phases (type system, CIL codegen).
It walks the parse tree, resolves symbols, type-checks expressions, and produces a **bound tree**
of `BoundStatement` / `BoundExpression` nodes that CIL codegen consumes.

## Architecture

```
ANTLR4 Parse Tree (CobolParserCore contexts)
        │
        ▼
┌──────────────────────┐
│ SemanticBuilder      │  Pass 1: declaration collection
│ (already built)       │  Creates symbols in SymbolTable
└──────────┬───────────┘
           ▼
┌──────────────────────┐
│ ReferenceResolver    │  Pass 2: identifier resolution
│ (already built)       │  Validates PERFORM/GO TO/file targets
└──────────┬───────────┘
           ▼
┌──────────────────────┐
│ Binder               │  Pass 3: type-checked bound tree
│                      │  Produces BoundStatement/BoundExpression
│                      │  Registers bound nodes in SemanticModel
└──────────┬───────────┘
           ▼
      SemanticModel
      (bound tree + diagnostics)
           │
           ▼
      CIL Code Generator
```

## Core Data Structures

### SemanticModel

```csharp
public sealed class SemanticModel
{
    public CompilationUnitSymbol Root { get; }
    public DiagnosticBag Diagnostics { get; } = new();

    // Map parse nodes → bound nodes
    private readonly Dictionary<ParserRuleContext, BoundNode> _boundNodes = new();

    public T? GetBoundNode<T>(ParserRuleContext ctx) where T : BoundNode
        => _boundNodes.TryGetValue(ctx, out var n) ? n as T : null;

    internal void RegisterBoundNode(ParserRuleContext ctx, BoundNode node)
        => _boundNodes[ctx] = node;
}
```

### Bound Node Hierarchy

```csharp
public abstract class BoundNode { public abstract BoundNodeKind Kind { get; } }
public abstract class BoundStatement : BoundNode { }
public abstract class BoundExpression : BoundNode
{
    public ITypeSymbol Type { get; }
    protected BoundExpression(ITypeSymbol type) => Type = type;
}

// Concrete nodes:
BoundVariableExpression  — resolved data item reference
BoundLiteralExpression   — numeric/string/figurative constant
BoundBinaryExpression    — arithmetic: +, -, *, /, **
BoundRelationalExpression — condition: =, <>, <, >, <=, >=
BoundMoveStatement       — MOVE source TO targets
BoundAddStatement        — ADD operands TO targets
BoundIfStatement         — IF condition THEN/ELSE blocks
BoundPerformStatement    — PERFORM target [THRU] [TIMES/UNTIL/VARYING]
BoundStatementList       — block of statements
```

### Type System Stub

```csharp
public interface ITypeSymbol : ISymbol
{
    bool IsNumeric { get; }
    bool IsBoolean { get; }
    bool IsAlphanumeric { get; }
}

public static class BuiltinTypes
{
    public static readonly PrimitiveTypeSymbol Numeric = new("NUMERIC", isNumeric: true);
    public static readonly PrimitiveTypeSymbol Boolean = new("BOOLEAN", isBoolean: true);
    public static readonly PrimitiveTypeSymbol Alphanumeric = new("ALPHANUMERIC", isAlphanumeric: true);
}
```

Later, the PIC/USAGE engine maps each DataSymbol to a concrete ITypeSymbol.

### Relational Operators

```csharp
public enum BoundRelationalOperator
{
    Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual
}
```

## Binder Walk Pattern

The binder dispatches on statement type:

```csharp
private BoundStatement BindStatement(CobolParserCore.StatementContext ctx)
{
    if (ctx.moveStatement() != null) return BindMove(ctx.moveStatement());
    if (ctx.addStatement() != null) return BindAdd(ctx.addStatement());
    if (ctx.ifStatement() != null) return BindIf(ctx.ifStatement());
    if (ctx.performStatement() != null) return BindPerform(ctx.performStatement());
    // ... all statement types
}
```

Each `BindXxx` method:
1. Resolves identifiers against the symbol table
2. Type-checks operands
3. Creates a `BoundXxx` node
4. Registers it in the SemanticModel
5. Emits diagnostics for errors

## Next Steps

1. **PIC/USAGE typing** — map each DataSymbol's PIC string + Usage to a concrete ITypeSymbol
2. **Flow analysis** — PERFORM range validation, unreachable paragraph detection
3. **CIL emitter** — consumes bound tree, emits .NET IL via Mono.Cecil
