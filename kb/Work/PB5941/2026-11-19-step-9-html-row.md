## Step 9 adds the HTML page to the `All` row: +7.3 to +36.1 MB more, three times the page's size (w20262104j, 2026-11-19)

Listing step 9 (branch `listing-step-9`) makes `CompileBenchmarks.ListingMode.All` write every format, the HTML page included.
Measured on one Windows host, Release, `dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks -- --filter
'*CompileBenchmarks.FullCompile*None*' '*CompileBenchmarks.FullCompile*All*'`, on the branch (0ed088d9a1) and then on its parent
9427f2ede0 (raw output: `E:/COBOL-coord/scratch/w20262104j-step9/bench-branch.txt`, `bench-parent.txt`); the parent's in
parentheses, allocated bytes per full compile:

| Program | None | All | All over None | All over the parent's All |
|---|---|---|---|---|
| IX113A | 195.13 (193.27) MB | 299.91 (263.77) MB | +53.7 % (+36.5 %) | +36.1 MB (+13.7 %) |
| NC105A | 117.75 (118.20) MB | 182.47 (158.89) MB | +55.0 % (+34.4 %) | +23.6 MB (+14.8 %) |
| NC218A | 115.64 (114.99) MB | 185.01 (161.47) MB | +60.0 % (+40.4 %) | +23.5 MB (+14.6 %) |
| SM201A | 41.79 (42.16) MB | 64.27 (56.97) MB | +53.8 % (+35.1 %) | +7.3 MB (+12.8 %) |

**Why three times the page.** IX113A's page is 6,595,954 bytes (`cobol IX113A.cob --std 85 --nist --listing-format html
--listing-format text --listing-format json --listing-sections all`; the JSON listing is 6,734,003 bytes, the text 2,628,177). Every
format is rendered to a STRING (`ListingOutput.Render`) that `ListingWriter.Write` hands to `File.WriteAllText`: the `StringBuilder`
holds the page at two bytes a character, `ToString` copies it, and the encoder writes it again. The JSON writer does the same. Most of
the HTML page's rows are IL instructions (52,026 table rows, 4,392 instruction tables), which the page shows twice: under each
statement (DESIGN-listing.md section 7.2, "each statement opens its method, IL offset range and (when requested) IL instructions
beside it") and in the object-code section.

**The fix the rows point to (this note's, with the instructions' own cost):**
1. Each renderer writes to the file's stream: `ListingOutput` takes a `TextWriter` (the file's `StreamWriter`, or standard output for
   `--listing-output -`), the HTML renderer appends to it instead of a `StringBuilder`, the JSON writer serializes to a `Utf8JsonWriter`
   over the file stream, and the text renderer's `PageComposer` writes each page as it composes it. No format then holds its whole
   rendering, so the page's cost falls from three times its size to a buffer.
2. The page shows each statement's instructions once: under the statement when the source section is held, the object-code section
   linking to them (or the reverse), which section 7.2 must say.
