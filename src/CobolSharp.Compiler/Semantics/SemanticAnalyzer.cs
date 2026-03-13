using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Parsing;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Performs semantic analysis on the AST: builds symbol table, resolves references,
/// validates types. Phase 1 minimal implementation.
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
        // Build symbol table from DATA DIVISION
        if (program.Data?.WorkingStorage != null)
        {
            foreach (var entry in program.Data.WorkingStorage.Entries)
            {
                AnalyzeDataEntry(entry, model.SymbolTable);
            }
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

    private void AnalyzeDataEntry(DataDescriptionEntry entry, SymbolTable symbols)
    {
        PictureInfo? pic = null;
        if (entry.PictureString != null)
        {
            pic = PictureParser.Parse(entry.PictureString);
        }

        int byteSize = ComputeByteSize(pic, entry.Usage);

        var symbol = new DataSymbol(entry.Name ?? "FILLER", entry.LevelNumber, pic, entry.Usage)
        {
            ByteSize = byteSize
        };

        if (entry.Name != null && !symbols.TryDeclare(symbol))
        {
            _diagnostics.ReportError("CS0400",
                $"Duplicate data-name '{entry.Name}'",
                new SourceLocation("<unknown>", entry.Span.Start, 0, 0), entry.Span);
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
