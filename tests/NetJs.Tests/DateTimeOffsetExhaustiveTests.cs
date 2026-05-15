using System;
using System.Diagnostics;
using System.Globalization;


namespace NetJs.Tests
{
    /// <summary>
    /// Exhaustive tests for all public APIs of System.DateTimeOffset.
    /// Validation is performed exclusively via Debug.Assert.
    /// Run in Debug configuration so assertions are active.
    /// </summary>
    public static class DateTimeOffsetExhaustiveTests
    {
        // ─────────────────────────────────────────────
        //  Entry point
        // ─────────────────────────────────────────────
        public static void Run()
        {
            TestConstructors();
            TestStaticFields();
            TestInstanceProperties();
            TestStaticProperties();
            TestAddMethods();
            TestComparisonMethods();
            TestConversionMethods();
            TestParsingMethods();
            TestFormattingMethods();
            TestOperators();
            TestFromFileTime();
            TestFromUnixTimeSeconds();
            TestFromUnixTimeMilliseconds();
            TestToUnixTimeSeconds();
            TestToUnixTimeMilliseconds();
            TestToFileTime();
            TestToBinaryAndFromBinary();       // not exposed — covered via round-trip note
            TestEqualsExact();
            TestIComparableIEquatable();
            TestMinMaxValues();
            TestOffsetBehavior();
            TestImplicitCastFromDateTime();
            TestGetDateTimeFormats();
            TestDateOnlyTimeOnlyIntegration();  // .NET 6+

            Console.WriteLine("✅ DateTimeOffset tests passed.");
        }

        // ─────────────────────────────────────────────
        //  1. Constructors
        // ─────────────────────────────────────────────
        static void TestConstructors()
        {
            var plusTwo = TimeSpan.FromHours(2);
            var minusFive = TimeSpan.FromHours(-5);
            var zero = TimeSpan.Zero;

            // (DateTime)
            var dt = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            var dto1 = new DateTimeOffset(dt);
            Debug.Assert(dto1.UtcDateTime == dt, "ctor(DateTime) Utc");

            var dtLocal = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Local);
            var dto1b = new DateTimeOffset(dtLocal);
            Debug.Assert(dto1b.DateTime == dtLocal, "ctor(DateTime) Local");

            // (DateTime, TimeSpan)
            var dtUnspec = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
            var dto2 = new DateTimeOffset(dtUnspec, plusTwo);
            Debug.Assert(dto2.Offset == plusTwo, "ctor(DateTime, TimeSpan).Offset");
            Debug.Assert(dto2.Hour == 12, "ctor(DateTime, TimeSpan).Hour");

            // (long ticks, TimeSpan)
            var dto3 = new DateTimeOffset(dt.Ticks, zero);
            Debug.Assert(dto3.UtcTicks == dt.Ticks, "ctor(ticks, offset=0).UtcTicks");

            // (int year, int month, int day, int hour, int minute, int second, TimeSpan)
            var dto4 = new DateTimeOffset(2024, 6, 15, 12, 30, 45, plusTwo);
            Debug.Assert(dto4.Year == 2024 && dto4.Month == 6 && dto4.Day == 15, "ctor(y,m,d,h,min,s,offset) date");
            Debug.Assert(dto4.Hour == 12 && dto4.Minute == 30 && dto4.Second == 45, "ctor(y,m,d,h,min,s,offset) time");
            Debug.Assert(dto4.Offset == plusTwo, "ctor(y,m,d,h,min,s,offset).Offset");

            // (int year, int month, int day, int hour, int minute, int second, int millisecond, TimeSpan)
            var dto5 = new DateTimeOffset(2024, 6, 15, 12, 30, 45, 500, plusTwo);
            Debug.Assert(dto5.Millisecond == 500, "ctor(...,ms,offset).Millisecond");

            // (int year, int month, int day, int hour, int minute, int second, int millisecond, Calendar, TimeSpan)
            var dto6 = new DateTimeOffset(2024, 6, 15, 12, 30, 45, 500, new GregorianCalendar(), plusTwo);
            Debug.Assert(dto6.Year == 2024 && dto6.Millisecond == 500, "ctor(...,Calendar,offset)");

            // (int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond, TimeSpan) — .NET 7+
#if NET7_0_OR_GREATER
            var dto7 = new DateTimeOffset(2024, 6, 15, 12, 30, 45, 500, 123, plusTwo);
            Debug.Assert(dto7.Microsecond == 123, "ctor(...,microsecond,offset).Microsecond");

            // (int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond, Calendar, TimeSpan)
            var dto8 = new DateTimeOffset(2024, 6, 15, 12, 30, 45, 500, 123, new GregorianCalendar(), plusTwo);
            Debug.Assert(dto8.Microsecond == 123, "ctor(...,microsecond,Calendar,offset)");
#endif

            // Negative offset
            var dto9 = new DateTimeOffset(2024, 6, 15, 12, 0, 0, minusFive);
            Debug.Assert(dto9.Offset == minusFive, "ctor with negative offset");

