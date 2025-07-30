using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DelveLib
{
    /// <summary>
    /// Provides tasks to a task thread.
    /// </summary>
    public class TaskSource
    {
        public List<TaskItem> Tasks { get; private set; } = new List<TaskItem>();

        int inFlight = 0;
        public int TasksInFlight { get { int ret = 0; lock (this) { ret = inFlight; } return ret; } set { lock (this) { inFlight = value; } } }
        public int TaskCount { get { int ret = 0; lock (this) { ret = Tasks.Count; } return ret; } }

        public virtual void AddTask(TaskItem item)
        {
            lock (this)
            {
                Tasks.Add(item);
            }
        }

        public void WaitForCompletion()
        {
            for (;;)
            {
                var task = Next();
                if (task != null)
                {
                    if (task.IsCanceled)
                        continue;
                    task.TaskLaunch();
                    if (task.MainAction != null)
                        task.MainAction();
                    FinishTask(task);
                    continue;
                }
                else if (TasksInFlight > 0)
                    continue;
                else
                    break;
            }
        }

        public virtual TaskItem Next()
        {
            lock (this)
            {
                while (Tasks.Count > 0)
                {
                    var testTask = Tasks[0];
                    Tasks.RemoveAt(0);
                    if (!testTask.IsCanceled)
                    {
                        IncTaskInFlight();
                        return testTask;
                    }
                    else
                        testTask.Dispose();
                }
            }
            return null;
        }

        public virtual void FinishTask(TaskItem task)
        {
            DecTaskInFlight();
            if (!task.IsCanceled)
            {
                task.TaskEnd();
                if (task.Consequences.Count > 0)
                {
                    foreach (var subItem in task.Consequences)
                        subItem.ParentFinished(task);

                    lock (this)
                    {
                        Tasks.InsertRange(0, task.Consequences);
                    }
                }
            }
            else
                task.CanceledEnd();

            task.Dispose();
        }

        void IncTaskInFlight() { lock (this) { inFlight++; } }
        void DecTaskInFlight() {  lock(this) { inFlight--; } }
    }

    //[IOCInitialized(Count = 3)]
    public class TaskThread : TaskSource, IDisposable
    {
        EventWaitHandle wh = new AutoResetEvent(false);
        TaskSource source_;
        Thread thread_;
        int threadTag_;
        bool halt_ = false;
        public TaskThread(int threadTag, TaskSource src = null)
        {
            if (src == null)
                source_ = this;
            else
                source_ = src;

            threadTag_ = threadTag;
            thread_ = new Thread(new System.Threading.ThreadStart(() =>
            {
                while (halt_ == false)
                {
                    var task = source_.Next(); // grab next task
                    //try
                    {
                        while (task != null) // as long as we have a task then keep working
                        {
                            //App.WipeWindowMessage(threadTag_);
                            task.Message = new WinMsg { Message = task.TaskName, Tag = threadTag_, Duration = 0 };
                            //App.PushWindowMessage(task.Message);
                            task.TaskLaunch();
                            if (task.MainAction != null)
                                task.MainAction();
                            source_.FinishTask(task);

                            task = source_.Next(); // try to grab next available
                        }
                        //App.WipeWindowMessage(threadTag_);
                    }
                    //catch (Exception ex)
                    //{
                    //    if (task != null)
                    //        task.ThrewException(ex);
                    //    App.PushWindowMessage(new WinMsg { Text = string.Format("Failed: {0}", task.ToString()) });
                    //    App.WipeWindowMessage(1);
                    //    ErrorHandler.inst().Error(ex);
                    //}
                }
            }));
            thread_.Priority = ThreadPriority.AboveNormal;
            thread_.IsBackground = true;
            thread_.Start();
        }

        public void Dispose()
        {
            halt_ = true;
        }
    }

    public class TaskThreadPool : TaskSource, IDisposable
    {
        TaskThread[] threads;
        int numThreads;

        public TaskThreadPool(int threadCt, int threadTag = 0)
        {
            numThreads = Math.Max(threadCt, 1);
            threads = new TaskThread[numThreads];
            for (int i = 0; i < numThreads; ++i)
                threads[i] = new TaskThread(threadTag, this);
        }

        public void Dispose()
        {
            foreach (var thread in threads)
                thread.Dispose();
            threads = null;
            numThreads = 0;
        }
    }
}
