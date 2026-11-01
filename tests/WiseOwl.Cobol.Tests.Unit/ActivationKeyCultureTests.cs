// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.Globalization;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The activation-description keys are the same text on every compiling host (kb/Work PB3257). A universal INVOKE
/// compares a caller's description with the method's ordinally at run time (<see cref="ActivationRelations"/>), and the
/// two descriptions are built by two compilations, possibly on two hosts — so a key that formats a number with the
/// host culture keys one type apart from itself. The ISO relations they carry are §8.5.3.1's same type ("Two type
/// declarations are considered equivalent when they have the same type-name", with the elementary items' essential
/// characteristics) and §14.8.2.2's "both shall be of the same type" for a strongly-typed group argument.
/// <para>The culture is a clone of the invariant culture whose negative sign is U+2212 MINUS SIGN, which is what
/// sv-SE's is on .NET 10: deterministic on every host, ICU or not. Each item's PICTURE <c>99P</c> has a NEGATIVE scale
/// (the PICTURE scaling symbol P), the value that printed U+2212 in the record dump the strong-type key embedded, and
/// the absent currency string's <c>-1</c> did the same in every PICTURE clause key.</para>
/// </summary>
public sealed class ActivationKeyCultureTests
{
    private static readonly CultureInfo MinusSign = MinusSignCulture();

    private static CultureInfo MinusSignCulture()
    {
        var c = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        c.NumberFormat.NegativeSign = "−";
        return c;
    }

    private static DataItem Item(string picture)
    {
        var dollar = new Dictionary<char, CurrencyDefinition> { ['$'] = new("$", WiseOwl.Cobol.Common.LiteralClass.Alphanumeric) };
        var pic = PictureAnalyzer.Analyze(picture, Usage.Display, new EditionContext(2023), "data item 'P'",
            currencies: dollar, decimalPointIsComma: false);
        Assert.False(pic.IsRecovery, $"PIC {picture} did not analyze");
        return new DataItem { Level = 1, CobolName = "P", ClrName = "P", Pic = pic, PictureText = picture };
    }

    private static string Under(CultureInfo culture, Func<string> key)
    {
        var host = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = culture;
        try { return key(); }
        finally { CultureInfo.CurrentCulture = host; }
    }

    public static TheoryData<string> Keys => new() { "TypeIdentityKey", "ElementaryClauses", "PictureClauseKey" };

    private static string KeyOf(string which, DataItem item) => which switch
    {
        "TypeIdentityKey" => StrongTypeModel.TypeIdentityKey(item),
        "ElementaryClauses" => ActivationDescriptions.ElementaryClauses(item).Clauses,
        "PictureClauseKey" => item.Pic!.Clause!.Key,
        _ => throw new ArgumentOutOfRangeException(nameof(which), which, null),
    };

    [Theory]
    [MemberData(nameof(Keys))]
    public void Key_IsTheSameTextUnderAHostCultureWithAUnicodeMinusSign(string which)
    {
        var item = Item("99P");
        Assert.True(item.Pic!.Scale < 0, "the witness needs a negative scale: PIC 99P no longer analyzes to one");
        Assert.Equal("−" + "2", Under(MinusSign, () => (-2).ToString()));   // the culture does what the witness assumes

        string invariant = Under(CultureInfo.InvariantCulture, () => KeyOf(which, item));
        string minus = Under(MinusSign, () => KeyOf(which, item));

        Assert.Equal(invariant, minus);
        Assert.DoesNotContain('−', minus);
    }
}
