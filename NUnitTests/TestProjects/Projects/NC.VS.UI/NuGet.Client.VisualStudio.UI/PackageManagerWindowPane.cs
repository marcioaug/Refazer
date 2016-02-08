﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.Client.VisualStudio.UI
{
    public class PackageManagerWindowPane : WindowPane
    {
        private PackageManagerControl _content;

        /// <summary>
        /// Initializes a new instance of the EditorPane class.
        /// </summary>
        public PackageManagerWindowPane(PackageManagerModel myDoc, IUserInterfaceService ui)
            : base(null)
        {
            PackageManagerControl control = new PackageManagerControl(myDoc, ui);
            _content = control;
        }

        public PackageManagerModel Model
        {
            get
            {
                return _content.Model;
            }
        }

        ///-----------------------------------------------------------------------------
        /// <summary>
        /// IVsWindowPane
        /// </summary>
        ///-----------------------------------------------------------------------------
        public override object Content
        {
            get
            {
                return _content;
            }
        }

        ///-----------------------------------------------------------------------------
        /// <summary>
        /// IVsWindowPane
        /// </summary>
        ///-----------------------------------------------------------------------------
        protected override void OnCreate()
        {
            base.OnCreate();
        }

        ///-----------------------------------------------------------------------------
        /// <summary>
        /// IVsWindowPane
        /// </summary>
        ///-----------------------------------------------------------------------------
        protected override void OnClose()
        {
            base.OnClose();

            this.CleanUp();
        }

        ///-----------------------------------------------------------------------------
        /// <summary>
        /// Cleanpu
        /// </summary>
        ///-----------------------------------------------------------------------------
        private void CleanUp()
        {
            _content = null;
        }

        ///-----------------------------------------------------------------------------
        /// <summary>
        /// Flush the control
        /// </summary>
        ///-----------------------------------------------------------------------------
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    CleanUp();

                    // Because Dispose() will do our cleanup, we can tell the GC not to call the finalizer.
                    GC.SuppressFinalize(this);
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}