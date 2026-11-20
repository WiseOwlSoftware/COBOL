## PB5941: the listing streams every format to its file, and the `all` row is back inside its budget

kb/Work PB5941 (branch `listing-step-pb5941`): the `all` listing (every section in text, JSON and HTML) allocated +50.2 to +69.3 % over
a compile with no listing on the parent (`tests/WiseOwl.Cobol.Benchmarks`, Release, two runs), against a budget of 25 %
(DESIGN-listing.md section 9).

**Measured first.** A probe of each part of IX113A's `all` listing put the document at +14.6 MB over no listing and the writing at
99.7 MB: the text listing 33.2 MB for a 2.6 MB file, the JSON 27.0 MB for 6.7 MB, the HTML page 39.5 MB for 6.6 MB. Each format was
rendered whole into a string before it was written (a `StringBuilder`, the string copied from it, the string encoded into the file; the
JSON serialized to a string and copied again to append its line feed), the text renderer listed every row of a section before it
paginated it, and the page printed every IL instruction twice.

**Fixed at the root.** `ListingOutput.Write(format, document, request, Stream)` replaces `ListingOutput.Render`: each renderer writes
to the stream as it renders, in UTF-8 with no byte order mark. The paginator reads rows as they are made and writes each page when it
closes. The listing's exit streams each format into its file; `--listing-output -` streams the one format to standard output as the
bytes its file would hold, UTF-8 whatever the console's code page. The HTML page shows each instruction once, under its statement's
line, and the object-code section links to that block (it holds the instructions itself only when the page has no source section).
The object code is decoded into one array per method and each statement's stretch is a slice of it.

**Measured after**, twice on each tree, alternating: `all` is +16.5 to +20.7 % over no listing and allocates 15.0 to 73.8 MB less per
compile; the probe reads the text listing at 13.3 MB, the JSON at 0.16 MB and the page at 8.3 MB (the page itself 4.9 MB, from 6.6).
A differential of the command line's `all` listing of four NIST programs against the parent: the text and JSON listings identical, the
page different only in the stretch blocks' ids and the object-code section's entries.

Also here: the benchmark's witness read the listing files after the check-only compile had overwritten them, so the step-8 and
step-9 witness lines quoted a listing with no object code (0.9 MB for IX113A, where the full compile writes 2.6 MB); it now reads them
first and requires the object code. The command line's help for `--listing-output` and `--listing-format` named the text and JSON
formats only; it names all three. The leads of the listing reviews were checked again on origin/main: every one already fixed.
