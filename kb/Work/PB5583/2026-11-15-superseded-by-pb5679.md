**Superseded by PB5679 (2026-11-15).** This note makes PB5574 decision 2 ("every older edition chains straight to 2023")
the stated design; PB5679 supersedes that decision. Its target, `next_held` the newest edition for every older one, is
not to be built. Its deletion of `Edition.linked`, the schema's `linked` key and `link_errors`' linking clause happens in
PB5682 for another reason: a neighbour pair is linked when it is complete (DESIGN-spec-conformance-review.md section
10.7.2), so no flag is stored. The owner of this note retires it (frontmatter only), naming PB5682 as its successor.