            // UTC (offset = zero)
            var dto10 = new DateTimeOffset(2024, 6, 15, 12, 0, 0, zero);
            Debug.Assert(dto10.Offset == TimeSpan.Zero, "ctor with zero offset");
        }

        // ─────────────────────────────────────────────
        //  2. Static fields
        // ─────────────────────────────────────────────
        static void TestStaticFields()
        {
            Debug.Assert(DateTimeOffset.MinValue.Year == 1, "MinValue.Year == 1");
            Debug.Assert(DateTimeOffset.MaxValue.Year == 9999, "MaxValue.Year == 9999");
            Debug.Assert(DateTimeOffset.MinValue < DateTimeOffset.MaxValue, "MinValue < MaxValue");
            Debug.Assert(DateTimeOffset.MinValue.Offset == TimeSpan.Zero, "MinValue.Offset == Zero");
            Debug.Assert(DateTimeOffset.MaxValue.Offset == TimeSpan.Zero, "MaxValue.Offset == Zero");

#if NET6_0_OR_GREATER
            var epoch = DateTimeOffset.UnixEpoch;
            Debug.Assert(epoch.Year == 1970 && epoch.Month == 1 && epoch.Day == 1, "UnixEpoch date");
            Debug.Assert(epoch.Hour == 0 && epoch.Minute == 0 && epoch.Second == 0, "UnixEpoch time");
            Debug.Assert(epoch.Offset == TimeSpan.Zero, "UnixEpoch.Offset == Zero");
            Debug.Assert(epoch.ToUnixTimeSeconds() == 0, "UnixEpoch.ToUnixTimeSeconds() == 0");
#endif
        }

        // ─────────────────────────────────────────────
        //  3. Instance properties
        // ─────────────────────────────────────────────
        static void TestInstanceProperties()
        {
            var offset = TimeSpan.FromHours(5.5); // IST +05:30
            var dto = new DateTimeOffset(2024, 2, 29, 13, 45, 30, 250, offset);

            // Calendar components (local to the offset)
            Debug.Assert(dto.Year == 2024, "Year");
            Debug.Assert(dto.Month == 2, "Month");
            Debug.Assert(dto.Day == 29, "Day");
            Debug.Assert(dto.Hour == 13, "Hour");
            Debug.Assert(dto.Minute == 45, "Minute");
            Debug.Assert(dto.Second == 30, "Second");
            Debug.Assert(dto.Millisecond == 250, "Millisecond");

            // Offset
            Debug.Assert(dto.Offset == offset, "Offset");

            // Date — strips time, preserves offset
            Debug.Assert(dto.Date == new DateTime(2024, 2, 29), "Date");

            // TimeOfDay
            var tod = dto.TimeOfDay;
            Debug.Assert(tod.Hours == 13 && tod.Minutes == 45 && tod.Seconds == 30, "TimeOfDay");

            // DayOfWeek — 2024-02-29 is Thursday
            Debug.Assert(dto.DayOfWeek == DayOfWeek.Thursday, "DayOfWeek");

            // DayOfYear — leap year: Jan(31)+Feb(29)=60
            Debug.Assert(dto.DayOfYear == 60, "DayOfYear");

            // Ticks — local ticks including offset
            Debug.Assert(dto.Ticks > 0, "Ticks > 0");

            // UtcTicks — ticks relative to UTC
            Debug.Assert(dto.UtcTicks > 0, "UtcTicks > 0");
            Debug.Assert(dto.UtcTicks < dto.Ticks, "UtcTicks < Ticks for positive offset");

            // DateTime — unspecified-kind local-time DateTime
            var localDt = dto.DateTime;
            Debug.Assert(localDt.Kind == DateTimeKind.Unspecified, "DateTime.Kind == Unspecified");
            Debug.Assert(localDt.Hour == 13, "DateTime.Hour");

            // LocalDateTime — converted to machine local
            var localMachine = dto.LocalDateTime;
            Debug.Assert(localMachine.Kind == DateTimeKind.Local, "LocalDateTime.Kind == Local");

            // UtcDateTime
            var utcDt = dto.UtcDateTime;
            Debug.Assert(utcDt.Kind == DateTimeKind.Utc, "UtcDateTime.Kind == Utc");
            // UTC hour = 13 - 5 = 8, minus 30 min = 08:15
            Debug.Assert(utcDt.Hour == 8 && utcDt.Minute == 15, "UtcDateTime offset conversion");

#if NET7_0_OR_GREATER
            var dtoMicro = new DateTimeOffset(2024, 2, 29, 13, 45, 30, 250, 777, offset);
            Debug.Assert(dtoMicro.Microsecond == 777, "Microsecond");
            Debug.Assert(dtoMicro.Nanosecond >= 0, "Nanosecond >= 0");
#endif
        }

