## PB3369: the owner's SORT and file I/O test requirement binds only one campaign's groups

The operator files kb/Work PB3369. PB3359 (PR #223) carries the owner's standing test requirement (2026-10-17 ~22:25:
SORT and file I/O tests cover the abnormal and exception cases as fully as the success cases) into a group's spec and
its branch review. It does so only through campaign membership: the bullets of a lead note's `## Standing
requirements` bind the groups whose notes name that lead in `cluster:`, and only PB3353 has the section. So the
keyed-store engine's notes (cluster PB3330), any SORT or file I/O note filed after the campaign, and a hand-dispatched
finisher outside a campaign get none of it. The note asks for a binding computed from what a group changes or what its
notes are about, one home for the rule's text, and a drift test. No code changes in this landing.
