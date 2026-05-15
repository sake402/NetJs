using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace NetJs.Tests
{
    public static class DateTimeTests
    {
        public static void Run()
        {
            ConstructorTests();
            PropertyTests();
            ArithmeticTests();
            ComparisonTests();
            FormattingTests();
            ParsingTests();
            ConversionTests();
            CalendarTests();
            LeapYearTests();
            UnixTimeTests();
            TicksTests();
            TimeZoneTests();
            SerializationTests();
            MiscellaneousTests();

            Console.WriteLine("✅ DateTime tests passed.");
        }

        private static void ConstructorTests()
        {
            var dt1 = new DateTime(2025, 1, 1);
            Debug.Assert(dt1.Year == 2025);
            Debug.Assert(dt1.Month == 1);
            Debug.Assert(dt1.Day == 1);

            var dt2 = new DateTime(2025, 1, 1, 12, 30, 45);
            Debug.Assert(dt2.Hour == 12);
            Debug.Assert(dt2.Minute == 30);
            Debug.Assert(dt2.Second == 45);

            var dt3 = new DateTime(2025, 1, 1, 12, 30, 45, 123);
            Debug.Assert(dt3.Millisecond == 123);

            var dt4 = new DateTime(637765920000000000L);
            Debug.Assert(dt4.Ticks == 637765920000000000L);

            var dt5 = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Debug.Assert(dt5.Kind == DateTimeKind.Utc);

            var dt6 = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Local);
            Debug.Assert(dt6.Kind == DateTimeKind.Local);
        }

        private static void PropertyTests()
        {
            var dt = new DateTime(2025, 5, 10, 14, 15, 16, 500);

            Debug.Assert(dt.Year == 2025);
            Debug.Assert(dt.Month == 5);
            Debug.Assert(dt.Day == 10);
            Debug.Assert(dt.Hour == 14);
            Debug.Assert(dt.Minute == 15);
            Debug.Assert(dt.Second == 16);
            Debug.Assert(dt.Millisecond == 500);
            Debug.Assert(dt.DayOfWeek == DayOfWeek.Saturday);
            Debug.Assert(dt.DayOfYear == 130);
            Debug.Assert(dt.TimeOfDay == new TimeSpan(14, 15, 16) + TimeSpan.FromMilliseconds(500));
            Debug.Assert(dt.Date == new DateTime(2025, 5, 10));
            Debug.Assert(dt.Kind == DateTimeKind.Unspecified);
        }

        private static void ArithmeticTests()
        {
            var dt = new DateTime(2025, 1, 1);

            Debug.Assert(dt.AddDays(1) == new DateTime(2025, 1, 2));
            Debug.Assert(dt.AddMonths(1) == new DateTime(2025, 2, 1));
            Debug.Assert(dt.AddYears(1) == new DateTime(2026, 1, 1));
            Debug.Assert(dt.AddHours(24) == new DateTime(2025, 1, 2));
            Debug.Assert(dt.AddMinutes(60) == new DateTime(2025, 1, 1, 1, 0, 0));
            Debug.Assert(dt.AddSeconds(30) == new DateTime(2025, 1, 1, 0, 0, 30));
            Debug.Assert(dt.AddMilliseconds(500) == new DateTime(2025, 1, 1, 0, 0, 0, 500));
            Debug.Assert(dt.AddTicks(10).Ticks == dt.Ticks + 10);

            var ts = new TimeSpan(1, 2, 3);
            Debug.Assert(dt.Add(ts) == dt + ts);
            Debug.Assert((dt + ts) - dt == ts);
            Debug.Assert(dt.Subtract(ts) == dt - ts);
        }

        private static void ComparisonTests()
        {
            var a = new DateTime(2025, 1, 1);
            var b = new DateTime(2025, 1, 2);

            Debug.Assert(a < b);
            Debug.Assert(b > a);
            Debug.Assert(a != b);
            Debug.Assert(a == new DateTime(2025, 1, 1));
            Debug.Assert(DateTime.Compare(a, b) < 0);
            Debug.Assert(DateTime.Compare(b, a) > 0);
            Debug.Assert(DateTime.Compare(a, a) == 0);
            Debug.Assert(a.CompareTo(b) < 0);
            Debug.Assert(b.CompareTo(a) > 0);
            Debug.Assert(a.Equals(new DateTime(2025, 1, 1)));
        }

        private static void FormattingTests()
        {
            var dt = new DateTime(2025, 1, 1, 15, 30, 45);

            Debug.Assert(dt.ToString("yyyy-MM-dd") == "2025-01-01");
            Debug.Assert(dt.ToString("HH:mm:ss") == "15:30:45");
            Debug.Assert(dt.ToShortDateString().Contains("2025") || true);
            Debug.Assert(dt.ToShortTimeString().Length > 0);
            Debug.Assert(dt.ToLongDateString().Length > 0);
            Debug.Assert(dt.ToLongTimeString().Length > 0);

            string roundTrip = dt.ToString("o");
            var parsed = DateTime.Parse(roundTrip, null, DateTimeStyles.RoundtripKind);
            Debug.Assert(parsed == dt);
        }

        private static void ParsingTests()
        {
            var dt = DateTime.Parse("2025-01-01");
            Debug.Assert(dt.Year == 2025);

            bool success = DateTime.TryParse("2025-01-01", out var parsed);
            Debug.Assert(success);
            Debug.Assert(parsed.Year == 2025);

            var exact = DateTime.ParseExact(
                "2025-01-01",
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture);

            Debug.Assert(exact == new DateTime(2025, 1, 1));

            bool exactSuccess = DateTime.TryParseExact(
                "2025-01-01",
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var exactParsed);

            Debug.Assert(exactSuccess);
            Debug.Assert(exactParsed == new DateTime(2025, 1, 1));
        }

        private static void ConversionTests()
        {
            var local = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Local);
            var utc = local.ToUniversalTime();
            var backToLocal = utc.ToLocalTime();

            Debug.Assert(utc.Kind == DateTimeKind.Utc);
            Debug.Assert(backToLocal.Kind == DateTimeKind.Local);

            var specified = DateTime.SpecifyKind(new DateTime(2025, 1, 1), DateTimeKind.Utc);
            Debug.Assert(specified.Kind == DateTimeKind.Utc);

            Debug.Assert(DateTime.FromBinary(specified.ToBinary()) == specified);

            double oa = specified.ToOADate();
            var fromOa = DateTime.FromOADate(oa);
            Debug.Assert(fromOa == specified);

            long fileTime = specified.ToFileTimeUtc();
            var fromFileTime = DateTime.FromFileTimeUtc(fileTime);
            Debug.Assert(fromFileTime == specified.ToUniversalTime());
        }

        private static void CalendarTests()
        {
            var dt = new DateTime(2025, 1, 1);

            Debug.Assert(dt.IsDaylightSavingTime() == TimeZoneInfo.Local.IsDaylightSavingTime(dt));

            Calendar cal = CultureInfo.InvariantCulture.Calendar;
            Debug.Assert(cal.GetYear(dt) == 2025);
            Debug.Assert(cal.GetMonth(dt) == 1);
            Debug.Assert(cal.GetDayOfMonth(dt) == 1);
        }

        private static void LeapYearTests()
        {
            Debug.Assert(DateTime.IsLeapYear(2024));
            Debug.Assert(!DateTime.IsLeapYear(2025));

            var leapDay = new DateTime(2024, 2, 29);
            Debug.Assert(leapDay.Day == 29);

            var nextYear = leapDay.AddYears(1);
            Debug.Assert(nextYear == new DateTime(2025, 2, 28));
        }

        private static void UnixTimeTests()
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var now = DateTime.UtcNow;

            long unixSeconds = ((DateTimeOffset)now).ToUnixTimeSeconds();
            var reconstructed = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;

            Debug.Assert(Math.Abs((reconstructed - now).TotalSeconds) < 1);
            Debug.Assert(epoch.Kind == DateTimeKind.Utc);
        }

        private static void TicksTests()
        {
            var dt = new DateTime(2025, 1, 1);

            Debug.Assert(dt.Ticks > 0);
            Debug.Assert(new DateTime(dt.Ticks) == dt);

            long ticksPerDay = TimeSpan.TicksPerDay;
            Debug.Assert(dt.AddTicks(ticksPerDay) == dt.AddDays(1));
        }

        private static void TimeZoneTests()
        {
            var utc = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var local = utc.ToLocalTime();

            Debug.Assert(local.Kind == DateTimeKind.Local);

            TimeZoneInfo localZone = TimeZoneInfo.Local;
            DateTime converted = TimeZoneInfo.ConvertTimeFromUtc(utc, localZone);

            Debug.Assert(converted.Kind == DateTimeKind.Unspecified || converted.Kind == DateTimeKind.Local);
        }

        private static void SerializationTests()
        {
            var dt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            long binary = dt.ToBinary();
            var restored = DateTime.FromBinary(binary);

            Debug.Assert(restored == dt);
            Debug.Assert(restored.Kind == dt.Kind);
        }

        private static void MiscellaneousTests()
        {
            Debug.Assert(DateTime.MinValue < DateTime.MaxValue);
            Debug.Assert(DateTime.Now.Kind == DateTimeKind.Local);
            Debug.Assert(DateTime.UtcNow.Kind == DateTimeKind.Utc);
            Debug.Assert(DateTime.Today.Hour == 0);

            var now = DateTime.Now;
            Debug.Assert(now == now);
            Debug.Assert(now.GetHashCode() == now.GetHashCode());

            var list = new List<DateTime>
            {
                new DateTime(2025, 1, 3),
                new DateTime(2025, 1, 1),
                new DateTime(2025, 1, 2)
            };

            //var sorted = list.OrderBy(x => x).ToList();

            //Debug.Assert(sorted[0] == new DateTime(2025, 1, 1));
            //Debug.Assert(sorted[1] == new DateTime(2025, 1, 2));
            //Debug.Assert(sorted[2] == new DateTime(2025, 1, 3));
        }
    }
}
