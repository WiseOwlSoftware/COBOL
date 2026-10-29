## Entry 2734 — 2026-10-29 13:30 PDT — PB3840: a CONFORMANCE.md row key names one determination (DOC-A.1-92 and DOC-A.1-92b)

`docs/CONFORMANCE.md` §7 filed two determinations of Annex A.1 item 92 under the key `DOC-A.1-92`: the characteristics,
representation and text image of a numeric function's returned value, and the evaluation of an exact-family function whose
argument is floating-point. The audit, the Python register reader and the C# reader each carried a comment that called two rows
per item legitimate, and each kept the first row and dropped the other without saying so. Thirty-odd code and doc citations of
`DOC-A.1-92` could not say which determination they relied on.

Re-probe: `grep -oE '^\| DOC-[A-Z0-9.-]+ ' docs/CONFORMANCE.md | sort | uniq -d` printed `| DOC-A.1-92` and nothing else, so item 92
was the only repeat (item 56, which the old comments also named, has one row). Reading each citing site against the two rows
sorted them: the text image, the device form, the nominal returned-value item, `FUNCTION LENGTH` of the temporary, and the float
family's binary64 returned value (PB1566, inside the first row) rely on the first row; only the three places that say "a list with a
floating argument is evaluated in binary64" (`IntrinsicRenderer.cs`, `CobolIntrinsics.Float.cs`, `INTRINSICS_DESIGN.md`) rely on the
second.

Fix: a row key is unique. An item's first determination keeps the item's rule-id, the key its inventory row anchors on; a further one
takes the id plus a letter, so the floating-argument row is `DOC-A.1-92b`. `audit_annex_a1.py` refuses a repeated key and a lettered key
with no row for its item, counts each token on its own, and checks that a lettered token cited in `src`, `tests` or `docs` is a row (bare
tokens stay free, since a bare id is also the inventory rule-id a comment may cite with no row behind it). Its self-test gained a case for
each refusal and for the lettered key it accepts. `register_determinations` (Python) and `ConformanceRegister.Build` (C#) throw on a repeated
key instead of keeping the first, and `TheRegisterParser_ReadsSection7_AndNotAnEmptyTable` asserts the keys are `DOC-A.1-<n>[a-z]` and distinct.
The three citations of the second row now name `DOC-A.1-92b`; the goldens' `rules:` headers name the inventory rule-id, which both
determinations discharge, so no golden changed. The change closes no inventory row (`closes_rows_reason` in the note).

Self-review: architecture (one key rule, written in `ITEM_KEY_RX`, read by the audit; the two register readers refuse a repeat instead of each
choosing a winner); full code (a lettered key cannot shadow its bare token because the letter sits inside the token); performance (the
lettered-citation sweep reads each `.cs` and `.md` once, as the existing source sweep does); duplication (no second copy of the key grammar).

