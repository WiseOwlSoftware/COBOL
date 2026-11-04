## Four notes: a PERFORM transcription defect, the Communication facility at 85 and 2002, and two leads of wave 20261866

PB4676: `specs/ISO_COBOL.md` drew PERFORM Format 3 with imperative-statement-1 inside the repeated WHEN brace. Folio 682 (PDF
page 712), rendered and read, prints it outside; `CobolControlFlow.g4` already followed the page. The figure is corrected to
match print, since an extractor of format alternatives reading the markdown (PB4614's catalog) would have inherited the error.

PB4677: the owner decided on 2026-11-04 (PB4614 decision 12) that WiseOwl implements the Communication facility, the
COMMUNICATION SECTION and its statements, at COBOL-85 and 2002. Nothing in the grammar names it today. Filed MAJOR with the nine
NIST CM programs as witnesses; no deep-dive doc owns the facility, so a design section comes first.

PB4678: `CheckValueLiteralClassesPass` never walks `Forest.TypeDecls`, so an oversize national literal on a USAGE NATIONAL
numeric-edited TYPEDEF is refused only at each user of the type, and not at all if the type is unused. Lead R2-1.

PB4679: `EntryConventionGateTests` carries a theory that repeats what construct row `options-entry-convention-2014` and two
goldens pin. Lead R1-2.
