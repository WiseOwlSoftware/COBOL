## Entry 2790 — 2026-10-30 11:27 PDT — GnuCOBOL: 18 tracker reports posted, seven new upstream notes, PB4111 and PB4112

The owner approved the drafted GnuCOBOL tracker text ("approve all") and the reports went out on 2026-10-30 as bugs
#1267 to #1284 on SourceForge, plus a comment on bugs #1103. Bugs #1267 and #1268 carry the same DATE-TO-YYYYMMDD
text (PB3889); #1267 is the first, less readable post and asks the maintainers to close it as a duplicate of #1268.
The others: #1269 boolean items (PB3906, PB3921, PB3925), #1270 the nine COBOL 2014 syntax refusals (PB3902, PB3907,
PB3909, PB3910, PB3911, PB3931, PB3934, PB4042, PB4044), #1271 literal-subscript and INSPECT size errors refused at
compile time (PB3904, PB3905), #1272 PIC 9.99E+99 (PB3900, PB3901), #1273 INSPECT with a variable-length ref-mod
operand (PB4098), #1274 six unimplemented intrinsics (PB3879 to PB3884), #1275 LENGTH of a ref-mod (PB3999), #1276
national arguments (PB3891, PB3895, PB3896, PB3903, PB3936, PB4001, PB4004), #1277 national storage (PB3894, PB3897,
PB3898, PB3937, PB3998, PB4000), #1278 comma insertion (PB4041), #1279 OBJECT REFERENCE without USAGE (PB3929), #1280
OCCURS DYNAMIC (PB3917), #1281 run unit ending on a non-fatal I-O status (PB3875), #1282 NOT ON SIZE ERROR on a COMP-5
receiver (PB3995), #1283 table VALUES FROM/TO (PB3933), #1284 TEST-NUMVAL (PB4003, PB4040). The comment on #1103
(PB3876) asks for the ticket to be reopened and gives the CALL repro; it awaits SourceForge moderation.

SourceForge's ticket rate limit stopped the batch after #1284. Six items are unposted: T18 (a bugs ticket), T19 to T21
(feature requests), T22 (a comment on feature-requests #284) and T24 (a comment on bugs #971).

Each posted note is now `upstream_status: reported` with its `upstream_ref` and an Upstream state paragraph naming the
other notes of its ticket. Seven new upstream notes join the register (PB4098, PB4099, PB4100, PB4102, PB4104, PB4105,
PB4106); PB4098 is already reported as bugs #1273. The ids PB4101 (folded into PB3874) and PB4103 (dropped: GnuCOBOL is
right per Table 10) are unused.

Three notes were corrected while the text was drafted. PB3886 now records that patches #66 (2025-01, not committed)
already fixes the stored result of `0 ** 0` under ON SIZE ERROR, that its `0 ** -1` case is bugs #924, and that the
constant fold (PB3885) is not covered. PB3995 now says that running NOT SIZE ERROR on a size error is the defect, with
the stored 043 within implementor latitude (§14.6.13.1.3 rule 8), and that the repro runs in the default dialect
because `-std=cobol2014` refuses COMP-5 and COMP-X. PB3874 gains a Witnesses line: an unreferenced `01 R USAGE OBJECT
REFERENCE.` reaches the same internal error under the default dialect (3.2.0 tree.c:1606, exit 134; 3.3-dev r5751
tree.c:1613 then SIGSEGV, exit 139).

Two WiseOwl notes were filed. PB4111 (analysis, wrong answer) records that VARIANCE, STANDARD-DEVIATION and
PRESENT-VALUE read a wide argument through its 34-digit image, the same intake root as PB4075, and states the
determination owed first because the mean is itself a 34-digit value. PB4112 (defect, minor) lists the stale comments
and the missing DEVLOG statements the review of the landed PB4075 found.

