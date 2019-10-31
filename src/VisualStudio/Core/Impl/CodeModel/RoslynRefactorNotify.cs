
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [Export(typeof(IRoslynRefactorNotifyService))]
    class RoslynRefactorNotify : IRoslynRefactorNotifyService
    {
        private IEnumerable<IRefactorNotifyService> _refactorNotifyService;

        [ImportingConstructor]
        RoslynRefactorNotify()
        {
            /* TO-DO  For _refactorNotifyService, which object to instantiate here ? */
        }


        [ImportingConstructor]
        RoslynRefactorNotify([ImportMany] IEnumerable<IRefactorNotifyService> refactorNotifyServices)
        {
            _refactorNotifyService = refactorNotifyServices;
        }

        public void RefactorNotify(ISymbol symbol, string newName, Workspace workspace, Solution oldSolution)
        {
            var newSolution = Renamer.RenameSymbolAsync(oldSolution, symbol, newName, oldSolution.Options).WaitAndGetResult_CodeModel(CancellationToken.None);
            var changedDocuments = newSolution.GetChangedDocuments(oldSolution);

            // Notify third parties of the coming rename operation and let exceptions propagate out
            _refactorNotifyService.TryOnBeforeGlobalSymbolRenamed(workspace, changedDocuments, symbol, newName, throwOnFailure: true);

            // Update the workspace.
            if (!workspace.TryApplyChanges(newSolution))
            {
                throw Exceptions.ThrowEFail();
            }

            // Notify third parties of the completed rename operation and let exceptions propagate out
            _refactorNotifyService.TryOnAfterGlobalSymbolRenamed(workspace, changedDocuments, symbol, newName, throwOnFailure: true);

            RenameTrackingDismisser.DismissRenameTracking(workspace, changedDocuments);
        }

    }
}
