using System;

namespace Window
{
    /// <summary>
    /// Geolocation API.
    /// </summary>
    [NetJs.External]
    public class Geolocation
    {
        public extern long watchPosition(Action<GeolocationPosition> success, Action<object>? error = null, object? options = null);
        public extern void clearWatch(long id);
        public extern void getCurrentPosition(Action<GeolocationPosition> success, Action<object>? error = null, object? options = null);
    }

    [NetJs.External]
    public class GeolocationPosition
    {
        public extern GeolocationCoordinates coords { get; }
        public extern long timestamp { get; }
    }

    [NetJs.External]
    public class GeolocationCoordinates
    {
        public extern double latitude { get; }
        public extern double longitude { get; }
        public extern double accuracy { get; }
        public extern double? altitude { get; }
        public extern double? altitudeAccuracy { get; }
        public extern double? heading { get; }
        public extern double? speed { get; }
    }
}