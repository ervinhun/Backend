using FeatureHubSDK;
using IO.FeatureHub.SSE.Model;

namespace FeatureHubHelper;

public class FeatureStateProvider : IFeatureFlags
{
   private readonly EdgeFeatureHubConfig _config;

   public FeatureStateProvider()
   {
      var hubUrl = Environment.GetEnvironmentVariable("FEATUREHUB_URL");
      var apiKey = Environment.GetEnvironmentVariable("FEATUREHUB_API_KEY");
      if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(hubUrl))
      {
         throw new InvalidOperationException("Feature Hub key or url is missing");
      }
      var config = new EdgeFeatureHubConfig(hubUrl, apiKey);
      config.Init().Wait();
      _config = config;
   }

   public async Task<bool> IsEnabledAsync(string featureKey, string? requestId = null, string? countryName = null, string? platform = null, string? version = null, string? planTier = null)
   {
      var builder = _config.NewContext();

      // Percentage rollout
      if (requestId != null)
      {
         builder.UserKey(requestId);
      }
      
      // Country rollout
      if (Enum.TryParse<StrategyAttributeCountryName>(countryName, ignoreCase: true, out var country) && Enum.IsDefined(country))
      {
         builder.Country(country);
      }

      // Platform rollout (eg.: Android vs iOS)
      if (Enum.TryParse<StrategyAttributePlatformName>(platform, ignoreCase: true, out var platformName) && Enum.IsDefined(platformName))
      {
         builder.Platform(platformName);
      }

      // Version rollout (eg.: above version 1.4 only)
      if (version != null)
      {
         builder.Version(version);
      }

      // Tier rollout
      if (planTier != null)
      {
         builder.Attr("plan_tier", planTier);
      }

      var context = await builder.Build();
      return context[featureKey].IsEnabled;
   }
}