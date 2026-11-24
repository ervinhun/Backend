namespace BusinessLogic.Exceptions;

public class FeatureNotEnabledException(string feature) : Exception($"'{feature}' feature is not enabled at the moment.");