# Data Division Layout Design — Canonical Model

Reference design for production-quality OCCURS, REDEFINES, RENAMES, and USAGE
handling. Based on user spec from session 9.

## Grammar (already partially implemented)

### OCCURS
```antlr
occursClause
    : OCCURS integerLiteral (TO integerLiteral)? TIMES?
      (DEPENDING ON identifier)?
    ;
```

### REDEFINES
```antlr
redefinesClause
    : REDEFINES dataName
    ;
```

### RENAMES (level 66 only)
```antlr
renamesClause
    : RENAMES dataName (THROUGH dataName)?
    ;
```

### USAGE normalization
```antlr
usageClause
    : USAGE IS? usageKeyword
    ;
```
COMP/COMPUTATIONAL → Binary, COMP-3/COMPUTATIONAL-3 → PackedDecimal, etc.

## Binder Model

### OccursInfo
```csharp
public sealed class OccursInfo
{
    public int From { get; }
    public int To { get; }
    public string? DependingOnName { get; }
    public bool IsVariable => DependingOnName != null;
}
```

### RedefinesInfo
```csharp
public sealed class RedefinesInfo
{
    public string TargetName { get; }
    public DataDescriptionInfo? Target { get; set; } // resolved in second pass
}
```

### RenamesInfo
```csharp
public sealed class RenamesInfo
{
    public string FromName { get; }
    public string? ThroughName { get; }
}
```

## Layout Algorithm

Three passes per group:
1. **Resolve REDEFINES** targets (symbolic → structural, within same parent)
2. **Layout non-redefines** sequentially (offset accumulates)
3. **Layout redefines** as overlays (share base node's offset)

### OCCURS layout
- Layout one element to get element size
- Total size = elementSize × maxOccurs
- Variable OCCURS (DEPENDING ON) uses max as upper bound for allocation

### Size computation by USAGE
| Usage | Size Rule |
|-------|-----------|
| DISPLAY | digits + sign (if separate) |
| BINARY/COMP | 1-4 digits → 2, 5-9 → 4, 10-18 → 8 |
| PACKED-DECIMAL/COMP-3 | (digits + 1) / 2 |
| COMP-1 | 4 (float) |
| COMP-2 | 8 (double) |
| INDEX | 4 |

## Semantic Validation

### REDEFINES
- Same level number as target
- Same parent group
- No circular redefines
- Resolved in two-pass (already implemented in SemanticBuilder.ResolveRedefines)

### RENAMES
- Level 66 only
- Refers to siblings in same group
- THROUGH range is valid and ordered

### User-defined words
- 1-30 characters
- Must contain at least one letter
- No leading/trailing hyphen
- Enforced in semantic layer, not lexer
