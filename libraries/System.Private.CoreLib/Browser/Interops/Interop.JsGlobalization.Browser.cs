using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

internal static partial class Interop
{
    internal static unsafe partial class JsGlobalization
    {
        [NetJs.MemberReplace(nameof(GetLocaleInfo))]
        internal static unsafe nint GetLocaleInfoImpl(char* locale, int localeLength, char* culture, int cultureLength, char* buffer, int bufferLength, out int resultLength)
        {
            var cName = new string(locale, 0, localeLength);
            var jlocale = NetJs.Script.Write<Locale>("new Intl.Locale(cName)");
            var language = NetJs.Script.Write<string>("new Intl.DisplayNames([jlocale.language], { type: 'language' }).of(jlocale.region??'US')");
            var country = NetJs.Script.Write<string>("new Intl.DisplayNames([jlocale.language], { type: 'region' }).of(jlocale.region??'US')");
            var str = $"{jlocale.language}##{country}";//TODO add ##{countryname} => $"{jlocale.language}##{countryname}"
            var reff = Unsafe.AsPointer(in str.GetPinnableReference());
            Unsafe.CopyBlockFinal(buffer, reff, str.Length.As<nuint>() * 2);
            resultLength = str.Length;
            return 0;
        }

    }
}
