---
order: 3
---
3. **VALIDATE facility** (§2023:14.9.50, §2023:13.16–13.18, F.2 item 5 / **Annex A.4.14**), in full:
   * the `VALIDATE` **statement** (§2023:14.9.50): accepted-inert with **COBOL1580**. Its OPERANDS are still
     identifiers (kb/Work PB3568): declining the facility declines its behaviour, never §2023:14.9.50.3's syntax rules, so
     every identifier-1 is resolved (an undefined name is the ordinary **COBOL1639**) and screened against SR1
     (reference-modified, or not described in the file, linkage, local-storage or working-storage section,
     **COBOL4777**), SR2 (class index, message-tag, object or pointer, **COBOL4778**), SR4 (a 66-level entry) and SR5 (ANY LENGTH),
     **COBOL4779**. SR3 reads a `VALIDATE-STATUS` clause, which the decline below refuses by name (COBOL1708), and SR6 is **COBOL1607**.
     Pinned by `conformance:cases/14.9.50-VALIDATE/pb3568-validate-*` and the control
     `conformance:wiseowl/cases/14.9.50-VALIDATE/pb3568-validate-legal-operands-inert`.
   * the **validation clauses** of the §2023:13.16.2 *validation-clauses* group — `CLASS` (§2023:13.18.11), `DEFAULT`
     (§2023:13.18.17), `DESTINATION` (§2023:13.18.18), `INVALID` (§2023:13.18.31), `PRESENT WHEN` **format 2** (§2023:13.18.41),
     `VARYING`'s validation leg (§2023:13.18.64), `VALIDATE-STATUS` / `VAL-STATUS` (§2023:13.18.62) — and the `VALUE`
     clause's **format-5 content-validation entry** (§2023:13.18.63; §2023:13.16.2 format 4): refused by name,
     **COBOL1708**.
   * the **EC-VALIDATE** family (§2023:14.6.13.1.6 Table 13): refused by name, **COBOL1710**.
   An obsolete optional element (§2023:4.2.13). ⚠ `PRESENT WHEN` and `VARYING` are **shared** with report writer,
   which IS claimed (Annex A.4.11 items 14 and 20; §5 records it *Partial* with both implemented) — the two legs
   are told apart by WHERE the clause is written, and the report-group forms are untouched.
   ⚖ The **`CLASS` clause (§2023:13.18.11)** is part of this decline by **owner decision, 2026-09-02**
   (`kb/Work PB375`). Annex A.4 never lists it under any optional module and it is the only clause of the group
   carrying no "obsolete feature" NOTE, so the disposition was open; the deciding ground is §2023:13.16.2 itself,
   whose printed Format-1 *validation-clauses* block opens with `[ class-clause ]` and maps it to
   "13.18.11, CLASS clause", while §2023:13.18.11.1 leaves the clause no content outside the module. A.4.1's second
   sentence carries the licence to an optional element's associated rules "even if not explicitly listed".
   ⚠ **`CLASS` is reserved at every edition** (§2023:12.3.7's SPECIAL-NAMES `CLASS` clause, continuous since 1985),
   unlike its siblings — so at `--std 85` a data description entry writing it is an ordinary syntax error, not
   this decline, and §2023:12.3.7's own `CLASS` clause together with the §2023:8.8.4.4 class condition stay **claimed and
   implemented**.
   ⚠ §2023:13.18.40.4 GR15/GR19 (PICTURE "takes effect during the format validation stage") have no syntax of their
   own; they are general rules about what a supported clause does *inside* a VALIDATE statement, so the
   statement's own COBOL1580 is their witness.
