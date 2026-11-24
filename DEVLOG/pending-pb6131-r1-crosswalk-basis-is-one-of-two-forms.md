## A crosswalk entry's basis is one of two forms (PB6131 review r1)

The PB6131 merge treated any basis other than exactly `mechanical` as an adjudication. A missing basis, or a typo like
`Mechanical`, was therefore kept forever, and both `--build` and `--check` stayed GREEN. Now exactly two bases are legal:
`mechanical` and `adjudicated: <reason>`. `--build` keeps an entry with any other basis, reports it and ends `BUILD RED`.
`--check` refuses it. Self-test case 22 pins seven illegal forms, and the crosswalk fixtures now write through
`crosswalk.write_file`. A whole-crosswalk rebuild still changes no file and keeps the 11 NC115A adjudications. The finding
is filed at kb/Work/PB6131/2026-11-23-review-r1-strict-basis.md.
