using NetJs;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Intrinsics;
using System.Text;
using System.Runtime.CompilerServices;

internal static partial class Interop
{

    [NetJs.External]
    class DateTimeOptions
    {
        public string calendar = default!;
        public string day = default!;
        public string locale = default!;
        public string month = default!;
        public string numberingSystem = default!;
        public string timeZone = default!;
        public string year = default!;
    }

    [NetJs.External]
    class Locale
    {
        public string baseName = default!;
        public string calendar = default!;
        public string caseFirst = default!;
        public string collation = default!;
        public string firstDayOfWeek = default!;
        public string hourCycle = default!;
        public string language = default!;
        public string numberingSystem = default!;
        public bool numeric;
        public string? region;
        public string script = default!;

        public extern int getWeekInfo();
        public extern Locale maximize();
    }
    [NetJs.External]
    enum LocaleNumberData : uint
    {
        /// <summary>language id (corresponds to LOCALE_ILANGUAGE)</summary>
        LanguageId = 0x00000001,
        /// <summary>geographical location id, (corresponds to LOCALE_IGEOID)</summary>
        GeoId = 0x0000005B,
        /// <summary>0 = context, 1 = none, 2 = national (corresponds to LOCALE_IDIGITSUBSTITUTION)</summary>
        DigitSubstitution = 0x00001014,
        /// <summary>0 = metric, 1 = US (corresponds to LOCALE_IMEASURE)</summary>
        MeasurementSystem = 0x0000000D,
        /// <summary>number of fractional digits (corresponds to LOCALE_IDIGITS)</summary>
        FractionalDigitsCount = 0x00000011,
        /// <summary>negative number mode (corresponds to LOCALE_INEGNUMBER)</summary>
        NegativeNumberFormat = 0x00001010,
        /// <summary># local monetary digits (corresponds to LOCALE_ICURRDIGITS)</summary>
        MonetaryFractionalDigitsCount = 0x00000019,
        /// <summary>positive currency mode (corresponds to LOCALE_ICURRENCY)</summary>
        PositiveMonetaryNumberFormat = 0x0000001B,
        /// <summary>negative currency mode (corresponds to LOCALE_INEGCURR)</summary>
        NegativeMonetaryNumberFormat = 0x0000001C,
        /// <summary>type of calendar specifier (corresponds to LOCALE_ICALENDARTYPE)</summary>
        CalendarType = 0x00001009,
        /// <summary>first day of week specifier (corresponds to LOCALE_IFIRSTDAYOFWEEK)</summary>
        FirstDayOfWeek = 0x0000100C,
        /// <summary>first week of year specifier (corresponds to LOCALE_IFIRSTWEEKOFYEAR)</summary>
        FirstWeekOfYear = 0x0000100D,
        /// <summary>
        /// Returns one of the following 4 reading layout values:
        ///  0 - Left to right (eg en-US)
        ///  1 - Right to left (eg arabic locales)
        ///  2 - Vertical top to bottom with columns to the left and also left to right (ja-JP locales)
        ///  3 - Vertical top to bottom with columns proceeding to the right
        /// (corresponds to LOCALE_IREADINGLAYOUT)
        /// </summary>
        ReadingLayout = 0x00000070,
        /// <summary>Returns 0-11 for the negative percent format (corresponds to LOCALE_INEGATIVEPERCENT)</summary>
        NegativePercentFormat = 0x00000074,
        /// <summary>Returns 0-3 for the positive percent format (corresponds to LOCALE_IPOSITIVEPERCENT)</summary>
        PositivePercentFormat = 0x00000075,
        /// <summary>default ansi code page (corresponds to LOCALE_IDEFAULTCODEPAGE)</summary>
        OemCodePage = 0x0000000B,
        /// <summary>default ansi code page (corresponds to LOCALE_IDEFAULTANSICODEPAGE)</summary>
        AnsiCodePage = 0x00001004,
        /// <summary>default mac code page (corresponds to LOCALE_IDEFAULTMACCODEPAGE)</summary>
        MacCodePage = 0x00001011,
        /// <summary>default ebcdic code page (corresponds to LOCALE_IDEFAULTEBCDICCODEPAGE)</summary>
        EbcdicCodePage = 0x00001012,
    }
    [NetJs.External]
    enum LocaleStringData : uint
    {
        /// <summary>localized name of locale, eg "German (Germany)" in UI language (corresponds to LOCALE_SLOCALIZEDDISPLAYNAME)</summary>
        LocalizedDisplayName = 0x00000002,
        /// <summary>Display name (language + country usually) in English, eg "German (Germany)" (corresponds to LOCALE_SENGLISHDISPLAYNAME)</summary>
        EnglishDisplayName = 0x00000072,
        /// <summary>Display name in native locale language, eg "Deutsch (Deutschland) (corresponds to LOCALE_SNATIVEDISPLAYNAME)</summary>
        NativeDisplayName = 0x00000073,
        /// <summary>Language Display Name for a language, eg "German" in UI language (corresponds to LOCALE_SLOCALIZEDLANGUAGENAME)</summary>
        LocalizedLanguageName = 0x0000006f,
        /// <summary>English name of language, eg "German" (corresponds to LOCALE_SENGLISHLANGUAGENAME)</summary>
        EnglishLanguageName = 0x00001001,
        /// <summary>native name of language, eg "Deutsch" (corresponds to LOCALE_SNATIVELANGUAGENAME)</summary>
        NativeLanguageName = 0x00000004,
        /// <summary>localized name of country, eg "Germany" in UI language (corresponds to LOCALE_SLOCALIZEDCOUNTRYNAME)</summary>
        LocalizedCountryName = 0x00000006,
        /// <summary>English name of country, eg "Germany" (corresponds to LOCALE_SENGLISHCOUNTRYNAME)</summary>
        EnglishCountryName = 0x00001002,
        /// <summary>native name of country, eg "Deutschland" (corresponds to LOCALE_SNATIVECOUNTRYNAME)</summary>
        NativeCountryName = 0x00000008,
        /// <summary>abbreviated language name (corresponds to LOCALE_SABBREVLANGNAME)</summary>
        AbbreviatedWindowsLanguageName = 0x00000003,
        /// <summary>list item separator (corresponds to LOCALE_SLIST)</summary>
        ListSeparator = 0x0000000C,
        /// <summary>decimal separator (corresponds to LOCALE_SDECIMAL)</summary>
        DecimalSeparator = 0x0000000E,
        /// <summary>thousand separator (corresponds to LOCALE_STHOUSAND)</summary>
        ThousandSeparator = 0x0000000F,
        /// <summary>native digits for 0-9, eg "0123456789" (corresponds to LOCALE_SNATIVEDIGITS)</summary>
        Digits = 0x00000013,
        /// <summary>local monetary symbol (corresponds to LOCALE_SCURRENCY)</summary>
        MonetarySymbol = 0x00000014,
        /// <summary>English currency name (corresponds to LOCALE_SENGCURRNAME)</summary>
        CurrencyEnglishName = 0x00001007,
        /// <summary>Native currency name (corresponds to LOCALE_SNATIVECURRNAME)</summary>
        CurrencyNativeName = 0x00001008,
        /// <summary>uintl monetary symbol (corresponds to LOCALE_SINTLSYMBOL)</summary>
        Iso4217MonetarySymbol = 0x00000015,
        /// <summary>monetary decimal separator (corresponds to LOCALE_SMONDECIMALSEP)</summary>
        MonetaryDecimalSeparator = 0x00000016,
        /// <summary>monetary thousand separator (corresponds to LOCALE_SMONTHOUSANDSEP)</summary>
        MonetaryThousandSeparator = 0x00000017,
        /// <summary>AM designator (corresponds to LOCALE_S1159)</summary>
        AMDesignator = 0x00000028,
        /// <summary>PM designator (corresponds to LOCALE_S2359)</summary>
        PMDesignator = 0x00000029,
        /// <summary>positive sign (corresponds to LOCALE_SPOSITIVESIGN)</summary>
        PositiveSign = 0x00000050,
        /// <summary>negative sign (corresponds to LOCALE_SNEGATIVESIGN)</summary>
        NegativeSign = 0x00000051,
        /// <summary>ISO abbreviated language name (corresponds to LOCALE_SISO639LANGNAME)</summary>
        Iso639LanguageTwoLetterName = 0x00000059,
        /// <summary>ISO abbreviated country name (corresponds to LOCALE_SISO639LANGNAME2)</summary>
        Iso639LanguageThreeLetterName = 0x00000067,
        /// <summary>ISO abbreviated language name (corresponds to LOCALE_SISO639LANGNAME)</summary>
        Iso639LanguageName = 0x00000059,
        /// <summary>ISO abbreviated country name (corresponds to LOCALE_SISO3166CTRYNAME)</summary>
        Iso3166CountryName = 0x0000005A,
        /// <summary>3 letter ISO country code (corresponds to LOCALE_SISO3166CTRYNAME2)</summary>
        Iso3166CountryName2 = 0x00000068,   // 3 character ISO country name
        /// <summary>Not a Number (corresponds to LOCALE_SNAN)</summary>
        NaNSymbol = 0x00000069,
        /// <summary>+ Infinity (corresponds to LOCALE_SPOSINFINITY)</summary>
        PositiveInfinitySymbol = 0x0000006a,
        /// <summary>- Infinity (corresponds to LOCALE_SNEGINFINITY)</summary>
        NegativeInfinitySymbol = 0x0000006b,
        /// <summary>Fallback name for resources (corresponds to LOCALE_SPARENT)</summary>
        ParentName = 0x0000006d,
        /// <summary>Fallback name for within the console (corresponds to LOCALE_SCONSOLEFALLBACKNAME)</summary>
        ConsoleFallbackName = 0x0000006e,
        /// <summary>Returns the percent symbol (corresponds to LOCALE_SPERCENT)</summary>
        PercentSymbol = 0x00000076,
        /// <summary>Returns the permille (U+2030) symbol (corresponds to LOCALE_SPERMILLE)</summary>
        PerMilleSymbol = 0x00000077
    }
    internal static partial class Globalization
    {
        internal static partial int LoadICU()
        {
            return 1;
        }

