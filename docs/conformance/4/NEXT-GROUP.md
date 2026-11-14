---
order: 21
---
> ⚖ **DETERMINATIONS — three points §2023:13.18.37.4 (the NEXT GROUP clause) leaves open** (2026-09-22; kb/Work
> PB957). **(1) When WITH RESET resets PAGE-COUNTER.** §2023:8.4.3.15.4 GR2 says PAGE-COUNTER "is reset to 1 when a
> report group that contains a NEXT GROUP clause with a RESET phrase is printed". §2023:13.18.37.4 GR6 places the reset
> "immediately after the page feed caused by the next page advance, chronologically between the printing of any page
> footing and the printing of any page heading", and §2023:14.9.16.4 GR6 d) makes it the PAGE-COUNTER step of that page
> advance. WiseOwl COBOL follows GR6 and §2023:14.9.16.4: the clause ARMS the reset and the next page advance performs it, so
> the page footing still prints the old page number. **(2) A relative body-group NEXT GROUP in a report that is not
> divided into pages.** §2023:13.18.37.4 GR4 b) compares against "the FOOTING integer", and such a report has no FOOTING.
> WiseOwl COBOL adds integer-2 to LINE-COUNTER; it does not clamp to a FOOTING of 0. **(3) "no effect at all if a
> TERMINATE is next executed" (GR4 a)).** WiseOwl COBOL's TERMINATE discards the save location and restores
> LINE-COUNTER to the group's last line. Leaving LINE-COUNTER at FOOTING would force a page advance before the
> control footings print, and that is an effect. Pinned by `cases/8.4.3.15-Report-counters/pb957_next_group_forms` and
> `cases/13.18.37-NEXT-GROUP/pb957_next_group_control` (its COBOL-85 twin `pb957_next_group_control_85`). The mechanism is in `docs/REPORT_WRITER_DESIGN.md` (the NEXT GROUP row).
