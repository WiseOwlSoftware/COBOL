## Entry 3526 — 2026-11-12 10:36 PST — PB5373: how the 21 no-witness rows of the 1985 edition close; the account 1 hold lifted

Notes only; no code changed.

**The decision (kb/Work PB5373).** kb/Work PB4995 listed 21 rules of the 1985 edition whose confirmation record was
UNDECIDED because no draft program can witness them. The Mythos session decided them by class: (1) a rule that itself
declares the result undefined closes on a derivation of a new mechanical arm, `undefined-in-rule`, since the 1985 standard
has no Annex A.2; (2) a rule whose antecedent needs a medium or device WiseOwl defines none of closes on
`unpopulatable-antecedent`, where `docs/CONFORMANCE.md` documents the absence; (3) a rule about an element WiseOwl refuses
or declines by a documented decision is DOCUMENTED-NON-SUPPORT, witnessed by the element's named diagnostic (the ENTER
rows wait for PB5308); (4) a rule whose antecedent the implementor defines, and WiseOwl does define, gets a WiseOwl-only
test, a harness test with the environment, or a `unit:` test, never a derivation; (5) the listing rule and the
computer-name rule are settled by what WiseOwl produces and by an acceptance test; (6) a syntax rule that cannot be broken
alone is witnessed by a refusal test asserting its own diagnostic among those at the line.

Checking the cited rules corrected one premise: PB4995 called `1985-GR-VII.2.8.4-6` "refused COBOL1778"; COBOL1778 is a
warning, and the RECORD DELIMITER clause is accepted inert (`docs/CONFORMANCE.md` section 2, row 26). The three 1985
citations in the note passed `cite.py --check`.

The decision also moves the signature on a derivation from the owner to the deciding session (`claude: <date>`, mapped to
its decision note) and keeps the independent read: an argued arm is recorded only with an UPHELD `cobol-refuter` record.
The owner set the original bound himself (PB386), so the session reported the change to him.

**The tooling it needs (kb/Work PB5374, open).** No 1985 row carries a derivation today (0 of the files under
`tests/version-matrix/inventory-1985/`, 45 in the 2023 inventory). The note asks for the fifth arm, the signature form with
its refuter requirement, and section 8 rows checked against their own edition's catalog.

**The hold (kb/Work PB5357, dated file).** The owner lifted the account 1 hold for this week and announced a reset; his
three messages are quoted. A Mythos or Fable dispatch still needs his approval per dispatch.

