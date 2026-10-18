## PB3369: the owner's SORT and file I/O test requirement binds a group only through a campaign's hand copy

The operator files kb/Work PB3369. PB3359 (PR #223) carries the owner's standing test requirement (2026-10-17 ~22:25:
SORT and file I/O tests cover the abnormal and exception cases as fully as the success cases) into a group's spec and
its branch review only through campaign membership. The bullets of a lead note's `## Standing requirements` bind the
groups whose notes name that lead in `cluster:`. Two leads now carry their own copy of the owner's sentence: PB3353
on main, and PB3330, copied by hand on the design branch (`5ee56d4c3`) so that the keyed-store engine's notes are
bound. So each new campaign needs another hand copy, the copies can drift, and a SORT or file I/O note in no such
campaign, or a hand-dispatched finisher outside one, gets nothing. The note asks for a binding computed from what a
group changes or what its notes are about, one home for the rule's text (both copies move into it), and a drift test.
No code changes in this landing.
