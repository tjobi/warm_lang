namespace WarmLangCompiler.Utils;

public static class DefaultRuntimeConfig
{
    private static readonly string DEFAULT_CONFIG = 
"""
{
    "runtimeOptions": {
      "tfm": "net7.0",
      "framework": {
        "name": "Microsoft.NETCore.App",
        "version": "7.0.0"
      }
    }
}
""";

    public static void Write(string outName)
    {
        var configPath = Path.GetFileNameWithoutExtension(outName) + ".runtimeconfig.json";
        using var writer = new StreamWriter(configPath);
        writer.Write(DEFAULT_CONFIG);
    } 
}