        // ─────────────────────────────────────────────
        //  4. Static properties
        // ─────────────────────────────────────────────
        static void TestStaticProperties()
        {
            var before = DateTimeOffset.UtcNow;
            var now = DateTimeOffset.Now;
            var utcNow = DateTimeOffset.UtcNow;
            var after = DateTimeOffset.UtcNow;

            Debug.Assert(utcNow >= before && utcNow <= after, "UtcNow within expected window");
            Debug.Assert(utcNow.Offset == TimeSpan.Zero, "UtcNow.Offset == Zero");
            Debug.Assert(now.Offset == TimeZoneInfo.Local.GetUtcOffset(now.DateTime),
                         "Now.Offset matches local timezone");
            Debug.Assert(now.UtcDateTime >= before.UtcDateTime, "Now.UtcDateTime >= before");
        }

        // ─────────────────────────────────────────────
        //  5. Add* instance methods
        // ─────────────────────────────────────────────
        static void TestAddMethods()
        {
            var offset = TimeSpan.FromHours(3);
            var base_ = new DateTimeOffset(2024, 1, 1, 0, 0, 0, offset);

            // Add(TimeSpan)
            var addTs = base_.Add(TimeSpan.FromHours(2));
            Debug.Assert(addTs.Hour == 2 && addTs.Offset == offset, "Add(TimeSpan)");

            // AddTicks
            var addTk = base_.AddTicks(TimeSpan.TicksPerMillisecond);
            Debug.Assert(addTk.Millisecond == 1, "AddTicks");

            // AddMilliseconds
            var addMs = base_.AddMilliseconds(1500);
            Debug.Assert(addMs.Second == 1 && addMs.Millisecond == 500, "AddMilliseconds");

            // AddSeconds
            var addSec = base_.AddSeconds(90);
            Debug.Assert(addSec.Minute == 1 && addSec.Second == 30, "AddSeconds");

            // AddMinutes
            var addMin = base_.AddMinutes(75);
            Debug.Assert(addMin.Hour == 1 && addMin.Minute == 15, "AddMinutes");

            // AddHours
            var addH = base_.AddHours(25);
            Debug.Assert(addH.Day == 2 && addH.Hour == 1, "AddHours crosses midnight");

            // AddDays
            var addD = base_.AddDays(31);
            Debug.Assert(addD.Month == 2 && addD.Offset == offset, "AddDays crosses month");

            // AddMonths
            var addMo = base_.AddMonths(13);
            Debug.Assert(addMo.Year == 2025 && addMo.Month == 2, "AddMonths");

            // AddYears
            var addY = base_.AddYears(4);
            Debug.Assert(addY.Year == 2028, "AddYears");

            // Negative
            var sub = base_.AddDays(-1);
            Debug.Assert(sub.Year == 2023 && sub.Month == 12 && sub.Day == 31, "AddDays negative");

            // Offset is always preserved after all add operations
            Debug.Assert(addTs.Offset == offset, "Add preserves offset");
            Debug.Assert(addD.Offset == offset, "AddDays preserves offset");
            Debug.Assert(addMo.Offset == offset, "AddMonths preserves offset");
            Debug.Assert(addY.Offset == offset, "AddYears preserves offset");

#if NET7_0_OR_GREATER
            var addMicro = base_.AddMicroseconds(1500);
            Debug.Assert(addMicro.Millisecond == 1, "AddMicroseconds");
#endif
        }

        // ─────────────────────────────────────────────
        //  6. Comparison methods
        // ─────────────────────────────────────────────
        static void TestComparisonMethods()
        {
            // Two DateTimeOffset values representing the same UTC instant but different offsets
            var a = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.FromHours(2));  // 10:00 UTC
            var b = new DateTimeOffset(2024, 6, 15, 15, 0, 0, TimeSpan.FromHours(5));  // 10:00 UTC — same instant
            var c = new DateTimeOffset(2024, 6, 15, 14, 0, 0, TimeSpan.FromHours(2));  // 12:00 UTC — later

            // Compare by UTC instant → a == b
            Debug.Assert(DateTimeOffset.Compare(a, b) == 0, "Compare: same UTC instant == 0");
            Debug.Assert(DateTimeOffset.Compare(a, c) < 0, "Compare: earlier < later");
            Debug.Assert(DateTimeOffset.Compare(c, a) > 0, "Compare: later > earlier");

            Debug.Assert(a.CompareTo(b) == 0, "CompareTo: same instant");
            Debug.Assert(a.CompareTo(c) < 0, "CompareTo: earlier");

            // Equals (compares UTC instant — same as ==)
            Debug.Assert(DateTimeOffset.Equals(a, b), "DateTimeOffset.Equals same instant");
            Debug.Assert(!DateTimeOffset.Equals(a, c), "!DateTimeOffset.Equals different instant");
            Debug.Assert(a.Equals(b), "a.Equals(b) same instant");
            Debug.Assert(!a.Equals(c), "!a.Equals(c)");

