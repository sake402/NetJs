namespace NetJs
{
    /// <summary>
    /// The Error constructor creates an error object. Instances of Error objects are thrown when runtime errors occur. The Error object can also be used as a base objects for user-defined exceptions. See below for standard built-in error types.
    /// </summary>
    [External]
    [Name("Error")]
    public class Error
    {
        public extern Error();
        public extern Error(string message);
        public string message = default!;
        public string name = default!;
        public string stack = default!;

        public string fileName = default!;
        public int lineNumber;
        public int columnNumber;
    }

    [External]
    [Name("TypeError")]
    public class TypeError : Error
    {

    }


    [External]
    [Name("RangeError")]
    public class RangeError : Error
    {

    }
}