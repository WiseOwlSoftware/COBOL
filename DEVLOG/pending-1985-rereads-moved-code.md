## The 1985 code reads the listing moved, read again: four rows contradicted, two defects filed

The source listing's step 6b (kb/Work PB5932) moved the binder's name resolution into the name table, and 30 stored code reads
at `--std 85` named members that no longer exist where the read found them. The store does not call such a read wrong; it
calls it owed a re-read (kb/Work PB5863), and `code_reads.py plan --edition 1985` listed the 30 rows. They were read again, three
slices of ten, one reader and one refuter each, on a compiler built at main 832bc39c61, with the earlier records out of
reach so each read started fresh from the 1985 text.

The readers confirmed 29 and called one PARTIAL; the refuters upheld 25, overturned three and doubted one. I re-ran every
overturning program on the same compiler before ruling, and read the 1985 text and the PDF page for the scope rule:

- **PB5972.** 1985 puts condition-names, data-names and record-names in one disjoint set (§1985:IV.4.2.2.1.1), so a level-88
  `X` and a data item `X` in another record are legal when every reference is qualified. The compiler refuses the program at
  the declaration (COBOL2692): its one-type-per-word census uses the latest edition's sharing list at every edition.
- **PB5973.** Three rows of §1985:X.1.3.8.2 fail on one mechanism: hiding a container's GLOBAL name is decided per name class,
  by spelling, before qualification. A contained program's own data item `R1` does not hide the container's GLOBAL report
  `R1`, so `INITIATE R1` runs the container's report, a wrong answer; the converse writes the container's GLOBAL data item.
  A hidden GLOBAL file-name cannot qualify its own record, and a GLOBAL report's sum counter vanishes when the contained
  program declares a data-name of that spelling. The fix is one resolver over GR1's name set, with no per-class hide lists.
- The doubted row (RENAMES SR4, the same-word reading) stays CONFORMS under the recorded determination D-85-RENAMES-SAME-NAME.

The store now reads 1,351 confirmed, 4 contradicted, 0 owed and 0 owed a re-read at 1985. The four contradicted rows are the
1985 work left, and both notes are open.
