## Landed by branch `listing-step-pb5941` (w20262104w): every format streams to its file, each instruction once on the page

**Measured first.** A Release probe of each part of the `all` listing (a temporary entry point in the benchmark project, not
committed; `GC.GetTotalAllocatedBytes(true)` around each part after three warm compiles), IX113A on origin/main 8dd07d2eab:
the document +14.6 MB over no listing; writing it 99.7 MB: text 33.2 MB for a 2.6 MB file, JSON 27.0 MB for 6.7 MB, HTML
39.5 MB for 6.6 MB.

**Mechanism.** Each format was rendered WHOLE into a string and then written: `ListingOutput.Render` returned the text of a
`StringBuilder` (two bytes a character, then copied by `ToString`), `ListingWriter` encoded it with `File.WriteAllText`; the JSON
writer serialized to a string and concatenated `"\n"` (a second copy); the text renderer materialized every row of a section into a
list before paginating it. The page printed every IL instruction twice (under its statement and in the object-code section). The
object code's stretches were copied out of each method's decoded list.

**Fixed at the root** (commit ff0b71271f): `ListingOutput.Write(format, document, request, Stream)` in place of `Render`, each
renderer writing as it renders (`TextListingRenderer.Write`, `HtmlListingRenderer.Write` over a `StreamWriter`,
`JsonListingWriter.Write(document, Stream)`); `PageComposer.Compose(TextWriter, IEnumerable<ListingRow>, ...)` holding one page;
`ListingWriter` streaming into a `FileStream`, the CLI's `-` into standard output as UTF-8; the page's instructions once (under the
line, the object-code section linking to the block, `HtmlListingRenderer.RangeId`); `IlDisassembler.Decode` returning one array per
method body and each stretch an `ArraySegment` of it. Design: DESIGN-listing.md sections 4.2, 7, 7.1, 7.2, 7.3, 8, 9, 10, 11, 13,
marked "(PB5941)".

**Measured after**, twice on each tree, alternating parent 8dd07d2eab and branch ff0b71271f (`tests/WiseOwl.Cobol.Benchmarks/README.md`,
the PB5941 table; raw output `E:/COBOL-coord/scratch/w20262104w-pb5941/bench-{parent,branch}-{1,2}.txt`): `All` over `None` on a
full compile is +16.5 to +20.7 % on the branch (parent +50.2 to +69.3 %), inside the 25 % budget on every program and run; `All`
allocates 15.0 to 73.8 MB less. The probe after the change: text 13.3 MB, JSON 0.16 MB, HTML 8.3 MB (the page 4.9 MB).

**Differential** (the CLI's `all` listing in every format of IX113A, NC105A, NC218A and SM201A, branch against parent, the side's
directory and the instant normalized): the text and JSON listings identical; the page's differences all of three classes (each
stretch block gaining its `id="C<n>"`, the object-code section's entries linking to it without the instruction tables, the closing
`</div>` lines the diff aligns differently, as many removed as added).

**What remains**, recorded and not a defect: the text listing allocates about five times its size, one string per printed row
(the paginator's row is a string). The budget holds with it (section 9), so the row stays a string.
