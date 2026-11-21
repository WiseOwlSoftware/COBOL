## PB5993: the wrong-edition citation class re-measured; eight 1985 arms fixed

The note counted 20 codes citing only a later edition at `--std 85`. Re-run on this tree with the net's own rules (an edition
gate's "(targeting COBOL-85)" words, a descriptor's `No1985Rule`), 18 of the 27 named codes were never offenders. Real ones:
COBOL2403 now cites §1985:VI.5.3.4 GR1, 0854 §1985:VI.5.8.3 SR1b, the report arm of 2422 §1985:XIII.3.17.3, 2198
§1985:XIII.3.21.3 SR3 (and no longer says NATIONAL or prints the enum name), 1858 names the omitted ORGANIZATION as §1985:VII.2.6.3
GR3 through `FileModel.OrganizationFace(EditionInfo)`, 1641 cites §1985:XII.3.2, 2894 names ANSI X3.23-1985 and §1985:I.1.5.2.5.2,
and 2691 records `No1985Rule` (1985 has no `>>` directive). COBOL1927 was not an edition defect: BINARY is a 1985 word.

Compiling every suite case and refusal not refused at 85 at `--std 85` shows the class is wider: 336 codes. That work, and the
register's `Standard` column citing §2023 at every edition, stay open in PB5993. Six goldens `pb5993-*-85` pin the citations. Before and after: of the note's 27 named codes 9 were real offenders and 8 are fixed; the wider class stands at 336 codes, listed with their sites in `kb/Work/PB5993/2026-11-21-split-codes-left.md`.
