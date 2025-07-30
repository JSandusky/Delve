using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocModel
{
    public abstract class UndoRedoCmd : IDisposable
    {
        bool isFirstRedo_ = true;
        bool lastActionWasUndo_ = false;
        DateTime time_;

        public UndoRedoCmd()
        {
            time_ = DateTime.Now;
        }

        public UndoStack OwningStack { get; set; }

        public bool IsCurrent
        {
            get
            {
                return OwningStack != null ? OwningStack.Undo.LastOrDefault() == this : false;
            }
        }

        public bool IsUndo { get { return lastActionWasUndo_ == false; } }
        public bool IsRedo { get { return lastActionWasUndo_ == true; } }

        /// <summary>
        /// Called when this UndoRedoCmd is the top-most in the undo stack.
        /// </summary>
        public virtual void MadeCurrent() { }

        /// <summary>
        /// Free any resources when the stack is cleared.
        /// </summary>
        public virtual void Dispose() { }

        /// <summary>
        /// Performs the redo operations.
        /// </summary>
        public void Redo()
        {
            DoRedo();
            isFirstRedo_ = false;
            lastActionWasUndo_ = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd">Other UndoRedoCmd to test for candidacy to merge into this one</param>
        /// <returns>True if the merge should happen.</returns>
        public virtual bool ShouldMerge(UndoRedoCmd cmd)
        {
            if (!RecentEnough())
                return false;
            return false;
        }

        /// <summary>
        /// Tests time to see if an UndoRedoCmd is in a tight enough time window to allow merging.
        /// </summary>
        /// <returns>True if the UndoRedoCmd is close enough in time to consider merging.</returns>
        public bool RecentEnough()
        {
            // 10 second window
            return DateTime.Now.Subtract(time_).TotalSeconds < 10;
        }

        public void Undo()
        {
            DoUndo();
            lastActionWasUndo_ = true;
        }

        protected bool IsFirstRedo { get { return isFirstRedo_; } }

        protected virtual void DoRedo() { Execute(true); }
        protected virtual void DoUndo() { Execute(false); }

        protected abstract void Execute(bool isRedo);
        public abstract void Merge(UndoRedoCmd cmd);

        string message_;
        public string Message { get { return message_; } set { message_ = value; OnPropertyChanged(); } }

        protected abstract string GetObjectName(object obj);
    }

    public sealed class UndoStack
    {
        public delegate void UndoRedoActionPerformedHandler(UndoStack stack);

        public event UndoRedoActionPerformedHandler UndoRedoActionPerformed;

        public ObservableCollection<UndoRedoCmd> Undo { get; private set; } = new ObservableCollection<UndoRedoCmd>();
        public ObservableCollection<UndoRedoCmd> Redo { get; private set; } = new ObservableCollection<UndoRedoCmd>();

        public List<UndoRedoCmd> InlineUndoRedo
        {
            get
            {
                List<UndoRedoCmd> ret = new List<UndoRedoCmd>();
                ret.AddRange(Undo);
                ret.AddRange(Redo);
                return ret;
            }
        }

        void Signal()
        {
            if (UndoRedoActionPerformed != null)
                UndoRedoActionPerformed(this);
        }

        public void Add(UndoRedoCmd cmd)
        {
            cmd.OwningStack = this;
            if (Undo.Count > 0)
            {
                if (Undo[Undo.Count - 1].RecentEnough() && Undo[Undo.Count - 1].ShouldMerge(cmd))
                {
                    Undo[Undo.Count - 1].Merge(cmd);
                    Redo.Clear();
                    Undo[Undo.Count - 1].MadeCurrent();
                    return;
                }
            }


            //cmd.Redo();
            cmd.MadeCurrent();
            Undo.Add(cmd);
            if (Undo.Count > 64)
                Undo.RemoveAt(0);
            Redo.Clear();
            OnPropertyChanged("InlineUndoRedo");
        }

        public bool IsLatest(UndoRedoCmd cmd)
        {
            if (Undo.Count > 0)
                return Undo[Undo.Count - 1] == cmd;
            return false;
        }

        public void RedoUntil(UndoRedoCmd cmd)
        {
            using (var blocker = new Notify.Tracker.TrackingSideEffects())
            {
                while (Redo.Count > 0)
                {
                    var item = Redo[0];
                    item.Redo();
                    Undo.Add(item);
                    Redo.RemoveAt(0);
                    item.OnPropertyChanged(string.Empty);
                    OnPropertyChanged("InlineUndoRedo");
                    if (item == cmd)
                        break;
                }
            }
            NotifyCurrent();
        }

        public void UndoOne()
        {
            if (Undo.Count > 0)
                UndoUntil(Undo[Undo.Count - 1]);
        }

        public void UndoAll()
        {
            if (Undo.Count > 0)
                UndoUntil(Undo[0]);
        }

        public void RedoOne()
        {
            if (Redo.Count > 0)
                RedoUntil(Redo[0]);
        }

        public void RedoAll()
        {
            if (Redo.Count > 0)
                RedoUntil(Redo[Redo.Count - 1]);
        }

        public void UndoUntil(UndoRedoCmd cmd)
        {
            while (Undo.Count > 0)
            {
                using (var blocker = new Notify.Tracker.TrackingSideEffects())
                {
                    var item = Undo[Undo.Count - 1];
                    item.Undo();
                    Redo.Insert(0, item);
                    Undo.Remove(item);
                    item.OnPropertyChanged(string.Empty);
                    OnPropertyChanged("InlineUndoRedo");
                    if (item == cmd)
                        break;
                }
            }
            NotifyCurrent();
        }

        void NotifyCurrent()
        {
            if (Undo.Count > 0)
                Undo[Undo.Count - 1].MadeCurrent();
            Signal();
        }
    }
}
}
