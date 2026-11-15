---
order: 4
---
4. **Screen handling** (§2023:13.9, optional §2023:4.2.7; Annex A.4.2): the SCREEN SECTION, every screen description
   entry clause, the SPECIAL-NAMES `CURSOR` and `CRT STATUS` clauses, the OPTIONS `INITIALIZE … SCREEN`
   target, ACCEPT format 3, DISPLAY format 2, `SET … ATTRIBUTE`, and the EC-SCREEN family. Not provided —
   and **REFUSED, not accepted inert** (2026-09-02, kb/Work PB260). Annex A.4.1: "An implementation shall
   accept the syntax and provide the functionality for an optional element only when support for that
   language element is claimed by the implementor." Unclaimed ⇒ the syntax is not accepted, so every one of
   the seven source shapes draws a named **Error**: `COBOL1560` for the data / environment surface and
   `COBOL1707` for the procedure surface, both from the one funnel
   `src/WiseOwl.Cobol.Compiler/Binding/ScreenFacility.cs`, whose message names the construct the program actually
   wrote ("the AUTO clause (ISO §2023:13.18.3)"). Two codes and not
   one because a Format-3 ACCEPT needs a screen-name and a screen-name needs a SCREEN SECTION: with a single
   code every statement witness would pass on the section's own diagnostic. ACCEPT format 3 and DISPLAY format 2
   are named in every spelling — bare, positioned, and with the exception phrases with or without the positioning
   phrase (kb/Work PB1528: the unpositioned `ACCEPT screen-name ON EXCEPTION …` used to be a parse error). Witnessed
   by 43 negative-corpus programs `a42-*` (`conformance-suite/wiseowl/cases/`) and by `ScreenFacilityConstructDriftTests`, which reads
   `CobolScreen.g4` and fails when a grammar alternative has no clause row — so a clause added to the grammar
   cannot ship un-named. At `--std 85` the posture is pinned by `negative/a42-screen-section-at-85`.
   ⚠ Declining the module does **not** cost the user its WORDS. §2023:8.10 makes fifteen screen words
   context-sensitive and §2023:8.9 reserves none of them at any edition, so §2023:8.3.2.1 rule 3 leaves each a legal
   user-defined word everywhere (`01 REVERSE-VIDEO PIC X.`); §2023:8.9 adds `COL`, `COLS`, `COLUMNS`, `CRT`,
   `CURSOR` and `SCREEN` in 2002, so each is a legal COBOL-85 user word (`01 CURSOR PIC X.`); only `COLUMN`
   is reserved at every edition. All 22 carry `cobol-words.json` `nameSlot` rows, and the POPULATION is
   DERIVED from `CobolScreen.g4` plus the two SPECIAL-NAMES screen clauses rather than listed, so a screen
   token added without a name slot fails a gate instead of costing a word in silence. Corpus lanes:
   `{85,2002,2014,2023}/pb301_screen_words_as_user_words`, `negative/pb301-screen-words-reserved-from-2002`
   (both the data-name and the §2023:12.3.7 system-name slot §2023:8.3.2.1 rule 1 bars) and
   `negative/pb301-column-reserved-at-every-edition` (kb/Work PB301).
   > **The A.4.2 item-10 EC-SCREEN naming surface used to be the one SILENT position in this facility, and it
   > is now refused with the rest** (kb/Work PB302, closed by the PB260 wave). Item 10 makes the EC-SCREEN
   > condition NAMES optional wherever a program can write one — the RAISING phrase of the EXIT and GOBACK
   > statements, the RAISING phrase of the procedure division header, the USE statement, the WHEN phrase of
   > the PERFORM statement, the RAISE statement, and the TURN compiler directive. **Those six positions need
   > no SCREEN SECTION**, so none of them reached the section-level diagnostic, and the five EC-SCREEN names
   > sat in `ExceptionCatalog` with no setting site and no diagnostic at all: `>>TURN EC-SCREEN-LINE-NUMBER
   > CHECKING ON` compiled clean against a facility that does not exist. §2023:14.6.13.1.1 licenses raising NO
   > condition for a level-3 name belonging to an optional element, but it does not license ACCEPTING the
   > name. All six positions now resolve through the one EC-name seam that serves every declined module,
   > `EcNameResolution.Advise` over its `DeclinedEcFamilies` table, where this module is one Error row with
   > descriptor `COBOL1707` (not `ScreenFacility.cs`; kb/Work PB374), whose message names the
   > exception-name and the facility (`negative/a42-ec-screen-raise`, `-use`, `-turn`, `-perform-when`).
