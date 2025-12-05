using System.Reflection;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests.Helpers;

public static class EmbeddedResourceHelper
{
    private static Stream GetResourceStream(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var fullResourceName = $"AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests.Resources.{resourceName}";

        var stream = assembly.GetManifestResourceStream(fullResourceName);
        if (stream is not null) return stream;
        var available = string.Join(", ", assembly.GetManifestResourceNames());
        throw new InvalidOperationException(
            $"Resource '{fullResourceName}' not found. Available resources: {available}");
    }

    public static string ExtractToTempFile(string resourceName, string tempDir)
    {
        var targetPath = Path.Combine(tempDir, resourceName);
        using var resourceStream = GetResourceStream(resourceName);
        using var fileStream = File.Create(targetPath);
        resourceStream.CopyTo(fileStream);
        return targetPath;
    }
}