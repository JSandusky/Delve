using System;
using System.Collections.Generic;
using System.Linq;

namespace DelveLib.EC
{
    public class Test
    {
        public event EventHandler<float> TestEvent;
        List<EventHandler<float>> handlers_ = new List<EventHandler<float>>();

        public void Stuff()
        {
            TestEvent(this, 0.0f);
        }

        public void Unsubscribe(object receiver)
        {
            foreach (var del in TestEvent.GetInvocationList().OfType<EventHandler<float>>())
            {
                if (del.Target == receiver)
                    TestEvent -= del;
            }
        }
    }

    public class TestOther
    {
        List<KeyValuePair<Test, EventHandler<float>>> handlers_ = new List<KeyValuePair<Test, EventHandler<float>>>();

        public void Link(Test t)
        {
            t.TestEvent += EventThing;
            handlers_.Add(new KeyValuePair<Test, EventHandler<float>>(t, EventThing));
        }

        void EventThing(object src, float val)
        {
            
        }

        public void Unsubscribe()
        {
            foreach (var handler in handlers_)
                handler.Key.Unsubscribe(this);
            handlers_.Clear();
        }
    }

    /// <summary>
    /// The interface exists so the EventObject can drop it's event handlers.
    /// </summary>
    public interface BaseCallback
    {
        void Remove();
        int EventName();
    }

    /// <summary>
    /// Primary interface for event handling.
    /// </summary>
    /// <typeparam name="T">parameter type for the target action</typeparam>
    public interface Callback<T> : BaseCallback
    {
        void Invoke(T value);
        object Target();
    }

    /// <summary>
    /// A target that needs to both dump it's callbacks and have it's callbacks dumped.
    /// Also may maintain a list of event signals that it will send.
    /// </summary>
    public class EventObject : IDisposable
    {
        internal List<BaseCallback> callbacks_ = new List<BaseCallback>();
        internal Dictionary<int, SignalBase> signals_ = new Dictionary<int, SignalBase>();

        /// Registers a named signal for event exposure.
        public void RegisterSignal(int eventID, SignalBase sig) { signals_.Add(eventID, sig); }

        /// Returns true if this listener is subscribed to the given sender
        public bool IsSubscribed(EventObject sender, int eventName)
        {
            SignalBase outVal = null;
            if (sender.signals_.TryGetValue(eventName, out outVal))
                return outVal.IsSubscribed(this);
            return false;
        }
        /// Subscribes an action for this object as a listener to an event an event on the sender
        public bool SubscribeToEvent<T>(EventObject sender, int eventName, Action<T> action)
        {
            SignalBase outVal = null;
            if (sender.signals_.TryGetValue(eventName, out outVal))
            {
                var sig = ((Signal<T>)outVal);
                if (!sig.IsSubscribed(this))
                    ((Signal<T>)outVal).Subscribe<EventObject>(this, action, eventName);
                return true;
            }
            return false;
        }
        /// Unsubscribes from the given event, regardless of the sender.
        public void UnsubscribeFromEvent(int eventName)
        {
            for (int i = 0; i < callbacks_.Count; ++i)
            {
                if (callbacks_[i].EventName() == eventName)
                {
                    callbacks_[i].Remove();
                    i -= 1;
                }
            }
        }
        /// Unsubscribes from the specific event, only if from the given sender.
        public void UnsubscribeFromEvent(EventObject sender, int eventName)
        {
            SignalBase outVal = null;
            if (sender.signals_.TryGetValue(eventName, out outVal))
            {
                var sig = ((SignalBase)outVal);
                sig.Unsubscribe(this);
            }
        }
        /// Unsubscribes this listener from all events.
        public void UnsubscribeFromAll()
        {
            for (int i = 0; i < callbacks_.Count; ++i)
                callbacks_[i].Remove();
            callbacks_.Clear();
        }

        /// Detaches self from listeners and senders.
        public virtual void Dispose()
        {
            UnsubscribeFromAll();
            foreach (var sig in signals_)
                sig.Value.Dispose();
            signals_.Clear();
            callbacks_.Clear();
        }
    }

    /// <summary>
    /// Final version of the callback types above.
    /// </summary>
    /// <typeparam name="T">The subscriber type</typeparam>
    /// <typeparam name="V">The handler method param type</typeparam>
    public struct CallbackImpl<T,V> : Callback<V> where T : EventObject
    {
        public Signal<V> source_;
        public T target_;
        public int eventName_;
        public Action<V> action_;

        public void Invoke(V value)
        {
            action_(value);
        }

        public object Target() { return target_; }
        public int EventName() { return eventName_; }
        public void Remove()
        {
            target_.callbacks_.Remove(this);
            source_ -= this;
        }
    }

    public interface SignalBase : IDisposable
    {
        bool IsSubscribed(object receiver);
        void Unsubscribe(object receiver);
    }

    /// <summary>
    /// A signal is like an event, objects can subscribe to it but they can trivially remove themselves from it without genuinely caring.
    /// Unlike events, this is meant for more robust removal.
    /// </summary>
    /// <typeparam name="T">Parameter the signal sends</typeparam>
    public class Signal<T> : SignalBase, IDisposable
    {
        List<Callback<T>> callbacks_ = new List<Callback<T>>();

        public void Invoke(T value)
        {
            for (int i = 0; i < callbacks_.Count; ++i)
                callbacks_[i].Invoke(value);
        }

        public bool IsSubscribed(object dest)
        {
            for (int i = 0; i < callbacks_.Count; ++i)
                if (callbacks_[i].Target() == dest)
                    return true;
            return false;
        }

        public void Subscribe<V>(V target, Action<T> action, int key) where V : EventObject
        {
            Unsubscribe(target);
            Callback<T> call = new CallbackImpl<V, T> { source_ = this, target_ = target, action_ = action, eventName_ = key };
            target.callbacks_.Add(call);
            callbacks_.Add(call);
        }

        public void UnsubscribeAll()
        {
            for (int i  = 0; i < callbacks_.Count; ++i)
            {
                callbacks_[i].Remove();
                i -= 1;
            }
            callbacks_.Clear();
        }

        public void Unsubscribe(object receiver)
        {
            for (int i = 0; i < callbacks_.Count; ++i)
            {
                if (callbacks_[i].Target() == receiver)
                {
                    callbacks_.RemoveAt(i);
                    i -= 1;
                }
            }
        }

        public void Dispose()
        {
            UnsubscribeAll();
        }

        public static Signal<T> operator+(Signal<T> self, Callback<T> callback)
        {
            self.callbacks_.Add(callback);
            return self;
        }

        public static Signal<T> operator-(Signal<T> self, Callback<T> callback)
        {
            self.callbacks_.Remove(callback);
            return self;
        }
    }
}
