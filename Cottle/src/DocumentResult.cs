using System;
using System.Collections.Generic;
using System.Linq;
using Cottle.Documents;
using Cottle.Exceptions;

namespace Cottle
{
    public readonly struct DocumentResult
    {
        public IDocument DocumentOrThrow => Success
            ? Document
            : throw (Reports.Count > 0
                ? new ParseException(Reports[0].Column, Reports[0].Line, Reports[0].Message)
                : new ParseException(0, 0, "unknown error"));

        public readonly IDocument Document;
        public readonly IReadOnlyList<DocumentReport> Reports;
        public readonly bool Success;

        public static DocumentResult CreateFailure(IEnumerable<DocumentReport> reports)
        {
            return new DocumentResult(EmptyDocument.Instance, reports, false);
        }

        public static DocumentResult CreateSuccess(IDocument document)
        {
            return new DocumentResult(document, Array.Empty<DocumentReport>(), true);
        }

        private DocumentResult(IDocument document, IEnumerable<DocumentReport> reports, bool success)
        {
            Document = document;
            Reports = reports.ToArray();
            Success = success;
        }
    }
}