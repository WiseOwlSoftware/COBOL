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

**Leads carried.** Every lead of the listing steps' review records was checked on origin/main: all but one were already fixed or
belong to branches not yet landed. The one still true, step 7b's R1-1, was fixed here: the last `§2023:14.7.8 rule 1 / rule 2`
comments now name GR1 and GR2. The same "rule N" form is written at 1544 other sites, which kb/Work PB4976's sweep now counts.
