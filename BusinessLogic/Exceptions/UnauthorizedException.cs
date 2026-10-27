namespace BusinessLogic.Exceptions;

/// <summary>Authentication failed (mapped to HTTP 401).</summary>
public class UnauthorizedException(string message) : Exception(message);
