using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DocModel
{
    public class DocumentManager : INotifyPropertyChanged
    {
        public IDialogService DialogService { get; private set; }

        public IUserDataService UserDataService { get; private set; }

        public delegate void NewDocumentHandler(Document newDoc);
        public event NewDocumentHandler OnDocumentOpened = delegate { };

        public delegate void DocumentClosedHandler(Document closing);
        public event DocumentClosedHandler OnDocumentClosed = delegate { };

        public delegate void ActiveDocumentChangedHandler(Document newDoc, Document oldDoc);
        public event ActiveDocumentChangedHandler OnActiveDocumentChanged = delegate { };
        public event PropertyChangedEventHandler PropertyChanged;

        Document activeDocument_;
        public Document ActiveDocument { get { return activeDocument_; } private set { activeDocument_ = value; OnPropertyChanged(); } }

        public ObservableCollection<Document> OpenDocuments { get; private set; } = new ObservableCollection<Document>();

        public DocumentManager(IDialogService dlgService, IUserDataService userDataService)
        {
            DialogService = dlgService;
            UserDataService = userDataService;
        }

        public void AddDocument(Document newDoc)
        {
            OpenDocuments.Add(newDoc);
            OnDocumentOpened(newDoc);
            SetActiveDocument(newDoc);
        }

        public void SetActiveDocument(Document newDoc)
        {
            if (newDoc == activeDocument_)
                return;

            var oldDoc = activeDocument_;
            if (activeDocument_ != null)
                activeDocument_.OnDeactivate();

            ActiveDocument = newDoc;
            OnActiveDocumentChanged(newDoc, oldDoc);

            if (newDoc != null)
                newDoc.OnActivate();
        }

        public bool CloseDocument(Document doc)
        {
            if (doc.Close())
            {
                OpenDocuments.Remove(doc);
                OnDocumentClosed(doc);
                if (doc == activeDocument_)
                {
                    if (OpenDocuments.Count > 0)
                        SetActiveDocument(OpenDocuments[0]);
                    else
                        SetActiveDocument(null);
                }
                doc.Dispose();
                return true;
            }
            return false;
        }

        public void SaveAll()
        {
            foreach (var doc in OpenDocuments)
                doc.Save();
        }

        public void CloseCurrent()
        {
            if (ActiveDocument != null)
                CloseDocument(ActiveDocument);
        }

        public bool CloseAllFiles()
        {
            var docs = OpenDocuments.ToArray();
            foreach (var doc in docs)
            {
                if (!CloseDocument(doc))
                    return false;
            }
            OpenDocuments.Clear();
            return true;
        }

        #region Generic Utilities
        public T ActiveDoc<T>() where T : class
        {
            return ActiveDocument as T;
        }
        #endregion

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
