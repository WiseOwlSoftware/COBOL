## Lead: the listing model's records compare their lists by reference (found by listing step 3)

Every type of `WiseOwl.Cobol.Listing` is an immutable `sealed record`, and a record's generated `Equals` compares an
`IReadOnlyList<T>` member by REFERENCE. Until step 3 `ListingLine` had no list member, so tests compared lines with
`Assert.Equal(document.Lines, read.Lines)`; step 3 adds `ListingLine.Diagnostics`, and a line read back from JSON no longer
equals the line written though every value matches. The tests now compare a projection (`ByValue` in
`JsonListingWriterTests` and `ListingJsonTests`: the line with its list emptied, beside the list's elements). Step 7 adds
`ListingLine.Names` and step 6 `Margin`, and every other record with a list (`ListingDiagnostic.Citations`, the data map's,
the cross-reference's) has the same trap: a consumer that compares two listings, or keys a dictionary by a line, gets the
wrong answer silently.

A shape that makes the next case automatic: one value-equal read-only list type for the model's list members (equality and
hash by element), which the JSON contract reads and writes as an array (a converter beside the enum converter of
`ListingJson`), with a drift test that every list member of every model record is of that type. Decide it before step 6
adds the next list members; until then a test compares a projection.
