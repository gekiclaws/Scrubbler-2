using Scrubbler.Host.Services;

namespace Scrubbler.Test.Services;

[TestFixture]
internal sealed class PluginManagerTests
{
    private string _rootDirectory = null!;
    private string _shadowDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), $"scrubbler-plugin-tests-{Guid.NewGuid():N}");
        _shadowDirectory = Path.Combine(_rootDirectory, ".shadow");

        Directory.CreateDirectory(_rootDirectory);
        Directory.CreateDirectory(_shadowDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_rootDirectory))
            Directory.Delete(_rootDirectory, recursive: true);
    }

    [Test]
    public void GetPluginAssemblyPaths_RecursiveDiscovery_ExcludesShadowCopies()
    {
        var pluginDirectory = Path.Combine(_rootDirectory, "manual-scrobbler");
        var shadowCopyDirectory = Path.Combine(_shadowDirectory, Guid.NewGuid().ToString("N"));
        var similarlyNamedDirectory = Path.Combine(_rootDirectory, ".shadow-copy");
        Directory.CreateDirectory(pluginDirectory);
        Directory.CreateDirectory(shadowCopyDirectory);
        Directory.CreateDirectory(similarlyNamedDirectory);

        var pluginAssembly = CreatePluginAssembly(pluginDirectory, "Scrubbler.Plugin.ManualScrobbler.dll");
        CreatePluginAssembly(shadowCopyDirectory, "Scrubbler.Plugin.ManualScrobbler.dll");
        var similarlyNamedAssembly = CreatePluginAssembly(similarlyNamedDirectory, "Scrubbler.Plugin.Other.dll");

        var result = PluginManager.GetPluginAssemblyPaths(_rootDirectory, _shadowDirectory, recursive: true);

        Assert.That(result, Is.EquivalentTo(new[] { pluginAssembly, similarlyNamedAssembly }));
    }

    [Test]
    public void GetPluginAssemblyPaths_ReturnsSnapshotBeforeShadowCopiesAreCreated()
    {
        var pluginDirectory = Path.Combine(_rootDirectory, "manual-scrobbler");
        Directory.CreateDirectory(pluginDirectory);
        var pluginAssembly = CreatePluginAssembly(pluginDirectory, "Scrubbler.Plugin.ManualScrobbler.dll");

        var result = PluginManager.GetPluginAssemblyPaths(_rootDirectory, _shadowDirectory, recursive: true);

        var shadowCopyDirectory = Path.Combine(_shadowDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(shadowCopyDirectory);
        CreatePluginAssembly(shadowCopyDirectory, "Scrubbler.Plugin.ManualScrobbler.dll");

        Assert.That(result, Is.EqualTo(new[] { pluginAssembly }));
    }

    private static string CreatePluginAssembly(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        File.WriteAllBytes(path, []);
        return path;
    }
}
