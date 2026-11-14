---
order: 10
---
> ⚖ **DETERMINATION — the OPTIONS INITIALIZE background reaches NUMERIC storage too (§2023:11.9.10 / §2023:14.6.2.3.2
> action 1)** (2026-09-02, kb/Work PB152; re-derived 2026-09-30 by owner decision kb/Work R53, kb/Work PB1134).
> §2023:11.9.10.4 GR5 makes the specified-fill-character a CHARACTER, and §2023:14.6.2.3.2 action 1 sets "the storage
> allocated for the implied or associated sections" to it — with no exception for numeric items; §2023:13.18.63.4
> GR4 c)'s "undefined and set to a value that may or may not be allowed" is the baseline §2023:11.9.10.4 GR6 scopes to
> "If the INITIALIZE clause is not specified". **The realization:** character-formed storage takes the fill
> (alphanumeric, national, numeric-edited, DISPLAY-form boolean — D-B1 — including every such member of a Tier-B
> REDEFINES backing); a VALUE-less fixed-point NUMERIC item of a governed section is given image storage at bind
> time and takes the fill as its storage — USAGE DISPLAY the fill characters, BINARY / COMP-5 / PACKED the fill
> byte in every byte (HIGH-VALUES is the byte 0xFF, A.1 item 31), so `INITIALIZE ALL TO X"5A"` leaves a `PIC 9(4)`
> item holding `ZZZZ` and a `PIC 9(4) COMP` item holding X'5A5A'; a floating-point item takes the value its fill
> bytes encode; a USAGE INDEX item's eight bytes each take the fill byte, a NATIONAL-usage numeric item is promoted
> like any other, and an OO method's LOCAL-STORAGE is walked under the method's own effective OPTIONS clause (kb/Work
> PB1134 — no residual remains). **Two carve-outs, each from a RULE and not from convenience:** class
> object / message-tag / pointer take NULL, because GR4 c) states that as a positive requirement in the same
> sentence; and USAGE BIT keeps its packed ceil(n/8) zero seed (D19 / kb/Work PB43), because its storage is
> packed bytes laid out by the §2023:8.5.1.6.3 walk rather than a run of character positions.
> **Scope:** the clause is COBOL-2023 (Annex E.3.3 item 33), so below 2023 it cannot be written and the
> unchanged space/zero seed remains the conformant realization of §2023:11.9.10.4 GR6's "undefined or specified by
> the implementor" there. **§2023:11.9.10.4 GR3 (the SCREEN arm)** has no data items to reach, and since
> 2026-09-02 it is not reached at all: the SCREEN SECTION is a §2023:4.2.7 optional facility documented as
> unsupported in §4 below, so the
> explicit `INITIALIZE … SCREEN …` target is REFUSED by name (COBOL1560,
> `negative/a42-options-initialize-screen`) — it was the module's seventh source shape, parsed into an
> `OptionsSections.Screen` flag that nothing read. ⛔ Only the EXPLICIT leg: GR1 makes `ALL` imply
> LOCAL-STORAGE, SCREEN and WORKING-STORAGE and two of those three are supported, so `INITIALIZE ALL` stays
> legal source. Pinned by `2023/pb152_options_initialize_background`,
> `_sections`, `_local_storage`, `_tier_b`, `_external`, `_figurative` and `_arm_agreement`.