            // EqualsExact — must have same UTC AND same offset
            Debug.Assert(!a.EqualsExact(b), "EqualsExact: same UTC, different offset → false");
            var aCopy = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.FromHours(2));
            Debug.Assert(a.EqualsExact(aCopy), "EqualsExact: same UTC and offset → true");

            // GetHashCode — equal UTC instants must have equal hashes
            Debug.Assert(a.GetHashCode() == b.GetHashCode(), "Same UTC instant → same hash");
        }

        // ─────────────────────────────────────────────
        //  7. EqualsExact (standalone section)
        // ─────────────────────────────────────────────
        static void TestEqualsExact()
        {
            var x = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.FromHours(1));
            var y = new DateTimeOffset(2024, 1, 1, 13, 0, 0, TimeSpan.FromHours(2)); // same UTC
            var z = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.FromHours(1)); // exact copy

            Debug.Assert(x.EqualsExact(z), "EqualsExact: identical values → true");
            Debug.Assert(!x.EqualsExact(y), "EqualsExact: same UTC, different offset → false");
            Debug.Assert(x.Equals(y), "Equals: same UTC instant → true");
        }

        // ─────────────────────────────────────────────
        //  8. Conversion methods
        // ─────────────────────────────────────────────
        static void TestConversionMethods()
        {
            var offset = TimeSpan.FromHours(3);
            var dto = new DateTimeOffset(2024, 6, 15, 15, 0, 0, offset); // UTC = 12:00

            // ToOffset
            var dto0 = dto.ToOffset(TimeSpan.Zero);
            Debug.Assert(dto0.Hour == 12 && dto0.Offset == TimeSpan.Zero, "ToOffset(0) → UTC 12:00");

            var dtoMinus = dto.ToOffset(TimeSpan.FromHours(-5));
            Debug.Assert(dtoMinus.Hour == 7, "ToOffset(-5) → 07:00");

            // ToLocalTime
            var local = dto.ToLocalTime();
            Debug.Assert(local.Offset == TimeZoneInfo.Local.GetUtcOffset(local.DateTime),
                         "ToLocalTime().Offset matches local TZ");

            // ToUniversalTime
            var utc = dto.ToUniversalTime();
            Debug.Assert(utc.Offset == TimeSpan.Zero, "ToUniversalTime().Offset == Zero");
            Debug.Assert(utc.Hour == 12, "ToUniversalTime() UTC hour == 12");

            // UtcDateTime / DateTime / LocalDateTime round-trips
            var backFromUtcDt = new DateTimeOffset(dto.UtcDateTime);
            Debug.Assert(backFromUtcDt.UtcDateTime == dto.UtcDateTime, "UtcDateTime round-trip");

            //// ToLongDateString / ToShortDateString
            //Debug.Assert(!string.IsNullOrEmpty(dto.ToLongDateString()), "ToLongDateString");
            //Debug.Assert(!string.IsNullOrEmpty(dto.ToShortDateString()), "ToShortDateString");

            //// ToLongTimeString / ToShortTimeString
            //Debug.Assert(!string.IsNullOrEmpty(dto.ToLongTimeString()), "ToLongTimeString");
            //Debug.Assert(!string.IsNullOrEmpty(dto.ToShortTimeString()), "ToShortTimeString");
        }

        // ─────────────────────────────────────────────
        //  9. Parsing
        // ─────────────────────────────────────────────
        static void TestParsingMethods()
        {
            var inv = CultureInfo.InvariantCulture;

            // ISO 8601 with explicit offset
            const string iso = "2024-06-15T12:30:00+03:00";
            var expected = new DateTimeOffset(2024, 6, 15, 12, 30, 0, TimeSpan.FromHours(3));

            // Parse(string)
            var p1 = DateTimeOffset.Parse(iso);
            Debug.Assert(p1 == expected, "Parse(string)");

            // Parse(string, IFormatProvider)
            var p2 = DateTimeOffset.Parse(iso, inv);
            Debug.Assert(p2 == expected, "Parse(string, IFormatProvider)");

            // Parse(string, IFormatProvider, DateTimeStyles)
            var p3 = DateTimeOffset.Parse(iso, inv, DateTimeStyles.None);
            Debug.Assert(p3 == expected, "Parse(string, IFormatProvider, DateTimeStyles)");

            // ParseExact — single format
            var p4 = DateTimeOffset.ParseExact("15/06/2024 12:30:00 +03:00",
                                               "dd/MM/yyyy HH:mm:ss zzz", inv);
            Debug.Assert(p4 == expected, "ParseExact(single format)");

            // ParseExact — format array
            var formats = new[] { "dd/MM/yyyy HH:mm:ss zzz", "yyyy-MM-ddTHH:mm:sszzz" };
            var p5 = DateTimeOffset.ParseExact(iso, formats, inv, DateTimeStyles.None);
            Debug.Assert(p5 == expected, "ParseExact(format array)");

            // TryParse(string, out)
            bool ok1 = DateTimeOffset.TryParse(iso, out var tp1);
            Debug.Assert(ok1 && tp1 == expected, "TryParse(string, out)");

            // TryParse(string, IFormatProvider, DateTimeStyles, out)
            bool ok2 = DateTimeOffset.TryParse(iso, inv, DateTimeStyles.None, out var tp2);
            Debug.Assert(ok2 && tp2 == expected, "TryParse(string, IFormatProvider, DateTimeStyles, out)");

            // TryParse failure
            bool fail = DateTimeOffset.TryParse("not-a-date", out _);
            Debug.Assert(!fail, "TryParse returns false for invalid input");

            // TryParseExact — single format
            bool ok3 = DateTimeOffset.TryParseExact(iso, "yyyy-MM-ddTHH:mm:sszzz",
                                                     inv, DateTimeStyles.None, out var tp3);
            Debug.Assert(ok3 && tp3 == expected, "TryParseExact(single format)");

            // TryParseExact — format array
            bool ok4 = DateTimeOffset.TryParseExact(iso, formats, inv, DateTimeStyles.None, out var tp4);
            Debug.Assert(ok4 && tp4 == expected, "TryParseExact(format array)");

            // TryParseExact failure
            bool fail2 = DateTimeOffset.TryParseExact("bad", "yyyy-MM-dd", inv, DateTimeStyles.None, out _);
            Debug.Assert(!fail2, "TryParseExact returns false for invalid input");

            // Round-trip via 'o' (ISO 8601 with offset)
            var dto = new DateTimeOffset(2024, 6, 15, 12, 30, 0, TimeSpan.FromHours(3));
            var roundTrip = DateTimeOffset.Parse(dto.ToString("o", inv), inv, DateTimeStyles.RoundtripKind);
            Debug.Assert(roundTrip == dto, "Round-trip via 'o' format");

            // Offset preserved after parse
            Debug.Assert(roundTrip.Offset == TimeSpan.FromHours(3), "Offset preserved after parse");
        }

        // ─────────────────────────────────────────────
        //  10. Formatting / ToString overloads
        // ─────────────────────────────────────────────
        static void TestFormattingMethods()
        {
            var inv = CultureInfo.InvariantCulture;
            var offset = TimeSpan.FromHours(3);
            var dto = new DateTimeOffset(2024, 3, 15, 10, 30, 45, offset);

            // ToString()
            Debug.Assert(!string.IsNullOrEmpty(dto.ToString()), "ToString()");

            // ToString(IFormatProvider)
            Debug.Assert(!string.IsNullOrEmpty(dto.ToString(inv)), "ToString(IFormatProvider)");

            // ToString(string)
            Debug.Assert(dto.ToString("yyyy") == "2024", "ToString(\"yyyy\")");
            Debug.Assert(dto.ToString("MM") == "03", "ToString(\"MM\")");
            Debug.Assert(dto.ToString("dd") == "15", "ToString(\"dd\")");
            Debug.Assert(dto.ToString("HH") == "10", "ToString(\"HH\")");
            Debug.Assert(dto.ToString("mm") == "30", "ToString(\"mm\")");
            Debug.Assert(dto.ToString("ss") == "45", "ToString(\"ss\")");

            // Offset part in format
            var zzz = dto.ToString("zzz", inv);
            Debug.Assert(zzz == "+03:00", "ToString(\"zzz\") == +03:00");
            Debug.Assert(dto.ToString("%z", inv) == "+3", "ToString(\"z\") == +3");
            Debug.Assert(dto.ToString("zz", inv) == "+03", "ToString(\"zz\") == +03");

            // ToString(string, IFormatProvider)
            Debug.Assert(dto.ToString("o", inv).Contains("+03:00"), "ToString(\"o\") contains offset");
            Debug.Assert(dto.ToString("r", inv).Contains("GMT"), "ToString(\"r\") RFC1123 contains GMT");

            // Standard specifiers — round-trip
            var oStr = dto.ToString("o", inv);
            var fromO = DateTimeOffset.Parse(oStr, inv, DateTimeStyles.RoundtripKind);
            Debug.Assert(fromO == dto, "Round-trip via 'o'");

            var rStr = dto.ToString("r", inv);
            var fromR = DateTimeOffset.Parse(rStr, inv);
            Debug.Assert(fromR.UtcDateTime == dto.UtcDateTime, "Round-trip via 'r' (UTC instant)");
        }

        // ─────────────────────────────────────────────
        //  11. GetDateTimeFormats
        // ─────────────────────────────────────────────
        static void TestGetDateTimeFormats()
        {
            //var inv = CultureInfo.InvariantCulture;
            //var dto = new DateTimeOffset(2024, 6, 15, 12, 30, 0, TimeSpan.FromHours(3));

            //var all = dto.GetDateTimeFormats(inv);
            //Debug.Assert(all.Length > 0, "GetDateTimeFormats(IFormatProvider) non-empty");

            //foreach (char spec in "dDfFgGmMoOrRstTuUyY")
            //{
            //    var arr = dto.GetDateTimeFormats(spec, inv);
            //    Debug.Assert(arr.Length > 0, $"GetDateTimeFormats('{spec}') non-empty");
            //}

            //// Overload without IFormatProvider
            //var allDefault = dto.GetDateTimeFormats();
            //Debug.Assert(allDefault.Length > 0, "GetDateTimeFormats() non-empty");
        }

        // ─────────────────────────────────────────────
        //  12. Operators
        // ─────────────────────────────────────────────
        static void TestOperators()
        {
            var a = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.FromHours(2));
            var b = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.FromHours(2));

            // Subtraction: DateTimeOffset - DateTimeOffset → TimeSpan
            TimeSpan diff = b - a;
            Debug.Assert(diff.TotalDays == 14, "b - a == 14 days");

            // Addition: DateTimeOffset + TimeSpan → DateTimeOffset
            var c = a + TimeSpan.FromDays(14);
            Debug.Assert(c == b, "a + 14days == b");
            Debug.Assert(c.Offset == a.Offset, "Addition preserves offset");

            // Subtraction: DateTimeOffset - TimeSpan → DateTimeOffset
            var d = b - TimeSpan.FromDays(14);
            Debug.Assert(d == a, "b - 14days == a");

            // Comparison operators — compare UTC instants
            var earlier = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.FromHours(0));
            var later = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.FromHours(0));
            Debug.Assert(earlier < later, "earlier < later");
            Debug.Assert(later > earlier, "later > earlier");
            Debug.Assert(earlier <= later, "earlier <= later");
            Debug.Assert(later >= earlier, "later >= earlier");
            Debug.Assert(earlier != later, "earlier != later");

            // == compares UTC instants (not offsets)
            var x = new DateTimeOffset(2024, 6, 1, 2, 0, 0, TimeSpan.FromHours(2)); // UTC 00:00
            var y = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.FromHours(0)); // UTC 00:00
            Debug.Assert(x == y, "== compares UTC instant, not local time or offset");

            // != with genuinely different instants
            Debug.Assert(a != b, "a != b (different UTC instants)");
        }

        // ─────────────────────────────────────────────
        //  13. Implicit cast from DateTime
        // ─────────────────────────────────────────────
        static void TestImplicitCastFromDateTime()
        {
            // DateTime with Utc kind
            DateTime dtUtc = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            DateTimeOffset dtoUtc = dtUtc;
            Debug.Assert(dtoUtc.Offset == TimeSpan.Zero, "Implicit cast from Utc DateTime → offset 0");
            Debug.Assert(dtoUtc.UtcDateTime == dtUtc, "Implicit cast from Utc preserves value");

            // DateTime with Local kind
            DateTime dtLocal = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Local);
            DateTimeOffset dtoLocal = dtLocal;
            Debug.Assert(dtoLocal.Offset == TimeZoneInfo.Local.GetUtcOffset(dtLocal),
                         "Implicit cast from Local DateTime → local offset");

            // DateTime with Unspecified kind → treated as local
            DateTime dtUnspec = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
            DateTimeOffset dtoUnspec = dtUnspec;
            Debug.Assert(dtoUnspec.DateTime == dtUnspec, "Implicit cast from Unspecified preserves local time");
        }

        // ─────────────────────────────────────────────
        //  14. FromFileTime
        // ─────────────────────────────────────────────
        static void TestFromFileTime()
        {
            var utc = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
            long ft = utc.ToFileTime();

            var back = DateTimeOffset.FromFileTime(ft);
            // FromFileTime returns local time equivalent
            Debug.Assert(back.UtcDateTime == utc.UtcDateTime, "FromFileTime → same UTC instant");

            // FILETIME 0 == 1601-01-01 00:00:00 UTC
            var epoch = DateTimeOffset.FromFileTime(0);
            Debug.Assert(epoch.UtcDateTime == new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                         "FILETIME epoch == 1601-01-01 UTC");
        }

        // ─────────────────────────────────────────────
        //  15. ToFileTime
        // ─────────────────────────────────────────────
        static void TestToFileTime()
        {
            var dto = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.FromHours(3));
            long ft = dto.ToFileTime();
            Debug.Assert(ft > 0, "ToFileTime() > 0");

            // Round-trip via FromFileTime
            var back = DateTimeOffset.FromFileTime(ft);
            Debug.Assert(back.UtcDateTime == dto.UtcDateTime, "ToFileTime → FromFileTime UTC round-trip");

            // Two offsets representing the same instant produce the same FileTime
            var same = new DateTimeOffset(2024, 6, 15, 9, 0, 0, TimeSpan.Zero); // same UTC instant
            Debug.Assert(dto.ToFileTime() == same.ToFileTime(),
                         "Same UTC instant, different offsets → same FileTime");
        }

        // ─────────────────────────────────────────────
        //  16. FromUnixTimeSeconds
        // ─────────────────────────────────────────────
        static void TestFromUnixTimeSeconds()
        {
            // Epoch
            var epoch = DateTimeOffset.FromUnixTimeSeconds(0);
            Debug.Assert(epoch.Year == 1970 && epoch.Month == 1 && epoch.Day == 1, "UnixTime 0 date");
            Debug.Assert(epoch.Hour == 0 && epoch.Minute == 0 && epoch.Second == 0, "UnixTime 0 time");
            Debug.Assert(epoch.Offset == TimeSpan.Zero, "FromUnixTimeSeconds always Utc (offset 0)");

            // Positive
            var pos = DateTimeOffset.FromUnixTimeSeconds(86400); // +1 day
            Debug.Assert(pos.Day == 2 && pos.Month == 1 && pos.Year == 1970, "+1 day from epoch");

            // Negative (before 1970)
            var neg = DateTimeOffset.FromUnixTimeSeconds(-86400);
            Debug.Assert(neg.Year == 1969 && neg.Month == 12 && neg.Day == 31, "-1 day from epoch");

            // Known timestamp: 2024-06-15 00:00:00 UTC = 1718409600
            var known = DateTimeOffset.FromUnixTimeSeconds(1718409600);
            Debug.Assert(known.Year == 2024 && known.Month == 6 && known.Day == 15,
                         "FromUnixTimeSeconds(1718409600) == 2024-06-15");
        }

        // ─────────────────────────────────────────────
        //  17. FromUnixTimeMilliseconds
        // ─────────────────────────────────────────────
        static void TestFromUnixTimeMilliseconds()
        {
            var epoch = DateTimeOffset.FromUnixTimeMilliseconds(0);
            Debug.Assert(epoch.Year == 1970, "FromUnixTimeMilliseconds(0).Year == 1970");
            Debug.Assert(epoch.Offset == TimeSpan.Zero, "FromUnixTimeMilliseconds always UTC");

            var halfSec = DateTimeOffset.FromUnixTimeMilliseconds(500);
            Debug.Assert(halfSec.Millisecond == 500, "FromUnixTimeMilliseconds(500).Millisecond == 500");

            // Round-trip
            var dto = new DateTimeOffset(2024, 6, 15, 12, 30, 45, 250, TimeSpan.Zero);
            long ms = dto.ToUnixTimeMilliseconds();
            var back = DateTimeOffset.FromUnixTimeMilliseconds(ms);
            Debug.Assert(back == dto, "FromUnixTimeMilliseconds round-trip");
        }

        // ─────────────────────────────────────────────
        //  18. ToUnixTimeSeconds
        // ─────────────────────────────────────────────
        static void TestToUnixTimeSeconds()
        {
            var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
            Debug.Assert(epoch.ToUnixTimeSeconds() == 0, "Epoch.ToUnixTimeSeconds() == 0");

            var oneDay = new DateTimeOffset(1970, 1, 2, 0, 0, 0, TimeSpan.Zero);
            Debug.Assert(oneDay.ToUnixTimeSeconds() == 86400, "+1 day → 86400 seconds");

            // Fractional seconds are truncated (floored)
            var withMs = new DateTimeOffset(1970, 1, 1, 0, 0, 1, 999, TimeSpan.Zero);
            Debug.Assert(withMs.ToUnixTimeSeconds() == 1, "Milliseconds truncated in ToUnixTimeSeconds");

            // Offset-independent: same UTC instant → same unix seconds
            var a = new DateTimeOffset(2024, 6, 15, 15, 0, 0, TimeSpan.FromHours(3));
            var b = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
            Debug.Assert(a.ToUnixTimeSeconds() == b.ToUnixTimeSeconds(),
                         "Same UTC instant, different offsets → same unix seconds");
        }

        // ─────────────────────────────────────────────
        //  19. ToUnixTimeMilliseconds
        // ─────────────────────────────────────────────
        static void TestToUnixTimeMilliseconds()
        {
            var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
            Debug.Assert(epoch.ToUnixTimeMilliseconds() == 0, "Epoch.ToUnixTimeMilliseconds() == 0");

            var halfSec = new DateTimeOffset(1970, 1, 1, 0, 0, 0, 500, TimeSpan.Zero);
            Debug.Assert(halfSec.ToUnixTimeMilliseconds() == 500, "500ms → 500 unix ms");

            // Offset-independent
            var a = new DateTimeOffset(2024, 6, 15, 15, 0, 0, TimeSpan.FromHours(3));
            var b = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
            Debug.Assert(a.ToUnixTimeMilliseconds() == b.ToUnixTimeMilliseconds(),
                         "Same UTC instant → same unix ms");
        }

        // ─────────────────────────────────────────────
        //  20. ToBinary / FromBinary note
        //      DateTimeOffset does NOT expose ToBinary / FromBinary.
        //      Serialization is done via Ticks + Offset pattern.
        // ─────────────────────────────────────────────
        static void TestToBinaryAndFromBinary()
        {
            // Manual binary round-trip (the idiomatic substitute)
            var dto = new DateTimeOffset(2024, 6, 15, 12, 30, 0, TimeSpan.FromHours(5));
            long ticks = dto.Ticks;
            long offsetMinutes = (long)dto.Offset.TotalMinutes;

            var restored = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offsetMinutes));
            Debug.Assert(restored == dto, "Manual Ticks+Offset serialisation round-trip");
            Debug.Assert(restored.Offset == dto.Offset, "Offset preserved in manual round-trip");

            // UtcTicks round-trip (alternative)
            var fromUtcTicks = new DateTimeOffset(dto.UtcTicks, TimeSpan.Zero)
                                   .ToOffset(dto.Offset);
            Debug.Assert(fromUtcTicks == dto, "UtcTicks round-trip preserves UTC instant");
        }

        // ─────────────────────────────────────────────
        //  21. IComparable / IEquatable explicit coverage
        // ─────────────────────────────────────────────
        static void TestIComparableIEquatable()
        {
            var a = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var b = new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero);

            IComparable<DateTimeOffset> ic = a;
            Debug.Assert(ic.CompareTo(b) < 0, "IComparable<DateTimeOffset>.CompareTo earlier < later");

            IComparable ic2 = a;
            Debug.Assert(ic2.CompareTo(b) < 0, "IComparable.CompareTo(object)");

            IEquatable<DateTimeOffset> ie = a;
            Debug.Assert(ie.Equals(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)),
                          "IEquatable.Equals same");
            Debug.Assert(!ie.Equals(b), "IEquatable.Equals diff");
        }

        // ─────────────────────────────────────────────
        //  22. Min/Max value edge cases
        // ─────────────────────────────────────────────
        static void TestMinMaxValues()
        {
            var min = DateTimeOffset.MinValue;
            var max = DateTimeOffset.MaxValue;

            Debug.Assert(min.Year == 1 && min.Month == 1 && min.Day == 1, "MinValue date");
            Debug.Assert(max.Year == 9999 && max.Month == 12 && max.Day == 31, "MaxValue date");
            Debug.Assert(min.Offset == TimeSpan.Zero, "MinValue.Offset == Zero");
            Debug.Assert(max.Offset == TimeSpan.Zero, "MaxValue.Offset == Zero");
            Debug.Assert(min < max, "MinValue < MaxValue");

            // Arithmetic at boundaries should not throw for sensible deltas
            var nearMax = DateTimeOffset.MaxValue.AddSeconds(-1);
            Debug.Assert(nearMax < DateTimeOffset.MaxValue, "nearMax < MaxValue");

            var nearMin = DateTimeOffset.MinValue.AddSeconds(1);
            Debug.Assert(nearMin > DateTimeOffset.MinValue, "nearMin > MinValue");
        }

        // ─────────────────────────────────────────────
        //  23. Offset behaviour / ToOffset
        // ─────────────────────────────────────────────
        static void TestOffsetBehavior()
        {
            // All offsets represent the SAME instant in time
            var utc = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
            var plus3 = utc.ToOffset(TimeSpan.FromHours(3));
            var minus5 = utc.ToOffset(TimeSpan.FromHours(-5));

            // Same UTC instant
            Debug.Assert(utc == plus3, "UTC == +03:00 (same instant)");
            Debug.Assert(utc == minus5, "UTC == -05:00 (same instant)");

            // Different local times
            Debug.Assert(plus3.Hour == 15, "ToOffset(+3) → 15:00 local");
            Debug.Assert(minus5.Hour == 7, "ToOffset(-5) → 07:00 local");

            // Offset limits: ±14:00
            var maxOff = new DateTimeOffset(2024, 6, 15, 14, 0, 0, TimeSpan.FromHours(14));
            Debug.Assert(maxOff.Offset == TimeSpan.FromHours(14), "Max legal offset +14");

            var minOff = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.FromHours(-14));
            Debug.Assert(minOff.Offset == TimeSpan.FromHours(-14), "Min legal offset -14");

            // Subtract preserves relative difference
            var diff = plus3 - minus5;
            Debug.Assert(diff == TimeSpan.Zero, "Same instant − same instant == Zero");

            // ToOffset preserves UTC instant
            var shifted = utc.ToOffset(TimeSpan.FromHours(9)); // Tokyo
            Debug.Assert(shifted.UtcDateTime == utc.UtcDateTime, "ToOffset preserves UTC instant");
        }

        // ─────────────────────────────────────────────
        //  24. DateOnly / TimeOnly integration (.NET 6+)
        // ─────────────────────────────────────────────
        static void TestDateOnlyTimeOnlyIntegration()
        {
#if NET6_0_OR_GREATER
            var offset = TimeSpan.FromHours(5);
            var dto = new DateTimeOffset(2024, 6, 15, 10, 30, 45, 250, offset);

            // DateOnly / TimeOnly from DateTimeOffset's DateTime component
            var dateOnly = DateOnly.FromDateTime(dto.DateTime);
            var timeOnly = TimeOnly.FromDateTime(dto.DateTime);

            Debug.Assert(dateOnly.Year == 2024 && dateOnly.Month == 6 && dateOnly.Day == 15,
                         "DateOnly from DTO.DateTime");
            Debug.Assert(timeOnly.Hour == 10 && timeOnly.Minute == 30 && timeOnly.Second == 45,
                         "TimeOnly from DTO.DateTime");

            // Reconstruct DateTimeOffset from DateOnly + TimeOnly + offset
            var backDt = dateOnly.ToDateTime(timeOnly, DateTimeKind.Unspecified);
            var backDto = new DateTimeOffset(backDt, offset);
            Debug.Assert(backDto.Year == dto.Year, "Reconstructed year");
            Debug.Assert(backDto.Month == dto.Month, "Reconstructed month");
            Debug.Assert(backDto.Day == dto.Day, "Reconstructed day");
            Debug.Assert(backDto.Hour == dto.Hour, "Reconstructed hour");
            Debug.Assert(backDto.Minute == dto.Minute, "Reconstructed minute");
            Debug.Assert(backDto.Offset == offset, "Reconstructed offset");
#endif
        }
    }
}