﻿using System.Globalization;
using System.IO;
using System.Linq;
using Cottle.Documents.Simple;
using Cottle.Exceptions;
using Cottle.Settings;
using Cottle.Stores;

namespace Cottle.Documents
{
    /// <summary>
    /// Simple document renders templates using an interpreter. If offers better garbage collection and easier debugging
    /// but average rendering performance.
    /// </summary>
    public sealed class SimpleDocument : AbstractDocument
    {
        private readonly INode _renderer;

        private readonly ISetting _setting;

        public SimpleDocument(TextReader reader, ISetting setting)
        {
            var parser = ParserFactory.BuildParser(AbstractDocument.CreateConfiguration(setting));

            if (!parser.Parse(reader, out var command, out var reports))
            {
                var report = reports.Count > 0 ? reports[0] : new DocumentReport("unknown error", 0, 0);

                throw new ParseException(report.Column, report.Line, report.Message);
            }

            _renderer = Compiler.Compile(command);
            _setting = setting;
        }

        public SimpleDocument(TextReader reader) :
            this(reader, DefaultSetting.Instance)
        {
        }

        public SimpleDocument(string template, ISetting setting) :
            this(new StringReader(template), setting)
        {
        }

        public SimpleDocument(string template) :
            this(new StringReader(template), DefaultSetting.Instance)
        {
        }

        public override Value Render(IContext context, TextWriter writer)
        {
            _renderer.Render(new ContextStore(context), writer, out var result);

            return result;
        }

        public void Source(TextWriter writer)
        {
            _renderer.Source(_setting, writer);
        }

        public string Source()
        {
            var writer = new StringWriter(CultureInfo.InvariantCulture);

            Source(writer);

            return writer.ToString();
        }
    }
}