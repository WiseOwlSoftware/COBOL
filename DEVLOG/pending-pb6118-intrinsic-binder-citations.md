## PB6118: the intrinsic binder and the SPECIAL-NAMES binder cite 2002's and 2014's own rules

**Mechanism.** A diagnostic at `--std 2002` or `--std 2014` named the 2023 clause when the call site passed only a 1985 text and a "later"
text, or spelled a §2023:15.n rule in its message. Group C of wave 20262174 took `IntrinsicBinder.cs`, `SpecialNamesBinder.cs` and
`CollationEngine.cs` (`TryGetOrderingTable`).

**What the re-probe showed (Debug build, `run_probe.py` at 2002 and 2014, programs in the wave scratch).** Every one of the 17
two-arm `Cite`/`CiteRule` sites of the two binders printed `ISO §2023:...` at both editions: COBOL1543 (word FUNCTION omitted,
argument list, ALL as an argument), COBOL1630 and COBOL1629 (reference modification of a function result), COBOL1501 (summary of
functions), COBOL1504 and COBOL1645 (15.3), COBOL0895, COBOL0894 and COBOL5989 (DECIMAL-POINT IS COMMA, PROGRAM COLLATING
SEQUENCE). `CollationEngine.cs` has no citation of its own: the ORDER TABLE message that names `TryGetOrderingTable` is `SpecialNamesBinder.OrderTableBind`.
ORDER TABLE is a 2002 clause, so its COBOL1662 and COBOL5990 text cited the 2023 rule at 2002 and 2014 too.

**Fix, first shape (the 17 sites).** Each carries its 2002 and 2014 rule, found by the wording of the 2023 rule in each edition's
transcription (`tr.py`-style match on five-word runs) and checked with `cite.py --check`: §2002:8.4.2.2.2 SR2/SR4/SR6, §2014:8.4.2.2.2
SR2/SR6/SR8, §2002/§2014:8.4.2.3.2 SR2/SR3, §2002/§2014:8.4.1.2.2 SR2, §2002:12.2.6.3 GR14a (§2014:12.3.6.3), §2002:12.2.5.2 SR1/SR2
(§2014:12.3.5.2), the numeric-literal clause §2002/§2014:8.3.1.2.2.1, and the 2002/2014 forms of the general format.

**Fix, second shape (the function rules).** About 100 messages of `IntrinsicBinder.cs` and the schema rows of `IntrinsicArgumentRules.cs`
write a §2023:15.n rule, and each edition numbers its functions and rules differently (2002 has no TRIM, no SUBSTITUTE; every later
paragraph shifts). `scripts/spec/gen_intrinsic_citation_concordance.py` writes `IntrinsicCitationConcordance.cs` (122 clauses: function,
2002 and 2014 counterpart); `IntrinsicCitations.Rule` and `BareRule` print the edition's own citation, or the 2023 one with the edition
that begins it (`FUNCTION CONVERT begins in COBOL-2023 (targeting COBOL-2002)`), or fail loudly on a clause the table lacks. A RULE number
within a clause is not guessed from wording: main's PB5922 landed `IntrinsicCitations.Edition2002` and `Edition2014`, the verified
ordinals held to each edition's text by `IntrinsicEditionClausesDriftTests`, and `Rule` reads those for every `15.n.3 ARk` (one table of
rule ordinals; the concordance holds clauses only). The same change gives `ArgumentTypes`, `IntegerType`, `NumericFunctionAsInteger` and
`FloatingPointItem` their 2002 and 2014 arms and canonicalizes the legacy `rule 1` / `r1` spellings of the binder.

**Review round 1.** R1-1 (COBOL1644 printed `§2002:15.55.2 rule 3`, the ANYCASE rule): the message now cites `§2023:15.68.3 AR3`
through `Rule`, which prints AR4 at both editions (checked with `cite.py`), pinned in `ARuleIsPrintedAtEachEditionsOwnNumber`; the
wording-matched rule numbers of the first concordance (the root of the error) are gone. R1-2 (GR12 not GR13) is main's `FloatingPointItem`
after the merge; R1-3's docstring was rewritten.

**Result.** The ratchet `edition-citation-sweep.json` lost 23 codes at 2002 and 20 at 2014 (510 to 487, 519 to 499); the measured refusal lines fell from 3189 to 3008 at 2002 and from 2948 to 2774 at 2014 (COBOL1627 145 to 5, COBOL1516 8 to 0,
COBOL1504 3 to 0). Seven refusal files stopped pinning a 2023 citation at other editions (`expect` now names the message, not the rule).

**What it did not close.** `kb/Work/PB6118` stays open: the other binders' sites, `SpecialNamesBinder`'s partial files, and the
sites listed in its 2026-11-27 addendum.
