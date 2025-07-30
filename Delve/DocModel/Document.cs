using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace DocModel
{
    public delegate void NewDocumentHandler(Document doc);
    public delegate void DocumentSavedHandler(Document doc);
    public delegate void DocumentClosedHandler(Document doc);

    public class Document : INotifyPropertyChanged, IDisposable
    {
        #region Events
        public event DocumentSavedHandler DocumentSaved = delegate { };
        public event DocumentClosedHandler DocumentClosed = delegate { };
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        public SelectionContext Selection { get; private set; } = new SelectionContext();
        public UndoStack UndoRedo { get; private set; } = new UndoStack();

        bool active_ = false;
        public bool IsActive { get { return active_; } set { active_ = value; OnPropertyChanged(); } }

        public string DocumentTypeName { get; protected set; } = "<ERROR-UNSPECIFIED-DOC-NAME>";
        public string FileMask { get; protected set; }

        Uri uri_;
        public Uri FileURI { get { return uri_; } set { uri_ = value; OnPropertyChanged("DisplayName"); OnPropertyChanged("DocumentName"); OnPropertyChanged(); } }

        bool dirty_ = true;
        public bool IsDirty { get { return dirty_; } set { dirty_ = value; OnPropertyChanged(); } }

        public virtual string DisplayName
        {
            get
            {
                if (FileURI != null)
                    return string.Format("{0} : {1}", System.IO.Path.GetFileNameWithoutExtension(FileURI.AbsolutePath), DocumentTypeName);
                return string.Format("<unnamed> : {0}", DocumentTypeName);
            }
        }

        public DocumentManager DocumentManager { get; private set; }

        public Document(DocumentManager docMan)
        {
            DocumentManager = docMan;
        }

        public virtual bool WriteFile(Uri path) { throw new NotImplementedException(); }

        public virtual void OnActivate()
        {
            IsActive = true;
        }

        public virtual void OnDeactivate()
        {
            IsActive = false;
        }

        public virtual bool Save(bool safetyPrompt = false)
        {
            if (FileURI == null)
            {
                Uri saveTarget = DocumentManager.DialogService.SaveFile("Save " + DocumentTypeName, FileMask);
                if (saveTarget != null)
                {
                    FileURI = saveTarget;
                    if (WriteFile(FileURI) && FileURI != null)
                    {
                        IsDirty = false;
                        DocumentManager.UserDataService.AddRecentFile(saveTarget);
                        return true;
                    }
                }
                return false;
            }
            else
            {
                Debug.Assert(FileURI != null && FileURI.IsFile, "Document FileURI must be valid and routed to a file");
                if (safetyPrompt)
                {
                    if (DocumentManager.DialogService.Confirm(string.Format("Save changes to file '{0}'", System.IO.Path.GetFileName(FileURI.LocalPath)), "Save"))
                    {
                        if (WriteFile(FileURI))
                        {
                            DocumentManager.UserDataService.AddRecentFile(FileURI);
                            IsDirty = false;
                            return true;
                        }
                    }
                }
                else
                {
                    if (WriteFile(FileURI))
                    {
                        DocumentManager.UserDataService.AddRecentFile(FileURI);
                        IsDirty = false;
                        return true;
                    }
                }

            }
            return true;
        }

        public virtual bool SaveAs()
        {
            Uri saveTarget = DocumentManager.DialogService.SaveFile("Save " + DocumentTypeName + " As...", FileMask);
            if (saveTarget != null)
            {
                FileURI = saveTarget;
                if (WriteFile(FileURI))
                {
                    DocumentManager.UserDataService.AddRecentFile(FileURI);
                    IsDirty = false;
                    return true;
                }
            }
            return false;
        }

        public virtual bool Close()
        {
            if (FileURI == null || IsDirty)
            {
                bool pass = true;
                if (FileURI != null) // exists but is dirty
                {
                    var result = DocumentManager.DialogService.SaveIgnoreCancel(string.Format("Save changes to '{0}'", FileURI), "Save Changes?");
                    if (result == SaveIgnoreCancelResult.Save)
                    {
                        Save();
                        return true;
                    }
                    else if (result == SaveIgnoreCancelResult.Cancel)
                        return false;
                }
                else
                {
                    var result = DocumentManager.DialogService.SaveIgnoreCancel("This brand new file has never been saved.", string.Format("Save New {0}", DocumentTypeName));
                    if (result == SaveIgnoreCancelResult.Save)
                    {
                        return Save();
                    }
                    else if (result == SaveIgnoreCancelResult.Cancel)
                        return false;
                }
                if (pass)
                    return true;
            }
            return true;
        }

        /// <summary>
        /// Derived documents must overload.
        /// </summary>
        public virtual void Dispose()
        {
            throw new NotImplementedException();
        }

        #region Internals
        // Create the OnPropertyChanged method to raise the event 
        public virtual void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}
