## Review leads of waves 20261879 to 20261882 filed as kb/Work notes PB4818-PB4847

Fifteen merged fix-lane branches left leads in their reports that no note held. Thirty notes are filed, one per mechanism,
each with a Sites paragraph and the lead's words attributed to its reviewer; no code, golden or document was changed.

Defects with a harm flag from the lead's own text: PB4818 (misleading diagnostic, report-group SCREEN SECTION), PB4822
(COBOL1505 cascade), PB4826 and PB4827 (wrong answer, MEDIAN and bounded lists; PB4826 needs an owner call because
pb4592 pins the 34-digit rounding), PB4830 (under-rejects, UnsignedIntegerViolation and LENGTH shapes), PB4831 (rejects
legal source, PERFORM VARYING Classify), PB4836 and PB4837 (extra class diagnostic beside COBOL0900/0869), PB4838 (FD slot
records, owner decision on what a file holds for a tag or pointer), PB4841 (14.9.3.4 GR9 claim in user-facing text), PB4845
(foreign run before a keyword-opened header). Process-only notes (stale comments, citations, tests, documents and the
prose of landed notes): PB4819, PB4821, PB4823-PB4825, PB4828, PB4829, PB4832-PB4835, PB4839-PB4840, PB4842-PB4844, PB4846,
PB4847. One analysis: PB4820 (Identity design lead: macOS case folding, Windows 8.3 names).

Citations run through `cite.py --check`: PB4799's quoted 13.18.40.5 7) b) text FAILs as the reviewer said (the standard
reads "zero suppression with replacement"); 9.3.6 3), 8.4.3.2.3 12), 13.17.2 and 13.18.57.3 8) are OK as quoted; the
8.4.3.2 SR2 and the 13.9.2, 13.8.2 and 13.15 claims could not be settled by the check and are recorded as the reviewers'.

Dropped, not leads: PB4735 "New leads 1" (the merge group is their first run by design) and every report's "None", and
each finding the digest marks fixed. No lead was found already fixed on re-reading its site.
