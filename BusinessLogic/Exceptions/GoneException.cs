namespace BusinessLogic.Exceptions;

/// <summary>The requested resource was removed for good (mapped to HTTP 410).</summary>
public class GoneException(string message) : Exception(message);
