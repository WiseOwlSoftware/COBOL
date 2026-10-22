## Entry 2344 — 2026-10-22 01:53 PDT — PB3505: the note names its code sites, so the fix lane can plan it

PB3505 (DISPLAY of a numeric item follows GnuCOBOL's default form, the owner's PB3467 decision) was filed without the
source files it changes, so `fix_clusters.py` listed it as unsited and no wave could take it. The note now names the
DISPLAY emitter, the operand-text renderer and the runtime's numeric display formatter, with one trap: the renderer
also serves MOVE, whose results §14.9.25 fixes, so the new form belongs to DISPLAY's conversion only.

