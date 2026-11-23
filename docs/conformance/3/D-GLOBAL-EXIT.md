- **D-GLOBAL-EXIT — EC-FLOW-GLOBAL-EXIT is set at 2002 and 2014, and never from 2023** (kb/Work PB3102, PB5715). §2002:14.8.13.3
  GR2 (§2014:14.9.13.3 GR2 the same) sets it: "If an EXIT statement is executed within the range of a declarative procedure whose
  USE statement contains the GLOBAL phrase and that USE statement is specified in the same function, method, or program as the
  function format, method format, or program format EXIT statement respectively, the EC-FLOW-GLOBAL-EXIT exception condition is
  set to exist", and a GOBACK in a function or method operates as that EXIT (§2002:14.8.17.3 GR3, GR4). At `--std 2002` and
  `--std 2014` `CallEmitter.EmitExitProgram` and `EmitGoback` raise it through `ExceptionState.FlowGlobalExitError` when its checking
  is enabled, by the same run-time `__useActive` test as EC-FLOW-GLOBAL-GOBACK; an EXIT PROGRAM raises it only in a program under the control of a calling runtime element (`__asCalled`), because §2002:14.8.13.3 GR5 (§2014:14.9.13.3 GR5) treats it in any other program as CONTINUE with "No exception condition is raised" (golden `pb5715_exit_program_main_global_declarative_is_continue`); an EXIT METHOD is unreachable until a method may
  carry declaratives. §2023:14.6.13.1.6 Table 13 still names it ("EXIT PROGRAM in a global Declarative") as a Fatal condition, and
  §2023:14.6.13.1.3 GR6 mentions it beside EC-FLOW-GLOBAL-GOBACK, but the standard states no general rule that SETS it from 2023: the
  sibling is set by §2023:14.9.18.4 GR6 (a GOBACK executed within the range of a global declarative), §2023:14.9.14.4 GR3 gives an
  EXIT PROGRAM only GOBACK's GR3 and GR4, not GR6, and the one normative statement about EXIT PROGRAM in a global declarative is
  §2023:14.9.14.3 SR2, which the compiler enforces at compile time. At `--std 2023` the name is catalogued and accepted wherever an
  exception-name may be written, and the implementation never raises it (`DialectBehavior.FlowGlobalExitIsNeverSet`, VCR row 7.118):
  inventing a raise site for a condition the edition's text never sets would be behaviour with no rule behind it. The 2002 and 2014
  edge is held by `conformance:cases/14.8.13@2002-EXIT-STATEMENT/pb5715_exit_program_global_declarative_range` and
  `conformance:cases/14.8.17@2002-GOBACK-STATEMENT/pb5715_goback_in_function_global_declarative_range`.
