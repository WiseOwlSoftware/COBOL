// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Runtime;
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.Collation.Locale;
using CobolNet.Runtime.Exceptions;
using CobolNet.Runtime.Globalization;
using Xunit;

namespace CobolNet.Tests.Unit.Collation;

/// <summary>
/// The LOCALE intrinsics' runtime (Runtime/Intrinsics/CobolLocale.cs + Runtime/Globalization/LocaleFacts.cs;
/// DESIGN-locale-facility §4.7/§4.8/§8; kb/Work PB64 T4): LOCALE-COMPARE IS the locale-based comparison (§15.51.4
/// r2–r5); LOCALE-DATE/-TIME/-TIME-FROM-SECONDS render per the locale's <c>d_fmt</c>/<c>t_fmt</c> (L10 — the
/// culture's short date / long time patterns) with §15.53.3 r3's own hour/second ranges, the fraction of a scaled
/// seconds argument, EC-ARGUMENT-FUNCTION on invalid arguments; the three locale conditions — EC-LOCALE-MISSING (a
/// named unavailable locale), EC-LOCALE-INVALID (an available locale without culture data — a site-tailored made-up
/// tag), and the gating (nothing raised with checking off; the documented stand-in answers). The golden
/// tests/conformance/2014/pb64t4_locale_functions proves the same through compiled COBOL.
/// </summary>
[Collection("site-tailoring-directory")]
public sealed class CobolLocaleTests
{
    [Fact]
    public void Compare_IsTheLocaleBasedRelation_WithTheSignMap()
    {
        RunUnit.Run(ru =>
        {
            Assert.Equal(">", CobolLocale.Compare("nz", "ñu", null));          // root: ñ = n + tilde, z > u
            Assert.Equal("<", CobolLocale.Compare("nz", "ñu", "es-ES"));       // Spanish: ñ a primary after n
            Assert.Equal("=", CobolLocale.Compare("ab", "ab   ", null));       // §15.51.4 r2 — trailing spaces
            Assert.Equal("=", CobolLocale.Compare("   ", " ", null));          // all spaces → one space
            Assert.Equal("<", CobolLocale.Compare("", "a", "fr-FR"));
            ru.Locale.SetFromLocale(LocaleCategorySet.Collate, "es-ES");        // the CURRENT locale (§14.6.6 r7)
            Assert.Equal("<", CobolLocale.Compare("nz", "ñu", null));
        });
    }

    [Fact]
    public void Date_RendersPerTheLocalesShortDatePattern_AndRejectsAnInvalidDate()
    {
        RunUnit.Run(ru =>
        {
            ru.Locale.Set(LocaleCategory.All, "");                                 // the root (the test host's own culture is not the subject)
            Assert.Equal("08/19/2026", CobolLocale.Date("20260819", null));     // the root: invariant d_fmt
            Assert.Equal(new DateTime(2026, 8, 19).ToString(CultureInfo.GetCultureInfo("fr-FR").DateTimeFormat.ShortDatePattern, CultureInfo.GetCultureInfo("fr-FR")),
                CobolLocale.Date("20260819", "fr-FR"));
            Assert.Equal("", CobolLocale.Date("20261399", null));               // EC-ARGUMENT-FUNCTION off: the §15.3 default
            Assert.Equal("", CobolLocale.Date("2026081", null));
            Assert.Equal("", CobolLocale.Date("16000101", null));               // year < 1601 — not a CURRENT-DATE value
            ExceptionState.ArgumentFunctionChecking = true;
            try
            {
                var ex = Assert.Throws<CobolFatalException>(() => CobolLocale.Date("20260230", null));
                Assert.Equal("EC-ARGUMENT-FUNCTION", ex.EcName);
            }
            finally { ExceptionState.ArgumentFunctionChecking = false; }
        });
    }

    [Fact]
    public void Time_RendersPerTheLocalesLongTimePattern_WithItsOwnRanges()
    {
        RunUnit.Run(ru =>
        {
            ru.Locale.Set(LocaleCategory.All, "");
            Assert.Equal("13:05:09", CobolLocale.Time("130509", null));
            Assert.Equal("24:00:00", CobolLocale.Time("240000", null));          // §15.53.3 r3a — hour 24 is legal
            Assert.Equal("23:59:99", CobolLocale.Time("235999", null));          // r3b — seconds to 99
            Assert.Equal("", CobolLocale.Time("250000", null));                  // hour 25 is not
            Assert.Equal("", CobolLocale.Time("126000", null));                  // minute 60 is not
            Assert.Equal("", CobolLocale.Time("12345", null));
            var us = CultureInfo.GetCultureInfo("en-US");
            if (us.DateTimeFormat.LongTimePattern.Contains('t'))
                Assert.EndsWith(us.DateTimeFormat.PMDesignator, CobolLocale.Time("130509", "en-US"));
        });
    }

    [Fact]
    public void TimeFromSeconds_CarriesTheFraction_AndScreensTheStandardForm()
    {
        RunUnit.Run(ru =>
        {
            ru.Locale.Set(LocaleCategory.All, "");
            Assert.Equal("13:05:09", CobolLocale.TimeFromSeconds(47109, 0, null));
            Assert.Equal("13:05:09.25", CobolLocale.TimeFromSeconds(4710925, 2, null));
            Assert.Equal("00:00:00", CobolLocale.TimeFromSeconds(0, 0, null));
            Assert.Equal("23:59:59", CobolLocale.TimeFromSeconds(86399, 0, null));
            Assert.Equal("", CobolLocale.TimeFromSeconds(86400, 0, null));       // §7.3.17.4 GR5 — not standard form
            Assert.Equal("24:00:00", CobolLocale.TimeFromSeconds(86400, 0, null, leapSecond: true));   // GR4 (LEAP-SECOND ON)
            Assert.Equal("", CobolLocale.TimeFromSeconds(-1, 0, null));
            var fr = CultureInfo.GetCultureInfo("fr-FR");
            Assert.Equal("13:05:09" + fr.NumberFormat.NumberDecimalSeparator + "250", CobolLocale.TimeFromSeconds(47109250, 3, "fr-FR"));
        });
    }

