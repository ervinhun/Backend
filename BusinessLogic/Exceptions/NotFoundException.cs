namespace BusinessLogic.Exceptions;

/// <summary>The requested resource does not exist (mapped to HTTP 404).</summary>
public class NotFoundException(string message) : Exception(message);