        internal static partial void InitICUFunctions(IntPtr icuuc, IntPtr icuin, string version, string? suffix)
        {

        }

        internal static partial int GetICUVersion()
        {
            return 0;
        }

        static string? NormalizationFormToString(NormalizationForm form)
        {
            return form switch
            {
                NormalizationForm.FormC => "NFC",
                NormalizationForm.FormD => "NFD",
                NormalizationForm.FormKC => "NFKC",
                NormalizationForm.FormKD => "NFKD",
                _ => null,
            };
        }

        internal static unsafe partial int IsNormalized(NormalizationForm normalizationForm, char* src, int srcLen)
        {
            var span = new Span<char>(src, srcLen);
            var str = span.ToString();
            var formStr = NormalizationFormToString(normalizationForm);
            var normalized = Script.Write<string>("str.normalize(formStr)");
            return normalized == formStr ? 1 : 0;
        }

        internal static unsafe partial int NormalizeString(NormalizationForm normalizationForm, char* src, int srcLen, char* dstBuffer, int dstBufferCapacity)
        {
            var span = new Span<char>(src, srcLen);
            var str = span.ToString();
            var formStr = NormalizationFormToString(normalizationForm);
            var normalized = Script.Write<string>("str.normalize(formStr)");
            var dst = new Span<char>(dstBuffer, dstBufferCapacity);
            normalized.CopyTo(dst);
            return normalized.Length;
        }
        //static DateTimeOptions? dateTimeOptions;
        internal static unsafe partial bool GetLocaleName(string localeName, char* value, int valueLength)
        {
            var dateTimeOptions = NetJs.Script.Write<DateTimeOptions>("Intl.DateTimeFormat(localeName).resolvedOptions()");
            var reff = Unsafe.AsPointer(in dateTimeOptions.locale.GetPinnableReference());
            Unsafe.CopyBlockFinal(value, reff, dateTimeOptions.locale.Length.As<nuint>() * 2);
            return true;
        }


