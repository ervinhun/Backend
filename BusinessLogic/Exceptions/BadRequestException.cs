namespace BusinessLogic.Exceptions;

/// <summary>The request is invalid in a way its data annotations can't express (mapped to HTTP 400).</summary>
public class BadRequestException(string message) : Exception(message);
