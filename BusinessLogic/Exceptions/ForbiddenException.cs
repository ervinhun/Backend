namespace BusinessLogic.Exceptions;

/// <summary>The caller is known but not allowed to do this (mapped to HTTP 403).</summary>
public class ForbiddenException(string message) : Exception(message);
