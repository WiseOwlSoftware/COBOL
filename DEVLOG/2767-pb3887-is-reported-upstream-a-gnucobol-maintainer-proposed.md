## Entry 2767 — 2026-10-29 20:59 PDT — PB3887 is reported upstream: a GnuCOBOL maintainer proposed the fix, and it is verified

The owner's introduction to bug-gnucobol@gnu.org (2026-10-29) used PB3887, right-to-left `**` in COMPUTE, as its example.
Sergey Kashyrin answered the same evening with a one-line parser.y change, `exp_factor EXPONENTIATION exp_unary`. The
Mythos session read the rules in the 3.3-dev source and built the r5751 snapshot with the change. COMPUTE then evaluates
left to right, agreeing with conditions (typeck.c build_expr_reduce), which 3.2 already evaluates that way. Unary minus
still binds to its operand first, as ISO 8.8.1.2 rule 2 requires. The owner replied with those results. PB3887 is
`upstream_status: reported`, linked to the list archive thread, and records the probes and the patch.

