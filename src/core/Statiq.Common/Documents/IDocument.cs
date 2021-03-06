using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Statiq.Common
{
    /// <summary>
    /// Contains content and metadata for each item as it propagates through the pipeline.
    /// </summary>
    public interface IDocument : IMetadata, IDisplayable, IContentProviderFactory, ILogger
    {
        /// <summary>
        /// An identifier that is generated when the document is created and stays the same after cloning.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// An identifier for the document meant to reflect the source of the data. These should be unique (such as a file name).
        /// This property is always an absolute path. If you want to get a relative path, use <see cref="NormalizedPath.GetRelativeInputPath()"/>.
        /// </summary>
        /// <value>
        /// The source of the document, or <c>null</c> if the document doesn't have a source.
        /// </value>
        NormalizedPath Source { get; }

        /// <summary>
        /// The destination of the document. Can be either relative or absolute.
        /// </summary>
        NormalizedPath Destination { get; }

        /// <summary>
        /// The content provider responsible for creating content streams for the document.
        /// </summary>
        /// <remarks>
        /// This will always return a content provider, even if there is empty or no content.
        /// </remarks>
        IContentProvider ContentProvider { get; }

        /// <summary>
        /// Clones this document.
        /// </summary>
        /// <param name="source">The new source. If this document already contains a source, then it's used and this is ignored.</param>
        /// <param name="destination">The new destination or <c>null</c> to keep the existing destination.</param>
        /// <param name="items">New metadata items or <c>null</c> not to add any new metadata.</param>
        /// <param name="contentProvider">The new content provider or <c>null</c> to keep the existing content provider.</param>
        /// <returns>A new document of the same type as this document.</returns>
        IDocument Clone(
            NormalizedPath source,
            NormalizedPath destination,
            IEnumerable<KeyValuePair<string, object>> items,
            IContentProvider contentProvider = null);

        /// <inheritdoc />
        IContentProvider IContentProviderFactory.GetContentProvider() => ContentProvider;

        /// <inheritdoc />
        IContentProvider IContentProviderFactory.GetContentProvider(string mediaType) => ContentProvider.CloneWithMediaType(mediaType);

        /// <inheritdoc />
        string IDisplayable.ToDisplayString() => Source.IsNull ? "unknown source" : Source.ToDisplayString();

        /// <summary>
        /// Gets a hash of the provided document content and metadata appropriate for caching.
        /// Custom <see cref="IDocument"/> implementations may also contribute additional state
        /// data to the resulting hash code by overriding this method.
        /// </summary>
        /// <returns>A hash appropriate for caching.</returns>
        Task<int> GetCacheHashCodeAsync();

        /// <summary>
        /// A default implementation of <see cref="GetCacheHashCodeAsync()"/>.
        /// </summary>
        /// <returns>A hash appropriate for caching.</returns>
        public static async Task<int> GetCacheHashCodeAsync(IDocument document)
        {
            HashCode hash = default;

            // Add the content hash
            hash.Add(await document.ContentProvider.GetCacheHashCodeAsync());

            // We exclude ContentProvider from hash as we already added CRC for content above
            // Also exclude settings and IMetadataValue implementations
            // And try to convert to a string and hash that for consistency
            foreach ((string key, object value) in document
                .WithoutSettings()
                .GetRawEnumerable()
                .Where(x => x.Key != nameof(ContentProvider) && !(x.Value is IMetadataValue))
                .Select(x => (x.Key, TypeHelper.TryConvert(x.Value, out string stringValue) ? stringValue : x.Value))
                .OrderBy(x => x.Key))
            {
                hash.Add(key);
                hash.Add(value?.GetHashCode() ?? 0);
            }
            return hash.ToHashCode();
        }

        // ILogger default implementation

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) =>
            IExecutionContext.CurrentOrNull?.Log(logLevel, this, eventId, state, exception, formatter);

        bool ILogger.IsEnabled(LogLevel logLevel) => IExecutionContext.CurrentOrNull?.IsEnabled(logLevel) ?? false;

        IDisposable ILogger.BeginScope<TState>(TState state) => IExecutionContext.CurrentOrNull?.BeginScope(state);

        /// <summary>
        /// A hash of the property names in <see cref="IDocument"/> generated using reflection (generally intended for internal use).
        /// </summary>
        public static ImmutableHashSet<string> Properties = ImmutableHashSet.CreateRange(
            StringComparer.OrdinalIgnoreCase,
            typeof(IDocument)
                .GetProperties()
                .Select(x => (x.Name, x.GetGetMethod()))
                .Where(x => x.Item2?.GetParameters().Length == 0)
                .Select(x => x.Name));
    }
}