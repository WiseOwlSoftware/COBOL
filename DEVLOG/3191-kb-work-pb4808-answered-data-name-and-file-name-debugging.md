## Entry 3191 — 2026-11-05 20:16 PST — kb/Work PB4808 answered: data-name and file-name debugging triggers have a source; PB4759 split

A read-only adjudicator found that GnuCOBOL 3.3-dev models the data-name and file-name USE FOR DEBUGGING triggers in full
(parser.y, typeck.c, codegen.c; probed on cobc 3.2.0), overturning the wave-20261879 claim that it has nothing to follow, and
that Micro Focus general rules 1-23 agree. Rule 1's precedence settles the two edges. PB4759 is split: PB4810 (data-name) and
PB4811 (file-name, after PB4810) carry the work; PB4759 keeps the cd-name trigger, which no readable source defines, held on
the owner (the X3.23-1985 Debug module text).

