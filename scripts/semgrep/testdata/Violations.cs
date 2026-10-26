// Deliberate violations — the negative control for scripts/semgrep/invariants.yml.
//
// This file is NOT compiled (it is outside every .csproj). It exists so a rule that has silently stopped matching
// is caught: `scripts/semgrep/verify.py` asserts EVERY rule fires here, and that the greenfield tree's finding
// count has not grown. A rule that never fires reports green and is worse than no rule at all — an earlier draft
// of the rule set had exactly that failure, from an invented suppression construct that disabled two rules.
//
// Each violation is annotated with the rule it must trip. Keep one clean example per rule.

namespace WiseOwl.Cobol.SemgrepTestData;

public sealed record BoundFakeNode(int Id, string RenderedText);   // wiseowl-cobol-bound-node-carries-rendered-text

public class Violations
{
    private byte[] _persistedState = new byte[256];                // wiseowl-cobol-no-persistent-byte-storage

    public void Numerics()
    {
        decimal scaled = 1.5m;                                     // wiseowl-cobol-no-decimal
        System.Numerics.BigInteger wide = 2;                       // wiseowl-cobol-no-biginteger
        _ = scaled;
        _ = wide;
    }

    public void Diagnostics()
    {
        // TODO: this is the silent-no-op shape the loud-failure invariant forbids  <- wiseowl-cobol-no-silent-todo
        // The bypass shape is a bare code literal passed to an emit API, not merely the string existing:
        Emit(Severity.Error, "COBOL1999", "bypasses the catalog");   // wiseowl-cobol-raw-diagnostic-code-literal
    }

    private enum Severity { Error }
    private static void Emit(Severity severity, string code, string message) { }

    // A raw picture read where the operand's CATEGORY is being decided - null for every group, so a bit /
    // national group silently takes the alphanumeric branch.
    public bool ConditionCategory(FakeItem item) =>
        item.Pic?.Category == 1;                                   // wiseowl-cobol-condition-category-from-raw-picture

    // An integer-n read with a throwing / silently-failing parse instead of IntegerOperandRules.HostValue.
    public int IntegerOperand(FakeLiteral lit) =>
        int.Parse(lit.integerLiteral().GetText());                 // wiseowl-cobol-integer-n-parsed-outside-host-value

    // A COBOL literal's token text parsed with C# rules instead of IntegerOperandRules.TryHostValue (kb/Work PB1579).
    public int? RefModStart(FakeToken tok) =>
        int.TryParse(tok.Text, out int v) ? v : null;              // wiseowl-cobol-integer-n-parsed-outside-host-value

    // A COBOL integer value narrowed with a C# cast, which wraps, instead of RuntimeApi.HostInt32 / HostInt64.
    public string Selector(string sel) =>
        $"int d = (int)({NumericRenderer.Align(sel, 0)});";        // wiseowl-cobol-integer-value-cast-narrowed

    public sealed class FakeLiteral { public FakeLiteral integerLiteral() => this; public string GetText() => "1"; }
    public sealed class FakeToken { public string Text => "1"; }
    private static class NumericRenderer { public static string Align(string s, int scale) => s; }
    public sealed class FakePic { public int Category; }
    public sealed class FakeItem { public FakePic? Pic; public FakePic? OperandPic; }
}
