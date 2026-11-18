## Entry 3840 — 2026-11-18 06:43 PST — PB5925: the source listing, step 4a - the directive hooks, witnessed at 2023

Step 4a of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5925's decision): the standard's listing controls of 2002 and
later now act on the listing. `>>LISTING OFF` stops the listing of the lines after it until ON, and every LISTING directive
line is listed (§2023:7.3.18.3 GR2 to GR5); a listed `>>PAGE` line starts a page (§2023:7.3.19.4 GR2); the library text of
`COPY ... SUPPRESS` is not listed and its COPY statement is (§2023:7.2.3.4 GR4); a LISTING directive in library text does not
outlive it (GR5); `>>DISPLAY` UPON LISTING, or with no UPON phrase, prints `DISPLAY: text` at its directive even under
LISTING OFF (§2023:7.3.12.4 GR1, GR2, GR5 a, GR6); and a line REPLACE or COPY REPLACING changed is printed as written, marked
`R`, with the text compiled from it under it.

**One holder of the LISTING state.** The design had the text stages record each LISTING directive and the listing's
visibility pass decide GR5 (the state at the end of library text) by itself. The code proved that incomplete: the LISTING
directive is pushable (§2023:7.3.22.4 GR2), so a PUSH saves its state and a POP restores it, and the implicit PUSH ALL / POP
ALL of an exception-checking PERFORM (§2023:14.9.28.4 GR14) restore it at END-PERFORM; neither is visible to a pass over
physical lines, and GR5 has to be the driver's anyway, since a PUSH after the COPY saves the restored state. The
conditional-compilation driver now holds the state (ON by default), carried by its `DirectiveStateStack`
(`DirectiveStateRegistry`'s LISTING row stops saying "NONE HELD"), restored in `RenderCopybook`, and records every change as
a `TextListingSwitch` (a LISTING directive, a written POP, an implicit POP at its END PERFORM line, an inclusion's end). The
listing replays the changes and decides nothing. The trailing implicit ops, keyed after the last directive and never applied
before, are applied at the end of the source text when a listing records.

**Every fact in the right copy.** The trace records each directive, DISPLAY transfer and COPY REPLACING match with the
inclusion open when the stage meets it, so a copybook copied twice places each fact in its own block; a REPLACE match, made
after every inclusion closed, is recorded by its output line and carried to the resultant frame with the inclusion ranges.
`CompileOutputLine` gains no origin, against the design: `ApplyDisplay` decides once where the LISTING destination goes (into
the trace when a listing is produced, a new `CompileOutputStream.Listing` transfer otherwise, which the command line writes
to standard output as before). The `R` rows are the resultant text (`Frontend.ResultantText`) the one index places on the
line, so a replacement that spans or joins lines shows whole. `DirectiveEffectsBuilder` is the one placement of these facts,
and a fact no listing line holds throws.

**Model and schema.** `ListingDocument.Displays` (`ListingDisplay`), `ListingInclusion.Suppress`,
`ListingReplacement.Replacing`; the schema is regenerated and stays 1.0 (no release has shipped the JSON listing).

**Records.** The seven 2023 rows that rested on "no listing is produced" (GR-7.3.18.3-2 to -5, GR-7.3.19.4-2,
GR-7.2.3.4-4, -5) are witnessed by the new `Listing2023Tests`, and their owner-signed derivations of 2026-10-08 are withdrawn
(their files deleted, the RepoDrift population and the signer note say so). DOC-A.1-200 leaves DOCUMENTED-NON-SUPPORT: its
condition (the implementor producing listings) holds, so its §7 cell opens "Provided" and the row is CONFORMS.
DOC-A.1-53, -54 and -117, D-IGNORED-DIRECTIVES, and the notes of GR-7.3.12.4-1, -2, -5, -6, GR-7.3.18.3-1, GR-7.3.19.4-3 and
FMT-7.3.18.2 now describe a compiler that produces a listing; a renamed DISPLAY test is retired as a witness with its reason.
The 2023 inventory stays at 4 GAP.

**Tests.** `Listing2023Tests` (Conformance, nine cases: LISTING OFF/ON, PAGE and PAGE under OFF, COPY SUPPRESS, GR5, PUSH /
POP, the GR14 implicit POP, DISPLAY into the listing, DISPLAY with no listing, REPLACE and REPLACING), each value read off the
test's own source and the rule; `ListingVisibilityTests` (one per new rule); `DirectiveStateStackTests`' behavioural case for
the newly carried LISTING row; `ListingOffDoesNoWorkDriftTests` extended with a 2023 program that reaches every new recording
point, with its control. Mutations showed the checks can fail: with the trailing implicit ops and the GR5 recording removed,
`AListingOffInAWhenPhrase_EndsAtEndPerform` and `AListingOffInLibraryText_DoesNotOutliveIt` went red.

**Measured** (design section 9; `tests/WiseOwl.Cobol.Benchmarks/README.md`). Step 4a touches the no-listing path, so `None` was
measured against the parent (14fc6f26ac) back to back: the front end and bind allocate within 0.21 MB of the parent on all
four programs, and the full-compile `None` rows are at or under it; the budget holds. Time rows are noise again (StdDev
20-40 %, kb/Work PB5927).

