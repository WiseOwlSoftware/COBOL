## Entry 3842 — 2026-11-18 08:29 PST — A listing test asserted the length of the host's scratch path

The merge group's Windows shard 7 of PR #1864 failed `ListingDiagnosticsTests.ABindError_IsPlacedOnItsLine_...`: the test
asserted `"     5  error COBOL0801 <source path>(5,8): "` as one substring of the listing's summary. The renderer wraps a summary entry at
the page width (132), breaking at a space; on a GitHub runner the scratch path is long enough that the path no longer fits
after the code, so it starts the next row and the substring is not there. Locally the path is short. The wrap is designed
behavior, so the renderer stays and the assertion changes (kb/Work PB5931).

The test is now a Theory run in a short directory and in one padded so the path is longer than the page. It reads the entry
as the row that opens it plus the rows indented under it, and asserts that no row is wider than the page, that the rows hold
the entry's words in order, that they equal `TextListingRenderer.Indented(entry, 0, 8, 132)`, and that the long case wraps.
Before the change, the old assertion failed in the long directory on this host and passed in the short one. A sweep of every
test that reads listing text found no other assertion on a host path inside a rendered row. The long-path case is the guard
against the next one; lengthening every scratch directory was not done, because it moves the whole suite toward Windows path
limits for one site.

