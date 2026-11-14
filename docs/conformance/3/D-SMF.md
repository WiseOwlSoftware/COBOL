- **D-SMF — the short-record fill of a SORT/MERGE transfer (§2023:14.9.40.4 GR7, GR16; §2023:14.9.24.4 GR2, GR13)**: three
  readings. **(a) Arm b) cannot arise.** "multiple record description entries … and the descriptions include a SELECT
  WHEN clause" needs the SELECT WHEN clause, which Annex A.4.8 declines by name (COBOL1705), so a file with several
  record descriptions takes c), the alphanumeric space. **(b) SORT GR16 a) / b) print "file-name-2"** where the rule's
  own lead-in (GR16: "If the file referenced by file-name-3 contains only fixed-length records") and its MERGE twin
  (GR13 a), file-name-4) name the GIVING file: a slip of the standard, read against the GIVING file. **(c) A LINE
  SEQUENTIAL or variable-length GIVING file has no fixed record length to fill to**: §2023:14.9.51.4 GR21 strips the
  trailing spaces of a line sequential record on write, and a variable-length file keeps each record at its own size,
  so the connector's own fit-and-trim owns their shape and the rule's fill is applied to fixed-length files only. The
  test of arm a) is one function of the file's own record descriptions (`FileModel.ShortRecordFillNational`), never
  the connector's per-record-area national flag, which is §2023:14.9.30.4 GR15's READ fill. (kb/Work PB1140; golden
  `2002/pb1140_sort_merge_short_record_fill`.)
