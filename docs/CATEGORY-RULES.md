# COBOL Category Compatibility Rules

Reference for the category lattice and compatibility matrices implemented in
`CategoryCompatibility.cs`, `LoweringTable.cs`, and `PicRuntime.cs`.

Based on ISO/IEC 1989:2023 §6.1.2, §6.1.3, §6.13, §13.5, §14.9.24.

---

## 1. Category Lattice

```
CobolCategory:
  Numeric              — PIC 9, S9, 9V9, COMP, COMP-3, etc.
  NumericEdited        — PIC Z, ZZ9, $99.99, CR, DB, etc.
  Alphanumeric         — PIC X(n)
  AlphanumericEdited   — PIC A(n), edited alphanumeric
  National             — PIC N(n)
  NationalEdited       — PIC G(n), edited national
```

Key distinction: **NumericEdited is NOT a numeric category**. It is a display/editing
category. It cannot be used as an arithmetic operand.

---

## 2. MOVE Rules (§14.9.24)

### 2.1 Numeric as sending item

| Receiving Category     | Legal? | Semantics |
|------------------------|--------|-----------|
| Numeric                | YES    | Scale, sign, truncation, size error |
| NumericEdited          | YES    | Numeric formatted into edited picture |
| Alphanumeric           | YES    | Numeric converted to character form |
| AlphanumericEdited     | YES    | Numeric formatted into edited alpha |
| National               | YES    | Numeric → character → national representation |
| NationalEdited         | YES    | Numeric formatted into edited national |

**Rule: Numeric can be moved to any category.**

### 2.2 NumericEdited as sending item

| Receiving Category     | Legal? | Notes |
|------------------------|--------|-------|
| Numeric                | NO     | Edited is not numeric category (§6.1.3.2) |
| NumericEdited          | YES    | Character-level copy, re-interpreted as edited |
| Alphanumeric           | YES    | Treated as character data |
| AlphanumericEdited     | YES    | Character → edited alpha |
| National               | YES    | Character → national |
| NationalEdited         | YES    | Character → edited national |

### 2.3 Alphanumeric / AlphanumericEdited as sending item

| Receiving Category     | Legal? | Notes |
|------------------------|--------|-------|
| Numeric                | NO     | Not category-compatible (§6.1.3) |
| NumericEdited          | NO     | Not category-compatible |
| Alphanumeric           | YES    | Left-justified, space-padded |
| AlphanumericEdited     | YES    | Character → edited alpha |
| National               | YES    | Alpha → national representation |
| NationalEdited         | YES    | Alpha → edited national |

**Note:** Some vendors allow Alphanumeric → Numeric if content is numeric.
This is an extension, not standard. Our compiler should emit a diagnostic.

### 2.4 National / NationalEdited as sending item

| Receiving Category     | Legal? | Notes |
|------------------------|--------|-------|
| Numeric                | NO     | No defined numeric interpretation |
| NumericEdited          | NO     | No defined numeric interpretation |
| Alphanumeric           | YES    | National → alpha via transcoding |
| AlphanumericEdited     | YES    | National → edited alpha |
| National               | YES    | Direct copy |
| NationalEdited         | YES    | National → edited national |

### 2.5 Full MOVE Truth Table

```
                 Target →
Source ↓     Num   NumEd   Alpha   AlphaEd   Nat   NatEd
---------------------------------------------------------
Numeric        T      T       T        T       T      T
NumericEdited  F      T       T        T       T      T
Alphanumeric   F      F       T        T       T      T
AlphaEdited    F      F       T        T       T      T
National       F      F       T        T       T      T
NatEdited      F      F       T        T       T      T
```

---

## 3. Arithmetic Rules (§6.13)

Arithmetic is defined on **numeric category items only**.

### 3.1 Operands

- **Legal:** Numeric
- **Illegal:** NumericEdited, Alphanumeric, AlphanumericEdited, National, NationalEdited

NumericEdited is NOT a legal arithmetic operand. Some vendors allow it by
parsing the edited representation, but ISO treats arithmetic as operating on
numeric category only.

### 3.2 Results

- **Legal:** Numeric, NumericEdited
- **Illegal:** Alphanumeric, AlphanumericEdited, National, NationalEdited

Numeric result: stored with scale/rounding/truncation.
NumericEdited result: numeric result formatted into edited picture.

### 3.3 Examples

```cobol
ADD A TO B.
  A: PIC 9(3) VALUE 123.        (Numeric — legal operand)
  B: PIC 9(3) VALUE 456.        (Numeric — legal result)
  Result: B = 579.

ADD A TO C.
  A: PIC 9(3) VALUE 123.        (Numeric — legal operand)
  C: PIC ZZ9.                   (NumericEdited — legal result)
  Result: numeric 123 + C's value, formatted into ZZ9.

ADD X TO B.
  X: PIC X(3).                  (Alphanumeric — ILLEGAL operand)
  Diagnostic: alphanumeric operand in arithmetic.
```

---

## 4. Comparison Rules (§6.1.2)

Comparisons are defined by **category families**.

### 4.1 Numeric Comparisons

- **Legal:** Numeric ↔ Numeric, Numeric ↔ NumericEdited, NumericEdited ↔ NumericEdited
- Comparison is by **numeric value** (sign, magnitude, scale)

### 4.2 Alphanumeric Comparisons

- **Legal:** Alphanumeric ↔ Alphanumeric, Alphanumeric ↔ AlphanumericEdited,
  AlphanumericEdited ↔ AlphanumericEdited
- Comparison is by **collating sequence** (default: character code order;
  can be overridden by SPECIAL-NAMES / COLLATING SEQUENCE)
- Shorter operand is space-padded on the right

### 4.3 National Comparisons

- **Legal:** National ↔ National, National ↔ NationalEdited,
  NationalEdited ↔ NationalEdited
- Comparison is by **national collating sequence** (implementation-defined;
  often Unicode code point order)

### 4.4 Cross-Family Comparisons

| Left | Right | Legal? |
|------|-------|--------|
| Numeric | Alphanumeric | NO — category mismatch |
| Numeric | National | NO — category mismatch |
| Alphanumeric | National | NO — category mismatch |

**All cross-family comparisons are illegal and require diagnostics.**

---

## 5. MoveKind Extension (Future)

When group moves are implemented, the matrix extends with a `MoveKind` dimension:

```
MoveKind:
  Elementary      — single field → single field (matrix applies directly)
  Group           — group → group (byte copy; matrix enforced per child)
  Corresponding   — MOVE CORRESPONDING (per-matching-subfield)
```

For Group and Corresponding moves, compatibility is checked **recursively on
elementary children**, not at the group level.

---

## 6. Implementation Map

| Component | File | Role |
|-----------|------|------|
| CobolCategory enum | PicDescriptor.cs | Canonical lattice (Runtime) |
| CategoryCompatibility | CategoryCompatibility.cs | MOVE/Arithmetic/Compare legality |
| LoweringTable | LoweringTable.cs | (op, src, dst) → PicRuntime MethodInfo |
| PicRuntime | PicRuntime.cs | 28 MOVE + 6 Arith + 3 Compare helpers |
| PicLayout.Category | TypeSystem.cs | Source of truth for data items |
| DataTypeSymbol.Category | TypeSystem.cs | Threaded through binder |
| BoundExpression.Category | BoundNodes.cs | Threaded through bound tree |
