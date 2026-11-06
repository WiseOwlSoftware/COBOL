---
title: Floating-point items in arithmetic
description: How an expression with a FLOAT-SHORT, FLOAT-LONG or FLOAT-EXTENDED item is evaluated, why a fixed-point result can lose its last digit, and how to get exact results.
sidebar:
  order: 9
---

A `FLOAT-SHORT`, `FLOAT-LONG` or `FLOAT-EXTENDED` item (and the extensions `COMP-1` and `COMP-2`) holds an IEEE 754
binary floating-point value. `FLOAT-SHORT` is binary32, and `FLOAT-LONG` and `FLOAT-EXTENDED` are binary64. Choosing one
of them chooses binary floating-point arithmetic, with its usual rounding. This page says where that arithmetic applies
and what it does to a fixed-point result.

## The rule

An arithmetic expression that has a floating-point **item** as an operand, or a floating-point item as a receiver, is
evaluated entirely in IEEE binary64. Every other operand is converted to binary64 first: a fixed-point item, a literal, or
the result of a function such as `FUNCTION EXP10`. An expression with no floating-point item is evaluated in decimal, and
is exact wherever the standard defines an exact value.

Native arithmetic, the default, is "an implementor-defined method of evaluating an arithmetic expression" (ISO/IEC
1989:2023, 8.8.1.3). This rule is the determination for it, recorded in the conformance statement as A.1 item 123.

## What it does to a fixed-point result

The binary64 result is stored by its exact binary value. Many decimal fractions have no exact binary form, so the stored
value can differ from the decimal you wrote. When the receiver is a fixed-point item without `ROUNDED`, the extra digits
are truncated, and a value just below a decimal boundary loses its last digit.

```cobol
       01 A1 USAGE FLOAT-LONG VALUE 0.1.
       01 A2 USAGE FLOAT-LONG VALUE 0.2.
       01 ZF USAGE FLOAT-LONG VALUE 0.
       01 D1 PIC 9V9 VALUE 0.1.
       01 D2 PIC 9V9 VALUE 0.2.
       01 R  PIC S9V9(19).
       01 R6 PIC 9V9(6).
       ...
           COMPUTE R = A1 + A2
           COMPUTE R = D1 + D2
           COMPUTE R6 = FUNCTION EXP10 (-6)
           COMPUTE R6 = ZF + FUNCTION EXP10 (-6)
           COMPUTE R6 ROUNDED = ZF + FUNCTION EXP10 (-6)
```

| Statement | Stored |
|---|---|
| `COMPUTE R = A1 + A2` (floating-point items) | `+0.3000000000000000444` |
| `COMPUTE R = D1 + D2` (fixed-point items) | `+0.3000000000000000000` |
| `COMPUTE R6 = FUNCTION EXP10 (-6)` (no floating-point item) | `0.000001` |
| `COMPUTE R6 = ZF + FUNCTION EXP10 (-6)` | `0.000000` |
| `COMPUTE R6 ROUNDED = ZF + FUNCTION EXP10 (-6)` | `0.000001` |

The fourth line is the surprise. `ZF` is zero, but because it is a floating-point item, 10<sup>-6</sup> is evaluated in
binary64, where its nearest value is 0.000000999999999999999954…. Truncating that to six decimal places gives
0.000000. The same applies to `FUNCTION SUM`, `ABS`, `RANGE`, `MEAN` and `MIDRANGE` when an argument is a
floating-point item.

## Getting exact results

- **Use fixed-point items** (`PIC S9(n)V9(m)` with `DISPLAY`, `PACKED-DECIMAL` or `BINARY` usage) for values that must
  be exact, such as money. Their arithmetic is decimal and exact.
- **Use `ROUNDED`** when a floating-point result is stored into a fixed-point item, so the binary value rounds to the
  nearest decimal instead of truncating below it.

## Other compilers

GnuCOBOL 3.2 converts each floating-point operand to decimal before it adds, so it stores `.2999999999999999750` for
`A1 + A2` and `0.000001` for `ZF + FUNCTION EXP10 (-6)`. A program moved from GnuCOBOL that stores a floating-point result
into a fixed-point item without `ROUNDED` can therefore differ in its last digit. See [Migrating](../../migrating/).

Storing the shortest decimal that reads back to the same binary value, which would give `0.000001` in the fourth line,
is a recorded option the compiler may adopt later. Until then, the behavior above is the documented rule.
