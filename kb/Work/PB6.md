---
title: "PB6 — LANDED (DEVLOG 1126) — CALL BY VALUE quoted §8.8.1.1 at a programmer w"
id: PB6
kind: defect
status: landed
severity: MAJOR
area: interprogram
wrong_answer: false
crashes: false
silent: false
rejects_legal_source: false
under_rejects: false
process_only: false
blocked: false
blocked_by: []
spec_refs: [14.9.4.3, 8.8.1.1]
tags: [cobolsharp, work, defect]
---

# PB6 — LANDED (DEVLOG 1126) — CALL BY VALUE quoted §8.8.1.1 at a programmer who broke §14.9.4.3 SR22

> Found by the pre-merge GnuCOBOL differential, which no other gate leg could have caught: the VERDICT was right,
> so nothing failed anywhere — only the rule quoted was wrong. `CALL … USING BY VALUE <alphanumeric>` was refused
> by DA6's §8.8.1.1 ARITHMETIC screen because the grammar production is named `arithmeticExpression` and the
> binder called `BindExpr` on it. §14.9.4.3 SR22 is the governing rule ("identifier-4 shall be of class numeric,
> object, or pointer"), so the operand IS illegal — but a diagnostic naming the wrong clause sends the programmer
> to the wrong place. **A production's NAME is not its operand's rule.**
> `COBOLNET1628` + `OperandContext.CallByValue`; golden `pb6-call-by-value-alphanumeric`.
> ⚠ **The first fix was a SILENT REGRESSION that looked like success**: screening the wrapper via
> `IntrinsicArgumentRules.ClassOf` made both cases compile CLEAN, because that method maps any
> `BoundComputedOperand` to NUMERIC. A wrongly-worded reject had become no enforcement at all. **A fix whose
> evidence is "the error went away" cannot distinguish a fix from a deletion.**
