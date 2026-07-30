using Scrubbler.Plugin.Scrobblers.FileParseScrobbler.Parser.CSV;
using Scrubbler.Plugin.Scrobblers.FileParseScrobbler.Parser.JSON;
using Scrubbler.PluginBase.Settings;

namespace Scrubbler.Plugin.Scrobblers.FileParseScrobbler;

internal sealed class PluginSettings : IPluginSettings
{
    public const double DefaultScrobbleGapSeconds = 3;

    public CsvFileParserConfiguration CsvConfig { get; set; } = CsvFileParserConfiguration.Default;

    public JsonFileParserConfiguration JsonConfig { get; set; } = JsonFileParserConfiguration.Default;

    public double ScrobbleGapSeconds { get; set; } = DefaultScrobbleGapSeconds;
}
