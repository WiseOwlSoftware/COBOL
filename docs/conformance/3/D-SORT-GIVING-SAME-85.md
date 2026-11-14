- **D-SORT-GIVING-SAME-85 — two GIVING files of one SORT in a SAME RECORD AREA clause** (kb/Work PB5276; the text decides, no latitude). §1985:XI.4.4.3
  SR9: "File-names associated with the GIVING phrase may not be specified in the same SAME clause", and SAME [RECORD] AREA is the 1985 clause
  (§1985:VII.2.13.2), so COBOL-85 refuses two GIVING files in one SAME RECORD AREA clause (COBOL1757, `SortBinder.ScreenSameClauses`);
  §2023:14.9.40.3 SR10 narrows the sentence to the SAME AREA clause, so they may share a record area at COBOL-2023 (the editions between are not decided here: the refusal is asked below 2002 only). MERGE's GIVING files may share a record
  area at every edition (§1985:XI.4.1.3 SR8). Witnesses: `conformance:wiseowl/diagnostics/pb5276-sort-giving-same-record-area-85`,
  `conformance:cases/14.9.40-SORT/pb5276_sort_giving_same_record_area_2023`.
