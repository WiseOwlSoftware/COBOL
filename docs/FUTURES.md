# CobolSharp — Future Design Notes

Design ideas captured during development for later implementation.

---

## 1. Runtime Category Tracing (Empirical Validation)

### Motivation

Cross-check the category compatibility matrix against actual NIST test execution.
Instrument PicRuntime to log every (OperationKind, MoveKind, source category,
target category) tuple during test runs, then assert all executed combinations
are legal per CategoryCompatibility.

### Design Sketch

```csharp
// Trace record — one per operation at runtime
public readonly struct CategoryTraceRecord
{
    public OperationKind Operation { get; init; }
    public MoveKind MoveKind { get; init; }       // Move only
    public CobolCategory SourceCategory { get; init; }
    public CobolCategory TargetCategory { get; init; }
    public string? SourceSymbol { get; init; }     // for diagnostics
    public string? TargetSymbol { get; init; }
}

// Collector — static, opt-in via environment variable or flag
public static class CategoryTracer
{
    private static readonly List<CategoryTraceRecord> s_records = new();
    public static bool IsEnabled { get; set; }

    public static void Record(CategoryTraceRecord rec)
    {
        if (IsEnabled) s_records.Add(rec);
    }

    public static IReadOnlyList<CategoryTraceRecord> Flush()
    {
        var result = s_records.ToList();
        s_records.Clear();
        return result;
    }
}
```

### Instrumentation Points

- PicRuntime.MoveNumericToNumeric et al: `CategoryTracer.Record(...)` at entry
- Binder/Emitter: emit trace calls in debug builds only (conditional compilation)
- Test harness: enable tracer, run NC101A, assert all records are legal

### Test Pattern

```csharp
[Fact]
public void Nc101a_category_compliance()
{
    CategoryTracer.IsEnabled = true;
    RunNc101a();
    var trace = CategoryTracer.Flush();

    foreach (var rec in trace)
    {
        switch (rec.Operation)
        {
            case OperationKind.Move:
                Assert.True(CategoryCompatibility.IsMoveLegal(rec.SourceCategory, rec.TargetCategory));
                break;
            case OperationKind.Compare:
                Assert.True(CategoryCompatibility.IsComparisonLegal(rec.SourceCategory, rec.TargetCategory));
                break;
            // arithmetic...
        }
    }
}
```

### Decision: Defer

Not needed for NC101A correctness push. Implement when we have multiple NIST
tests passing and want regression coverage on the category system.

---

## 2. WRITE AFTER ADVANCING

NC101A uses `WRITE record AFTER ADVANCING n LINES` extensively. Currently ignored.

### Semantics

- `AFTER ADVANCING n LINES`: output n-1 blank lines, then the record
- `AFTER ADVANCING PAGE`: form feed / page break
- `BEFORE ADVANCING n LINES`: output the record, then n-1 blank lines

### Implementation Plan

1. Add `AdvancingLines` and `IsAfterAdvancing` to `BoundWriteStatement`
2. Parse `writeBeforeAfter` in BoundTreeBuilder
3. Pass through to IR (extend `IrWriteRecordFromStorage` or new instruction)
4. Runtime: `StorageHelpers.WriteRecordToFile` gains advancing parameter

---

## 3. ON SIZE ERROR / NOT ON SIZE ERROR

Tests F1-3 through F1-29 in NC101A depend on SIZE ERROR handling.

### Implementation Plan

1. Parse `multiplyOnSizeError` in `BindMultiply` (grammar already supports it)
2. Add `OnSizeErrorStatements` / `NotOnSizeErrorStatements` to `BoundMultiplyStatement`
3. PicRuntime arithmetic methods return `ArithmeticStatus` with `SizeError` flag
4. Binder creates conditional branches based on status flag
5. Apply same pattern to ADD/SUBTRACT/DIVIDE

---

## 4. MoveKind — Group and CORRESPONDING Moves

### Design

```csharp
public enum MoveKind
{
    Elementary,      // single field → single field
    Group,           // byte-level copy (group → group)
    Corresponding,   // MOVE CORRESPONDING (per-matching-subfield)
}
```

Group moves are category-agnostic at the group level. The binder enforces
legality by walking children and using IsMoveLegal on each elementary pair.

CORRESPONDING walks source/target group children by name, matching subfields,
and performs elementary moves on each pair.
