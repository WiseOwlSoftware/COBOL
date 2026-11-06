## The 1985 standard converted to raw Markdown; the tooling campaign filed (PB4815, PB4816, PB4817)

The owner approved converting the purchased ANSI INCITS 23-1985 (R2001) PDF through the Message Batches API. A 10-page
pilot ($0.60, both models) showed Opus 5.5 and Sonnet 5.5 agree on prose and tables and both read general-format words and
underlines correctly, while only Opus kept the bracket and brace nesting; Opus 5.5 was chosen. All 976 pages went as two
batches from a restartable script outside the repository (`E:\COBOL-coord\scratch\transcribe-1985\`): 971 pages converted for
$19.39. Five pages (page 4 and glossary pages 122, 132, 137, 142) were refused by the API's output content filter; support
said it cannot be exempted per organization, and the owner wrote to Anthropic's Safeguards team. Nothing is worked around.

The PDF carries the X3.23a-1989 and X3.23b-1993 amendments; the transcription keeps them marked as amendments. PB4812's
citation of PDF page 919 as a general-format page was wrong (it is the X3.23b change list) and now cites page 196.

The owner: "make the tooling updates now, but we won't run the tools until I say". Filed as a campaign under PB4812, with
that rule as its standing requirement: PB4815 (every spec tool takes an edition from one table in `spec_pdf.py`), PB4816
(one restartable, edition-aware batch converter in `scripts/spec/`, replacing `specs-private/scripts`), PB4817 (a raster
measurer for the scanned general formats). PB4812 is blocked on the three.
