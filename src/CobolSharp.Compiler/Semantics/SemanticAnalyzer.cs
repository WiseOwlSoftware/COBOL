using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Parsing;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Performs semantic analysis on the AST: builds symbol table with data hierarchy,
/// resolves references, validates types.
/// </summary>
public sealed class SemanticAnalyzer
{
    private readonly DiagnosticBag _diagnostics;

    public SemanticAnalyzer(DiagnosticBag diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public SemanticModel Analyze(CompilationUnit compilationUnit)
    {
        var model = new SemanticModel();

        foreach (var program in compilationUnit.Programs)
        {
            AnalyzeProgram(program, model);
        }

        return model;
    }

    private void AnalyzeProgram(ProgramNode program, SemanticModel model)
    {
        // Build symbol table and hierarchy from DATA DIVISION
        if (program.Data?.WorkingStorage != null)
        {
            BuildDataHierarchy(program.Data.WorkingStorage.Entries, model.SymbolTable);
        }

        // Resolve references in PROCEDURE DIVISION
        if (program.Procedure != null)
        {
            foreach (var stmt in program.Procedure.Statements)
            {
                AnalyzeStatement(stmt, model.SymbolTable);
            }
        }
    }

    /// <summary>
    /// Build the data hierarchy from a flat list of data description entries.
    /// COBOL uses level numbers to define hierarchy:
    /// - 01 starts a new record
    /// - 02-49: higher number = deeper nesting (child of nearest preceding lower number)
    /// - 66: RENAMES (special)
    /// - 77: standalone elementary item
    /// - 88: condition-name (attached to parent)
    /// </summary>
    private void BuildDataHierarchy(List<DataDescriptionEntry> entries, SymbolTable symbols)
    {
        // Stack tracks the current nesting path: stack[0] is the 01-level, etc.
        var parentStack = new Stack<DataSymbol>();

        foreach (var entry in entries)
        {
            PictureInfo? pic = null;
            if (entry.PictureString != null)
            {
                pic = PictureParser.Parse(entry.PictureString);
            }

            int byteSize = ComputeByteSize(pic, entry.Usage);
            bool isGroup = pic == null && entry.LevelNumber != 66 &&
                           entry.LevelNumber != 77 && entry.LevelNumber != 88;

            var symbol = new DataSymbol(entry.Name ?? "FILLER", entry.LevelNumber, pic, entry.Usage)
            {
                ByteSize = byteSize,
                IsGroup = isGroup,
                OccursCount = entry.OccursCount > 0 ? entry.OccursCount : 1,
                OccursDependingOn = entry.OccursDependingOn,
            };

            // Handle level-based nesting
            if (entry.LevelNumber == 1 || entry.LevelNumber == 77)
            {
                // 01 or 77: top-level item, clear the parent stack
                parentStack.Clear();
                parentStack.Push(symbol);
            }
            else if (entry.LevelNumber == 66)
            {
                // RENAMES: standalone, no nesting
            }
            else if (entry.LevelNumber == 88)
            {
                // Condition-name: attached to the item at the top of the stack
                if (parentStack.Count > 0)
                {
                    var parent = parentStack.Peek();
                    symbol.Parent = parent;
                    parent.Children.Add(symbol);
                }
            }
            else
            {
                // Level 02-49: find the correct parent
                // Pop until we find a symbol with a lower level number
                while (parentStack.Count > 0 &&
                       parentStack.Peek().LevelNumber >= entry.LevelNumber)
                {
                    parentStack.Pop();
                }

                if (parentStack.Count > 0)
                {
                    var parent = parentStack.Peek();
                    symbol.Parent = parent;
                    parent.Children.Add(symbol);
                    parent.IsGroup = true; // ensure parent is marked as group
                }

                parentStack.Push(symbol);
            }

            // Handle REDEFINES
            if (entry.RedefinesName != null)
            {
                var target = symbols.Resolve(entry.RedefinesName);
                if (target != null)
                {
                    symbol.RedefinesTarget = target;
                    symbol.Offset = target.Offset; // shares same memory location
                }
                else
                {
                    _diagnostics.ReportError("CS0402",
                        $"REDEFINES target '{entry.RedefinesName}' not found",
                        new SourceLocation("<unknown>", entry.Span.Start, 0, 0), entry.Span);
                }
            }

            // Register in symbol table
            if (entry.Name != null && !symbols.TryDeclare(symbol))
            {
                _diagnostics.ReportError("CS0400",
                    $"Duplicate data-name '{entry.Name}'",
                    new SourceLocation("<unknown>", entry.Span.Start, 0, 0), entry.Span);
            }
        }

        // Second pass: compute group sizes (sum of children's sizes)
        ComputeGroupSizes(symbols);
        ComputeOffsets(symbols);
    }

    private static void ComputeGroupSizes(SymbolTable symbols)
    {
        foreach (var (_, symbol) in symbols.AllSymbols)
        {
            if (symbol.IsGroup && symbol.LevelNumber <= 49)
            {
                symbol.ByteSize = ComputeGroupByteSize(symbol);
            }
        }
    }

    private static int ComputeGroupByteSize(DataSymbol group)
    {
        int total = 0;
        foreach (var child in group.Children)
        {
            if (child.LevelNumber == 88) continue; // condition-names don't occupy space

            if (child.IsGroup)
            {
                child.ByteSize = ComputeGroupByteSize(child);
            }

            total += child.TotalByteSize;
        }
        return total;
    }

    private static void ComputeOffsets(SymbolTable symbols)
    {
        int offset = 0;
        foreach (var (_, symbol) in symbols.AllSymbols)
        {
            if (symbol.LevelNumber == 1 || symbol.LevelNumber == 77)
            {
                if (symbol.RedefinesTarget != null)
                {
                    // REDEFINES shares target's offset — already set during hierarchy build
                    AssignChildOffsets(symbol, symbol.Offset);
                }
                else
                {
                    symbol.Offset = offset;
                    AssignChildOffsets(symbol, offset);
                    offset += symbol.TotalByteSize;
                }
            }
        }
    }

    private static void AssignChildOffsets(DataSymbol parent, int baseOffset)
    {
        int childOffset = baseOffset;
        foreach (var child in parent.Children)
        {
            if (child.LevelNumber == 88) continue;
            if (child.RedefinesTarget != null) continue; // REDEFINES shares offset

            child.Offset = childOffset;
            if (child.IsGroup)
            {
                AssignChildOffsets(child, childOffset);
            }
            childOffset += child.TotalByteSize;
        }
    }

    private static int ComputeByteSize(PictureInfo? pic, UsageType usage)
    {
        if (pic == null) return 0;

        return usage switch
        {
            UsageType.Display => pic.Size + (pic.IsSigned ? 1 : 0),
            UsageType.Binary => pic.IntegerDigits + pic.DecimalDigits switch
            {
                <= 4 => 2,
                <= 9 => 4,
                _ => 8
            },
            UsageType.PackedDecimal => (pic.IntegerDigits + pic.DecimalDigits + 2) / 2,
            UsageType.Index => 4,
            UsageType.Pointer or UsageType.FunctionPointer or UsageType.ProcedurePointer => 8,
            _ => pic.Size
        };
    }

    private void AnalyzeStatement(Statement stmt, SymbolTable symbols)
    {
        switch (stmt)
        {
            case DisplayStatement display:
                foreach (var op in display.Operands)
                    ResolveExpression(op, symbols);
                break;

            case MoveStatement move:
                ResolveExpression(move.Source, symbols);
                foreach (var target in move.Targets)
                    ResolveIdentifier(target, symbols);
                break;

            case AddStatement add:
                foreach (var op in add.Operands)
                    ResolveExpression(op, symbols);
                foreach (var target in add.Targets)
                    ResolveIdentifier(target, symbols);
                break;

            case SubtractStatement sub:
                foreach (var op in sub.Operands)
                    ResolveExpression(op, symbols);
                foreach (var target in sub.Targets)
                    ResolveIdentifier(target, symbols);
                break;

            case ComputeStatement compute:
                ResolveIdentifier(compute.Target, symbols);
                ResolveExpression(compute.Value, symbols);
                break;

            case IfStatement ifStmt:
                ResolveExpression(ifStmt.Condition, symbols);
                foreach (var s in ifStmt.ThenStatements) AnalyzeStatement(s, symbols);
                foreach (var s in ifStmt.ElseStatements) AnalyzeStatement(s, symbols);
                break;

            case PerformStatement perform:
                if (perform.Times != null) ResolveExpression(perform.Times, symbols);
                if (perform.Until != null) ResolveExpression(perform.Until, symbols);
                foreach (var s in perform.Body) AnalyzeStatement(s, symbols);
                break;
        }
    }

    private void ResolveExpression(Expression expr, SymbolTable symbols)
    {
        switch (expr)
        {
            case IdentifierExpression id:
                ResolveIdentifier(id, symbols);
                break;
            case BinaryExpression bin:
                ResolveExpression(bin.Left, symbols);
                ResolveExpression(bin.Right, symbols);
                break;
            case UnaryExpression unary:
                ResolveExpression(unary.Operand, symbols);
                break;
        }
    }

    private void ResolveIdentifier(IdentifierExpression id, SymbolTable symbols)
    {
        if (symbols.Resolve(id.Name) == null)
        {
            _diagnostics.ReportError("CS0401",
                $"Undefined data-name '{id.Name}'",
                new SourceLocation("<unknown>", id.Span.Start, 0, 0), id.Span);
        }
    }
}

/// <summary>
/// Result of semantic analysis — contains the symbol table and resolved info.
/// </summary>
public sealed class SemanticModel
{
    public SymbolTable SymbolTable { get; } = new();
}
