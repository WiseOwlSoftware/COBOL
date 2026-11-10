## PB5044: the 1985 underline detector judges short words and says what it cannot see

`figure_raster.underlined_hints` (PB5041) skipped every proportional word under 14 pt and every one-character word, so the
running head of Amendment 1's change list (`to`, `and`, `ISO`, `No.`) and every short underlined word went unmeasured. The
re-probe over PDF pages 831-906 confirmed it, and showed that the PB5041 run also missed page 882's `Argument` and page
905's `Contents`, whose OCR boxes sit a little high of their rules.

Fix. A short word is underlined when the rule runs on across the space beside it (`_spaces`: the columns between the
neighbouring OCR words, trimmed by 0.3 pt), which separates a rule from the serif feet that cover `in` or `a` whole; the
rule window reaches 0.30 of the box height (was 0.27). The sweep of pages 831-906 against the old detector gained the
running head's short words on pages 831-856, `Argument` (882), `Contents` (905), `FUNCTION` (866, 867) and `PRESENT-VALUE`
(893), all already marked `<u>` in the transcription; it also read the bold ligature feet of `final` (851) and `the` (842)
as rules, and values 0.34 and above read more of the next face's serifs, so 0.30 stands. Page 869's `Contents` (a box
displaced 5 pt) is still missed. TRANSCRIPTION-STATE now names every blind spot instead of one.

Tests. Case 8b of `test_figure_raster.py` plants the monospaced arm (a ragged rule found, a 70 % rule not), a short word
inside an underlined phrase and one with feet alone, a rule at the box's top (the comment's fourth negative, now planted)
and a displaced box, which fails at the old 0.27. Self-test 78/78. No inventory row closed; no compiled program changes.
