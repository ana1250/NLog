//
// Copyright (c) 2004-2024 Jaroslaw Kowalski <jaak@jkowalski.net>, Kim Christensen, Julian Verdurmen
//
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
//
// * Redistributions of source code must retain the above copyright notice,
//   this list of conditions and the following disclaimer.
//
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// * Neither the name of Jaroslaw Kowalski nor the names of its
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
// THE POSSIBILITY OF SUCH DAMAGE.
//

namespace NLog.LayoutRenderers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Text;
    using System.Xml;
    using NLog.Common;
    using NLog.Config;
    using NLog.Layouts;
    using NLog.Targets;
    using NLog.Targets.Internal;

    /// <summary>
    /// XML event description compatible with log4j, Chainsaw and NLogViewer.
    /// </summary>
    /// <remarks>
    /// <a href="https://github.com/NLog/NLog/wiki/Log4JXMLEvent-Layout-Renderer">See NLog Wiki</a>
    /// </remarks>
    /// <seealso href="https://github.com/NLog/NLog/wiki/Log4JXMLEvent-Layout-Renderer">Documentation on NLog Wiki</seealso>
    [LayoutRenderer("log4jxmlevent")]
    public class Log4JXmlEventLayoutRenderer : LayoutRenderer, IUsesStackTrace
    {
        private static readonly DateTime log4jDateBase = new DateTime(1970, 1, 1);

        private static readonly string dummyNamespace = "https://nlog-project.org/dummynamespace/" + Guid.NewGuid();
        private static readonly string dummyNamespaceRemover = " xmlns:log4j=\"" + dummyNamespace + "\"";

        private readonly ScopeContextNestedStatesLayoutRenderer _scopeNestedLayoutRenderer = new ScopeContextNestedStatesLayoutRenderer();
        private  Log4JXmlEventLayout _log4JXmlEventLayout;


        /// <summary>
        /// Initializes a new instance of the <see cref="Log4JXmlEventLayoutRenderer" /> class.
        /// </summary>
        public Log4JXmlEventLayoutRenderer()
        {
            AppInfo = string.Format(
                CultureInfo.InvariantCulture,
                "{0}({1})",
                AppDomain.CurrentDomain.FriendlyName,
                System.Diagnostics.Process.GetCurrentProcess().Id);

            Parameters = new List<Log4JXmlEventParameter>();

            try
            {
                _machineName = Environment.MachineName;
                if (string.IsNullOrEmpty(_machineName))
                {
                    InternalLogger.Info("MachineName is not available.");
                }
            }
            catch (Exception exception)
            {
                InternalLogger.Error(exception, "Error getting machine name.");
                if (LogManager.ThrowExceptions)
                {
                    throw;
                }

                _machineName = string.Empty;
            }
        }

        /// <inheritdoc/>
        protected override void InitializeLayoutRenderer()
        {
            base.InitializeLayoutRenderer();
        }

        internal void SetLayout(Log4JXmlEventLayout layout)
        {
            _log4JXmlEventLayout = layout;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to include NLog-specific extensions to log4j schema.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        [Obsolete("Non standard extension to the Log4j-XML format. Marked obsolete with NLog 6.0")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IncludeNLogData { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the XML should use spaces for indentation.
        /// </summary>
        /// <docgen category='Layout Options' order='50' />
        public bool IndentXml { get; set; }

        /// <summary>
        /// Gets or sets the log4j:event logger-xml-attribute. Default: ${logger}
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        public Layout LoggerName { get; set; }

        /// <summary>
        /// Gets or sets the log4j:event message-xml-element. Default: ${message}
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        public Layout FormattedMessage { get; set; }

        /// <summary>
        /// Gets or sets the log4j:event log4japp-xml-element. By default it's the friendly name of the current AppDomain.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        public Layout AppInfo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include call site (class and method name) in the information sent over the network.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        public bool IncludeCallSite { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include source info (file name and line number) in the information sent over the network.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        public bool IncludeSourceInfo { get; set; }

        /// <summary>
        /// Obsolete and replaced by <see cref="IncludeScopeProperties"/> with NLog v5.
        ///
        /// Gets or sets a value indicating whether to include contents of the <see cref="MappedDiagnosticsContext"/> dictionary.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        [Obsolete("Replaced by IncludeScopeProperties. Marked obsolete on NLog 5.0")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IncludeMdc { get => _includeMdc ?? false; set => _includeMdc = value; }
        private bool? _includeMdc;

        /// <summary>
        /// Obsolete and replaced by <see cref="IncludeScopeProperties"/> with NLog v5.
        ///
        /// Gets or sets a value indicating whether to include contents of the <see cref="MappedDiagnosticsLogicalContext"/> dictionary.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        [Obsolete("Replaced by IncludeScopeProperties. Marked obsolete on NLog 5.0")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IncludeMdlc { get => _includeMdlc ?? false; set => _includeMdlc = value; }
        private bool? _includeMdlc;

        /// <summary>
        /// Obsolete and replaced by <see cref="IncludeNdc"/> with NLog v5.
        ///
        /// Gets or sets a value indicating whether to include contents of the <see cref="NestedDiagnosticsLogicalContext"/> stack.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        [Obsolete("Replaced by IncludeNdc. Marked obsolete on NLog 5.0")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IncludeNdlc { get => _includeNdlc ?? false; set => _includeNdlc = value; }
        private bool? _includeNdlc;

        /// <summary>
        /// Gets or sets whether to include log4j:NDC in output from <see cref="ScopeContext"/> nested context.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        public bool IncludeNdc { get => _includeNdc ?? false; set => _includeNdc = value; }
        private bool? _includeNdc;

        /// <summary>
        /// Gets or sets whether to include the contents of the <see cref="ScopeContext"/> properties-dictionary.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        public bool IncludeScopeProperties { get => _includeScopeProperties ?? (_includeMdlc == true || _includeMdc == true); set => _includeScopeProperties = value; }
        private bool? _includeScopeProperties;

        /// <summary>
        /// Gets or sets whether to include log4j:NDC in output from <see cref="ScopeContext"/> nested context.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        public bool IncludeScopeNested { get => _includeScopeNested ?? (_includeNdlc == true || _includeNdc == true); set => _includeScopeNested = value; }
        private bool? _includeScopeNested;

        /// <summary>
        /// Gets or sets the stack separator for log4j:NDC in output from <see cref="ScopeContext"/> nested context.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        public string ScopeNestedSeparator
        {
            get => _scopeNestedLayoutRenderer.Separator;
            set => _scopeNestedLayoutRenderer.Separator = value;
        }

        /// <summary>
        /// Obsolete and replaced by <see cref="NdcItemSeparator"/> with NLog v5.
        ///
        /// Gets or sets the stack separator for log4j:NDC in output from <see cref="ScopeContext"/> nested context.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        [Obsolete("Replaced by NdcItemSeparator. Marked obsolete on NLog 5.0")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string NdlcItemSeparator { get => ScopeNestedSeparator; set => ScopeNestedSeparator = value; }

        /// <summary>
        /// Obsolete and replaced by <see cref="IncludeEventProperties"/> with NLog v5.
        ///
        /// Gets or sets the option to include all properties from the log events
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        [Obsolete("Replaced by IncludeEventProperties. Marked obsolete on NLog 5.0")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IncludeAllProperties { get => IncludeEventProperties; set => IncludeEventProperties = value; }

        /// <summary>
        /// Gets or sets the option to include all properties from the log events
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        public bool IncludeEventProperties { get; set; } = true;

        /// <summary>
        /// Gets or sets the stack separator for log4j:NDC in output from <see cref="ScopeContext"/> nested context.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        public string NdcItemSeparator { get => ScopeNestedSeparator; set => ScopeNestedSeparator = value; }

        /// <summary>
        ///  Gets or sets whether the log4j:throwable xml-element should be written as CDATA
        /// </summary>
        /// <docgen category='Layout Options' order='50' />
        public bool WriteThrowableCData { get; set; }

        private readonly string _machineName;

        /// <inheritdoc/>
        StackTraceUsage IUsesStackTrace.StackTraceUsage
        {
            get
            {
                if (IncludeSourceInfo)
                    return StackTraceUsage.WithCallSite | StackTraceUsage.WithCallSiteClassName | StackTraceUsage.WithSource;
                else if (IncludeCallSite)
                    return StackTraceUsage.WithCallSite | StackTraceUsage.WithCallSiteClassName;
                else
                    return StackTraceUsage.None;
            }
        }

        internal IList<Log4JXmlEventParameter> Parameters { get; set; }

        internal void AppendBuilder(LogEventInfo logEvent, StringBuilder builder)
        {
            Append(builder, logEvent);
        }

        /// <inheritdoc/>
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            _log4JXmlEventLayout.Render(logEvent, builder);
        }
    }
}
