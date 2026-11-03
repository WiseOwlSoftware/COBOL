// Deliberate violations in a file semgrep's C# grammar CANNOT READ — the negative control for kb/Work PB4462.
//
// This file is NOT compiled (it is outside every .csproj). semgrep's C# parser predates C# 12 and 13: it fails on a
// primary-constructor class header, an `extension` member, a list pattern, `allows ref struct` and a raw interpolated
// string, and it skips the member (or, after a failure early in a class, a long span) it could not read. An AST rule
// is then blind over that span while the scan still says "success": PB4462 found 130 of the product's files in that
// state, the data binder among them, and the raw-code-literal count under-read 230 of 248.
//
// `scripts/semgrep/verify.py` therefore keeps every rule TEXT (one `pattern-regex`, which matches over the file's
// characters), and asserts that EVERY rule fires in this file, which semgrep reads only in part.

namespace WiseOwl.Cobol.SemgrepTestData;

public sealed class UnparsedViolations(int primaryConstructorParameter)   // unreadable header (C# 12)
{
    public sealed record BoundUnparsedNode(int Id, string RenderedText);   // wiseowl-cobol-bound-node-carries-rendered-text

    // The first statement fails semgrep's grammar too (a C# 11 list pattern with a slice designation); the member holds
    // the violations of every rule that can be written inside a method.
    public void Everything(string sel, UnparsedItem item, UnparsedLiteral lit)
    {
        bool a = sel is ['a', .. var rest]; byte[] persistedState = new byte[256];   // wiseowl-cobol-no-persistent-byte-storage
        decimal scaled = 1.5m;                                     // wiseowl-cobol-no-decimal
        System.Numerics.BigInteger wide = 2;                       // wiseowl-cobol-no-biginteger
        // TODO: the silent-no-op shape, in an unreadable file     <- wiseowl-cobol-no-silent-todo
        Emit(Severity.Error, "COBOL1999", "bypasses the catalog");   // wiseowl-cobol-raw-diagnostic-code-literal
        bool category = item.Pic?.Category == 1;                   // wiseowl-cobol-condition-category-from-raw-picture
        int operand = int.Parse(lit.integerLiteral().GetText());   // wiseowl-cobol-integer-n-parsed-outside-host-value
        string cast = $"int d = (int)({NumericRenderer.Align(sel, primaryConstructorParameter)});";   // wiseowl-cobol-integer-value-cast-narrowed
        _ = (a, persistedState, scaled, wide, category, operand, cast);
    }

    private enum Severity { Error }
    private static void Emit(Severity severity, string code, string message) { }

    public sealed class UnparsedLiteral { public UnparsedLiteral integerLiteral() => this; public string GetText() => "1"; }
    private static class NumericRenderer { public static string Align(string s, int scale) => s; }
    public sealed class UnparsedPic { public int Category; }
    public sealed class UnparsedItem { public UnparsedPic? Pic; }
}
