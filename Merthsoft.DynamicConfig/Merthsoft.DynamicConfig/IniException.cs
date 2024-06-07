using System;
using System.Collections.Generic;
using System.Linq;

namespace Merthsoft.DynamicConfig {
    /// <summary>
    /// The exception that is thrown when an INI file is invalid.
    /// </summary>
    [Serializable]
    public class IniException : Exception {
        /// <summary>
        /// Gets the line number where the error occurred.
        /// </summary>
        public int LineNumber { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the LineNumber class with a specified line number.
        /// </summary>
        /// <param name="lineNumber">The line number where the error occurred.</param>
        public IniException(int lineNumber) { LineNumber = lineNumber; }

        /// <summary>
        /// Initializes a new instance of the LineNumber class with a specified line number and error message.
        /// </summary>
        /// <param name="message">The error description.</param>
        /// <param name="lineNumber">The line number where the error occurred.</param>
        public IniException(string message, int lineNumber) : base(message) { LineNumber = lineNumber; }

        /// <summary>
        /// Initializes a new instance of the LineNumber class with a specified line number, inner exception, and error message.
        /// </summary>
        /// <param name="message">The error description.</param>
        /// <param name="inner">The Exception that threw the XmlException, if any. This value can be null.</param>
        /// <param name="lineNumber">The line number where the error occurred.</param>
        public IniException(string message, Exception inner, int lineNumber) : base(message, inner) { LineNumber = lineNumber; }

        /// <summary>
        /// Initializes a new instance of the LineNumber class using the information in the SerializationInfo and StreamingContext objects.
        /// </summary>
        /// <param name="info">The SerializationInfo object containing all the properties of an XmlException. </param>
        /// <param name="context">The StreamingContext object containing the context information. </param>
        protected IniException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
