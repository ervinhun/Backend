namespace DataAccess.Exceptions;

/// <summary>A write violated a UNIQUE constraint (e.g. two concurrent registrations with one email).</summary>
public class DuplicateKeyException(string message, Exception inner) : Exception(message, inner);
