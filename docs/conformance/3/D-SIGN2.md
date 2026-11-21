- **D-SIGN2 — the COBOL-85 group SIGN subject (kb/Work PB1319; VCR row 7.40).** §1985:VI.5.12.3 SR1 admits a group
  SIGN clause only on "a group item containing at least one such numeric data description entry" (one whose PICTURE
  contains the character 'S'), and §1985:VI.5.12.3 SR2 requires that the entries the clause applies to "must be
  described, implicitly or explicitly, as USAGE IS DISPLAY". §2002:13.16.50.2 and §2023:13.18.52.3 keep neither restriction. At `--std 85`
  each is refused with COBOL0900 through its construct row (`sign-group-unsigned-subordinates-2002`,
  `sign-group-nondisplay-subordinate-2002`; `CheckSignClausesPass.GateGroupSignSubordinates`); at 2002 and later the 2023
  rules and D-SIGN1 apply. Both 2002 edges are QUOTED from the editions' own texts (kb/Work PB3965): the 1985 SR1 and
  SR2 against §2002:13.16.50.2 SR1 ("an alphanumeric group item, national group item, or strongly-typed group item") and
  SR2 ("an elementary item for which the SIGN clause is specified"); §2002:F.2 states the first, its item 124: "A sign
  clause may be specified on any group item. Formerly, the group had to contain at least one numeric item." A subordinate that writes its own SIGN clause is outside the group's reach (§2023:13.18.52.4
  GR2/GR3), so `01 G SIGN LEADING SEPARATE. 05 C PIC S9(3) COMP SIGN TRAILING.` is refused for C's own clause
  (§2023:13.18.52.3 SR2, COBOL2422), never twice. Witnessed by `conformance:cases/13.18.52-SIGN/pb1319_sign_group_subjects_2002`,
  `conformance:cases/13.18.52-SIGN/pb1319-sign-group-unsigned-85` and `conformance:cases/13.18.52-SIGN/pb1319-sign-group-comp-85`.
  **The report arm (kb/Work PB3683).** A report group description entry's SIGN clause is narrower at 85 in two ways,
  both QUOTED from ISO/IEC 1989:2002 Annex F.2 item 123 ("The SEPARATE phrase is no longer required in a report
  description entry and the SIGN clause may be specified at the group level"): the clause needs SEPARATE CHARACTER, and
  it is written on an elementary entry only. Each is refused at `--std 85` with COBOL0900 through its construct row
  (`report-sign-without-separate-2002`, `report-group-sign-2002`; `DataBinder.ScreenReportSignClauses`, once per
  written entry); at 2002 and later §2023:13.18.52.3 SR1 and §2023:13.18.52.4 GR5 apply. Witnessed by
  `conformance:cases/13.18.52-SIGN/pb3683_report_sign_2002`, `conformance:cases/13.18.52-SIGN/pb3683-report-sign-without-separate-85`
  and `conformance:cases/13.18.52-SIGN/pb3683-report-group-sign-85`.
