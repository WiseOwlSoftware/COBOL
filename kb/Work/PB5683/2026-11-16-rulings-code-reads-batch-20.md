# Rulings on the re-read of slice 1985-247 (batch 20), and the design of kb/Work PB5891

Written 2026-11-16 by the Mythos lane session (Claude Opus 5.5, main thread). Read pass 19: run wf_9ab35a75-f6c of
`coderead-1985/coderead-sample.js` on a compiler built at main cdc9601608 (`E:/COBOL-wt/coderead85-tree-d`). Rows: the
four rows kb/Work PB5886 fixed (`1989-AR-A.2.9.3-1` CHAR, `1989-AR-A.2.14.3-1` FACTORIAL, `1989-AR-A.2.28.3-1` MOD,
`1989-AR-A.2.35.3-1` RANDOM) and `1985-SR-XIII.4.9.3-4` after kb/Work PB5890. Reader: 5 CONFORMS. Refuter: 4
OVERTURNED, 1 DOUBTFUL. Every citation below answered `python scripts/spec/cite.py --check` OK in this session.

## 1985-SR-XIII.4.9.3-4: the doubt is REJECTED; the row CONFORMS

The refuter attacked about 25 new storing forms (an index data item, a CD's fields, SAME RECORD AREA, the control
item of another report) and found every one refused. Its one doubt: a store reached through a PERFORM into another
declarative section is admitted. The text settles it. `§1985:XIII.4.9.3 SR3` reaches a PERFORM's range on purpose: "A
PERFORM statement in a USE BEFORE REPORTING procedure must not have GENERATE, INITIATE, or TERMINATE statements in
its range". `§1985:XIII.4.9.3 SR4` ("A USE BEFORE REPORTING procedure must not alter the value of any control data
item") has no such sentence, so it constrains the statements of the procedure itself. The lexical check is the rule.
CONFORMS. The CALL BY REFERENCE case of slice 246's reader falls under the same reading.

## The four intrinsic rows: the refuter is UPHELD; new defect kb/Work PB5891 (rejects legal source, every edition)

`§1989:A.2.2 AR4`: "An arithmetic expression which will always result in an integer value must be specified." The
compiler refuses (COBOL1627) `FUNCTION FACTORIAL (N * (N + 1) / 2)`, `FUNCTION MOD (N * (N + 1) / 2 4)`,
`FUNCTION RANDOM (N * (N + 1) / 2)` and `FUNCTION CHAR (N * (N - 1) / 2 + 65)` with N PIC 99, at `--std 85` and
`--std 2023` (`E:/COBOL-coord/scratch/coderead-1985/out/refute-1985-247/`, program `fn_edge.cob`). A product of two
consecutive integers is even, so each argument always yields an integer and is legal source. The rule sets no
method of proof.

**DECIDED (the design is mine): the screen decides integer-ness EXACTLY for every expression it can evaluate exactly,
by normalising it to a polynomial.** It does not add more shapes.
1. **Exactness first, as now.** The digit bound under the arithmetic in force (kb/Work PB5886's rounds 3 to 5) proves
   that every intermediate is exact. An expression with an intermediate that is not exact is not proven, as now.
2. **Then the value is a polynomial with rational coefficients** in atoms: integer data items (integer atoms),
   integer-typed pure function calls (integer atoms), and other pure terms (non-integer items, numeric function
   calls: real atoms). Two atoms are one when they are the same pure term: the same item reference, or the same
   function with the same arguments, compared as bound trees. Impure functions (RANDOM, CURRENT-DATE,
   WHEN-COMPILED and any other whose value is not fixed by its arguments: sweep the catalog) are never one atom
   with another. Division by a non-literal is not a polynomial, so it is not proven.
3. **Integer-valued test.** After like terms are collected, every monomial that contains a real atom must have
   coefficient zero, so `SQRT (N) - SQRT (N) + 5` and `0 * FUNCTION SQRT (N) + 5` are the constant 5. The polynomial
   left in the integer atoms must be integer-valued on the integers: P is integer-valued on Z^n exactly when it takes
   integer values on the grid {0..d1} x ... x {0..dn} of its degrees (the binomial basis; evaluate exactly with
   rationals). Degrees above a stated cap are not proven, and the cap is written in the design.
4. **ONE classifier.** This is `IsAlwaysIntegral`, the one answer kb/Work PB5886 made. The walks keep their
   exhaustive arms and the reflection drift test. The design section in `docs/INTRINSICS_DESIGN.md` is written FIRST.
5. kb/Work PB5889 (the decision that refused the identity forms) is closed by this for every form whose terms
   cancel. `S2 - FUNCTION MAX (S2)` stays refused: MAX(S2) and S2 are different atoms, and the result is an integer only
   by MAX's meaning. PB5889 keeps that residue, with a dated file saying so, and stays open.

## Leads that are not yet anything
- The reader: a literal -1 for FACTORIAL or RANDOM is accepted, and the result is undefined
  (`§1989:A.2.2 AR4` and each function's own rule). Nobody has ruled whether a literal argument outside the domain
  is to be refused at compile time.
