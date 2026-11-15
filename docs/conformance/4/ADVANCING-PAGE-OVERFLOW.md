---
order: 16
---
> ⚖ **DETERMINATION — WRITE ADVANCING PAGE on a logical page is a page overflow** (2026-10-16; kb/Work PB578). The
> standard's text decides it, so CLAUDE.md rule 1's precedence never reaches the implementations. §2023:13.18.34.4 GR7 c) 1
> resets the counter to one and says how: "During the resetting of LINAGE-COUNTER to the value one, the value of
> LINAGE-COUNTER is implicitly incremented to exceed the value specified by integer-1 or the data item referenced by
> data-name-1." A counter past the page size is §2023:14.9.51.4 GR26 a)'s trigger ("This occurs when the associated
> LINAGE-COUNTER is equal to or exceeds the page size"), and GR27 a) names the condition it causes: "If the
> end-of-page condition was caused by the action in General rule 26a, the EC-I-O-EOP-OVERFLOW exception condition is
> set to exist." So a WRITE with ADVANCING PAGE on a file whose logical page is established is SUCCESSFUL, takes the
> one overflow transition (GR25 g)'s destination is GR26 a)'s, "the first line that may be written on the next
> logical page"), leaves the counter at one, sets EC-I-O-EOP-OVERFLOW to exist (an applicable USE declarative, an
> exception-checking PERFORM's WHEN phrase and FUNCTION EXCEPTION-STATUS see it, GR27 c)/d)), and does not run a
> NOT END-OF-PAGE imperative (GR28: only "the end-of-page condition does not occur"). The PB686 determination
> above reads GR7 c) 1 the same way, as "the standard's own device for making arm a) fire on ADVANCING PAGE".
> **SR18 bars only the END-OF-PAGE phrase beside ADVANCING PAGE.** "The phrases ADVANCING PAGE and END-OF-PAGE shall
> not both be specified in a single WRITE statement" names GR27 b)'s imperative-statement-1 phrase; SR19 ("If the
> END-OF-PAGE or the NOT END-OF-PAGE phrase is specified") and GR28 name the NOT END-OF-PAGE phrase separately, so a
> NOT-only phrase beside ADVANCING PAGE is legal source (its imperative never runs) and the AT phrase is refused
> whichever phrase is written first (COBOL0861). **The surveyed implementation does not split this from the
> text:** GnuCOBOL 3.2 accepts the NOT-only form and runs its imperative after ADVANCING PAGE, but it also runs a NOT
> END-OF-PAGE imperative after an `ADVANCING 9 LINES` write that overflows a 5-line page with no FOOTING phrase, a
> case no reading of GR26 a) allows, so its end-of-page signal is not a witness here (probe 2026-10-16). **What a
> declarative reads mid-statement:** when GR6 b) 2's re-evaluation during the ADVANCING PAGE write violates GR6 b)'s
> value rules, the write is unsuccessful (no end-of-page condition) and the USE procedure, run "after completion of
> the standard input-output exception routine" (§2023:14.9.49.4 GR6), reads GR6 b) 2's counter value 0, never the
> transient value GR7 c) 1 passes through; the overflow arm's twin is pinned by `2023/pb526_linage_ec_condition`.
> Pinned by `85/pb578_advancing_page_overflow_85`, `2002/pb578_advancing_page_eop_overflow`, the negative
> `pb578-write-advancing-page-at-and-not-eop`, and `LinageConformanceTests.Sr18_AdvancingPageWithNotEopOnly_IsLegalAndTheEndOfPageConditionOccurs`
> / `Sr18_AdvancingPageWithNotFirstEopPair_IsRejected`.
