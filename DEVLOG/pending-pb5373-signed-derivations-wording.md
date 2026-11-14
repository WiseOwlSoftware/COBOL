## PB5373: the conformance statement's §8 says "signed derivations"; the ten open no-witness 1985 rows stay blocked

Wave 20262032, group A, on `fe96afd75f`.

**What the re-read showed.** Of the 21 UNDECIDED 1985 rows kb/Work PB5373 decided by class, 11 are recorded (eight on
2026-11-12, three class 1 rows on 2026-11-14) and 10 are open, each blocked outside an implementer's reach: the three
ENTER rows wait for kb/Work PB5308 (still open); the five class 2 rows need an UPHELD `cobol-refuter` record each, which
an implementer cannot produce and must not write for itself; and `1985-GR-IV.4.4.2-L2.1`/`-L2.2` cannot close on
`undefined-in-rule` because the 1985 catalog files the list's closing "undefined" paragraph under item L2.5.
`audit_derivations.py --check`: 48 rows claim a derivation, 48 close on one, no findings.

**The L2 design question, settled against the design doc.** `DESIGN-spec-conformance-review.md`'s "Lists" item says a
unit after a list that continues it belongs to the colon unit, not to the last item. The 1985 catalog breaks that at
§1985:IV.4.4.2 (`cite.py --check` OK; the text "lies in GRL2.5"), so the root is the catalog extraction, not this
register. The two rows stay open; no other arm is given to them. Reported as a lead with its code site.

**On `1985-GR-XIV.3.6.4-18`.** The runtime transmits a message only when it is complete, as one line, so no SEND
transmits to a line that already holds characters. A test of two SENDs could not fail, so the row stays with the
class 2 refuter pass rather than becoming a class 4 test.

**Fixed.** The shipped conformance statement's §8 heading read "owner-signed derivations", untrue since three rows were
signed `claude: 2026-11-12`. It now reads "signed derivations (§2023:4.4 / Annex A.2; the rule's own text for 1985)";
`derivation.register-heading` matches the prefix, so the schema is unchanged. The descriptions of the current mechanism
in `docs/conformance/README.md`, `docs/DOC_INDEX.md`, the design doc, the derivation tooling's docstrings and its
§7-row finding message, and the C# readers say "signed" too, and the drift test's summary of
`EveryDerivation_StandsUnderItsOwnArm` now names the five arms it checks, not three. Comments recording the owner's
2026-09 decision (kb/Work PB386) keep their wording.

Addendum: `kb/Work/PB5373/2026-11-14-signed-wording-and-the-ten-blocked-rows.md`. No row recorded, no inventory change,
no diagnostic code used (COBOL7216-COBOL7218 returned).
