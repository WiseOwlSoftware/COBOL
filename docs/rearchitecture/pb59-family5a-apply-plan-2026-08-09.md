# PB59 family-5a apply plan — FROZEN DESIGN (2026-08-09)
> The CONVERT positional-parse + r5/r6/r7-screen landing, drafted apply-ready during a battery freeze.
> 5b (the ANY raw-storage channel + r2/r4 bit padding + the runtime Convert signature) follows it; both
> defer to the mechanism map beside this file for sites and risks. NOT a worklist: kb/Work/PB59.md owns
> the rows. Verify every line anchor and citation (cite.py --check) at apply time.

> ## ⚠ APPLIED 2026-08-09 — WITH ONE DERIVATION CORRECTION (the screens are USAGE-keyed, not class-keyed)
> The positional walk, arity guard, catalog MinArgs 2→3 and DeliberatelyUnscreened rewrite landed as
> drafted. The r5/r6 screens below did NOT: re-deriving at apply time (rule 1), §15.19.3 keys the static
> half of r4/r5/r6 on the argument's REPRESENTATION — r4 says "of display or national usage" in so many
> words, and r5's NOTE ("distinct from simply requiring the string to be of class alphanumeric") cuts by
> what the storage HOLDS. A class screen mis-answers in both directions: it REJECTS a numeric DISPLAY
> item (class numeric, yet exactly "a valid string of characters from the alphanumeric coded character
> set" — the D9 corpus row pins its admission) and the corpus itself held the r6 counterexample —
> `argument_rule_equivalences_batch1`'s NATIONAL/NAT legs rode a PIC X item, which the class screen and
> the usage screen BOTH reject; the golden's source was corrected to PIC NN (output byte-identical).
> The landed shape: the SHARED `IntrinsicArgumentRules.StaticUsageOf` (the usage axis the mechanism map's
> fixInventory already demanded for §15.12.3 r1), read inline by BindConvert for r4 (display|national),
> r5 (display), r6 (national) and r7 (the exclusion list). See kb/Work/PB59.md family 5a for the row
> verdicts and the measured RV-15.19.4-4 residue (ANUM NAT HEX → 41 today; §15.19.4 r4's trailing
> pad-to-16-bits derives 4100, exactly the map's predicted golden — an initial "0041" counter-claim in
> the 5a landing text was a mis-derivation, corrected the same day).

# PB59-5a apply plan — CONVERT's positional parse + the r5/r6/r7 screens
(5b — the ANY raw-storage channel + r2/r4 bit padding + the runtime signature — is a separate landing.)

## Constraints honored
- IntrinsicArgumentClassDriftTests reads BindConvert's SOURCE TEXT: the literal call
  `CheckArgumentClasses(sig, operands)` must remain in the body, and NO new helper may sit between
  BindConvert and the next `private BoundExpr Bind…` (the body window ends there).
- Convert_SyntaxRuleViolations_1514's three negatives (`CONVERT(A ANUM ANUM)` etc.) must still reach
  SR3/SR8/SR9 — in the positional walk, `A` fills slot 0 and the format words fill slots 1+, so they do.
- The corpus's `CONVERT(H HEX ANUM)` etc. all have the operand first — position-compatible.

## BindConvert rewrite (IntrinsicBinder.cs ~:845-905)
Replace the harvest loop with:
```csharp
// §15.19.2: ( argument-1 source-format destination-format ) — POSITIONAL (PB59 / FMT-15.19.2).
// Slot 0 is ALWAYS argument-1 and binds as an OPERAND: NAT / ANUM / HEX / BYTE are §8.10
// CONTEXT-SENSITIVE words ("a context-sensitive word … used where its format does not permit it is
// treated as a user-defined word"), so a data item named NAT is legal there — the old position-blind
// harvest swallowed it as a keyword (measured: 1504 "0 operand + 3 format keyword(s)"), and accepted
// CONVERT(ANUM WS-A ANUM HEX) with the operand mid-list. ANY/ALPHANUMERIC/NATIONAL stay §8.9 reserved
// and cannot be data-names, so only the four context-sensitive words change behavior.
var operands = new List<BoundOperand>();
var kws = new List<string>();
foreach (var (a, i) in argCtxs.Select((a, i) => (a, i)))
{
    if (i == 0) { operands.Add(BindArgOperand(a)); continue; }
    if (KeywordWordOf(a) is { } w && IsConvertFormatWord(w)) { kws.Add(w); continue; }
    ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: the arguments after argument-1 shall be the "
        + "source-format and destination-format keywords, in that order (ISO §15.19.2)");
    return new BoundExprError("FUNCTION CONVERT format");
}
if (operands.Count != 1 || kws.Count is < 2 or > 3)
{
    ctx.Edition.Error("COBOLNET1504", "FUNCTION CONVERT takes ( argument-1 source-format "
        + $"destination-format ) (ISO §15.19.2); {operands.Count} operand + {kws.Count} format keyword(s) given");
    return new BoundExprError("FUNCTION CONVERT arity");
}
```
Then the existing decode (src/dst/hex from kws) + SR3/SR8/SR9 + the SR1 literal screen, UNCHANGED, plus:

## r5/r6 class screens (after the decode, before the ANY resolution)
```csharp
// §15.19.3 r5/r6 — the source-format keys the argument's required class (the keyword-dependent rule
// ArgSchema cannot express; the DeliberatelyUnscreened row now says "enforced in full by BindConvert").
// The r5 NOTE ("distinct from simply requiring … class alphanumeric") makes the class test the MINIMUM —
// the value half (a valid string of the SET's characters) is the runtime digit screen's (r4) and the
// item-33 total set's territory; nothing further is screenable at bind.
if (src == 1 && OperandCategory(operands[0]) is { } c5
        && c5 is not (PicCategory.Alphanumeric or PicCategory.NumericEdited))
    ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: an ANUM source-format takes an argument-1 of "
        + "class alphabetic or alphanumeric (ISO §15.19.3 rule 5)");
if (src == 3 && OperandCategory(operands[0]) is { } c6 && c6 is not PicCategory.National)
    ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: a NAT source-format takes an argument-1 of "
        + "class national (ISO §15.19.3 rule 6)");
// HEX source (r4's usage half): display or national usage — a class screen approximates it (numeric
// operands rejected); the byte-exact usage half stays with 5b's channel work.
if (src == 2 && OperandCategory(operands[0]) is { } c4
        && c4 is not (PicCategory.Alphanumeric or PicCategory.NumericEdited or PicCategory.National))
    ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: a HEX source-format takes an argument-1 of "
        + "display or national usage (ISO §15.19.3 rule 4)");
// §15.19.3 r7 — ANY excludes the non-data usages (index, object reference, pointer, function-pointer,
// program-pointer; MESSAGE-TAG has no Usage member — the Usage-inventory drift forces the decision when
// it lands). ClassOfCategory cannot express this (the pointer collapse), so the predicate reads Usage.
if (src == 0 && operands[0] is BoundFieldOperand { Place.Item.Pic.Usage: var u }
        && u is Usage.Index or Usage.ObjectReference or Usage.Pointer or Usage.ProgramPointer or Usage.FunctionPointer)
    ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: an ANY source-format argument-1 shall not be of "
        + "usage index, message-tag, object reference, pointer, function-pointer or program-pointer "
        + "(ISO §15.19.3 rule 7)");
```
(exact Usage member names verified against PicInfo.cs:56 at apply: Index/ObjectReference/Pointer/
ProgramPointer/FunctionPointer.)

## Catalog row (IntrinsicCatalog.cs:293) — MinArgs 2 → 3 (the format's minimum), MaxArgs 4 kept; check
IntrinsicResultTypeDriftTests / IntrinsicRealArgDriftTests for row-enumeration impact at apply.

## DeliberatelyUnscreened["CONVERT"] — rewrite to the HIGHEST-ALGEBRAIC disposition: "enforced in full by
BindConvert's own positional walk + r4/r5/r6/r7 screens; the value halves are runtime (the r4 digit
screen, the item-33 total set)".

## Tests
- Differential: the FMT-15.19.2 repro (`01 NAT PIC XX VALUE "41".` + `CONVERT(NAT HEX ANUM)` → "A");
  misplaced-operand negatives (`CONVERT(ANUM WS-A ANUM HEX)`, `CONVERT(ANUM ANUM HEX WS-A)` → 1514);
  r5 negative (numeric arg under ANUM), r6 negative (PIC X under NAT), r7 negative (POINTER under ANY).
- The corpus golden intrinsics_convert must stay green (all conforming, operand-first).
- Verdicts: FMT-15.19.2 → CONFORMS; AR-15.19.3-5 → CONFORMS; AR-15.19.3-6 → CONFORMS; AR-15.19.3-7's
  screen half (leg a) → the row stays open on the (b) raw-storage half until 5b; AR-15.12.3-1 (b)-(f)
  BASECONVERT screens — still open (a separate screen set; keep the row's note current).
