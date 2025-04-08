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

namespace NLog.Layouts
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using NLog.Config;
    using NLog.LayoutRenderers;

    /// <summary>
    /// A specialized layout that renders Log4j-compatible XML events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This layout is not meant to be used explicitly. Instead you can use ${log4jxmlevent} layout renderer.
    /// </para>
    /// <a href="https://github.com/NLog/NLog/wiki/Log4JXmlEventLayout">See NLog Wiki</a>
    /// </remarks>
    /// <seealso href="https://github.com/NLog/NLog/wiki/Log4JXmlEventLayout">Documentation on NLog Wiki</seealso>
    [Layout("Log4JXmlEventLayout")]
    [ThreadAgnostic]
    public class Log4JXmlEventLayout : XmlLayout
    {

        private bool _includeCallSite;
        private bool _includeSourceInfo;
        private readonly XmlElement _locationElement;

        /// <summary>
        /// Initializes a new instance of the <see cref="Log4JXmlEventLayout" /> class.
        /// </summary>
        public Log4JXmlEventLayout() : base("event", null)
        {
            Renderer = new Log4JXmlEventLayoutRenderer();
            // Configure default layouts
            Attributes.Add(new XmlAttribute("logger", "${logger}"));
            Attributes.Add(new XmlAttribute("level", "${level:uppercase=true}"));
            Attributes.Add(new XmlAttribute("timestamp", "${date:format=UNIXMS}"));
            Attributes.Add(new XmlAttribute("thread", "${threadid}"));

            // Add core elements
            Elements.Add(new XmlElement("message", "${message}"));
            Elements.Add(new XmlElement("throwable", "${exception:format=ToString}"));
            Elements.Add(new XmlElement("data", "${appdomain:format={1\\}}")
            {
                Name = "log4japp"
            });

            _locationElement = new XmlElement("locationInfo", null)
            {
                Attributes =
        {
            new XmlAttribute("class", "${callsite-class}"),
            new XmlAttribute("method", "${callsite-method}"),
            new XmlAttribute("file", "${callsite-file}"),
            new XmlAttribute("line", "${callsite-linenumber}")
        }
            };
            _locationElement.Attributes[2].Layout = null; // Disable file initially
            _locationElement.Attributes[3].Layout = null; // Disable line initially

            // Configure default properties
            IncludeEventProperties = true;
            IncludeScopeProperties = true;
            PropertiesElementName = "data";
            PropertiesElementKeyAttribute = "name";
            PropertiesElementValueAttribute = "value";
        }


        /// <summary>
        /// Gets the <see cref="Log4JXmlEventLayoutRenderer"/> instance that renders log events.
        /// </summary>
        public Log4JXmlEventLayoutRenderer Renderer { get; }

        /// <summary>
        /// Gets the collection of parameters. Each parameter contains a mapping
        /// between NLog layout and a named parameter.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        [ArrayParameter(typeof(Log4JXmlEventParameter), "parameter")]
        public IList<Log4JXmlEventParameter> Parameters { get => Renderer.Parameters; set => Renderer.Parameters = value; }

        ///// <summary>
        ///// Gets or sets the option to include all properties from the log events
        ///// </summary>
        ///// <docgen category='Layout Options' order='10' />
        //public bool IncludeEventProperties
        //{
        //    get => Renderer.IncludeEventProperties;
        //    set => Renderer.IncludeEventProperties = value;
        //}

        ///// <summary>
        ///// Gets or sets whether to include the contents of the <see cref="ScopeContext"/> properties-dictionary.
        ///// </summary>
        ///// <docgen category='Layout Options' order='10' />
        //public bool IncludeScopeProperties
        //{
        //    get => Renderer.IncludeScopeProperties;
        //    set => Renderer.IncludeScopeProperties = value;
        //}

        /// <summary>
        /// Gets or sets whether to include log4j:NDC in output from <see cref="ScopeContext"/> nested context.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        public bool IncludeScopeNested
        {
            get => Renderer.IncludeScopeNested;
            set => Renderer.IncludeScopeNested = value;
        }

        /// <summary>
        /// Obsolete and replaced by <see cref="XmlElementBase.IncludeEventProperties"/> with NLog v5.
        ///
        /// Gets or sets the option to include all properties from the log events
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        [Obsolete("Replaced by IncludeEventProperties. Marked obsolete on NLog 5.0")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ObsoleteIncludeAllProperties { get => IncludeEventProperties; set => IncludeEventProperties = value; }

        /// <summary>
        /// Obsolete and replaced by <see cref="XmlElementBase.IncludeScopeProperties"/> with NLog v5.
        ///
        /// Gets or sets a value indicating whether to include contents of the <see cref="MappedDiagnosticsContext"/> dictionary.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        [Obsolete("Replaced by IncludeScopeProperties. Marked obsolete on NLog 5.0")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ObsoleteIncludeMdc { get => Renderer.IncludeMdc; set => Renderer.IncludeMdc = value; }

        /// <summary>
        /// Gets or sets whether to include log4j:NDC in output from <see cref="ScopeContext"/> nested context.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        public bool IncludeNdc { get => Renderer.IncludeNdc; set => Renderer.IncludeNdc = value; }

        /// <summary>
        /// Obsolete and replaced by <see cref="XmlElementBase.IncludeScopeProperties"/> with NLog v5.
        ///
        /// Gets or sets a value indicating whether to include contents of the <see cref="MappedDiagnosticsLogicalContext"/> dictionary.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        [Obsolete("Replaced by IncludeScopeProperties. Marked obsolete on NLog 5.0")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ObsoleteIncludeMdlc { get => Renderer.IncludeMdlc; set => Renderer.IncludeMdlc = value; }

        /// <summary>
        /// Obsolete and replaced by <see cref="IncludeNdc"/> with NLog v5.
        ///
        /// Gets or sets a value indicating whether to include contents of the <see cref="NestedDiagnosticsLogicalContext"/> stack.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        [Obsolete("Replaced by IncludeScopeNested. Marked obsolete on NLog 5.0")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IncludeNdlc { get => Renderer.IncludeNdlc; set => Renderer.IncludeNdlc = value; }

        /// <summary>
        /// Gets or sets the log4j:event logger-xml-attribute. Default: ${logger}
        /// </summary>
        /// <docgen category='Layout Options' order='100' />
        public Layout LoggerName
        {
            get => Renderer.LoggerName;
            set => Renderer.LoggerName = value;
        }

        /// <summary>
        /// Gets or sets the log4j:event message-xml-element. Default: ${message}
        /// </summary>
        /// <docgen category='Layout Options' order='100' />
        public Layout FormattedMessage
        {
            get => Renderer.FormattedMessage;
            set => Renderer.FormattedMessage = value;
        }

        /// <summary>
        /// Gets or sets the log4j:event log4japp-xml-element. By default it's the friendly name of the current AppDomain.
        /// </summary>
        /// <docgen category='Layout Options' order='100' />
        public Layout AppInfo
        {
            get => Renderer.AppInfo;
            set => Renderer.AppInfo = value;
        }

        /// <summary>
        ///  Gets or sets whether the log4j:throwable xml-element should be written as CDATA
        /// </summary>
        /// <docgen category='Layout Options' order='100' />
        public bool WriteThrowableCData
        {
            get => Renderer.WriteThrowableCData;
            set => Renderer.WriteThrowableCData = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to include call site (class and method name) in the information sent over the network.
        /// </summary>
        /// <docgen category='Layout Options' order='100' />
        public bool IncludeCallSite
        {
            get => _includeCallSite;
            set
            {
                _includeCallSite = value;
                UpdateCallSiteElement();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to include source info (file name and line number) in the information sent over the network.
        /// </summary>
        /// <docgen category='Layout Options' order='100' />
        public bool IncludeSourceInfo
        {
            get => _includeSourceInfo;
            set
            {
                _includeSourceInfo = value;
                UpdateCallSiteElement();
            }
        }

        /// <inheritdoc/>
        protected override string GetFormattedMessage(LogEventInfo logEvent)
        {
            return Renderer.Render(logEvent);
        }

        /// <inheritdoc/>
        /// <inheritdoc/>
        protected override void RenderFormattedMessage(LogEventInfo logEvent, StringBuilder target)
        {
            // Add standard properties
            EnsureStandardProperties();

            // Add parameters as properties
            foreach (var param in Parameters)
            {
                if (!string.IsNullOrEmpty(param.Name))
                {
                    Elements.Add(new XmlElement("data", param.Layout)
                    {
                        Name = param.Name,
                        Encode = true
                    });
                }
            }

            base.RenderFormattedMessage(logEvent, target);
        }

        private void EnsureStandardProperties()
        {
            if (!HasElement("log4japp"))
                Elements.Add(new XmlElement("data", "${appdomain:format={1\\}}") { Name = "log4japp" });

            if (!HasElement("log4jmachinename"))
                Elements.Add(new XmlElement("data", "${machinename}") { Name = "log4jmachinename" });
        }

        private void AddCallSiteElement(LogEventInfo logEvent)
        {
            var locationElement = new XmlElement("locationInfo", null);

            if (!string.IsNullOrEmpty(logEvent.CallerClassName))
                locationElement.Attributes.Add(new XmlAttribute("class", logEvent.CallerClassName));

            locationElement.Attributes.Add(new XmlAttribute("method", logEvent.CallerMemberName));

            if (IncludeSourceInfo)
            {
                locationElement.Attributes.Add(new XmlAttribute("file", logEvent.CallerFilePath));
                locationElement.Attributes.Add(new XmlAttribute("line", logEvent.CallerLineNumber.ToString()));
            }

            Elements.Add(locationElement);
        }

        private bool HasElement(string name) => Elements.Any(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        private XmlElement GetElement(string name) => Elements.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        private void RemoveElement(string name)
        {
            var element = GetElement(name);
            if (element != null)
                Elements.Remove(element);
        }

        private void UpdateCallSiteElement()
        {
            if (_includeCallSite || _includeSourceInfo)
            {
                if (!Elements.Contains(_locationElement))
                    Elements.Add(_locationElement);

                _locationElement.Attributes[2].Layout = _includeSourceInfo ? "${callsite-file}" : null;
                _locationElement.Attributes[3].Layout = _includeSourceInfo ? "${callsite-linenumber}" : null;
            }
            else
            {
                Elements.Remove(_locationElement);
            }
        }
    }
}
