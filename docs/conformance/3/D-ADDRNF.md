- **D-ADDRNF — a fatal address-resolution condition with checking OFF continues with NULL (§2023:14.6.13.1.3 8);
  §2023:8.4.3.13.4 4); §2023:8.4.3.12.4 4))**: `SET … TO ADDRESS OF PROGRAM` (and `SET … TO ENTRY`) that cannot locate the
  program sets EC-PROGRAM-NOT-FOUND and gives the address-identifier the predefined address NULL (§2023:8.4.3.13.4 4));
  `ADDRESS OF FUNCTION` likewise sets EC-FUNCTION-NOT-FOUND with NULL (§2023:8.4.3.12.4 4)), and its EC-FUNCTION-PTR-INVALID
  twin is handled by the same site. All three are FATAL in Table 13. With checking ENABLED the condition goes through
  the one selection (`EcEmitter.EmitSelection`): the declarative runs, and a run that is not resumed terminates
  abnormally (§2023:14.6.13.1.3 5) / 7); kb/Work PB1549). With checking NOT enabled, §2023:14.6.13.1.3 8) makes it the
  implementor's choice: "the implementor defines whether or not execution will continue, how it will continue, and
  how any receiving operands are affected". **WiseOwl COBOL continues with the next statement, and the receiving
  pointer holds NULL** — the value GR4 of each clause already assigns, so a program can test the pointer against
  NULL exactly as it would after a handled condition. Rejected: terminating the run unit — GR4 already defines the
  receiving value, so continuing leaves a defined, testable result, and it is the behaviour unchanged since the SET
  surfaces landed.
  Code sites: `PtrEmitter.EmitSetEntry` and `PtrEmitter.EmitSetFunctionAddress` (the `_ = nf` lines); pinned by
  `conformance-test:FatalRaiseSelectionTests` (the checking-off case).
