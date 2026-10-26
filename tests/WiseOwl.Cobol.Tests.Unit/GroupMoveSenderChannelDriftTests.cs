// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ <b>EVERY CHANNEL THAT SENDS A VALUE INTO A PLAIN GROUP ASKS THE ONE SENDING CODEC FOR THE ALPHABET IT SENDS IN</b>
/// (kb/Work PB3562, PB3570, PB3587; docs/CONFORMANCE.md D-B3, D-B4). §14.9.25.4 GR4 makes a move to a group "an
/// alphanumeric to alphanumeric elementary move, except that there is no conversion of data from one form of internal
/// representation to another", so what a sender deposits is decided by <c>OperandText.NonElementaryMoveSender</c> alone:
/// a numeric literal's digits, a national value's UTF-16BE pairs, a bit value's packed bits, an item's storage image.
/// A CALL that cannot see its formal (a Format-1 CALL, a CALL located at run time) cannot choose between the elementary
/// formal's alphabet (characters) and the plain-group formal's (the representation), so each BY CONTENT argument states
/// BOTH in its <c>MoveSending</c> and the activated unit chooses (<c>CobolArgAdapt.Text</c>). The defect these pin was a
/// producer of a sending operand that stated only the characters: silent for every DISPLAY-usage sender, where the two
/// alphabets agree, and wrong for the national and bit ones. They assert the SHAPE that makes a new producer decide
/// (CLAUDE.md rule 5 - pair the structure with a drift test): the producers' signature REQUIRES the second alphabet, no
/// producer builds a <c>MoveSending</c> by hand, the group adapter reads it, and an INVOKE literal keeps its category.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class GroupMoveSenderChannelDriftTests
{
    private static string Src(params string[] path) => StripComments(File.ReadAllText(TestRepo.Src(path)));

    private static string StripComments(string src) =>
        Regex.Replace(Regex.Replace(src, @"/\*.*?\*/", "", RegexOptions.Singleline), @"//[^\n]*", "");

    private static IEnumerable<string> CompilerSources() =>
        Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    /// <summary>The top-level argument texts of every call of <paramref name="method"/> in <paramref name="src"/>
    /// (balanced parentheses, brackets, braces and string literals respected).</summary>
    private static List<List<string>> CallArguments(string src, string method)
    {
        var calls = new List<List<string>>();
        foreach (Match m in Regex.Matches(src, @"\b" + Regex.Escape(method) + @"\("))
        {
            int i = m.Index + m.Length, depth = 1;
            var args = new List<string>();
            var cur = new System.Text.StringBuilder();
            bool inString = false;
            for (; i < src.Length && depth > 0; i++)
            {
                char c = src[i];
                if (inString)
                {
                    cur.Append(c);
                    if (c == '\\' && i + 1 < src.Length) cur.Append(src[++i]);
                    else if (c == '"') inString = false;
                    continue;
                }
                if (c == '"') { inString = true; cur.Append(c); continue; }
                if (c is '(' or '[' or '{') depth++;
                else if (c is ')' or ']' or '}') { depth--; if (depth == 0) break; }
                if (c == ',' && depth == 1) { args.Add(cur.ToString().Trim()); cur.Clear(); continue; }
                cur.Append(c);
            }
            args.Add(cur.ToString().Trim());
            calls.Add(args);
        }
        return calls;
    }

    /// <summary>A numeric LITERAL, and a numeric FUNCTION value, sent to a group store their DIGITS only (D-B4):
    /// <c>NonElementaryMoveSender</c> has an arm for each, ahead of the operand-text fallthrough that kept the sign and the
    /// decimal point (kb/Work PB3570, PB3613), and both arms read the ONE digits rule, <c>CobolNum.GroupDigits</c> - the
    /// compiler calls it on a literal's text, the emitted code on a function value's run-time text, so a function the compiler
    /// folded to a literal and the same call evaluated at run time send the same characters. The rule is written nowhere
    /// else in the compiler.</summary>
    [Fact]
    public void NonElementaryMoveSender_NumericValue_SendsItsDigitsOnly_ThroughTheOneRule()
    {
        string src = Src("WiseOwl.Cobol.Compiler", "CodeGen", "Emit", "OperandText.cs");
        var body = Regex.Match(src, @"public\s+static\s+string\s+NonElementaryMoveSender\s*\([^)]*\)(?<b>.*?)\n    \};",
            RegexOptions.Singleline);
        Assert.True(body.Success, "OperandText.NonElementaryMoveSender is no longer recognizable");
        string b = body.Groups["b"].Value;
        // The literal arm covers a function value folded to a literal too (no FunctionValue exemption).
        Assert.Matches(@"BoundNumericLiteral\s+\w+\s*=>\s*EmitText\.CsLiteral\(\s*CobolNum\.GroupDigits\(", b);
        Assert.Matches(@"BoundComputedOperand\s*\{\s*IsNumericFunctionValue:\s*true\s*\}\s*=>\s*RuntimeApi\.NumGroupDigits\(", b);
        // …both ahead of the fallthrough that renders the operand text.
        int fallthrough = b.IndexOf("RepresentationOfText(", System.StringComparison.Ordinal);
        Assert.True(b.IndexOf("CobolNum.GroupDigits(", System.StringComparison.Ordinal) < fallthrough
                    && b.IndexOf("RuntimeApi.NumGroupDigits(", System.StringComparison.Ordinal) < fallthrough,
            "the numeric-value arms must precede the operand-text fallthrough");
        // One rule in one place: the compiler holds no second copy of the digit filter (it lives in CobolNum.GroupDigits).
        Assert.DoesNotContain("LiteralDigits(", src);
    }

    /// <summary>⛔ A FUNCTION-IDENTIFIER'S VALUE IS A MOVE SENDER, WHATEVER TREE THE BINDER LOWERED IT TO, AND THE ONE READER IS
    /// <c>BoundComputedOperand.IsNumericFunctionValue</c> (kb/Work PB3613). FUNCTION LENGTH of a variable-length group is lowered to
    /// a <c>BoundBinary</c> sum, which an arithmetic expression (no MOVE sender, §14.8.2.2 rule 2) is too; the binder states the
    /// difference once, in <c>IntrinsicBinder.OperandOf(expr, functionValue)</c> - a required parameter, so a new position that makes
    /// an operand from a bound expression must say which it is - and the activation description, the CALL arm, the INVOKE binder
    /// guard and the sending codec all ask the one property, never a node shape.</summary>
    [Fact]
    public void FunctionValue_IsStatedOnceByTheBinder_AndReadThroughOneProperty()
    {
        string intrinsic = Src("WiseOwl.Cobol.Compiler", "Binding", "Procedure", "Verbs", "IntrinsicBinder.cs");
        Assert.Matches(@"static\s+BoundOperand\s+OperandOf\(\s*BoundExpr\s+e,\s*bool\s+functionValue\s*\)", intrinsic);
        Assert.Matches(@"new\s+BoundComputedOperand\(e\)\s*\{\s*FunctionValue\s*=\s*functionValue\s*\}", intrinsic);
        foreach (string[] path in new[]
        {
            new[] { "WiseOwl.Cobol.Compiler", "Oo", "ActivationDescriptions.cs" },
            new[] { "WiseOwl.Cobol.Compiler", "CodeGen", "Verbs", "CallEmitter.cs" },
            new[] { "WiseOwl.Cobol.Compiler", "CodeGen", "Emit", "OperandText.cs" },
            new[] { "WiseOwl.Cobol.Compiler", "Binding", "Procedure", "Verbs", "OoBinder.cs" },
        })
            Assert.Contains("IsNumericFunctionValue: true", Src(path));
        // The INVOKE lane carries the fact from the binder to the emitter beside the expression it qualifies.
        Assert.Contains("ContentFunctionValue", Src("WiseOwl.Cobol.Compiler", "CodeGen", "Verbs", "OoEmitter.cs"));
    }

    /// <summary>⛔ A PARENTHESIZED FUNCTION IS AN ARITHMETIC EXPRESSION, WHATEVER TREE IT BINDS TO (kb/Work PB3678). The expression
    /// binder drops grouping parentheses, so <c>(FUNCTION MIN(N 3))</c> binds to the sole call's own <c>BoundIntrinsicCall</c>; the
    /// property that tells a MOVE sender from an expression therefore reads ONLY the binder's statement
    /// (<c>BoundComputedOperand.FunctionValue</c>, plus the result category), never the tree's shape, and the CALL argument
    /// binder wraps what is left after its sole-shape arms as the computed operand it is (<c>ConditionBinder.CompoundOperand</c>,
    /// kb/Work PB1935) instead of mapping it back to the item or call it encloses.</summary>
    [Fact]
    public void ParenthesizedFunction_IsAnExpression_TheTreeShapeIsNeverAsked()
    {
        string tree = Src("WiseOwl.Cobol.Compiler", "Binding", "Bound", "BoundTree.cs");
        var property = Regex.Match(tree, @"public\s+bool\s+IsNumericFunctionValue\s*=>(?<b>[^;]*);");
        Assert.True(property.Success, "BoundComputedOperand.IsNumericFunctionValue is no longer recognizable");
        string body = property.Groups["b"].Value;
        Assert.Matches(@"^\s*FunctionValue\s*&&", body);
        Assert.DoesNotMatch(@"Expr\s+is\s+BoundIntrinsicCall\s+\w+\s*\?", body);
        string call = Src("WiseOwl.Cobol.Compiler", "Binding", "Procedure", "Verbs", "CallBinder.cs");
        Assert.Contains("host.Cond.CompoundOperand(ax)", call);
        Assert.DoesNotContain("OperandOf(host.Expr.BindExpr(ax)", call);
    }

    /// <summary>"Is this a group operation?" - a group that is not a bit or national group acting as an elementary item - is
    /// <c>DataItem.IsPlainGroup</c> and written nowhere else (kb/Work PB3613): the INVOKE binder's guard, the CALL and INVOKE
    /// emitters and the MOVE classifier ask the one property, so the binder's routing and the emitter's arm cannot disagree.</summary>
    [Fact]
    public void PlainGroup_IsOneProperty_OnDataItem()
    {
        var offenders = CompilerSources()
            .Where(f => !f.EndsWith("DataItem.cs", System.StringComparison.Ordinal))
            .Where(f => Regex.IsMatch(StripComments(File.ReadAllText(f)),
                @"IsGroup\s*&&\s*!\s*[\w.]*IsAsIfElementary|IsGroup:\s*true,\s*IsAsIfElementary:\s*false"))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(offenders.Count == 0, "the plain-group test is written inline in " + string.Join(", ", offenders) + ": use DataItem.IsPlainGroup");
    }

    /// <summary>The usage-keyed codec (national through <c>NatBytes</c>, bit through <c>BitsPackAll</c>) is written ONCE, in
    /// <c>RepresentationOfText</c>, and both <c>NonElementaryMoveSender</c> and the run-time converter
    /// (<c>StorageImageConverter</c>, for an already-evaluated function value) read it through the one static-usage
    /// reader (kb/Work PB3562, PB3587).</summary>
    [Fact]
    public void RepresentationCodec_IsWrittenOnce_AndReadByBothTheSenderAndTheConverter()
    {
        string src = Src("WiseOwl.Cobol.Compiler", "CodeGen", "Emit", "OperandText.cs");
        var codec = Regex.Match(src, @"private\s+static\s+string\s+RepresentationOfText\s*\([^)]*\)(?<b>.*?)\n    \};",
            RegexOptions.Singleline);
        Assert.True(codec.Success, "OperandText.RepresentationOfText is no longer recognizable");
        Assert.Contains("IntrinsicArgumentRules.StaticUsageOf(op)", codec.Groups["b"].Value);
        Assert.Matches(@"Usage\.National\s*=>\s*RuntimeApi\.NatBytes\(", codec.Groups["b"].Value);
        Assert.Matches(@"Usage\.Bit\s*=>\s*RuntimeApi\.BitsPackAll\(", codec.Groups["b"].Value);
        // The sender and the converter both go through it, and neither spells the codec itself.
        var sender = Regex.Match(src, @"public\s+static\s+string\s+NonElementaryMoveSender\s*\([^)]*\)(?<b>.*?)\n    \};", RegexOptions.Singleline);
        Assert.True(sender.Success, "OperandText.NonElementaryMoveSender is no longer recognizable");
        Assert.Matches(@"_\s*=>\s*RepresentationOfText\(op,", sender.Groups["b"].Value);
        Assert.DoesNotMatch(@"RuntimeApi\.(NatBytes|BitsPackAll)\(", sender.Groups["b"].Value);
        var converter = Regex.Match(src, @"StorageImageConverter\(BoundOperand op\)(?<b>[^;]*);", RegexOptions.Singleline);
        Assert.True(converter.Success, "OperandText.StorageImageConverter is no longer recognizable");
        Assert.Contains("RepresentationOfText(op,", converter.Groups["b"].Value);
        Assert.DoesNotMatch(@"RuntimeApi\.(NatBytes|BitsPackAll)\(", converter.Groups["b"].Value);
    }

    /// <summary>No producer builds a <c>MoveSending</c> by hand: every one goes through <c>RuntimeApi.MoveSendingNew</c>
    /// (or <c>CobolArgAdapt.WithValueSending</c> at run time), whose signature REQUIRES the second alphabet. A producer
    /// written as <c>new MoveSending(chars)</c> would state only the characters and re-open the defect (kb/Work PB3587).</summary>
    [Fact]
    public void NoProducerBuildsAMoveSendingByHand()
    {
        var offenders = CompilerSources()
            .Where(f => !f.EndsWith("RuntimeApi.cs", System.StringComparison.Ordinal))
            .Where(f => Regex.IsMatch(StripComments(File.ReadAllText(f)), @"new\s+(?:global::)?(?:WiseOwl.Cobol\.Runtime\.)?MoveSending\s*\("))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(offenders.Count == 0,
            "a MoveSending is built by hand in " + string.Join(", ", offenders) + ": use RuntimeApi.MoveSendingNew, whose `image` "
            + "parameter forces the producer to say whether the sender's representation is not its characters (kb/Work PB3587)");
    }

    /// <summary>Every call of the producers states its second alphabet EXPLICITLY - <c>image</c> (the variable
    /// <c>OperandText.StorageImageIfDiffers</c> builds) or <c>image: null</c> for a sender whose characters ARE its
    /// representation (a figurative constant, a group), and <c>ArgWithValueSending</c> its converter or null - so the
    /// decision is visible at each producer (kb/Work PB3587).</summary>
    [Fact]
    public void EveryMoveSendingProducerStatesItsSecondAlphabet()
    {
        int seen = 0;
        foreach (string file in CompilerSources())
        {
            string src = StripComments(File.ReadAllText(file));
            if (file.EndsWith("RuntimeApi.cs", System.StringComparison.Ordinal)) continue;
            foreach (var args in CallArguments(src, "RuntimeApi.MoveSendingNew"))
            {
                seen++;
                Assert.True(args.Count >= 2 && Regex.IsMatch(args[1], @"^(image:\s*)?(null|image)$"),
                    $"{Path.GetFileName(file)}: RuntimeApi.MoveSendingNew(...) must state its second alphabet as `image` or `image: null`, found '{(args.Count > 1 ? args[1] : "")}'");
            }
            foreach (var args in CallArguments(src, "RuntimeApi.ArgWithValueSending"))
            {
                seen++;
                Assert.True(args.Count == 3, $"{Path.GetFileName(file)}: RuntimeApi.ArgWithValueSending(...) must state its storage-image converter (or null) and whether its binary64 value is an engine approximation (kb/Work PB1606)");
            }
        }
        Assert.True(seen >= 6, $"only {seen} producer call(s) found: the scan is no longer recognizing them");
    }

    /// <summary>The activated unit's adapter for a PLAIN-GROUP formal (<c>groupAtoms</c> non-null) reads the argument's
    /// <c>Sending.StorageImage</c> when it states one, BY CONTENT only: BY REFERENCE has no MOVE (§14.2.3 GR8) and an
    /// elementary formal reads the characters (kb/Work PB3587).</summary>
    [Fact]
    public void GroupFormalAdapter_ReadsTheSendingStorageImage_ByContentOnly()
    {
        string src = Src("WiseOwl.Cobol.Runtime", "Control", "CallAbi.cs");
        var text = Regex.Match(src, @"public\s+static\s+ManagedPointer<string>\s+Text\s*\((?<b>.*?)\n    public\s+static", RegexOptions.Singleline);
        Assert.True(text.Success, "CobolArgAdapt.Text is no longer recognizable");
        Assert.Matches(@"groupAtoms\s+is\s+not\s+null\s*&&\s*args\[i\]\s+is\s*\{\s*Mode:\s*CobolPassMode\.Content,\s*Sending\.StorageImage:\s*\{\s*\}\s*\w+\s*\}",
            text.Groups["b"].Value);
    }

    /// <summary>An INVOKE literal crosses as the BOUND literal, never its decoded string: a national literal is usage
    /// NATIONAL (§8.3.3.5.4 GR3 a)), which only the literal's category states (kb/Work PB3587).</summary>
    [Fact]
    public void InvokeLiteralArgument_KeepsItsCategory()
    {
        string record = Src("WiseOwl.Cobol.Compiler", "Binding", "Bound", "BoundOo.cs");
        Assert.Matches(@"BoundStringLiteral\?\s+StringLiteral\b", record);
        string binder = Src("WiseOwl.Cobol.Compiler", "Binding", "Procedure", "Verbs", "OoBinder.cs");
        Assert.DoesNotMatch(@"new\s+BoundInvokeArg\([^;]*\.Value\s*,\s*WriteBack", binder);
        string emitter = Src("WiseOwl.Cobol.Compiler", "CodeGen", "Verbs", "OoEmitter.cs");
        Assert.DoesNotMatch(@"new\s+BoundStringLiteral\(\s*slit\s*\)", emitter);
    }
}
