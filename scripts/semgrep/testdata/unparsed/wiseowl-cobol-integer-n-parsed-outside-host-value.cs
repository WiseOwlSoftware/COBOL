// Negative control for kb/Work PB4462 and PB4474 -- NOT compiled (outside every .csproj).
//
// The class header below is a C# 12 primary constructor, which semgrep's C# grammar cannot read: it reports the line as a
// PartialParsing span and skips it. That one line carries a violation of ONE rule, wiseowl-cobol-integer-n-parsed-outside-host-value.
// scripts/semgrep/verify.py (check 1) requires the rule to fire ON a line inside the span semgrep reports, so a rule
// that depended on the parser (blind in an unreadable span) fails there, and a semgrep that learns to read this file
// flags the control as obsolete instead of passing. One file per rule: semgrep gives up on a whole file after enough
// unreadable constructs, and then reports no span at all.

namespace WiseOwl.Cobol.SemgrepTestData;

public sealed class UnparsedControl(int p) { int N(UnparsedLiteral lit) => int.Parse(lit.integerLiteral().GetText()); }
