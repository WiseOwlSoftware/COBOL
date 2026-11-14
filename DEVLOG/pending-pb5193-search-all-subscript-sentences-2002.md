## PB5193: SEARCH ALL's two 2002 subscript sentences stop refusing 1985 source

At `--std 85`, `SEARCH ALL ... WHEN K (IX + 1) = 2`, `WHEN C1 (IX - 1)` and `WHEN C1 (JX)` drew COBOL1965 citing
§2023:14.9.37.3 SR8 and SR9. Re-probe on this build confirmed all three (exit 65, same text) at 85 and 2002.
§1985:VI.6.22.3 SR4 asks only that a key be subscripted by the first index-name "along with other subscripts as
required" and says nothing about a condition-name's subscript; both sentences ("The index-name subscript shall not be
followed by a '+' or a '-'", and the condition-name subscripted by the first index-name) first appear in §2002:14.8.34.2
SR6 and SR7 (both checked with `cite.py --check`).

Fix: `SearchAllFormat2Rules` gets one constant, `FirstEditionWithSubscriptSentences` (2002). `CheckKeySubscript` accepts
a relative subscript on the first index-name below it, and `CheckConditionNameSide` skips the subscript check for a
condition-name below it. Every other 1985 requirement (first index-name required, single-valued condition-name, key
membership, preceding keys) stays at every edition. After the fix the three probes run at 85 (FOUND, FOUND, NOT FOUND for
the uninitialised JX) and are still refused at 2002 and later.

Goldens: new accept-at-85 case `1985-SR-VI_6_22_3-4-relative-subscript` (both a data-name and a condition-name with
(IX + 1); its `.out` derived from §1985:VI.6.22.4 GR4); the two PB445 negatives `pb445-search-all-when-index-plus` and
`pb445-search-all-condition-name-index-plus` now refuse at 2002, 2014 and 2023. Rows SR-14.9.37.3-8 and -9 keep their 85 band: restating it to 2002,2014,2023 made the
conformance generator refuse the 85-accepted `pb1018_occurs_key_qualified` (first gate RED), so the edition difference lives
in the two negatives' `refuse-at` alone.
