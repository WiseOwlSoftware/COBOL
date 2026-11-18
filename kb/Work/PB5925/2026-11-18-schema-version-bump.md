## Lead: a schema change without a minor-version bump is caught by review, not by a test (found by listing step 2)

DESIGN-listing.md section 7.3's policy: a property or enum value added raises the minor of
`ListingDocument.CurrentSchemaVersion`; one removed, renamed or changing meaning raises the major. Step 2 makes the
SCHEMA follow the model mechanically (`ListingJsonSchemaDriftTests` regenerates it and is red on any difference, and its
message tells the author to bump the version), but nothing fails when the regenerated schema is committed with the
version unchanged, or when a property is removed within major 1. Git history is the only record of what 1.0 published.

A mechanism that would make it automatic: a committed, shrink-never surface record beside the schema (each property path
and enum value with the minor that introduced it), checked by a test that is red on (a) a surface element the record lacks,
(b) a recorded element the schema lost (a major change), and (c) a newest recorded minor different from
`CurrentSchemaVersion`; the regeneration mode would add new elements at the next minor. Not built in step 2: until the
listing ships in a release no reader depends on 1.0, and the first published schema is the right moment to freeze it.
Decide it before the first release that ships the JSON listing (the owner's call on publishing, design section 13 item 6,
is the natural point).
