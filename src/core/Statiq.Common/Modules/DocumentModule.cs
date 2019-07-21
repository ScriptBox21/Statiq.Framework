﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Statiq.Common
{
    /// <summary>
    /// An abstract base for modules that execute children and then combine the results with the
    /// input documents in some way.
    /// </summary>
    /// <remarks>
    /// The child modules are executed once and the original input documents
    /// are passed to the child modules. Wrap the child modules with a <c>ForEachDocument</c>
    /// module to execute the child modules for each input document individually. Add a
    /// <c>ClearDocuments</c> module as the first child if the original input documents
    /// should not be passed to the child modules.
    /// </remarks>
    public abstract class DocumentModule : ContainerModule
    {
        /// <summary>
        /// Executes the specified modules to get result documents.
        /// </summary>
        /// <param name="modules">The modules to execute.</param>
        protected DocumentModule(params IModule[] modules)
            : base(modules)
        {
        }

        /// <inheritdoc />
        public override async Task<IEnumerable<IDocument>> ExecuteAsync(IReadOnlyList<IDocument> inputs, IExecutionContext context) =>
            Children.Count > 0 ? await GetOutputDocumentsAsync(inputs, await context.ExecuteAsync(Children, inputs)) : Array.Empty<IDocument>();

        /// <summary>
        /// Gets the output documents given the input documents and the output documents from the execution of child modules.
        /// </summary>
        /// <param name="inputs">The original input documents to this module.</param>
        /// <param name="childOutputs">The output documents from the child modules.</param>
        /// <returns>The output documents of this module.</returns>
        protected abstract Task<IEnumerable<IDocument>> GetOutputDocumentsAsync(IReadOnlyList<IDocument> inputs, IReadOnlyList<IDocument> childOutputs);
    }
}