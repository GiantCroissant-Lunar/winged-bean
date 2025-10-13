using System;
using Lunar.Build.Configuration;
using Nuke.Common;
using Nuke.Common.IO;

#nullable enable

namespace WingedBean.Console.MNuke
{
    /// <summary>
    /// Build partial class implementing IBuildConfigurationComponent interface
    /// Handles build configuration management and path resolution
    /// Supports both Console and Framework builds
    /// </summary>
    partial class Build
    {
        /// <summary>
        /// Path to build-config.json using wrapper path resolution
        /// Selects console or framework config based on --project parameter
        /// </summary>
        public string BuildConfigPath
        {
            get
            {
                var configFile = Project?.ToLower() == "framework"
                    ? "build-config.framework.json"
                    : "build-config.json";

                return ((IWrapperPathComponent)this).EffectiveRootDirectory / configFile;
            }
        }
    }
}