    [Fact]
    public void FormatTime_HandlesTheTokenVocabulary()
    {
        var facts = LocaleFacts.Root;
        // Tokens rendered against a hand-built pattern through the invariant culture's designators/separators.
        Assert.Equal("13:05:09", CobolLocale.FormatTime(facts, 13, 5, 9, null));
        var us = LocaleFacts.For("en-US");
        string rendered = CobolLocale.FormatTime(us, 0, 7, 3, null);
        Assert.Contains("12", rendered);                                          // h/hh: hour 0 renders as 12 on a 12-hour pattern
        Assert.Contains(us.DateTimeFormat.AMDesignator, rendered);
        Assert.True(us.TimeFormat.Length > 0 && us.DateFormat.Length > 0);
    }

    [Fact]
    public void NamedUnavailableLocale_IsMissing_AndTheRootAnswers()
    {
        RunUnit.Run(ru =>
        {
            ru.Locale.Set(LocaleCategory.All, "");
            Assert.Equal("08/19/2026", CobolLocale.Date("20260819", "xx-NOWHERE"));   // checking off: the root's answer
            Assert.Equal(">", CobolLocale.Compare("nz", "ñu", "xx-NOWHERE"));
            ExceptionState.LocaleMissingChecking = true;
            try
            {
                Assert.Equal("EC-LOCALE-MISSING", Assert.Throws<CobolFatalException>(() => CobolLocale.Date("20260819", "xx-NOWHERE")).EcName);
                Assert.Equal("EC-LOCALE-MISSING", Assert.Throws<CobolFatalException>(() => CobolLocale.Time("130509", "xx-NOWHERE")).EcName);
                Assert.Equal("EC-LOCALE-MISSING", Assert.Throws<CobolFatalException>(() => CobolLocale.TimeFromSeconds(1, 0, "xx-NOWHERE")).EcName);
                Assert.Equal("EC-LOCALE-MISSING", Assert.Throws<CobolFatalException>(() => CobolLocale.Compare("a", "b", "xx-NOWHERE")).EcName);
                Assert.Equal("<", CobolLocale.Compare("nz", "ñu", "es-ES"));    // an available one: no raise
            }
            finally { ExceptionState.LocaleMissingChecking = false; }
        });
    }

    /// <summary>EC-LOCALE-INVALID (§8.2.1 "invalid or incomplete"): a locale that IS available — a site tailoring
    /// makes "zz-QQ" known — but has no .NET culture data for LC_TIME; the invariant format stands when checking is
    /// off and the condition is raised when on. LC_COLLATE is unaffected (the tailoring collates).</summary>
    [Fact]
    public void AvailableLocaleWithoutCultureData_IsInvalid_ForTheTimeFunctions()
    {
        string dir = Path.Combine(Path.GetTempPath(), "cobolnet-locales-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string saved = Environment.GetEnvironmentVariable(LocaleConfig.TailoringDirectoryVariable) ?? "";
        try
        {
            File.WriteAllText(Path.Combine(dir, "zz-QQ.tailor"), "@locale zz-QQ\n");
            Environment.SetEnvironmentVariable(LocaleConfig.TailoringDirectoryVariable, dir);
            LocaleManager.ClearCache();
            LocaleFacts.ClearCache();
            Assert.True(LocaleIdentification.IsAvailable("zz-QQ"));
            var facts = LocaleFacts.For("zz-QQ");
            Assert.False(facts.HasCultureData);
            Assert.Equal(CultureInfo.InvariantCulture, facts.Culture);
            RunUnit.Run(ru =>
            {
                ru.Locale.Set(LocaleCategory.All, "");
                Assert.Equal("08/19/2026", CobolLocale.Date("20260819", "zz-QQ"));   // checking off: the invariant stand-in
                Assert.Equal(">", CobolLocale.Compare("nz", "ñu", "zz-QQ"));        // LC_COLLATE is the tailoring (root order)
                ExceptionState.LocaleInvalidChecking = true;
                try
                {
                    Assert.Equal("EC-LOCALE-INVALID", Assert.Throws<CobolFatalException>(() => CobolLocale.Date("20260819", "zz-QQ")).EcName);
                    Assert.Equal("EC-LOCALE-INVALID", Assert.Throws<CobolFatalException>(() => CobolLocale.Time("130509", "zz-QQ")).EcName);
                    Assert.Equal("08/19/2026", CobolLocale.Date("20260819", null));     // the root has its data
                    Assert.Equal("19/08/2026", CobolLocale.Date("20260819", "fr-FR"));  // so has France
                }
                finally { ExceptionState.LocaleInvalidChecking = false; }
            });
            Assert.True(LocaleFacts.For("fr-FR").HasCultureData);
            Assert.True(LocaleFacts.For("es_ES.UTF-8").HasCultureData);             // L1-normalized before lookup
            Assert.Equal("es-ES", LocaleFacts.For("es_ES.UTF-8").Culture.Name);
            Assert.True(LocaleFacts.For("de-u-co-phonebook").HasCultureData);        // the -u- extension is stripped for culture data
        }
        finally
        {
            Environment.SetEnvironmentVariable(LocaleConfig.TailoringDirectoryVariable, saved.Length == 0 ? null : saved);
            LocaleManager.ClearCache();
            LocaleFacts.ClearCache();
            Directory.Delete(dir, recursive: true);
        }
    }
}
