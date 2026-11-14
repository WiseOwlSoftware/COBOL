---
order: 27
---
> ⚖ **DETERMINATION — a level-66 RENAMES THRU alias whose data-name-2 / data-name-3 is (or lies under) a
> REDEFINES entry (§2023:13.18.45.4 GR2)** (2026-08-18; kb/Work PB96). GR2 says the alias "defines an alphanumeric group
> item that includes all elementary items starting with data-name-2 … and concluding with data-name-3"; a
> redefinition adds no storage (§2023:13.18.44), so the elementary items named can overlap. WiseOwl COBOL's alias is the
> record's STORAGE WINDOW from the leftmost character position of data-name-2 to the rightmost character position
> of data-name-3 — a REDEFINES view inside the range contributes nothing beyond the storage it overlays, a
> data-name-2 / data-name-3 that is itself a redefinition maps to the area it overlays, and when data-name-3 is a
> SHORTER redefinition of a LATER item (its end still follows data-name-2's end) the alias ends at data-name-3's last
> character, a partial view of the underlying leaf. A data-name-3 that is a shorter redefinition of an EARLIER item —
> `RENAMES A THRU B`, B redefining A's first two characters — is not legal source: §2023:13.18.45.3 SR11 says "the end of the
> storage area described by data-name-3 shall follow the end of the storage area described by data-name-2" (`cite.py
> --check` OK 11)), and the offset arithmetic GnuCOBOL and IBM apply there is a vendor leniency the standard's syntax
> rule controls over (kb/Work PB1283; `negative/pb1283-renames-thru-redefinition`). Pinned by
> `2023/pb96_renames_span_over_redefines`.
