- **D-TRANSFER-TO-SECTION — Transfer of control includes sections (§2023:14.6; E.2 item 26)**: a section is a first-class transfer target at all
  editions — `ProcedureTableBuilder` gives each section a contiguous `[StartPc,EndPc]` pc range, so GO TO / PERFORM
  of a section resolve (§2023:14.9.17 / §2023:14.9.28). The 2023 clarification is met without a gate.
