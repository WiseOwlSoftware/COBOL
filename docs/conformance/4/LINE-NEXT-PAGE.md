---
order: 22
---
> ⚖ **DETERMINATIONS — three points the LINE clause's NEXT PAGE phrase leaves open** (2026-09-22; kb/Work
> PB1001). **(1) Where a bare `ON NEXT PAGE` operand places.** §2023:13.18.35.2 Format 1 prints it as a third operand
> form with no integer, and §2023:13.18.35.4 GR5 places a first line only from integer-1 or integer-2. WiseOwl COBOL reads it
> as a relative clause (GR4c's "relative … without the NEXT PAGE phrase" names the combination) whose integer-2 is
> never read where the phrase applies. A body group advances (GR4a) and is then the first body group on its page,
> so it begins at the FIRST DETAIL integer (GR5b3). A report footing is on a page by itself, so it begins at that
> footing's upper limit, the HEADING integer (§2023:13.18.57.4 GR7f). A later occurrence of a REPEATED bare clause
> advances one line. **(2) The bare operand in a report that is not divided into pages.** §2023:13.18.35.3 SR5 requires
> every LINE clause of such a report to be relative. WiseOwl COBOL reads "relative" as SR3's relative FORM, `{PLUS|+}
> integer-2`, so the bare operand is refused there (COBOL2199). This is the reading §2023:13.18.37.3 SR3 gives NEXT
> GROUP ("only the relative form of the clause may be specified"), and a report of one page of indefinite length
> (§2023:13.18.39.4 GR2a) has no next page to begin. **(3) GR4a 2 of the NEXT GROUP clause.** §2023:13.18.37.4 GR4a 2 says
> that a next body group opening with an absolute LINE … NEXT PAGE takes "a page advance". WiseOwl COBOL takes the ONE
> advance the save location already forces, then proceeds as GR4a 1 does. Where §2023:13.18.37.4 means a second advance
> it says so (GR4a 3: "a second page advance takes place, resulting in a page devoid of body groups"). A second
> advance here would leave a page with no report group on it. GnuCOBOL 3.2 parses the phrase but gives it no
> run-time effect, so it offers no precedent (the rule 1 precedence falls through). Pinned by
> `2002/pb1001_line_next_page` (R-BF's `RF2` on line 2 for (1); page 5's `N05` for (3)) and by
> `negative/pb1002-unpaged-next-page` for (2). The mechanism is in
> `docs/REPORT_WRITER_DESIGN.md` (the LINE NEXT PAGE row).
