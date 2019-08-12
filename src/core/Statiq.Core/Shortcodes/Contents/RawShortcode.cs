﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Statiq.Common;

namespace Statiq.Core
{
    /// <summary>
    /// A special shortcode that will output whatever is in it's content.
    /// </summary>
    /// <remarks>
    /// This will not evaluate nested shortcodes and is useful
    /// for escaping shortcode syntax.
    /// </remarks>
    /// <example>
    /// <code>
    /// &lt;?# Raw ?>&lt;?# ThisWillBeOutputVerbatim ?>&lt;?#/ Raw ?>
    /// </code>
    /// </example>
    public class RawShortcode : Shortcode
    {
        public const string RawShortcodeName = "Raw";

        /// <inheritdoc />
        public override async Task<IDocument> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context) =>
            context.CreateDocument(await context.GetContentProviderAsync(content));
    }
}
