---
order: 3
---
3. **VALIDATE facility** (§2023:14.9.50, §2023:13.16–13.18, F.2 item 5 / **Annex A.4.14**), in full:
   * the `VALIDATE` **statement** (§2023:14.9.50; ISO/IEC 1989:2002 §14.8.46): **implemented in stages** (kb/Work PB5577,
     `docs/rearchitecture/DESIGN-validate-facility.md`). Built: the statement, its operands (one implicit VALIDATE per identifier-1,
     §2023:14.9.50.4 GR3), the elements of the operand with a table's occurrences looped (GR2, GR7 b), stage one's PICTURE/USAGE
     format check of numeric, alphabetic and boolean items (GR6 a) and stage five's **EC-VALIDATE-FORMAT** (GR6 e), nonfatal.
     Not built yet, each in its stage: the format check of an edited picture, the DEFAULT value (GR6 a), DESTINATION (stage two),
     content validation (stage three), relation validation (stage four), the VALIDATE-STATUS update, the RECORD VARYING padding (GR8)
     and the overlap rules (GR9, GR10). Its OPERANDS are screened against §2023:14.9.50.3: every identifier-1 is resolved (an undefined name
     is the ordinary **COBOL1639**) and screened against SR1 (reference-modified, or not described in the file, linkage, local-storage or
     working-storage section, **COBOL4777**), SR2 (class index, message-tag, object or pointer, **COBOL4778**), SR4 (a 66-level entry) and
     SR5 (ANY LENGTH), **COBOL4779**. SR3 reads a `VALIDATE-STATUS` clause, which the decline below refuses by name (COBOL1708), and SR6 is
     **COBOL1607**. An operand part whose storage the element walk cannot reach is refused by name, **COBOL7363**, never validated in part.
     Pinned by `conformance:cases/14.9.50-VALIDATE/*` and the control
     `conformance:wiseowl/cases/14.9.50-VALIDATE/pb3568-validate-legal-operands-inert`.
   * the **validation clauses** of the §2023:13.16.2 *validation-clauses* group — `CLASS` (§2023:13.18.11), `DEFAULT`
     (§2023:13.18.17), `DESTINATION` (§2023:13.18.18), `INVALID` (§2023:13.18.31), `PRESENT WHEN` **format 2** (§2023:13.18.41),
     `VARYING`'s validation leg (§2023:13.18.64), `VALIDATE-STATUS` / `VAL-STATUS` (§2023:13.18.62) — and the `VALUE`
     clause's **format-5 content-validation entry** (§2023:13.18.63; §2023:13.16.2 format 4): refused by name,
     **COBOL1708**.
   * the **EC-VALIDATE** family (§2023:14.6.13.1.6 Table 13): **EC-VALIDATE-FORMAT** is raised by the statement's stage five and resolves as
     an ordinary exception-name; EC-VALIDATE-CONTENT, EC-VALIDATE-RELATION and EC-VALIDATE-VARYING resolve too but no statement sets them until the
     stages that raise them are built (§2023:14.6.13.1.1 licenses raising nothing for an unimplemented optional element).
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
   own; they are general rules about what a supported clause does *inside* a VALIDATE statement, and the statement's stage-one
   format check is their implementation for the items it covers.
