---
title: "PB4 — LANDED (DEVLOG 1119) — a HEXADECIMAL literal was not decoded in FIVE p"
id: PB4
kind: defect
status: landed
severity: MAJOR
area: literals
wrong_answer: false
crashes: false
silent: false
rejects_legal_source: false
under_rejects: false
process_only: false
blocked: false
blocked_by: []
spec_refs: [8.3.3.2]
tags: [cobolsharp, work, defect]
---

# PB4 — LANDED (DEVLOG 1119) — a HEXADECIMAL literal was not decoded in FIVE positions

> **Found by accident, which is the part worth keeping.** It surfaced while building a test vehicle for PB3: the
> vehicle used `PIC X VALUE X"FF"` and reported `ORD = 89`. 89 − 1 = 0x58 = **'X'** — the item held the letter X,
> because the VALUE path had stored the literal's own SOURCE TEXT and truncated it to the picture. PB3's agent
> report was measured through this same distortion.
>
> **§8.3.3.2 makes a hexadecimal literal one FORM of an alphanumeric literal** — "each pair of hexadecimal digits
> represents a single character" — so every position accepting an alphanumeric literal accepts it. Five did not,
> each failing differently, and the data division disagreed with the procedure division about the same literal:
>
> | site | before | correct |
> |---|---|---|
> | `01 B PIC X(2) VALUE X"4142"` | `X"` (source text, truncated) | `AB` |
> | `01 A PIC X(4) VALUE ALL X"41"` | `ALLX` | `AAAA` |
> | `05 E OCCURS 2 PIC X(2) VALUE X"4142"` | as VALUE | `AB` / `AB` |
> | `88 IS-AB VALUE X"4142"` | never matched | matches |
> | `MOVE ALL X"41" TO M` | parsed, then a RUN-TIME `NotImplementedCobolFeatureException` | `AA` |
> | (`MOVE X"4142" TO M` decoded correctly all along) | `AB` | `AB` |
>
> **⛔ ROOT CAUSE — one rule written down twice, and BOTH copies wrong the same way.** `CobolLiteral` carried the
> prefix-letter list in `Decode` AND in `IsStringLiteral`, and neither included `X`. So a hex literal was
> simultaneously *not recognised as a literal* and *not decoded as one*, which is why the five sites failed in
> five different ways: each had built its own compensation on top of a decoder that quietly returned the input
> unchanged. Adding a `DecodeHex` arm at `ValueInitializer` would have made it the **fifth** copy of the dispatch
> DA3 already found three of. The list is now one `PrefixLetters` constant behind one `SplitLiteral` helper.
> The remaining site was `ExpressionBinder.FigurativeOperand`, whose own comment said "(ALL HEXLIT / NULL stay a
> later slice)" while the grammar had listed `ALL HEXLIT` all along.
>
> ⚠ **The X-prefix guard is load-bearing**: `DecodeHex` returns "" for anything it does not recognise, so
> delegating on a leading `X` alone would turn the ordinary word `XYZ` — which `Decode` contracts to return
> unchanged — into the empty string. The golden pins `XYZ`.
>
> **GOLDEN** `conformance:2023/pb4_hex_literal_value` — all five sites plus the guard, expected values from
> §8.3.3.2 arithmetic.
