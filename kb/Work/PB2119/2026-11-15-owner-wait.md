**2026-11-15, the owner: the Delete program waits until the 2002 and 1985 inventories are closed.**

The owner was asked, on 2026-11-15, "Should the 48 open Delete notes (kb/Work PB2119) start now, or wait until 2002 and 1985 are
closed?" and answered "Wait".

The state when he answered: the PB1754 cluster had 136 notes landed and 260 open, the Delete program's share of them 24 landed and
48 open.

The program restarts without asking again when the 2002 and 1985 inventories read zero GAP (`python scripts/spec/inventory_view.py
--summary`). Before any Delete note is worked then, it is re-measured against the main of that day: a census taken now will be stale
by then, so a note's measured dead code is confirmed dead again before it is removed.
