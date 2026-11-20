## PB5947: the source listing's table of flagged constructs by edition, and the edition flag as a fact on its diagnostic

Section 6.11 of `docs/rearchitecture/DESIGN-listing.md` promised that the diagnostic summary "adds a table of flagged constructs by
edition"; step 9's check of what is built found that no step had built it (kb/Work PB5947). It is built: the text summary ends with
the table, the JSON carries it as `flaggedConstructs`, and the HTML page's diagnostics section holds it with each listing line a link
to its diagnostic.

**The root was the fact, not the table.** The design's note asked which diagnostics are edition flags, and a list of codes beside
the catalog was the obvious answer and the wrong one. A removed construct is reported under the code its registry row pins, a
pre-band code for some rows, and four front-end stages spelled the flag codes themselves (the reference-format stage's column-7
continuation and word continuation, COPY REPLACING's non-pseudo-text operand, the directive-word rule). So the flag became a record,
`EditionFlag(Kind, Construct, Description, Edition)`, stamped on the diagnostic where it is raised. It is made in two places:
`ConstructRegistry.VerdictAt`, the one verdict of a registry row at an edition (severity, code and flag), which `Check`,
`GateErrorCodeAt` and the four front-end stages now read, and `EditionContext.Extension`, the §2023:4.2.10 seam. Both sinks
(`EditionContext.Report`, the front end's `BagSink`) carry it onto the record. The listing maps it (`ListingFlag`) and groups the
placed diagnostics by edition, kind and construct (`DiagnosticSectionBuilder.FlaggedConstructs`); the renderers share one set of
words (`FlaggedConstructText`).

"By edition" is the edition of the rule that flags the construct: the row's `ObsoleteIn` or `RemovedIn`, so a debugging line at
`--std 2023 --permissive` is a 2014 removal; an extension takes the edition compiled to. A rule a later edition added that
`--permissive` relaxes reports under its own code and is no flagged construct; nor are the FLAG-85 directive's warnings or the
reserved-word check.

**Held so the next flag is in the table by construction.** `EditionFlagDriftTests` checks every registry row at every edition on both
axes (the flag its row names, through `Check` and through the binder's records), every flaggable extension, and, with a Roslyn scan
shown red on planted text, that no source outside the funnels names a flag code. `FlaggedConstructTests` covers the grouping, the
kind map and both renderers over hand-built diagnostics; `ListingFlaggedConstructsTests` compiles one program writing a construct of
each kind in the front end and the binder and finds the table the source and the registers call for in the model, the text, the JSON
and the page. Each test was mutated red: the verdict's flag dropped, an extension's edition changed, a flag code planted in a front-end
source, the kind map crossed, the binder's error path dropping the flag, the table re-derived from the code.

**Cost.** One run each of the front end and bind against the parent: every `None` row within 0.07 MB of the parent's; the NIST
programs flag nothing at `--std 85`, and the listing rows match but SM201A's noisy ones (the benchmark README's PB5947 table).

**Leads carried.** Every lead of the twenty-one listing review files and the dated file of slices 2 and 3 was checked on origin/main;
all but one had been fixed by slice 7 and the OO lane. The one still true was the residue of the step 7b review's `14.7.8 rule 2`
sweep: two comments in `ConditionRenderer` and the notes of inventory rows DOC-A.1-203 and SR-14.9.13.3-1 now cite
§2023:14.7.8 GR1 and GR2 by designator.
