- **D-MCS-NR — the exception-name is EC-MCS-NO-REQUESTER; the standard's other spelling EC-MCS-NO-REQUESTOR is
  not a name** (kb/Work PB3149; a defect in the printed standard, recorded here and not coded around). The ISO PDF prints
  both spellings, and the transcription matches it at every site. §2023:14.6.13.1.6 Table 13, which "is a list of the
  exception-names and their attributes", has the row `| EC-MCS-NO-REQUESTER | NF | RECEIVE statement |`, and the
  alphabetic Index spells it the same way. §2023:E.3.2 3) spells it EC-MCS-NO-REQUESTOR in its list of the Asynchronous
  messaging facility's new exception conditions. Annex E is informative and only summarizes changes; it defines no
  exception-name. §2023:7.3.25.3 SR2 binds the TURN directive's operand to Table 13: "Exception-name-1 shall be one of
  the exception names listed in 14.6.13.1". So the normative name is the Table 13 spelling, and no general rule raises
  either spelling (§2023:14.9.31.4, RECEIVE, raises EC-MCS-INVALID-TAG and EC-MCS-REQUESTOR-FAILED only). The -OR
  spelling of the sibling EC-MCS-REQUESTOR-FAILED is consistent between Table 13 and E.3.2, so only NO-REQUESTER
  differs. GnuCOBOL 3.2 agrees: `libcob/exception.def` registers EC-MCS-NO-REQUESTER (1A05) and no -OR alias.
  Accepting EC-MCS-NO-REQUESTOR as well would be a nonstandard extension (section 3.2 below) that nothing asks for. So
  `ExceptionCatalog` catalogues EC-MCS-NO-REQUESTER only (a COBOL-2023 name, raised by SEND and RECEIVE), and EC-MCS-NO-REQUESTOR is an unknown exception-name: COBOL0711 at every edition that has the TURN
  directive. Witnessed by `conformance:cases/_unassigned/pb3149_ec_mcs_no_requester_table13` and
  `conformance:cases/_unassigned/pb3149-ec-mcs-no-requestor-unknown`. Confirmed by the owner on 2026-10-18 ("keep
  EC-MCS-NO-REQUESTER only"; kb/Work PB3149, "Owner confirmation").
