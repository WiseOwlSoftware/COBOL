## Entry 3323 — 2026-11-08 06:51 PST — PB4993's obsolete-standing decision; PB4999 and PB5000 filed; two review leads fixed

PB4993 (lineage slice 2) had an open design question: how the 1985 catalog marks a rule of an element the standard calls
obsolete only in part, such as OPEN's REVERSED phrase or the STOP literal. It is now settled in the note. The authority
is the standard's own list of 18 obsolete elements (§1985:C.1, defined by §1985:I.1.5.2.3), held once as data beside the
catalog extractor. A whole element marks every rule of its sections. A partial element marks only the syntax and general
rules that name it, and a general format mixing current and obsolete syntax stays current. A drift test holds the data
to the text.

Two defects are filed from wave 20261926. PB4999: text cut at margin R in the middle of a token is silent when the
shortened line still parses; that is what made PB4981 look like a wrong answer. PB5000: the closes_rows backfill credits
a merge landing with every brought-in commit's message, so its one-note arm misses six rows today.

Two review leads from the same wave are fixed. The STRING live-read drift test and STRING_OPS_DESIGN D4 named D-STR4 as an
in-place rule, which PB4973 reversed. A doc comment in CobolIntrinsics.ExactValue carried a stray closing summary tag.

