- **D-IO-STATUS-07-OR-05 — I-O status '07' vs '05' on one OPEN (§2023:14.9.27.4 GR11 against §2023:9.1.13.2 item 4 a; kb/Work PB317)**: an
  `OPEN INPUT` of an OPTIONAL file that is not present, written `WITH NO REWIND`, reports **'07'**. Both values
  describe a successful open and the standard does not rank them, so this is a determination: GR11 is an explicit
  ASSIGNMENT in the statement's own general rules (*"the I-O status associated with file-name-1 is set to
  '07'"*), whereas item 4 a) is a DESCRIPTION in the status-value clause, and §2023:14.9.27.4 assigns '05' only in
  GR17 — whose EXTEND and I-O modes §2023:14.9.27.3 SR6 bars from carrying the phrase at all. The information '05'
  would have carried is not lost: the first READ of an absent optional file is the at-end '10' (§2023:9.1.13.4 item
  1 c). Golden `2023/pb317_open_no_rewind` (`OPT-ABSENT=07`).
