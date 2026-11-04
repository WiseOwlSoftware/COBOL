## Entry 3076 — 2026-11-04 08:10 PST — GnuCOBOL moves two reported tickets to feature requests

A sweep of the 43 SourceForge tickets that kb/Work's upstream notes link to, through the tracker's REST API, found new activity on two of them. Both had been filed on 2026-11-03.

PB4499: Simon Sobisch moved bugs #1287 (`CALL ... USING OMITTED` with no program prototype, accepted under the ISO dialects) to feature-requests #527 and accepted it. He wrote that prototypes need a dialect option and that this case should hang on it. He also asked whether OMITTED belongs in the COBOL-85 word list. It does: COBOL-85's `LABEL RECORDS ARE OMITTED` uses it, as NIST's OBNC1M shows. The note records that answer and that no reply has been posted.

PB4501: Simon Sobisch moved bugs #1288 (`-std=cobol85` accepts a procedure division that does not begin with a paragraph-name) to feature-requests #526, accepted it as a good first issue, and described the same `config.def` option this note proposed. Two users report that IBM compilers, up to z/OS Enterprise COBOL 6.5, accept the omission with no warning. That is IBM's extension and does not change the COBOL-85 rule.

Both notes now link to the new tickets. Their `upstream_status` stays `reported`, and the earlier 2026-10-30 replies on bugs #1271 and #1274 were already recorded.

