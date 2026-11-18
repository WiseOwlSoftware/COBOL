## Entry 3837 — 2026-11-18 03:16 PST — PB5925: the source listing, step 2 - the JSON listing and its generated schema

Step 2 of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5925's decision): the listing model built in step 1 gains a
versioned JSON form with a published schema, the stable programming interface no competitor's listing offers (IBM states
its text listing "is not a programming interface"; its SYSADATA is binary records).

**What was built.** In `WiseOwl.Cobol.Listing`: `Json/ListingJson.cs`, THE ONE serialization contract (the writer, the
reader and the schema generator all use `ListingJson.Options`), built on the internal source-generated `ListingJsonContext`
(camelCase, a null left out, nullable annotations respected, indented with LF) plus three rules decided once: a property
never null is required, no list holds null, and an enum value is its member's name (see review round 1 below);
`Json/JsonListingWriter.cs`, whose `Write` serializes the whole `ListingDocument` and whose `Read` is the compatibility
policy's reader (an unknown property ignored, and an unknown enum value of a later minor read as
`ListingJson.UnknownEnumValue`; another `$schema`, another major, a document naming neither, or one invalid against its own
version of the schema refused);
`ListingDocument.Schema` (`$schema`, `urn:wiseowl:cobol:listing:v1`) and `ListingDocument.SchemaVersion` (`1.0`);
`Json/ListingJsonSchema.cs`, which GENERATES the schema from the contract with `JsonSchemaExporter` and takes every
description from the model's XML documentation, committed as `Json/listing-v1.schema.json` and embedded in the assembly;
and `ListingOutput.cs`, the ONE table of formats (spelling, extension, renderer) that the listing's exit and the command
line's `-` both write through. `ListingRequest` gains `Formats` with one rule, `FormatsProblem` (at least one format,
exactly one on standard output), asked by the command line and by the request's own constructor; `ListingRequest.TextExtension`
is deleted and every caller reads `ListingOutput.Extension`. `ListingWriter` writes each requested format; a file the host
refuses is one error and the other formats are still written. The command line gains `--listing-format text|json`
(repeatable, one format per occurrence). The Listing project now builds its XML documentation file, so with
`TreatWarningsAsErrors` an undocumented public member is a build error: every enum member of the model is documented, and
the generator refuses any property, type or enum value the documentation does not describe (it found
`ListingSectionStatus`'s parameters undocumented on its first run).

**Design corrections (marked "(step 2)" in the design).** `ListingFormats` holds Text and Json only: a format joins the enum
with its renderer, so HTML arrives in step 9. `-` writes the ONE format requested, and two formats with `-` are an argument
error. The schema identity is two init properties of the document, so a reader sees the version a file was written at.
The exporter lists every constructor parameter as `required`; the generator writes each object's `required` from the
contract's one rule (`ListingJson.IsRequired`: never null, because the writer leaves nulls out), so a property a later
minor adds must be nullable. The schema drift test could not be
declared a preflight class: `buildgraph.py` admits only a class the merge group has measured, so it runs in the
population until then (lead `kb/Work/PB5925/2026-11-18-schema-drift-preflight.md`).

**Tests, and how each can fail.** `ListingJsonSchemaDriftTests` regenerates the schema and is red on any difference from
the committed file (shown red by renaming one property of the committed file: "stale ... at line 126"); it also holds the
embedded resource to the file, every property at every depth described, and an undocumented member refused (by removing
one enum value's documentation). `JsonListingWriterTests` checks names, enum values, left-out nulls, the reader's policy and
a strict round trip over a hand-built document holding every member. `ListingOutputTests` calls every switch of the format
table for every member. `ListingJsonTests` (Conformance) compiles a 1985 program with a copybook and DATE-COMPILED and reads
every expected value from the input files, the rule (§1985:VI.3.4.4 GR1) or the schema. Every document the listing tests
produce is held to `ListingJsonContract.AssertHolds`: valid against the committed schema and a strict round trip.
`tests/_shared/JsonSchemaSubset.cs` validates the exporter's keywords and treats any other keyword as an error; it is shown
refusing a document with a value no enum holds.

**Measured (design section 9).** `CompileBenchmarks.ListingMode.TextAndJson` beside `None` and `Text`, rows in
`tests/WiseOwl.Cobol.Benchmarks/README.md`. On a full compile, text plus JSON allocates 3.7-8.3 % over no listing and
0.1-3.2 % over the text listing alone; the host was loaded (StdDev 25-40 %), so no time row decides a budget, and the
quiet-host re-measure stays owed (kb/Work PB5927). IX113A's JSON listing is 2.1 MB, 3.5 times its text listing.

**Leads.** `kb/Work/PB5925/2026-11-18-schema-drift-preflight.md` (declare the drift test preflight once measured) and
`kb/Work/PB5925/2026-11-18-schema-version-bump.md` (a schema change without a minor bump is caught by review, not a test).

**Review round 1 (four findings, each fixed at its root).** R1-1: `Read` accepted a document missing a required property
(no `header`, a line without `text`) and returned null in non-nullable members, because `RespectNullableAnnotations` refuses
only an explicit null; the contract now marks every never-null property required by one rule, `ListingJson.IsRequired`,
which the schema generator's `required` lists also read, and refuses a null list element (a sibling the finding did not
name). `TheReader_RefusesADocumentMissingARequiredProperty_AndOnlyThat` removes every property of the written document at
every depth in turn and holds the reader's verdict to the committed schema's `required`; it went red when the rule was
switched off. R1-2: the policy makes an added enum value a minor change, but `Read` refused a 1.x document holding one, and
accepted `"kind": 42`; one converter per model enum now writes and reads names only, and in a document of a later minor
reads a name it does not know as `ListingJson.UnknownEnumValue` (-1, no member's value), while the same name in a document
of this reader's minor is refused. `Read` now reads `schemaVersion` first through the one parser `ListingJson.MinorOf`,
which also refuses `1.x` and `1.01` (the old `StartsWith("1.")` took them), and refuses a property written twice. R1-3:
`ListingRequest` checked its formats only in a property initializer, so a `with` expression or an object initializer built
a request the listing then failed on; every property now validates in its `init` accessor, and the sweep found the page
length and width rules unchecked by the record at all, now checked the same way. R1-4: DESIGN-listing.md section 4.6 still
described `ListingWritten(Document, File, Error)`; it now states the per-format exit and its witness. The committed schema
is unchanged byte for byte.

