using System;
using System.Collections.Generic;
using System.Threading;

namespace DelveLib
{
    public abstract class TaskItem : IDisposable
    {
        public CancellationToken? cancelationToken;

        public WinMsg Message { get; set; }
        public Stack<IDisposable> Disposables { get; private set; } = new Stack<IDisposable>();
        public List<TaskItem> Consequences { get; private set; } = new List<TaskItem>();

        public Action MainAction { get; set; }
        public Action FinishAction { get; set; }

        public abstract string TaskName { get; }

        public TaskItem(Action mainAction, Action finishAction = null)
        {
            MainAction = mainAction;
            FinishAction = finishAction;
        }

        /// <summary>
        /// Dispose will be called after TaskEnd/CanceledEnd
        /// </summary>
        public virtual void Dispose()
        {
            while (Disposables.Count > 0)
                Disposables.Pop().Dispose();
        }

        public TaskItem(CancellationToken token)
        {
            cancelationToken = token;
        }

        public virtual void TaskLaunch()
        {

        }

        public virtual void CanceledEnd()
        {

        }

        public virtual void TaskEnd()
        {
            if (FinishAction != null)
                FinishAction();
        }

        public virtual void ThrewException(Exception ex)
        {

        }

        public virtual void ParentFinished(TaskItem parent)
        {

        }

        public bool IsCanceled
        {
            get { return cancelationToken.HasValue ? cancelationToken.Value.IsCancellationRequested : false; }
        }

        public void PushDisposable(IDisposable disp)
        {
            Disposables.Push(disp);
        }
    }
}
