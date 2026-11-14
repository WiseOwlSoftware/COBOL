---
order: 7
---
> ⚖ **DETERMINATION — `FUNCTION LENGTH`'s `PHYSICAL` argument (§2023:15.50.4 rule 8)** (2026-08-04; fix-queue PB24).
> Rule 8 splits on whether argument-1 "is physically located where it is defined": if it is not, the returned
> value "includes only the length of the implementor-defined pointer"; if it is, "LENGTH returns the same value
> that would be returned had the PHYSICAL argument not been specified".
> **WiseOwl COBOL determines that a variable-length group IS physically located where it is defined**, so `PHYSICAL`
> is accepted and semantically transparent — it returns the rule-7 value. Grounds: this implementation exposes no
> addressable out-of-line pointer for a program to observe, and a group presents as a contiguous character image
> at its defined position; the alternative reading would require inventing a user-visible pointer width that
> nothing here exposes. Pinned by `2023/pb24_length_physical_keyword`, which asserts each PHYSICAL form equals
> its plain form. **The same determination governs `FUNCTION BYTE-LENGTH`'s `PHYSICAL` argument (§2023:15.14.4 rule 7,
> "the length of argument-1 in number of bytes")** — accepted since 2026-08-18 (kb/Work PB61, FMT-15.14.2) and equal
> to the rule-6 sum, pinned by `2023/pb61_length_byte_length_rule_branches`.
