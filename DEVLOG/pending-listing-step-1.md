## PB5923: the source listing, step 1 - the 1985 standard minimum

COBOL-85 requires each compiler to produce "an output listing of the source program in reference format" (§1985:IV.7.1),
and two Nucleus elements have no function anywhere else: the '/' comment line, which "causes page ejection prior to
printing the comment line in the listing" (§1985:IV.7.2.4), and the DATE-COMPILED paragraph, which "is replaced during
compilation with a paragraph of the form" `DATE-COMPILED. current date.` (§1985:VI.3.4.4 GR1). The compiler wrote no
listing in any edition. This builds step 1 of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5925's decision).

**What was built.** A new assembly, `WiseOwl.Cobol.Listing`, that references no project: the listing model
(`ListingDocument` with its header, files, inclusions, lines, programs and section statuses), `ListingRequest`, the one
spelling of the compilation instant (`ListingTimestamp`), the paginator (`PageComposer`) and the text renderer. The
front end now RECORDS what it already decided and threw away: each physical line's kind (`PhysicalLineKind`, replacing the
converter's private enum, with '*' and '/' distinct; refined by `LogicalLineBuilder.Classify` for blank, comment and
directive lines), each COPY inclusion and the resultant range its library text occupies (`TextStageTrace`, a no-op
`None` without a listing), and what the parse says of the source units, DATE-COMPILED paragraphs and comment-entry heads
(`SourceUnitFacts`), all from the one LISTED pass beside `LineMap`. The compiler's builder
(`WiseOwl.Cobol.Listing.Building`) places every position through one index (`ListingLineIndex`) and builds the source
section, the program ranges and the page breaks; `CompilerDriver.Compile` is now the one exit over `CompileCore`.
`CompilationInputs.ReadCompilationTime` captures the instant once, so the heading, DATE-COMPILED's replacement and
WHEN-COMPILED denote one time (§1989:A.2.46.3 RV3). The CLI gains `--listing [on|off]` (on by default at `--std 85`),
`--listing-output BASE|-`, `--listing-page-length` and `--listing-page-width`.

**Records.** `1985-GR-VI.3.4.4-1` NOT-IMPLEMENTED -> CONFORMS; `1985-GR-VI.3.3.4-1` and `1985-GR-X.3.1.4-1` PARTIAL ->
CONFORMS, witnessed by `Listing1985Tests`. 1985 went from 3 GAP to 0 GAP. DOC-A.1-117 now documents the listing.

**Design corrections** (each marked "(step 1)" in the design): the builder has its own namespace and layer and names no
composition-root type; `ListingLine.Replacement` says which replacement a line had, because DATE-COMPILED's prints in place
and REPLACE's (step 4a) prints under the line; the no-work drift test lives in the Unit assembly; the Conformance test reads
the instant from the listing, since the clock seam is internal. The round-5 review leads are answered: backend diagnostics
cross the seam as the front end's `Diagnostic` (R5-1), a use-site `def` label makes a section or paragraph header a
definition (R5-2), and the evidence file names `SymbolTable` (R5-3).

**Found on the way.** The tests found a listing request on a missing source throwing in the outcome map; fixed. Recording
the census the new project needs showed four tolerated architecture rows grown on main since the last census, none of them
the listing's (kb/Work PB5923 lead). The KEPT row `1989-RV-A.2.46.3-3` rests on a derivation whose "no listing" premise is
now false; its re-adjudication needs a suite case that can read a listing (kb/Work PB5926).

**Review round 1.** The first heading line cut its own left part when it was too long, so a release build's long version
pushed out the compilation instant and the program-name. The line now gives up the version first, then the edition, and
never the instant or the page number. Two other heading problems were fixed with it: a long source path now keeps its
file name, and a long COPY banner wraps. New tests cover every outcome a listing can follow: check-only, a refused
assembly write, a refused listing write, an output refused before the source is read, and the listed pass of a failed
parse. A test also holds the outcome map total, and the line index is tested on hand-built maps. A compilation without a
listing no longer evaluates the trace's arguments: each recording call checks `TextStageTrace.Records` first, and
`TextStageTrace.None` throws if called. kb/Work/PB5923/2026-11-18-listing-step-1-review-r1.md says how each finding was
fixed.
