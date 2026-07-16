namespace ManagedMidi.Smf;

public class SmfParserException : Exception
{
    public SmfParserException() : this("SMF parser error") { }
    public SmfParserException(string message) : base(message) { }
    public SmfParserException(string message, Exception innerException) : base(message, innerException) { }
}
