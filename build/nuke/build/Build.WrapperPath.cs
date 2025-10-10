using System;
using Lunar.Build.Configuration;
using Nuke.Common;
using Nuke.Common.IO;
using Serilog;

#nullable enable

namespace WingedBean.Console.MNuke
{
    /// <summary>
    /// Build partial class implementing IWrapperPathComponent interface
    /// Handles wrapper script path detection and configuration discovery
    /// Supports both Console and Framework projects
    /// </summary>
    partial class Build
    {
        /// <summary>
        /// Project-specific configuration identifiers for validating build-config.json files
        /// Includes identifiers for both console and framework builds
        /// </summary>
        public string[] ProjectConfigIdentifiers => new[]
        {
            "winged-bean.console",
            "console-dungeon",
            "winged-bean.framework",
            "yokan-framework"
        };

        /// <summary>
        /// NUKE root directory override from wrapper script parameter
        /// </summary>
        [Parameter("NUKE root directory override from wrapper script", Name = "wrapper-nuke-root")]
        readonly string? _wrapperNukeRootParam;

        /// <summary>
        /// Config path override from wrapper script parameter
        /// </summary>
        [Parameter("Config path override from wrapper script", Name = "wrapper-config-path")]
        readonly string? _wrapperConfigPathParam;

        /// <summary>
        /// Script directory from wrapper script parameter
        /// </summary>
        [Parameter("Script directory from wrapper script", Name = "wrapper-script-dir")]
        readonly string? _wrapperScriptDirParam;

        /// <summary>
        /// Exposed wrapper parameters for interface implementation
        /// </summary>
        public string? WrapperNukeRootParam => _wrapperNukeRootParam;
        public string? WrapperConfigPathParam => _wrapperConfigPathParam;
        public string? WrapperScriptDirParam => _wrapperScriptDirParam;
    }
}
