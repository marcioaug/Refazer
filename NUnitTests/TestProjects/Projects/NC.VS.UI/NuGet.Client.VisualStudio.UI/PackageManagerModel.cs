﻿using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client;
using NuGet.Client.Resolution;
using NuGet.Client.Installation;
using System.ComponentModel;

namespace NuGet.Client.VisualStudio.UI
{
    /// <summary>
    /// Encapsulates the document model behind the Package Manager document window
    /// </summary>
    /// <remarks>
    /// This class just proxies all calls through to the PackageManagerSession and implements IVsPersistDocData to fit
    /// into the VS model. It's basically an adaptor that turns PackageManagerSession into an IVsPersistDocData so VS is happy.
    /// </remarks>
    public class PackageManagerModel : IVsPersistDocData, INotifyPropertyChanged
    {
        public SourceRepositoryManager Sources { get; private set; }
        public InstallationTarget Target { get; private set; }
        
        public PackageManagerModel(SourceRepositoryManager sources, InstallationTarget target)
        {
            Sources = sources;
            Target = target;
        }

        private string _searchText;

        public string SearchText
        {
            get
            {
                return _searchText;
            }
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged("SearchText");
                }
            }
        }

        #region IVsPersistDocData

        public int Close()
        {
            return VSConstants.S_OK;
        }

        public int GetGuidEditorType(out Guid pClassID)
        {
            pClassID = PackageManagerEditorFactory.EditorFactoryGuid;
            return VSConstants.S_OK;
        }

        public int IsDocDataDirty(out int pfDirty)
        {
            pfDirty = 0;
            return VSConstants.S_OK;
        }

        public int IsDocDataReloadable(out int pfReloadable)
        {
            // Reload doesn't make sense
            pfReloadable = 0;
            return VSConstants.S_OK;
        }

        public int LoadDocData(string pszMkDocument)
        {
            return VSConstants.S_OK;
        }

        public int OnRegisterDocData(uint docCookie, IVsHierarchy pHierNew, uint itemidNew)
        {
            return VSConstants.S_OK;
        }

        public int ReloadDocData(uint grfFlags)
        {
            return VSConstants.S_OK;
        }

        public int RenameDocData(uint grfAttribs, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int SaveDocData(VSSAVEFLAGS dwSave, out string pbstrMkDocumentNew, out int pfSaveCanceled)
        {
            // We don't support save as so we don't need to the two out parameters.
            pbstrMkDocumentNew = null;
            pfSaveCanceled = 0;

            return VSConstants.S_OK;
        }

        public int SetUntitledDocPath(string pszDocDataPath)
        {
            return VSConstants.E_NOTIMPL;
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
