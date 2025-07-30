using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace DelveLib
{

    /// <summary>
    /// For singleton types, marks the type to require initialization.
    /// </summary>
    /// <exception>
    /// DO NOT USE! Forces full assembly load at startup ... very slow if not NGen'ed first
    /// </exception>
    [System.AttributeUsage(AttributeTargets.Class)]
    public class IOCInitializedAttribute : System.Attribute
    {
        public static void Execute()
        {
            Assembly asm = Assembly.GetEntryAssembly();
            //try
            {
                var types = asm.GetTypes();
                foreach (Type t in types)
                {
                    var attr = t.GetCustomAttribute<IOCInitializedAttribute>();
                    if (attr != null)
                    {
                        // assumes that the type (or some basetype) handles any static list management
                        Activator.CreateInstance(t);
                    }
                }
                //} catch (Exception)
                //{

            }
        }
    }

    /// <summary>
    /// While null it will automatically check to seek out
    /// </summary>
    /// <typeparam name="T">Type of the object we we to acquire</typeparam>
    public class IOCDependency<T> where T : class
    {
        public delegate void DependencyResolved(IOCDependency<T> self);

        public event DependencyResolved DependencyResolvedHandler = delegate { };

        T object_;
        object relTo_;
        int iocGeneration_ = -1; // always start as "don't care generation"

        /// <summary>
        /// Construct as a top-level dependency
        /// </summary>
        public IOCDependency()
        {

        }

        /// <summary>
        /// Construct a relative dependency.
        /// </summary>
        /// <param name="relTo">object we need to key off of to resolve the dependency</param>
        public IOCDependency(object relTo)
        {
            relTo_ = relTo;
        }

        public bool IsValid { get { return Object != null; } }

        public T Object
        {
            get
            {
                if (object_ != null)
                    return object_;

                // Don't have it so we need to find it
                if (relTo_ != null)
                    object_ = IOCLite.GetRelativeObject<T>(relTo_, iocGeneration_);
                else
                    object_ = IOCLite.GetObject<T>(iocGeneration_);

                // Update our generation so we skip early
                iocGeneration_ = IOCLite.Generation();

                // We now have it so send an event
                if (object_ != null)
                    DependencyResolvedHandler(this);

                return object_;
            }
        }
    }

    /// <summary>
    /// Objects are registered into the IoCLite container for lookup by IoCDependencies
    /// </summary>
    public class IOCLite
    {
        static IOCLite inst_;
        List<object> objects_ = new List<object>();
        Dictionary<object, List<object>> relativeObjects_ = new Dictionary<object, List<object>>();
        int iocGeneration_ = 0;

        /// <summary>
        /// Prevent unintended construction.
        /// </summary>
        private IOCLite() { }

        /// <summary>
        /// Access static singleton, initialize if necessary
        /// </summary>
        /// <returns>Reference to the singleton instance</returns>
        public static IOCLite inst()
        {
            if (inst_ == null)
                inst_ = new IOCLite();
            return inst_;
        }

        public static int Generation() { return inst().iocGeneration_; }

        /// <summary>
        /// Register an object for tracking in the container.
        /// </summary>
        /// <param name="obj">Object to store</param>
        public static void Register(object obj)
        {
            inst().objects_.Add(obj);
            inst().iocGeneration_ += 1;
        }

        /// <summary>
        /// Brute force removal
        /// </summary>
        /// <param name="obj">Object to remove, if this object is a key then EVERYTHING will be removed</param>
        public static void Unregister(object obj)
        {
            inst().objects_.Remove(obj);
            inst().relativeObjects_.Remove(obj);
            foreach (var key in inst().relativeObjects_)
                key.Value.Remove(obj);
        }

        /// <summary>
        /// Register an object with a key for relative lookup.
        /// </summary>
        /// <param name="relTo">Key to be used for the given "obj"</param>
        /// <param name="obj">Object to register into the table</param>
        public static void RegisterRelative(object relTo, object obj)
        {
            inst().iocGeneration_ += 1;
            var relObjects = inst().relativeObjects_;
            if (relObjects.ContainsKey(relTo))
                relObjects[relTo].Add(obj);
            else
            {
                relObjects[relTo] = new List<object>();
                relObjects[relTo].Add(obj);
            }
        }

        /// <summary>
        /// Retrieve an object reference from storage.
        /// </summary>
        /// <typeparam name="T">Type of the object to be sought</typeparam>
        /// <param name="generation">Optional 'generation' key</param>
        /// <returns>The found object or null</returns>
        public static T GetObject<T>(int generation = -1) where T : class
        {
            if (generation != -1 && generation >= inst().iocGeneration_)
                return null;
            Type tType = typeof(T);
            for (int i = 0; i < inst().objects_.Count; ++i)
            {
                if (inst().objects_[i].GetType() == tType || tType.IsAssignableFrom(inst().objects_[i].GetType()))
                    return inst().objects_[i] as T;
            }
            return null;
        }

        /// <summary>
        /// Grab an object relative to a given key object.
        /// </summary>
        /// <typeparam name="T">Type of object to acquire</typeparam>
        /// <param name="relTo">"Key" object used for looking up the right object</param>
        /// <param name="generation">A generation index to use for verifying</param>
        /// <returns>The found object or null</returns>
        public static T GetRelativeObject<T>(object relTo, int generation = -1) where T : class
        {
            if (generation != -1 && generation >= inst().iocGeneration_)
                return null;
            Type tType = typeof(T);
            var dict = inst().relativeObjects_;
            if (dict.ContainsKey(relTo))
            {
                var objects = dict[relTo];
                for (int i = 0; i < objects.Count; ++i)
                {
                    if (objects[i].GetType() == tType)
                        return objects[i] as T;
                }
            }
            return null;
        }
    }
}
