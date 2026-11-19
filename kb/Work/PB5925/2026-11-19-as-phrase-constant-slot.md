## Lead: an AS phrase's word is labelled `nn`, but it names a constant when one is declared (found by listing step 6b slice 3)

Step 7a labelled `externalizedNamePhrase`'s word `nnCobolWord1` (CobolParserCore.g4: `AS (literal | nnCobolWord1=cobolWord)`),
a word that names nothing the program declares. Step 6b slice 3 (kb/Work PB5932's dated file `2026-11-19-step-6b-slice-3`) sent
every constant-name reference through `SymbolTable.Identify`, and the AS phrase's word is one: "constant-name-1 may be used anywhere
that a format specifies a literal" (§2023:13.10.3 SR2), and the AS phrase specifies a literal, so `PROGRAM-ID P AS K`, a REPOSITORY
specifier's `AS K` and an EXTERNAL clause's `AS K` name the constant entry K when the element declares one (kb/Work PB2588). The
word is therefore a reference whose class depends on resolution, like the four kinds of the lead `2026-11-18-resolution-dependent-slots`
(ASSIGN TO's word, also a constant-name position, is labelled `ref`; `integerOperand`'s word is `refCobolWord1`).

Two things for step 7b to decide, with its completeness oracle:

- The label. As `nn`, the oracle expects no occurrence at the word, but the data bind's readers (`DataBinder.ReadConstant`, through
  `ExternalizedName.Screen`) call `Identify(..., NameClass.Constant, report: true)` there, so step 7b would record one. Relabel it
  `ref` and count it under the resolution-dependent rule, or pass `report: false` from the screen; the first matches the standard.
- The pre-bind readers. `SymbolTable.IdentifyConstantBeforeBind` (PROGRAM-ID / FUNCTION-ID, CLASS-ID, INTERFACE-ID, METHOD-ID and a
  REPOSITORY class or interface specifier's AS phrase) runs before the unit's table exists, so it can record nothing. A PROGRAM-ID's
  AS word is read only there; step 7b needs that reference recorded once, by the table, when the unit binds.
