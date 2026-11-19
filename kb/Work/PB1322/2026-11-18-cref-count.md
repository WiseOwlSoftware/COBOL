## The count of unresolved crefs, 2026-11-18 (found by listing step 6b review round 1, kb/Work PB5932)

A build of `src/WiseOwl.Cobol.Compiler/WiseOwl.Cobol.Compiler.csproj` with `-p:GenerateDocumentationFile=true` (CS1591 and the
other doc-shape warnings suppressed) at main 247b8dfdaa reports 188 distinct CS1574 warnings (unresolved cref) across the
compiler and the projects it builds, and several hundred CS0419 (ambiguous cref). Listing step 6b's review found a moved member
whose summary went to the wrong member and crefs to deleted names that no build flagged; the branch fixed the six it introduced,
measured by the same build at the branch against its merge base. The durable fix this note names is unchanged: build the
documentation file for the `src/WiseOwl.Cobol.*` projects with CS1574 an error, after this count is brought to zero.