        internal static partial bool GetLocaleInfoInt(string localeName, uint localeNumberData, ref int value)
        {
            var locale = NetJs.Script.Write<Locale>("new Intl.Locale(localeName).maximize()");

            switch ((LocaleNumberData)localeNumberData)
            {
                case LocaleNumberData.FirstDayOfWeek:
                    // JS: 1 (Mon) - 7 (Sun)
                    // .NET/Windows LOCALE_IFIRSTDAYOFWEEK: 0 (Mon) - 6 (Sun)
                    int jsFirstDay = NetJs.Script.Write<int>("locale.getWeekInfo().firstDay");
                    value = jsFirstDay - 1;
                    return true;

                case LocaleNumberData.FirstWeekOfYear:
                    // JS minimalDays: 1, 4. 
                    // .NET: 0 (FirstDay), 1 (FirstFullWeek), 2 (FirstFourDayWeek)
                    int minDays = NetJs.Script.Write<int>("locale.getWeekInfo().minimalDays ?? 0");
                    value = minDays >= 4 ? 2 : 0;
                    return true;

                case LocaleNumberData.MeasurementSystem:
                    // 0 = Metric, 1 = US
                    string region = locale.region;
                    value = (region == "US" || region == "LR" || region == "MM") ? 1 : 0;
                    return true;

                case LocaleNumberData.FractionalDigitsCount:
                    value = NetJs.Script.Write<int>("new Intl.NumberFormat(locale.baseName).resolvedOptions().maximumFractionDigits");
                    return true;

                case LocaleNumberData.MonetaryFractionalDigitsCount:
                    value = NetJs.Script.Write<int>("new Intl.NumberFormat(locale.baseName, {style:'currency', currency:'USD'}).resolvedOptions().maximumFractionDigits");
                    return true;

                case LocaleNumberData.ReadingLayout:
                    // 0: LTR, 1: RTL
                    string dir = NetJs.Script.Write<string>("locale.textInfo ? locale.textInfo.direction : (/^(ar|he|fa|ur)/.test(locale.baseName) ? 'rtl' : 'ltr')");
                    value = (dir == "rtl") ? 1 : 0;
                    return true;

                case LocaleNumberData.DigitSubstitution:
                    // 1 = None (Standard ASCII digits), 2 = National
                    string numberingSystem = locale.numberingSystem;
                    value = (numberingSystem == "latn" || numberingSystem == null) ? 1 : 2;
                    return true;

                case LocaleNumberData.CalendarType:
                    // 1 = Gregorian. Intl.Locale.calendar usually returns "gregory"
                    string cal = locale.calendar;
                    value = (cal == "gregory" || cal == null) ? 1 : 0; // 0 allows .NET fallback for specialized calendars
                    return true;

                case LocaleNumberData.NegativeNumberFormat:
                    // .NET/Windows patterns (0: (1.1), 1: -1.1, 2: - 1.1, 3: 1.1-, 4: 1.1 -)
                    // Hardcoding 1 (standard) as Intl doesn't expose the pattern index directly
                    value = 1;
                    return true;

                case LocaleNumberData.PositiveMonetaryNumberFormat:
                    // 0: $1, 1: 1$, 2: $ 1, 3: 1 $
                    value = NetJs.Script.Write<int>(@"
                                (function(){
                                    const parts = new Intl.NumberFormat(locale.baseName, {style:'currency', currency:'USD'}).formatToParts(1);
                                    return parts[0].type === 'currency' ? 0 : 1;
                                })()");
                    return true;

                case LocaleNumberData.AnsiCodePage:
                case LocaleNumberData.OemCodePage:
                case LocaleNumberData.MacCodePage:
                    value = 65001; // Default to UTF-8
                    return true;

                case LocaleNumberData.LanguageId:
                case LocaleNumberData.GeoId:
                    // No direct mapping in JS. Return false to allow .NET to resolve from locale name string.
                    return false;

                default:
                    return false;
            }
            //switch (localeNumberData)
            //{
            //    case 4108: // IFIRSTDAYOFWEEK
            //               // JS getWeekInfo().firstDay: 1 (Mon) ... 7 (Sun)
            //               // .NET DayOfWeek: 0 (Sun) ... 6 (Sat)
            //        int jsFirstDay = NetJs.Script.Write<int>("new Intl.Locale(localeName).getWeekInfo().firstDay");

            //        // Map 7 (JS Sunday) to 0 (.NET Sunday), otherwise keep value (1-6)
            //        value = (jsFirstDay == 7) ? 0 : jsFirstDay;
            //        return true;

            //    case 4109: // IFIRSTWEEKOFYEAR
            //               // JS minimalDays: 1 (SBN), 4 (ISO)
            //               // .NET: 0 (FirstDay), 1 (FirstFullWeek), 2 (FirstFourDayWeek)
            //        int minDays = NetJs.Script.Write<int>("new Intl.Locale(localeName).getWeekInfo().minimalDays");
            //        value = minDays >= 4 ? 2 : 0;
            //        return true;

            //    case 0x00000009: // ILANGUAGE (LCID-like identifier)
            //                     // Usually not supported well in JS, return false for fallback
            //        return false;
            //}
            //return false; // Return false instead of throwing to allow .NET fallback

            //switch (localeNumberData)
            //{
            //    case 4108: //First day of week
            //        var locale = NetJs.Script.Write<Locale>("new Intl.Locale(localeName)");
            //        value = locale.getWeekInfo();
            //        return true;
            //        break;
            //}
            //throw null;
        }


        //static Locale? locale;
        internal static unsafe partial bool GetLocaleInfoString(string localeName, uint localeStringData, char* value, int valueLength, string? uiLocaleName = null)
        {
            LocaleStringData localeData = (LocaleStringData)localeStringData;
            var locale = NetJs.Script.Write<Locale>("new Intl.Locale(localeName)");
            string? result = null;
            // Helper to extract parts from NumberFormat
            string getNFPart(string type, object options)
            {
                return NetJs.Script.Write<string>("new Intl.NumberFormat(locale.baseName, options).formatToParts(1.1).find(p => p.type === type)?.value");
            }

            //// Helper to get NumberFormat properties
            //string getNFProp(string type, object options)
            //{
            //    return NetJs.Script.Write<string>("new Intl.NumberFormat(localeName, options).formatToParts(1.1).find(p => p.type === type).value");
            //}

            //// Helper to transform Intl parts into a .NET format string
            //string getDynamicPattern(string style, bool isTime)
            //{
            //    object options = isTime ? new { timeStyle = style } : new { dateStyle = style };
            //    // Use a date that has distinct values (Month > 9, Day > 9) to detect padding
            //    var parts = NetJs.Script.Write<object[]>("new Intl.DateTimeFormat(localeName, options).formatToParts(new Date(2024, 11, 25, 13, 45, 59))");
            //    string pattern = "";

            //    foreach (var part in parts)
            //    {
            //        string type = part["type"].As<string>();
            //        string value = part["value"].As<string>();

            //        switch (type)
            //        {
            //            case "day": pattern += (value.Length > 1) ? "dd" : "d"; break;
            //            case "month": pattern += (value.Length > 2) ? "MMMM" : (value.Length > 1 ? "MM" : "M"); break;
            //            case "year": pattern += (value.Length > 2) ? "yyyy" : "yy"; break;
            //            case "weekday": pattern += (value.Length > 3) ? "dddd" : "ddd"; break;
            //            case "hour": pattern += (value.Length > 1) ? "HH" : "H"; break;
            //            case "minute": pattern += "mm"; break;
            //            case "second": pattern += "ss"; break;
            //            case "dayPeriod": pattern += "tt"; break;
            //            //case "literal": pattern += "'" + value + "'"; break; // Escape literals
            //            default: pattern += value; break;
            //        }
            //    }
            //    return pattern.Replace("''", ""); // Cleanup literal escapes
            //}

            switch ((LocaleStringData)localeStringData)
            {
                case LocaleStringData.LocalizedDisplayName:
                    result = NetJs.Script.Write<string>("new Intl.DisplayNames([uiLocaleName || 'en'], {type: 'language'}).of(locale.baseName)");
                    break;
                case LocaleStringData.EnglishDisplayName:
                    result = NetJs.Script.Write<string>("new Intl.DisplayNames(['en'], {type: 'language'}).of(locale.baseName)");
                    break;
                case LocaleStringData.NativeDisplayName:
                    result = NetJs.Script.Write<string>("new Intl.DisplayNames([locale.baseName], {type: 'language'}).of(locale.baseName)");
                    break;
                case LocaleStringData.LocalizedLanguageName:
                    result = NetJs.Script.Write<string>("new Intl.DisplayNames([uiLocaleName || 'en'], {type: 'language'}).of(locale.language)");
                    break;
                case LocaleStringData.EnglishLanguageName:
                    result = NetJs.Script.Write<string>("new Intl.DisplayNames(['en'], {type: 'language'}).of(locale.language)");
                    break;
                case LocaleStringData.NativeLanguageName:
                    result = NetJs.Script.Write<string>("new Intl.DisplayNames([locale.baseName], {type: 'language'}).of(locale.language)");
                    break;
                case LocaleStringData.LocalizedCountryName:
                    result = NetJs.Script.Write<string>("new Intl.DisplayNames([uiLocaleName || 'en'], {type: 'region'}).of(locale.region)");
                    break;
                case LocaleStringData.EnglishCountryName:
                    result = NetJs.Script.Write<string>("new Intl.DisplayNames(['en'], {type: 'region'}).of(locale.region)");
                    break;
                case LocaleStringData.NativeCountryName:
                    result = NetJs.Script.Write<string>("new Intl.DisplayNames([locale.baseName], {type: 'region'}).of(locale.region)");
                    break;
                case LocaleStringData.AbbreviatedWindowsLanguageName:
                    // .NET/Windows uses 3-letter codes (e.g., "ENU"). JS only provides 2-letter.
                    result = locale.language.ToUpper();
                    break;
                case LocaleStringData.ListSeparator:
                    result = (locale.region == "US") ? "," : ";";
                    break;
                case LocaleStringData.DecimalSeparator:
                    result = getNFPart("decimal", new { style = "decimal" });
                    break;
                case LocaleStringData.ThousandSeparator:
                    result = getNFPart("group", new { style = "decimal" });
                    break;
                case LocaleStringData.Digits:
                    result = "0123456789";
                    break;
                case LocaleStringData.MonetarySymbol:
                    result = getNFPart("currency", new { style = "currency", currency = "USD" });
                    break;
                case LocaleStringData.Iso4217MonetarySymbol:
                    result = NetJs.Script.Write<string>("new Intl.NumberFormat(locale.baseName, {style:'currency', currency:'USD'}).resolvedOptions().currency");
                    break;
                case LocaleStringData.MonetaryDecimalSeparator:
                    result = getNFPart("decimal", new { style = "currency", currency = "USD" });
                    break;
                case LocaleStringData.MonetaryThousandSeparator:
                    result = getNFPart("group", new { style = "currency", currency = "USD" });
                    break;
                case LocaleStringData.AMDesignator:
                    result = NetJs.Script.Write<string>("new Intl.DateTimeFormat(locale.baseName, {hour:'numeric', hour12:true}).formatToParts(new Date(2024,0,1,9)).find(p => p.type === 'dayPeriod')?.value");
                    break;
                case LocaleStringData.PMDesignator:
                    result = NetJs.Script.Write<string>("new Intl.DateTimeFormat(locale.baseName, {hour:'numeric', hour12:true}).formatToParts(new Date(2024,0,1,21)).find(p => p.type === 'dayPeriod')?.value");
                    break;
                case LocaleStringData.PositiveSign:
                    result = ""; // .NET usually expects empty string for positive sign
                    break;
                case LocaleStringData.NegativeSign:
                    result = "-";
                    break;
                case LocaleStringData.Iso639LanguageName: // Matches 0x59
                                                          //case LocaleStringData.Iso639LanguageTwoLetterName:
                    result = locale.language ?? "";
                    break;
                case LocaleStringData.Iso3166CountryName:
                    result = locale.region ?? "";
                    break;
                case LocaleStringData.ParentName:
                    int idx = locale.baseName.LastIndexOf('-');
                    result = idx > 0 ? locale.baseName.Substring(0, idx) : "";
                    break;
                case LocaleStringData.PercentSymbol:
                    result = getNFPart("percentSign", new { style = "percent" }) ?? "%";
                    break;
                case LocaleStringData.PerMilleSymbol:
                    result = "\u2030";
                    break;
                case LocaleStringData.NaNSymbol:
                    result = NetJs.Script.Write<string>("new Intl.NumberFormat(locale.baseName).format(NaN)");
                    break;
                case LocaleStringData.PositiveInfinitySymbol:
                    result = NetJs.Script.Write<string>("new Intl.NumberFormat(locale.baseName).format(Infinity)");
                    break;
                case LocaleStringData.NegativeInfinitySymbol:
                    result = NetJs.Script.Write<string>("new Intl.NumberFormat(locale.baseName).format(-Infinity)");
                    break;
                default:
                    return false;
            }
            if (result != null)
            {
                fixed (char* rPtr = result)
                {
                    int lengthToCopy = Math.Min(result.Length, valueLength);
                    Unsafe.CopyBlockFinal(value, rPtr, (nuint)(lengthToCopy * sizeof(char)));
                    return true;
                }
            }
            return false;
            ////CultureData.LocaleStringData data= localeStringData;
            //var locale = NetJs.Script.Write<Locale>("new Intl.Locale(localeName)");
            //switch (localeStringData)
            //{
            //    case 90:
            //        {
            //            var reff = Unsafe.AsPointer(in locale.region.GetPinnableReference());
            //            Unsafe.CopyBlockFinal(value, reff, locale.region.Length.As<nuint>() * 2);
            //            return true;
            //        }
            //    case 109: //Parent Name
            //        {
            //            var reff2 = Unsafe.AsPointer(in "".GetPinnableReference());
            //            Unsafe.CopyBlockFinal(value, reff2, 0);
            //            return true;
            //        }
            //}
            //throw null;
        }

        internal static unsafe partial bool GetDefaultLocaleName(char* value, int valueLength)
        {
            var options = NetJs.Script.Write<DateTimeOptions>("new Intl.DateTimeFormat().resolvedOptions()");
            var reff = Unsafe.AsPointer(in options.locale.GetPinnableReference());
            Unsafe.CopyBlockFinal(value, reff, options.locale.Length.As<nuint>() * 2);
            return true;
        }

        internal static partial bool IsPredefinedLocale(string localeName)
        {
            try
            {
                NetJs.Script.Write<DateTimeOptions>("Intl.getCanonicalLocales(localeName)");
                return true; // The locale is structurally valid and recognized
            }
            catch
            {
                return false; // The locale is invalid
            }
        }

        internal static unsafe partial bool GetLocaleTimeFormat(string localeName, bool shortFormat, char* value, int valueLength)
        {
            // Use Intl.DateTimeFormat with timeStyle to get the localized parts
            string? result = NetJs.Script.Write<string>(@"
                        (function() {
                            const timeStyle = shortFormat ? 'short' : 'medium';
                            const formatter = new Intl.DateTimeFormat(localeName, { timeStyle });
                            // Use a fixed time (13:45:59) to detect 12/24 hour and zero-padding
                            const date = new Date(2024, 0, 1, 13, 45, 59);
                            const parts = formatter.formatToParts(date);
                            const is12Hour = parts.some(p => p.type === 'dayPeriod');
            
                            let pattern = '';
                            for (const p of parts) {
                                switch (p.type) {
                                    case 'hour':
                                        // .NET: h/hh (12h), H/HH (24h)
                                        const h = p.value;
                                        if (is12Hour) {
                                            pattern += (h.length > 1) ? 'hh' : 'h';
                                        } else {
                                            pattern += (h.length > 1) ? 'HH' : 'H';
                                        }
                                        break;
                                    case 'minute':
                                        pattern += 'mm';
                                        break;
                                    case 'second':
                                        pattern += 'ss';
                                        break;
                                    case 'dayPeriod':
                                        pattern += 'tt';
                                        break;
                                    case 'literal':
                                        // Escape literals with single quotes for .NET
                                        pattern += (p.value === ' ') ? ' ' : ""'"" + p.value + ""'"";
                                        break;
                                    default:
                                        pattern += p.value;
                                }
                            }
                            return pattern.replace(/''/g, ''); // Cleanup adjacent escapes
                        })()");

            if (result != null)
            {
                fixed (char* rPtr = result)
                {
                    int lengthToCopy = Math.Min(result.Length, valueLength);
                    Unsafe.CopyBlockFinal(value, rPtr, (nuint)(lengthToCopy * sizeof(char)));
                    return true;
                }
            }

            return false;
        }

        internal static partial bool GetLocaleInfoGroupingSizes(string localeName, uint localeGroupingData, ref int primaryGroupSize, ref int secondaryGroupSize)
        {
            // Use a number large enough to trigger both primary and secondary grouping (100,000,000)
            // 100,000,000 allows us to see if the gap between 100 and 000 is different from 000 and 000
            string sizes = NetJs.Script.Write<string>(@"
                        (function() {
                            const nf = new Intl.NumberFormat(localeName, { useGrouping: true });
                            const parts = nf.formatToParts(100000000).filter(p => p.type === 'integer' || p.type === 'group');
            
                            // Reverse to process from right to left (primary group is the first encountered)
                            const reversedParts = parts.reverse();
                            let primary = 0;
                            let secondary = 0;
                            let groupCount = 0;

                            for (let i = 0; i < reversedParts.length; i++) {
                                if (reversedParts[i].type === 'integer') {
                                    if (groupCount === 0) {
                                        primary = reversedParts[i].value.length;
                                    } else if (groupCount === 1) {
                                        secondary = reversedParts[i].value.length;
                                    }
                                } else if (reversedParts[i].type === 'group') {
                                    groupCount++;
                                }
                            }

                            // .NET/Windows convention: If secondary is same as primary, secondary is often reported as 0 
                            // (meaning 'use primary for all subsequent groups')
                            if (secondary === primary || secondary === 0) {
                                return primary + ';0';
                            }
                            return primary + ';' + secondary;
                        })()");

            if (!string.IsNullOrEmpty(sizes))
            {
                var parts = sizes.Split(';');
                primaryGroupSize = int.Parse(parts[0]);
                secondaryGroupSize = int.Parse(parts[1]);
                return true;
            }

            return false;
        }

        internal static partial int GetLocales(char[]? value, int valueLength)
        {
            // Retrieve the user's preferred locales as a semicolon or null-delimited string
            string locales = NetJs.Script.Write<string>(@"
                        (function() {
                            // navigator.languages returns the user's preference list (e.g., ['en-US', 'fr-FR'])
                            const list = (typeof navigator !== 'undefined' && navigator.languages) 
                                ? navigator.languages 
                                : [Intl.DateTimeFormat().resolvedOptions().locale];
            
                            // Standard .NET/Native buffers often expect null-separated or single-null-terminated strings.
                            // We'll return them joined by a null character to match enumeration patterns.
                            return list.join('\0');
                        })()");

            if (value == null || valueLength == 0)
            {
                // Return the required length if no buffer is provided
                return locales.Length;
            }

            // Copy the joined string into the provided native buffer
            int lengthToCopy = Math.Min(locales.Length, valueLength);
            for (int i = 0; i < lengthToCopy; i++)
                value[i] = locales[i];
            // Return the actual number of characters written
            return lengthToCopy;
        }

        internal static partial int ToAscii(uint flags, ReadOnlySpan<char> src, int srcLen, Span<char> dstBuffer, int dstBufferCapacity)
        {
            int i = 0;
            for (i = 0; i < srcLen && i < dstBufferCapacity; i++)
            {
                if (src[i] <= 127)
                    dstBuffer[i] = src[i];
                else
                    dstBuffer[i] = '\0'; //TODO: Transliterating Unicode to ASCII to limit loss of information
            }
            return i;
        }

        internal static partial int ToUnicode(uint flags, ReadOnlySpan<char> src, int srcLen, Span<char> dstBuffer, int dstBufferCapacity)
        {
            int i = 0;
            for (i = 0; i < srcLen && i < dstBufferCapacity; i++)
            {
                dstBuffer[i] = src[i];
            }
            return i;
        }

        [Template("String.fromCharCode({c}).toUpperCase().split('')[0].charCodeAt(0)")]
        static extern char u_toupper(char c);
        [Template("String.fromCharCode({c}).toLowerCase().split('')[0].charCodeAt(0)")]
        static extern char u_tolower(char c);
        [Template("String.fromCharCode( ...{c} ).toUpperCase().split('').map(e => e.charCodeAt(0))")]
        static extern char[] u_toupper(char[] c);
        [Template("String.fromCharCode( ...{c} ).toLowerCase().split('').map(e => e.charCodeAt(0))")]
        static extern char[] u_tolower(char[] c);

        internal static unsafe partial void ChangeCase(char* lpSrc, int cwSrcLength, char* lpDst, int cwDstLength, bool bToUpper)
        {
            var srcPointer = Script.Ref(lpSrc);
            var srcArray = srcPointer.ToArray();
            var cased = bToUpper ? u_toupper(srcArray) : u_tolower(srcArray);
            for (int i = 0; i < cwSrcLength && i < cwDstLength; i++)
            {
                lpDst[i] = cased[i];
            }
            //// Iterate through the string, decoding the next one or two UTF-16 code units
            //// into a codepoint and updating srcIdx to point to the next UTF-16 code unit
            //// to decode.  Then upper or lower case it, write dstCodepoint into lpDst at
            //// offset dstIdx, and update dstIdx.

            //// (The loop here has been manually cloned for each of the four cases, rather
            //// than having a single loop that internally branched based on bToUpper as the
            //// compiler wasn't doing that optimization, and it results in an ~15-20% perf
            //// improvement on longer strings.)

            ////bool isError = false;
            //int srcIdx = 0, dstIdx = 0;
            //char srcCodepoint, dstCodepoint;

            //if (bToUpper)
            //{
            //    while (srcIdx < cwSrcLength)
            //    {
            //        //U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
            //        srcCodepoint = lpSrc[srcIdx];
            //        //dstCodepoint = u_toupper(srcCodepoint);
            //        dstCodepoint = Script.Write<char>("String.fromcharCode(srcCodepoint).toUpper()");
            //        //U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
            //        lpDst[dstIdx] = dstCodepoint;
            //        //assert(isError == false && srcIdx == dstIdx);
            //srcIdx++;
            //dstIdx++;
            //    }
            //}
            //else
            //{
            //    while (srcIdx < cwSrcLength)
            //    {
            //        //U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
            //        srcCodepoint = lpSrc[srcIdx];
            //        //dstCodepoint = u_tolower(srcCodepoint);
            //        //dstCodepoint = u_toupper(srcCodepoint);
            //        dstCodepoint = Script.Write<char>("String.fromcharCode(srcCodepoint).toLower()");
            //        //U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
            //        lpDst[dstIdx] = dstCodepoint;
            //        //assert(isError == false && srcIdx == dstIdx);
            //srcIdx++;
            //dstIdx++;
            //    }
            //}
        }

        internal static unsafe partial void ChangeCaseInvariant(char* lpSrc, int cwSrcLength, char* lpDst, int cwDstLength, bool bToUpper)
        {
            // See algorithmic comment in ChangeCase.

            //bool isError = false;
            //(void)isError; // only used for assert
            int srcIdx = 0, dstIdx = 0;
            char srcCodepoint, dstCodepoint;

            if (bToUpper)
            {
                while (srcIdx < cwSrcLength)
                {
                    // On Windows with InvariantCulture, the LATIN SMALL LETTER DOTLESS I (U+0131)
                    // capitalizes to itself, whereas with ICU it capitalizes to LATIN CAPITAL LETTER I (U+0049).
                    // We special case it to match the Windows invariant behavior.
                    //U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
                    srcCodepoint = lpSrc[srcIdx];
                    dstCodepoint = ((srcCodepoint == (char)0x0131) ? (char)0x0131 : u_toupper(srcCodepoint));
                    //U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
                    lpDst[dstIdx] = dstCodepoint;
                    //assert(isError == false && srcIdx == dstIdx);

                    srcIdx++;
                    dstIdx++;
                }
            }
            else
            {
                while (srcIdx < cwSrcLength)
                {
                    // On Windows with InvariantCulture, the LATIN CAPITAL LETTER I WITH DOT ABOVE (U+0130)
                    // lower cases to itself, whereas with ICU it lower cases to LATIN SMALL LETTER I (U+0069).
                    // We special case it to match the Windows invariant behavior.
                    //U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
                    srcCodepoint = lpSrc[srcIdx];
                    dstCodepoint = ((srcCodepoint == (char)0x0130) ? (char)0x0130 : u_tolower(srcCodepoint));
                    //U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
                    lpDst[dstIdx] = dstCodepoint;
                    //assert(isError == false && srcIdx == dstIdx);
                    srcIdx++;
                    dstIdx++;
                }
            }
        }

        internal static unsafe partial void ChangeCaseTurkish(char* lpSrc, int cwSrcLength, char* lpDst, int cwDstLength, bool bToUpper)
        {
            // See algorithmic comment in ChangeCase.

            //bool isError = false;
            //(void)isError; // only used for assert
            int srcIdx = 0, dstIdx = 0;
            char srcCodepoint, dstCodepoint;

            if (bToUpper)
            {
                while (srcIdx < cwSrcLength)
                {
                    // In turkish casing, LATIN SMALL LETTER I (U+0069) upper cases to LATIN
                    // CAPITAL LETTER I WITH DOT ABOVE (U+0130).
                    //U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
                    srcCodepoint = lpSrc[srcIdx];
                    dstCodepoint = ((srcCodepoint == (char)0x0069) ? (char)0x0130 : u_toupper(srcCodepoint));
                    //U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
                    lpDst[dstIdx] = dstCodepoint;
                    //assert(isError == false && srcIdx == dstIdx);

                    srcIdx++;
                    dstIdx++;
                }
            }
            else
            {
                while (srcIdx < cwSrcLength)
                {
                    // In turkish casing, LATIN CAPITAL LETTER I (U+0049) lower cases to
                    // LATIN SMALL LETTER DOTLESS I (U+0131).
                    //U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
                    srcCodepoint = lpSrc[srcIdx];
                    dstCodepoint = ((srcCodepoint == (char)0x0049) ? (char)0x0131 : u_tolower(srcCodepoint));
                    //U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
                    lpDst[dstIdx] = dstCodepoint;
                    //assert(isError == false && srcIdx == dstIdx);

                    srcIdx++;
                    dstIdx++;
                }
            }
        }

        internal static unsafe partial void InitOrdinalCasingPage(int pageNumber, char* pTarget)
        {
            pageNumber <<= 8;
            for (int i = 0; i < 256; i++)
            {
                // Unfortunately, to ensure one-to-one simple mapping we have to call u_toupper on every character.
                // Using string casing ICU APIs cannot give such results even when using NULL locale to force root behavior.
                pTarget[i] = u_toupper((char)(pageNumber + i));
            }

            if (pageNumber == 0x0100)
            {
                // Disable Turkish I behavior on Ordinal operations
                pTarget[0x31] = (char)0x0131;  // Turkish lowercase i
                pTarget[0x7F] = (char)0x017F;  // // 017F;LATIN SMALL LETTER LONG S
            }
        }


        const string GREGORIAN_NAME = "gregorian";
        const string JAPANESE_NAME = "japanese";
        const string BUDDHIST_NAME = "buddhist";
        const string HEBREW_NAME = "hebrew";
        const string DANGI_NAME = "dangi";
        const string PERSIAN_NAME = "persian";
        const string ISLAMIC_NAME = "islamic";
        const string ISLAMIC_UMALQURA_NAME = "islamic-umalqura";
        const string ROC_NAME = "roc";

        static string GetCalendarName(CalendarId calendarId)
        {
            switch (calendarId)
            {
                case CalendarId.JAPAN:
                    return JAPANESE_NAME;
                case CalendarId.THAI:
                    return BUDDHIST_NAME;
                case CalendarId.HEBREW:
                    return HEBREW_NAME;
                case CalendarId.KOREA:
                    return DANGI_NAME;
                case CalendarId.PERSIAN:
                    return PERSIAN_NAME;
                case CalendarId.HIJRI:
                    return ISLAMIC_NAME;
                case CalendarId.UMALQURA:
                    return ISLAMIC_UMALQURA_NAME;
                case CalendarId.TAIWAN:
                    return ROC_NAME;
                case CalendarId.GREGORIAN:
                case CalendarId.GREGORIAN_US:
                case CalendarId.GREGORIAN_ARABIC:
                case CalendarId.GREGORIAN_ME_FRENCH:
                case CalendarId.GREGORIAN_XLIT_ENGLISH:
                case CalendarId.GREGORIAN_XLIT_FRENCH:
                case CalendarId.JULIAN:
                case CalendarId.LUNAR_ETO_CHN:
                case CalendarId.LUNAR_ETO_KOR:
                case CalendarId.LUNAR_ETO_ROKUYOU:
                case CalendarId.SAKA:
                // don't support the lunisolar calendars until we have a solid understanding
                // of how they map to the ICU/CLDR calendars
                case CalendarId.CHINESELUNISOLAR:
                case CalendarId.KOREANLUNISOLAR:
                case CalendarId.JAPANESELUNISOLAR:
                case CalendarId.TAIWANLUNISOLAR:
                default:
                    return GREGORIAN_NAME;
            }
        }


        static CalendarId GetCalendarId(string calendarName)
        {
            if (calendarName.Equals(GREGORIAN_NAME, StringComparison.InvariantCultureIgnoreCase))
                // TODO: what about the other gregorian types?
                return CalendarId.GREGORIAN;
            else if (calendarName.Equals(JAPANESE_NAME, StringComparison.InvariantCultureIgnoreCase))
                return CalendarId.JAPAN;
            else if (calendarName.Equals(BUDDHIST_NAME, StringComparison.InvariantCultureIgnoreCase))
                return CalendarId.THAI;
            else if (calendarName.Equals(HEBREW_NAME, StringComparison.InvariantCultureIgnoreCase))
                return CalendarId.HEBREW;
            else if (calendarName.Equals(DANGI_NAME, StringComparison.InvariantCultureIgnoreCase))
                return CalendarId.KOREA;
            else if (calendarName.Equals(PERSIAN_NAME, StringComparison.InvariantCultureIgnoreCase))
                return CalendarId.PERSIAN;
            else if (calendarName.Equals(ISLAMIC_NAME, StringComparison.InvariantCultureIgnoreCase))
                return CalendarId.HIJRI;
            else if (calendarName.Equals(ISLAMIC_UMALQURA_NAME, StringComparison.InvariantCultureIgnoreCase))
                return CalendarId.UMALQURA;
            else if (calendarName.Equals(ROC_NAME, StringComparison.InvariantCultureIgnoreCase))
                return CalendarId.TAIWAN;
            else
                return CalendarId.UNINITIALIZED_VALUE;
        }

        internal static partial int GetCalendars(string localeName, CalendarId[] calendars, int calendarsCapacity)
        {
            return 0;
        }

        internal static unsafe partial ResultCode GetCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType calendarDataType, char* result, int resultCapacity)
        {
            return ResultCode.UnknownError;
        }

        // We skip the following DllImport because of 'Parsing function pointer types in signatures is not supported.' for some targeted
        // platforms (for example, WASM build).
        private static unsafe partial bool EnumCalendarInfo(IntPtr callback, string localeName, CalendarId calendarId, CalendarDataType calendarDataType, IntPtr context)
        {
            return false;
        }

        internal static partial int GetLatestJapaneseEra()
        {
            return 0;
        }

        internal static partial bool GetJapaneseEraStartDate(int era, out int startYear, out int startMonth, out int startDay)
        {
            startYear = -1;
            startMonth = -1;
            startDay = -1;
            return false;
        }

        [NetJs.External]
        class Collator
        {
            [NetJs.External]
            public class Options
            {
                public string caseFirst = default!;
                public string collation = default!;
                public bool ignorePunctuation;
                public string locale = default!;
                public bool numeric;
                public string sensitivity = default!;
                public string usage = default!;
            }
            public extern int compare(string a, string b);
            public extern Options resolvedOptions();
        }

        internal static unsafe partial ResultCode GetSortHandle(string localeName, out IntPtr sortHandle)
        {
            if (localeName.Length == 0)
            {
                localeName = "en-US";
            }
            var collator = Script.Write<Collator>("new Intl.Collator(localeName)");
            sortHandle = InteropUtility.castObject2Address(collator).As<IntPtr>();
            return ResultCode.Success;
        }

        internal static partial void CloseSortHandle(IntPtr handle)
        {
            InteropUtility.free(handle.As<uint>());
        }

        internal static unsafe partial int CompareString(IntPtr sortHandle, char* lpStr1, int cwStr1Len, char* lpStr2, int cwStr2Len, CompareOptions options)
        {
            var collator = InteropUtility.castAddress2Object(sortHandle.As<uint>()).As<Collator>();
            var str1 = string.Create(lpStr1, 0, cwStr1Len);
            var str2 = string.Create(lpStr2, 0, cwStr2Len);

            var localeName = collator.resolvedOptions().locale;

            // Map .NET CompareOptions to JS Intl.Collator sensitivity
            // None = 'variant' (case and accent sensitive)
            // IgnoreCase = 'accent' (case insensitive, accent sensitive)
            // IgnoreNonSpace = 'case' (case sensitive, accent insensitive)
            // IgnoreCase | IgnoreNonSpace = 'base' (both insensitive)
            string sensitivity = "variant";

            bool ignoreCase = (options & CompareOptions.IgnoreCase) != 0;
            bool ignoreNonSpace = (options & CompareOptions.IgnoreNonSpace) != 0;

            if (ignoreCase && ignoreNonSpace) sensitivity = "base";
            else if (ignoreCase) sensitivity = "accent";
            else if (ignoreNonSpace) sensitivity = "case";
            var numeric = (options & CompareOptions.NumericOrdering) != 0 ? "true" : "false";
            var newCollator = Script.Write<Collator>("new Intl.Collator({0}, { sensitivity: {1}, numeric: {2} })",
                localeName,
                sensitivity,
                numeric);
            return newCollator.compare(str1, str2);
        }

        internal static unsafe partial int IndexOf(IntPtr sortHandle, char* target, int cwTargetLength, char* pSource, int cwSourceLength, CompareOptions options, int* matchLengthPtr)
        {
            var collatorObj = InteropUtility.castAddress2Object(sortHandle.As<uint>()).As<Collator>();
            var localeName = collatorObj.resolvedOptions().locale;

            var source = string.Create(pSource, 0, cwSourceLength);
            var targetStr = string.Create(target, 0, cwTargetLength);

            // Map sensitivity same as CompareString
            string sensitivity = "variant";
            bool ignoreCase = (options & CompareOptions.IgnoreCase) != 0;
            bool ignoreNonSpace = (options & CompareOptions.IgnoreNonSpace) != 0;

            if (ignoreCase && ignoreNonSpace) sensitivity = "base";
            else if (ignoreCase) sensitivity = "accent";
            else if (ignoreNonSpace) sensitivity = "case";

            // Use usage: 'search' which is optimized for finding substrings
            var searchCollator = Script.Write<Collator>("new Intl.Collator({0}, { sensitivity: {1}, usage: 'search' })",
                localeName,
                sensitivity);

            // JS standard doesn't have a native 'collator.indexOf'. 
            // We must loop through the source to find the match that equals 0 (same as target).
            for (int i = 0; i <= cwSourceLength - cwTargetLength; i++)
            {
                // Check a substring starting at i
                // Note: This is a simplified approach; true ICU IndexOf handles variable-length contractions.
                var sub = source.NativeSubstring(i, cwTargetLength);
                if (searchCollator.compare(sub, targetStr) == 0)
                {
                    if (matchLengthPtr != null) *matchLengthPtr = cwTargetLength;
                    return i;
                }
            }

            return -1;
        }

        internal static unsafe partial int LastIndexOf(IntPtr sortHandle, char* target, int cwTargetLength, char* pSource, int cwSourceLength, CompareOptions options, int* matchLengthPtr)
        {
            var collatorObj = InteropUtility.castAddress2Object(sortHandle.As<uint>()).As<Collator>();
            var localeName = collatorObj.resolvedOptions().locale;

            var source = string.Create(pSource, 0, cwSourceLength);
            var targetStr = string.Create(target, 0, cwTargetLength);

            // Map sensitivity
            string sensitivity = "variant";
            bool ignoreCase = (options & CompareOptions.IgnoreCase) != 0;
            bool ignoreNonSpace = (options & CompareOptions.IgnoreNonSpace) != 0;

            if (ignoreCase && ignoreNonSpace) sensitivity = "base";
            else if (ignoreCase) sensitivity = "accent";
            else if (ignoreNonSpace) sensitivity = "case";

            // Create the search-optimized collator
            var searchCollator = Script.Write<Collator>("new Intl.Collator({0}, { sensitivity: {1}, usage: 'search' })",
                localeName,
                sensitivity);

            // Iterate backwards from the end of the source string
            for (int i = cwSourceLength - cwTargetLength; i >= 0; i--)
            {
                var sub = source.NativeSubstring(i, cwTargetLength);
                if (searchCollator.compare(sub, targetStr) == 0)
                {
                    if (matchLengthPtr != null) *matchLengthPtr = cwTargetLength;
                    return i;
                }
            }

            return -1;
        }

        internal static unsafe partial bool StartsWith(IntPtr sortHandle, char* target, int cwTargetLength, char* source, int cwSourceLength, CompareOptions options, int* matchedLength)
        {
            // If the target is longer than the source, it can't be a prefix
            if (cwTargetLength > cwSourceLength) return false;

            var collatorObj = InteropUtility.castAddress2Object(sortHandle.As<uint>()).As<Collator>();
            var localeName = collatorObj.resolvedOptions().locale;

            var sourceStr = string.Create(source, 0, cwSourceLength);
            var targetStr = string.Create(target, 0, cwTargetLength);

            // Map sensitivity flags
            string sensitivity = "variant";
            bool ignoreCase = (options & CompareOptions.IgnoreCase) != 0;
            bool ignoreNonSpace = (options & CompareOptions.IgnoreNonSpace) != 0;

            if (ignoreCase && ignoreNonSpace) sensitivity = "base";
            else if (ignoreCase) sensitivity = "accent";
            else if (ignoreNonSpace) sensitivity = "case";

            var searchCollator = Script.Write<Collator>("new Intl.Collator({0}, { sensitivity: {1}, usage: 'search' })",
                localeName,
                sensitivity);

            // Take a slice from the start of the source equal to the target length
            var startSub = sourceStr.NativeSubstring(0, cwTargetLength);

            if (searchCollator.compare(startSub, targetStr) == 0)
            {
                if (matchedLength != null) *matchedLength = cwTargetLength;
                return true;
            }

            return false;
        }

        internal static unsafe partial bool EndsWith(IntPtr sortHandle, char* target, int cwTargetLength, char* source, int cwSourceLength, CompareOptions options, int* matchedLength)
        {
            // If the target is longer than the source, it can't be the suffix
            if (cwTargetLength > cwSourceLength) return false;

            var collatorObj = InteropUtility.castAddress2Object(sortHandle.As<uint>()).As<Collator>();
            var localeName = collatorObj.resolvedOptions().locale;

            var sourceStr = string.Create(source, 0, cwSourceLength);
            var targetStr = string.Create(target, 0, cwTargetLength);

            // Map sensitivity flags
            string sensitivity = "variant";
            bool ignoreCase = (options & CompareOptions.IgnoreCase) != 0;
            bool ignoreNonSpace = (options & CompareOptions.IgnoreNonSpace) != 0;

            if (ignoreCase && ignoreNonSpace) sensitivity = "base";
            else if (ignoreCase) sensitivity = "accent";
            else if (ignoreNonSpace) sensitivity = "case";

            var searchCollator = Script.Write<Collator>("new Intl.Collator({0}, { sensitivity: {1}, usage: 'search' })",
                localeName,
                sensitivity);

            // Extract the end of the source string
            var endSub = sourceStr.NativeSubstring(cwSourceLength - cwTargetLength, cwTargetLength);

            if (searchCollator.compare(endSub, targetStr) == 0)
            {
                if (matchedLength != null) *matchedLength = cwTargetLength;
                return true;
            }

            return false;
        }

        internal static partial bool StartsWith(IntPtr sortHandle, string target, int cwTargetLength, string source, int cwSourceLength, CompareOptions options)
        {
            // Basic length check: if target is longer than source, it cannot be a prefix
            if (cwTargetLength > cwSourceLength) return false;

            // Retrieve the underlying JS Intl.Collator instance for locale info
            var collatorObj = InteropUtility.castAddress2Object(sortHandle.As<uint>()).As<Collator>();
            var localeName = collatorObj.resolvedOptions().locale;

            // Map .NET CompareOptions to JS Intl.Collator sensitivity
            string sensitivity = "variant";
            bool ignoreCase = (options & CompareOptions.IgnoreCase) != 0;
            bool ignoreNonSpace = (options & CompareOptions.IgnoreNonSpace) != 0;

            if (ignoreCase && ignoreNonSpace) sensitivity = "base";
            else if (ignoreCase) sensitivity = "accent";
            else if (ignoreNonSpace) sensitivity = "case";

            // Initialize the search-optimized collator
            var searchCollator = Script.Write<Collator>("new Intl.Collator({0}, { sensitivity: {1}, usage: 'search' })",
                localeName,
                sensitivity);

            // Extract the prefix from the source string based on target length
            // Using the provided lengths ensures we respect the caller's bounds
            var startSub = source.NativeSubstring(0, cwTargetLength);

            // Perform the locale-aware comparison
            return searchCollator.compare(startSub, target) == 0;
        }

        internal static partial bool EndsWith(IntPtr sortHandle, string target, int cwTargetLength, string source, int cwSourceLength, CompareOptions options)
        {
            // If the target is longer than the source, it cannot be the suffix
            if (cwTargetLength > cwSourceLength) return false;

            // Retrieve the underlying JS Intl.Collator instance for locale info
            var collatorObj = InteropUtility.castAddress2Object(sortHandle.As<uint>()).As<Collator>();
            var localeName = collatorObj.resolvedOptions().locale;

            // Map .NET CompareOptions to JS Intl.Collator sensitivity
            string sensitivity = "variant";
            bool ignoreCase = (options & CompareOptions.IgnoreCase) != 0;
            bool ignoreNonSpace = (options & CompareOptions.IgnoreNonSpace) != 0;

            if (ignoreCase && ignoreNonSpace) sensitivity = "base";
            else if (ignoreCase) sensitivity = "accent";
            else if (ignoreNonSpace) sensitivity = "case";

            // Initialize the search-optimized collator
            var searchCollator = Script.Write<Collator>("new Intl.Collator({0}, { sensitivity: {1}, usage: 'search' })",
                localeName,
                sensitivity);

            // Extract the suffix from the source string
            // Calculate start index based on provided source and target lengths
            var endSub = source.NativeSubstring(cwSourceLength - cwTargetLength, cwTargetLength);

            // Perform the locale-aware comparison
            return searchCollator.compare(endSub, target) == 0;
        }

        internal static unsafe partial int GetSortKey(IntPtr sortHandle, char* str, int strLength, byte* sortKey, int sortKeyLength, CompareOptions options)
        {
            throw null!;
        }

        internal static partial int GetSortVersion(IntPtr sortHandle)
        {
            throw null!;
        }

    }
}
