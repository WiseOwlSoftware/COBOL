## PB5951: the listing's object code shows each method's exception-handling regions, as ILDASM prints them

The owner, reading NC105A's listing on 2026-11-19: "I see endfinally and leave instructions, but there are no pseudo try or catch
instructions, such as would appear in IL source code", and "catch also" (kb/Work PB5951). The object-code section (DESIGN-listing.md
section 6.9) decoded each method body's IL and never read the body's exception-handling clauses, which ECMA-335 II.25.4.6 keeps in
the same body, so a `leave` or an `endfinally` stood with no protected block around it.

**Read once.** `ObjectCodeMapReader` now reads each body's clauses beside its IL into `ListingMethod.Regions`: the kind (catch,
filter, finally, fault), the protected block's and the handler's offsets, a filter's start and a catch's type, named by
`IlDisassembler.TokenName`, the one naming of a token the instructions' operands use too.

**Laid out once.** `ExceptionRegionLayout` turns a body's clauses into the lines ILDASM prints (ECMA-335 II.19): `.try {` and
`} // end .try` around a protected block (one for several clauses that protect the same block), then each handler, `catch <Type> {`,
`finally {`, `fault {`, or a filter's `filter {`, `} // end filter` and `{ // handler`, each closed by `} // end handler`. At one
offset the closing lines come first, innermost first, then the opening ones, outermost first; a body whose blocks overlap (which
ECMA-335 I.12.4.2.7 forbids) fails loudly. Each line goes to the stretch of code that holds its offset, so a block that ends where
the next statement begins closes at the top of that statement's stretch, and every line prints exactly once.

**Printed, not decided, by the renderers.** One merge (`ObjectCodeText.CodeLines`) and one spelling (`RegionText`) serve the text
listing and the HTML page, each indenting a level two columns, the instructions inside it too. The JSON listing holds the clauses on
each method and the placed lines on each stretch; the schema was regenerated within 1.0 (no release has shipped the JSON listing),
and its drift test is now a preflight class.

**Tests.** `ExceptionRegionLayoutTests` (hand-built nested, adjacent, shared-block and handler-nested regions, written by hand from
ECMA-335) and `ListingExceptionRegionTests` (a program whose OPEN compiles to a `finally`, whose ADD ON SIZE ERROR compiles to nested
protected blocks with two `catch` clauses each, and whose reference modification under `>>TURN EC-BOUND-REF-MOD CHECKING ON` compiles
to a `filter` inside a `finally`; the expected clauses read from the assembly with `System.Reflection.Metadata`). Each was shown able
to fail: dropping the filter clauses in the reader, opening the innermost block first, and dropping the lines in the merge.

**Leads carried.** Every lead of the listing steps' review records was checked on origin/main, and again for the two branches
that landed while this one ran. Four were still true and are fixed here. The last `§2023:14.7.8 rule 1 / rule 2` comments now name
GR1 and GR2 (the same "rule N" form is written at 1544 other sites, which kb/Work PB4976's sweep now counts). A MERGE in a SORT
procedure, a removal whose registry row pins a code of its own, was reported by hand without its edition flag and was missing from
the table of flagged constructs; it now reports the registry's verdict, and the drift test that holds the table complete reads every
row's pinned code instead of the three band codes (a reserved word used as a user word under `--permissive` is the one removal still
without a flag: kb/Work PB5947's new lead). A doc comment named a test that does not exist, and the HTML page's script resolved the
link target twice; both are one now.

**Measured** (section 9): nothing a compilation without a listing runs changed. The `all` request is +0.9 to +5.8 % over the
parent's on a full compile, inside the run-to-run spread on three of the four programs; the first build wrote an indentation
attribute on every instruction row of the HTML page, and writing it only where a region nests the row took 6.5 MB off IX113A. The
`all` row stays over its budget, as before (kb/Work PB5941).
