namespace BusinessLogic.Exceptions;

/// <summary>The request conflicts with existing state (mapped to HTTP 409).</summary>
public class ConflictException(string message) : Exception(message);
