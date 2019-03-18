﻿using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Implementation.Debugging
{
    [Shared]
    [ExportLanguageService(typeof(ILanguageDebugInfoService), LanguageNames.FSharp)]
    internal class FSharpLanguageDebugInfoService : ILanguageDebugInfoService
    {
        private readonly IFSharpLanguageDebugInfoService _service;

        [ImportingConstructor]
        public FSharpLanguageDebugInfoService(IFSharpLanguageDebugInfoService service)
        {
            _service = service;
        }

        public async Task<CodeAnalysis.Editor.Implementation.Debugging.DebugDataTipInfo> GetDataTipInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var result = await _service.GetDataTipInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
            return new CodeAnalysis.Editor.Implementation.Debugging.DebugDataTipInfo(result.Span, result.Text);
        }

        public async Task<CodeAnalysis.Editor.Implementation.Debugging.DebugLocationInfo> GetLocationInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var result = await _service.GetLocationInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
            return new CodeAnalysis.Editor.Implementation.Debugging.DebugLocationInfo(result.Name, result.LineOffset);
        }
    }
}
