---
heading: "## 8. Rules with no observable obligation — owner-signed derivations (§2023:4.4 / Annex A.2)"
columns: ["Rule", "Arm", "Names", "The derivation", "Signed", "Refuted"]
---
> **⛔ THIS SECTION IS THE ONE ALTERNATIVE TO A SPEC-DERIVED TEST, AND THE OWNER OPENED IT** — decision
> `kb/Work/PB386`, 2026-09-03, implementing `docs/rearchitecture/DESIGN-spec-conformance-review.md` **§1.1**.
> Some normative rules impose no obligation a conforming program can observe, and for those §1(c)'s covering
> test is not merely unwritten, it is **unmeetable**: any golden would pin an IMPLEMENTATION CHOICE as though it
> were the standard's answer, which is the exact thing the traceability inventory's `spec-derived` clause exists
> to prevent. A determination here stands in place of that test — **and in place of nothing else**. The verdict,
> its evidence, and (for an Annex A.1 row) the §7 determination and a greenfield implementing site are all still
> paid in full.
>
> ⚙ **The row key is `DRV-` plus the inventory `rule-id`, and the inventory row's `derivation` field carries
> `docs/CONFORMANCE.md#DRV-<rule-id>` COMPUTED from that same id** — never typed, for the reason §7's key is
> computed: a determination filed against a neighbouring rule cannot be *spelled*. Every cell below is machine
> read: `scripts/spec/audit_derivations.py` and `SpecTraceabilityInventoryDriftTests` both evaluate the arm, and
> `Schema.state_for` refuses to close a row whose determination does not stand. A row of EVERY held edition may
> stand here (kb/Work PB5374): an older edition's rule-id carries its year (`DRV-1985-…`), each row is checked against
> its own edition's catalog, and a row here that no row of a held edition claims is refused, never skipped.
>
> ⚙ **Five arms, and two of them are MECHANICAL.**
> · **`undefined-A.2`** is MECHANICAL. Annex A.2 is *"a list of the COBOL language elements within this Working
> Draft International Standard that are explicitly undefined"* (§2023:A.2), and §2023:4.4 2) makes the point that matters
> here: *"A COBOL run unit that allows these situations to happen is a conforming run unit, although the
> resultant execution is not defined by standard COBOL."* Each A.2 item ends in the standard's own citation of
> the rule it is about, so `scripts/spec/extract_annex_a2.py` resolves item → `rule-id` and the checker requires
> the named item to cover **this** row. Naming a real item about a different rule is refused.
> · **`undefined-in-rule`** is MECHANICAL too (kb/Work PB5373 class 1, PB5374), for an edition with no Annex A.2
> (1985): the rule's OWN text declares the result undefined or unpredictable. The `Names` cell quotes those words
> in double quotes (four words or more, containing *undefined* or *unpredictable*), and the checker requires the
> quotation to lie inside the row's own catalog text. A row Annex A.2 lists is refused this arm and keeps
> `undefined-A.2`: one fact, one arm.
> · **`unpopulatable-antecedent`** and **`indistinguishable-consequent`** are reviewed ARGUMENTS. Their shape is
> checked — the closed set must be STATED, the named rule must EXIST and not be this one — and the `Signed`
> cell is the record that the argument was read. An unchecked argument would make the GAP metric cheaper to move
> than the work it stands for, which is the failure this whole register is built against. The cell is one of the
> dated owner signatures `inventory-schema.json` lists under `derivation.signatures`, each mapped to the decision
> that read the rows it signs: `owner: 2026-09-03` (PB386, and the rows signed under it through 2026-09-25, whose
> text names a later decision where there was one) and `owner: 2026-10-08` (the PB1636 batch of 32), and `owner: 2026-10-23` (PB3539 option (i); PB1636's last derivation, DRV-GR-12.4.5.9.4-2).
> From kb/Work PB5373 (2026-11-12) the session that decides the rows signs them, `claude: <date>` mapped to its
> `kind: decision` note (`claude: 2026-11-12`, PB5373). The owner's signature was the independent read of an argued
> arm; for a `claude:` row it is a `cobol-refuter` pass that tried to write the witness the derivation says cannot
> exist: the `Refuted` cell names the refuter's record file, which must hold a record of the row's rule-id reading
> UPHELD. A mechanical arm, and every row the owner signed, carries `—` there.
> · **`definitional`** (owner decision `kb/Work/R43` item 4, 2026-09-24, extending `PB386` — answers `PB468`
> Q9) is a reviewed argument too: the rule DEFINES a property of the USER'S program and imposes no obligation on
> the implementation, so there is nothing an implementation can do that a program could observe as meeting or
> failing it. The `Names` cell must open `defines` and state the property and whose it is; a rule that says what
> an implementation or implementor SHALL do is never this arm. Its first population was §2023:4.4 GR1/GR2; the owner
> extended it on 2026-10-08 (kb/Work PB1636, the five definitional rows of that batch) to a CONSTRAINT on the
> user's run unit that no rule gives a consequence (§2023:4.4 2) then leaves a violation undefined) and to a TERM
> DEFINITION that no other rule reads.
>
> ⚖ **This does not reverse `kb/Work/PB280` Q2.** An Annex A.1 row asks the implementor to STATE a choice, so
> "there is nothing to observe" is unfalsifiable there and still cannot close a row: the A.1 row below is
> admitted because its determination **is** stated, in §7, and it names implementing code — only its WITNESS is
> impossible. `Schema.state_for` encodes that by ORDER, charging a DOC row's anchor and observability costs
> before it ever consults this register.
