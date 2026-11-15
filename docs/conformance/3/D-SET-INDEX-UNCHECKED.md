- **D-SET-INDEX-UNCHECKED — SET of an index to a value outside its table, with EC-BOUND-SUBSCRIPT checking not enabled:
  the value is stored and execution continues** (kb/Work PB5625 reading C, adjudicated 2026-11-15; ISO/IEC 1989:2002
  §2002:B.1 item 51, "Fatal exception condition (whether or not execution will continue, how it will continue, and how
  any receiving operands are affected" when checking is not enabled). In 2002, SET format 1 and format 2 make an
  occurrence number outside the table the EC-BOUND-SUBSCRIPT condition (§2002:14.8.35.3 GR2a.1 through §2002:8.4.1.2.3
  GR2, and GR3b: "otherwise the EC-BOUND-SUBSCRIPT exception condition is set to exist, the execution of the SET
  statement is unsuccessful, and the content of the receiving operand is unchanged"). But a condition a general rule
  says exists "is raised only if checking for that exception condition is enabled" (§2002:14.5.12.1), "By default,
  checking is not enabled for any exception condition" (same clause), and for a fatal condition whose checking is not
  enabled "the implementor defines whether or not execution will continue, how it will continue, and how any receiving
  operands are affected" (§2002:14.5.12.1.2). §2002:F.2 item 121, informative, describes the same outcome ("The result
  of setting an index can be outside of the range of the associated table"). **Determination, at `--std 2002`:** with
  checking for EC-BOUND-SUBSCRIPT not enabled (the default), WiseOwl COBOL stores the out-of-range occurrence number in
  the index and continues; no exception condition is raised. Measured on the build of main 24372b7b17, a table of 10:
  `SET IX TO 11` gives 11, `SET IX TO 3` then `UP BY 12` gives 15, then `DOWN BY 10` gives 5. An amount that is not an
  integer is a different arm (GR3a): the SET is unsuccessful and the index unchanged, with or without checking. With
  checking ENABLED, the 2002 rules apply as written; the compiler does not yet do so (kb/Work PB5674). **Survey (CLAUDE.md
  rule 1):** GnuCOBOL 3.2.0 (measured 2026-11-15 in WSL Ubuntu, `cobc -x -std=default`, `-std=cobol2002` and
  `-std=cobol2002 -debug`, the same program) stores the value and continues in all three, printing `S2=11 U2=15 U3=05`,
  the same as WiseOwl COBOL. IBM Enterprise COBOL and Micro Focus: unmeasured (neither is installed on this host, and
  GnuCOBOL already answers, so the precedence does not reach them).
