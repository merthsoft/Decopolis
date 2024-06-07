using System;
using System.Collections.Generic;
using System.Linq;

namespace Merthsoft.DynamicConfig {
    /// <summary>
    /// Options that control how the configuration is handled.
    /// </summary>
    public class ConfigOptions {
        /// <summary>
        /// Gets or sets if member access is case sensitive.
        /// </summary>
        public bool CaseSensitive { get; set; } = true;

        /// <summary>
        /// Gets or sets if null is returned when a member cannot be found.
        /// </summary>
        public bool ReturnNullWhenNotFound { get; set; } = false;

        /// <summary>
        /// The default configuration options.
        /// </summary>
        public static ConfigOptions Default = new ConfigOptions();
    }
}
