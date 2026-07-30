# First-party plugins

All first-party plugin source is maintained in this directory. The projects use direct project references to the shared bases, so changes to a plugin, `Scrubbler.PluginBase`, or `Scrubbler.MediaPlayerScrobblerBase` are compiled together.

The runtime architecture is still modular: each concrete `Scrubbler.Plugin.*` project builds as its own assembly. Debug builds copy concrete plugin outputs to `Scrubbler/DebugPlugins`, which the `Local Plugins` launch profile loads.

The complete project graph is in `../Scrubbler.sln`. Because the Apple Music plugin uses Windows UI Automation, Linux and macOS development uses `../Scrubbler.CrossPlatform.slnf`.

To add another first-party plugin:

1. Create its project directly under this directory.
2. Reference the shared base project or projects with `ProjectReference`.
3. Add the project and its tests to `../Scrubbler.sln`.
4. If it is cross-platform, also add it to `../Scrubbler.CrossPlatform.slnf`.

No sibling plugin checkout or locally published plugin-base package is required.
