// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

// ThreadPool.cs
//
// This file defines a custom ThreadPool class that supports the following
// characteristics (property and method names shown in []):
//
// * can be explicitly started and stopped (and restarted) [Start,Stop,StopAndWait]
//
// * configurable thread priority [Priority]
//
// * configurable foreground/background characteristic [IsBackground]
//
// * configurable minimum thread count (called 'static' or 'permanent' threads) [constructor]
//
// * configurable maximum thread count (threads added over the minimum are
//   called 'dynamic' threads) [constructor, MaxThreadCount]
//
// * configurable dynamic thread creation trigger (the point at which
//   the pool decides to add new threads) [NewThreadTrigger]
//
// * configurable dynamic thread decay interval (the time period
//   after which an idle dynamic thread will exit) [DynamicThreadDecay]
//
// * configurable limit (optional) to the request queue size (by default unbounded) [RequestQueueLimit]
//
// * pool extends WaitHandle, becomes signaled when last thread exits [StopAndWait, WaitHandle methods]
//
// * operations en-queued to the pool are cancellable [IWorkRequest returned by PostRequest]
//
// * enqueue operation supports early bound approach (ala ThreadPool.QueueUserWorkItem)
//   as well as late bound approach (ala Control.Invoke/BeginInvoke) to posting work requests [PostRequest]
//
// * optional propagation of calling thread call context to target [PropagateCallContext]
//
// * optional propagation of calling thread principal to target [PropagateThreadPrincipal]
//
// * optional propagation of calling thread HttpContext to target [PropagateHttpContext]
//
// * support for started/stopped event subscription & notification [Started, Stopped]
//
// Known issues/limitations/comments:
//
// * The PropagateCASMarkers property exists for future support for propagating
//   the calling thread's installed CAS markers in the same way that the built-in thread
//   pool does.  Currently, there is no support for user-defined code to perform that
//   operation.
//
// * PropagateCallContext and PropagateHttpContext both use reflection against private
//   members to due their job.  As such, these two properties are set to false by default,
//   but do work on the first release of the framework (including .NET Server) and its
//   service packs.  These features have not been tested on Everett at this time.
//
// Mike Woodring
// http://staff.develop.com/woodring
//
using System;
using System.Diagnostics;
using System.Threading;
using System.Collections;
using System.Security.Principal;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using Ptv.XServer.Controls.Map.Tools;

namespace DevelopMentor
{
    #region delegates
    /// <summary> Documentation in progress... </summary>
    /// <param name="state"> Documentation in progress... </param>
    /// <param name="requestEnqueueTime"> Documentation in progress... </param>
    internal delegate void WorkRequestDelegate(object state, DateTime requestEnqueueTime);

    /// <summary> Documentation in progress... </summary>
    internal delegate void ThreadPoolDelegate();
    #endregion

    #region IWorkRequest interface
    /// <summary> Documentation in progress... </summary>
    internal interface IWorkRequest
    {
        /// <summary> Documentation in progress... </summary>
        /// <returns> Documentation in progress... </returns>
        bool Cancel();
    }
    #endregion

    #region ThreadPool class
    /// <summary> Documentation in progress... </summary>
    internal sealed class ThreadPool : WaitHandle
    {
        /// <summary> Logging restricted to this class. </summary>
        private static readonly Logger logger = new Logger("ThreadPool");

        #region ThreadPool constructors
        /// <summary> Initializes a new instance of the <see cref="ThreadPool"/> class. </summary>
        /// <param name="initialThreadCount"> Documentation in progress... </param>
        /// <param name="maxThreadCount"> Documentation in progress... </param>
        /// <param name="poolName"> Documentation in progress... </param>
        internal ThreadPool(int initialThreadCount, int maxThreadCount, string poolName)
            : this(initialThreadCount, maxThreadCount, poolName,
                    DEFAULT_NEW_THREAD_TRIGGER_TIME,
                    DEFAULT_DYNAMIC_THREAD_DECAY_TIME,
                    DEFAULT_THREAD_PRIORITY,
                    DEFAULT_REQUEST_QUEUE_LIMIT)
        {
        }

        /// <summary> Initializes a new instance of the <see cref="ThreadPool"/> class. </summary>
        /// <param name="initialThreadCount"> Documentation in progress... </param>
        /// <param name="maxThreadCount"> Documentation in progress... </param>
        /// <param name="poolName"> Documentation in progress... </param>
        /// <param name="newThreadTrigger"> Documentation in progress... </param>
        /// <param name="dynamicThreadDecayTime"> Documentation in progress... </param>
        /// <param name="threadPriority"> Documentation in progress... </param>
        /// <param name="requestQueueLimit"> Documentation in progress... </param>
        public ThreadPool(int initialThreadCount, int maxThreadCount, string poolName,
                           int newThreadTrigger, int dynamicThreadDecayTime,
                           ThreadPriority threadPriority, int requestQueueLimit)
        {
            logger.Writeline(TraceEventType.Information, $"New thread pool {poolName} created:");
            logger.Writeline(TraceEventType.Information, $"  initial thread count:      {initialThreadCount}");
            logger.Writeline(TraceEventType.Information, $"  max thread count:          {maxThreadCount}");
            logger.Writeline(TraceEventType.Information, $"  new thread trigger:        {newThreadTrigger} ms");
            logger.Writeline(TraceEventType.Information, $"  dynamic thread decay time: {dynamicThreadDecayTime} ms");
            logger.Writeline(TraceEventType.Information, $"  request queue limit:       {requestQueueLimit} entries");

            SafeWaitHandle = stopCompleteEvent.SafeWaitHandle;

            if (maxThreadCount < initialThreadCount)
            {
                throw new ArgumentException("Maximum thread count must be >= initial thread count.", nameof(maxThreadCount));
            }

            if (dynamicThreadDecayTime <= 0)
            {
                throw new ArgumentException("Dynamic thread decay time cannot be <= 0.", nameof(dynamicThreadDecayTime));
            }

            if (newThreadTrigger <= 0)
            {
                throw new ArgumentException("New thread trigger time cannot be <= 0.", nameof(newThreadTrigger));
            }
            
            this.initialThreadCount = initialThreadCount;
            this.maxThreadCount = maxThreadCount;
            this.requestQueueLimit = (requestQueueLimit < 0 ? DEFAULT_REQUEST_QUEUE_LIMIT : requestQueueLimit);
            decayTime = dynamicThreadDecayTime;
            this.newThreadTrigger = new TimeSpan(TimeSpan.TicksPerMillisecond * newThreadTrigger);
            this.threadPriority = threadPriority;
            requestQueue = new Queue(requestQueueLimit < 0 ? 4096 : requestQueueLimit);

            threadPoolName = poolName ?? throw new ArgumentNullException(nameof(poolName), "Thread pool name cannot be null");
        }
        #endregion

        #region ThreadPool properties
        // The Priority & DynamicThreadDecay properties are not thread safe
        // and can only be set before Start is called.
        /// <summary> Gets or sets Documentation in progress... </summary>
        public ThreadPriority Priority
        {
            get => threadPriority;

            set
            {
                if (IsStarted)
                {
                    throw new InvalidOperationException("Cannot adjust thread priority after pool has been started.");
                }

                threadPriority = value;
            }
        }

        /// <summary> Gets or sets Documentation in progress... </summary>
        public int DynamicThreadDecay
        {
            get => (decayTime);

            set
            {
                if (IsStarted)
                {
                    throw new InvalidOperationException("Cannot adjust dynamic thread decay time after pool has been started.");
                }

                if (value <= 0)
                {
                    throw new ArgumentException("Dynamic thread decay time cannot be <= 0.", nameof(value));
                }

                decayTime = value;
            }
        }

        /// <summary> Gets or sets Documentation in progress... </summary>
        public int NewThreadTrigger
        {
            get => ((int)newThreadTrigger.TotalMilliseconds);

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("New thread trigger time cannot be <= 0.", nameof(value));
                }

                lock(this)
                {
                    newThreadTrigger = new TimeSpan(TimeSpan.TicksPerMillisecond * value);
                }
            }
        }

        /// <summary> Gets or sets Documentation in progress... </summary>
        public int RequestQueueLimit
        {
            get => (requestQueueLimit);
            set => requestQueueLimit = (value < 0 ? DEFAULT_REQUEST_QUEUE_LIMIT : value);
        }

        /// <summary> Gets Documentation in progress... </summary>
        public int AvailableThreads => maxThreadCount - currentThreadCount;

        /// <summary> Gets or sets Documentation in progress... </summary>
        public int MaxThreads
        {
            get => (maxThreadCount);

            set
            {
                if (value < initialThreadCount)
                {
                    throw new ArgumentException("Maximum thread count must be >= initial thread count.", nameof(maxThreadCount));
                }

                maxThreadCount = value;
            }
        }

        /// <summary> Gets a value indicating whether Documentation in progress... </summary>
        public bool IsStarted { get; private set; }

        /// <summary> Gets or sets a value indicating whether Documentation in progress... </summary>
        public bool PropagateThreadPrincipal
        {
            get => (propagateThreadPrincipal);
            set => propagateThreadPrincipal = value;
        }

        /// <summary> Gets or sets a value indicating whether Documentation in progress... </summary>
        public bool PropagateCallContext
        {
            get => (propagateCallContext);
            set => propagateCallContext = value;
        }

        /// <summary> Gets or sets a value indicating whether Documentation in progress... </summary>
        public bool PropagateHttpContext
        {
            get => (propagateHttpContext);
            set => propagateHttpContext = value;
        }

        /// <summary> Gets a value indicating whether Documentation in progress... </summary>
        public bool PropagateCASMarkers => false;


        /// <summary> Gets or sets a value indicating whether Documentation in progress... </summary>
        public bool IsBackground
        {
            get => (useBackgroundThreads);

            set
            {
                if (IsStarted)
                {
                    throw new InvalidOperationException("Cannot adjust background status after pool has been started.");
                }

                useBackgroundThreads = value;
            }
        }
        #endregion

        #region ThreadPool events
        /// <summary> Documentation in progress... </summary>
        public event ThreadPoolDelegate Started;
        /// <summary> Documentation in progress... </summary>
        public event ThreadPoolDelegate Stopped;
        #endregion

        #region ThreadPool.Start
        /// <summary> Documentation in progress... </summary>
        public void Start()
        {
            lock(this)
            {
                if (IsStarted)
                {
                    throw new InvalidOperationException("Pool has already been started.");
                }

                IsStarted = true;

                // Check to see if there were already items posted to the queue
                // before Start was called.  If so, reset their timestamps to
                // the current time.
                //
                if (requestQueue.Count > 0)
                {
                    ResetWorkRequestTimes();
                }

                for (int n = 0; n < initialThreadCount; n++)
                {
                    var thread = new ThreadWrapper(this, true, threadPriority, $"{threadPoolName} (static)");
                    thread.Start();
                }

                Started?.Invoke(); // TODO: reconsider firing this event while holding the lock...
            }
        }
        #endregion

        #region ThreadPool.Stop and InternalStop
        /// <summary> Documentation in progress... </summary>
        public void Stop()
        {
            InternalStop(false, Timeout.Infinite);
        }

        /// <summary> Documentation in progress... </summary>
        public void StopAndWait()
        {
            InternalStop(true, Timeout.Infinite);
        }

        /// <summary> Documentation in progress... </summary>
        /// <param name="timeout"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public bool StopAndWait(int timeout)
        {
            return InternalStop(true, timeout);
        }

        /// <summary> Documentation in progress... </summary>
        /// <param name="wait"> Documentation in progress... </param>
        /// <param name="timeout"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        private bool InternalStop(bool wait, int timeout)
        {
            if (!IsStarted)
                throw new InvalidOperationException("Cannot stop a thread pool that has not been started yet.");

            lock(this)
            {
                logger.Writeline(TraceEventType.Information,
                    $"[{Thread.CurrentThread.ManagedThreadId}, {Thread.CurrentThread.Name}] Stopping pool (# threads = {currentThreadCount})");

                stopInProgress = true;
                Monitor.PulseAll(this);
            }

            if (!wait) return true;

            if (!WaitOne(timeout, true)) return false;

            // If the stop was successful, we can support being
            // to be restarted.  If the stop was requested, but not
            // waited on, then we don't support restarting.
            IsStarted = false;
            stopInProgress = false;
            requestQueue.Clear();
            stopCompleteEvent.Reset();

            return true;
        }
        #endregion

        #region ThreadPool.PostRequest(early bound)
        // Overloads for the early bound WorkRequestDelegate-based targets.
        /// <summary> Documentation in progress... </summary>
        /// <param name="cb"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public bool PostRequest(WorkRequestDelegate cb)
        {
            return PostRequest(cb, (object)null);
        }

        /// <summary> Documentation in progress... </summary>
        /// <param name="cb"> Documentation in progress... </param>
        /// <param name="state"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public bool PostRequest(WorkRequestDelegate cb, object state)
        {
            return PostRequest(cb, state, out _);
        }

        /// <summary> Documentation in progress... </summary>
        /// <param name="cb"> Documentation in progress... </param>
        /// <param name="state"> Documentation in progress... </param>
        /// <param name="reqStatus"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public bool PostRequest(WorkRequestDelegate cb, object state, out IWorkRequest reqStatus)
        {
            var request = new WorkRequest(cb, state, propagateThreadPrincipal, propagateCallContext, PropagateCASMarkers);
            reqStatus = request;
            return PostRequest(request);
        }
        #endregion

        #region ThreadPool.PostRequest(late bound)
        // Overloads for the late bound Delegate.DynamicInvoke-based targets.
        /// <summary> Documentation in progress... </summary>
        /// <param name="cb"> Documentation in progress... </param>
        /// <param name="args"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public bool PostRequest(Delegate cb, object[] args)
        {
            return PostRequest(cb, args, out _);
        }

        /// <summary> Documentation in progress... </summary>
        /// <param name="cb"> Documentation in progress... </param>
        /// <param name="args"> Documentation in progress... </param>
        /// <param name="reqStatus"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public bool PostRequest(Delegate cb, object[] args, out IWorkRequest reqStatus)
        {
            var request = new WorkRequest(cb, args, propagateThreadPrincipal, propagateCallContext, PropagateCASMarkers);
            reqStatus = request;
            return PostRequest(request);
        }
        #endregion

        // The actual implementation of PostRequest.
        /// <summary> Documentation in progress... </summary>
        /// <param name="request"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        bool PostRequest(WorkRequest request)
        {
            lock(this)
            {
                // A requestQueueLimit of -1 means the queue is "unbounded"
                // (subject to available resources).  IOW, no artificial limit
                // has been placed on the maximum # of requests that can be
                // placed into the queue.
                //
                if ((requestQueueLimit != -1) && (requestQueue.Count >= requestQueueLimit)) return false;

                try
                {
                    requestQueue.Enqueue(request);
                    Monitor.Pulse(this);
                    return true;
                }
                catch { return false; }
            }
        }

        /// <summary> Documentation in progress... </summary>
        void ResetWorkRequestTimes()
        {
            lock(this)
            {
                var newTime = DateTime.Now; // DateTime.Now.Add(pool.newThreadTrigger);

                foreach (WorkRequest wr in requestQueue)
                {
                    wr.workingTime = newTime;
                }
            }
        }

        #region Private ThreadPool constants
        // Default parameters.
        /// <summary> Documentation in progress... </summary>
        const int DEFAULT_DYNAMIC_THREAD_DECAY_TIME = 5 /* minutes */ * 60 /* sec/min */ * 1000 /* ms/sec */;
        /// <summary> Documentation in progress... </summary>
        const int DEFAULT_NEW_THREAD_TRIGGER_TIME = 500; // milliseconds
        /// <summary> Documentation in progress... </summary>
        const ThreadPriority DEFAULT_THREAD_PRIORITY = ThreadPriority.Normal;
        /// <summary> Documentation in progress... </summary>
        const int DEFAULT_REQUEST_QUEUE_LIMIT = -1; // unbounded
        #endregion

        #region Private ThreadPool member variables
        private bool stopInProgress;
        private readonly string threadPoolName;
        /// <summary> Initial # of threads to create (called "static threads" in this class). </summary>
        private readonly int initialThreadCount;
        /// <summary> Cap for thread count.  Threads added above initialThreadCount are called "dynamic" threads. </summary>
        private int maxThreadCount;
        /// <summary> Current # of threads in the pool (static + dynamic). </summary>
        private int currentThreadCount;
        /// <summary> If a dynamic thread is idle for this period of time w/o processing work requests, it will exit. </summary>
        private int decayTime;
        /// <summary> If a work request sits in the queue this long before being processed, a new thread will be added to queue up to the max. </summary>
        private TimeSpan newThreadTrigger;
        private ThreadPriority threadPriority;
        /// <summary> Signaled after Stop called and last thread exits. </summary>
        private readonly ManualResetEvent stopCompleteEvent = new ManualResetEvent(false);
        private readonly Queue requestQueue;
        /// <summary> Throttle for maximum # of work requests that can be added. </summary>
        private int requestQueueLimit;
        private bool useBackgroundThreads = true;
        private bool propagateThreadPrincipal;
        private bool propagateCallContext;
        private bool propagateHttpContext;

        #endregion

        #region ThreadPool.ThreadInfo
        /// <summary> Documentation in progress... </summary>
        class ThreadInfo
        {
            #region private variables
            /// <summary> Documentation in progress... </summary>
            private readonly IPrincipal principal;
            /// <summary> Documentation in progress... </summary>
            private readonly LogicalCallContext callContext;
            /// <summary> Documentation in progress... </summary>
            private readonly CompressedStack compressedStack = null; // Always null until Get/SetCompressedStack are opened up.
            // Cached type information.
            /// <summary> Documentation in progress... </summary>
            const BindingFlags bfNonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;

            /// <summary> Documentation in progress... </summary>
            static readonly MethodInfo miGetLogicalCallContext = typeof(Thread).GetMethod("GetLogicalCallContext", bfNonPublicInstance);
            /// <summary> Documentation in progress... </summary>
            static readonly MethodInfo miSetLogicalCallContext = typeof(Thread).GetMethod("SetLogicalCallContext", bfNonPublicInstance);
            #endregion

            #region constructor

            /// <summary> Documentation in progress... </summary>
            /// <param name="propagateThreadPrincipal"> Documentation in progress... </param>
            /// <param name="propagateCallContext"> Documentation in progress... </param>
            /// <param name="propagateCASMarkers"> Documentation in progress... </param>
            /// <returns> Documentation in progress... </returns>
            public static ThreadInfo Capture(bool propagateThreadPrincipal, bool propagateCallContext, bool propagateCASMarkers)
            {
                return new ThreadInfo(propagateThreadPrincipal, propagateCallContext, propagateCASMarkers);
            }

            /// <summary> Documentation in progress... </summary>
            /// <param name="ti"> Documentation in progress... </param>
            /// <returns> Documentation in progress... </returns>
            public static ThreadInfo Impersonate(ThreadInfo ti)
            {
                if (ti == null) throw new ArgumentNullException(nameof(ti));

                ThreadInfo prevInfo = Capture(true, true, true);
                Restore(ti);
                return(prevInfo);
            }

            /// <summary> Initializes a new instance of the <see cref="ThreadInfo"/> class. </summary>
            /// <param name="propagateThreadPrincipal"> Documentation in progress... </param>
            /// <param name="propagateCallContext"> Documentation in progress... </param>
            /// <param name="propagateCASMarkers"> Documentation in progress... </param>
            private ThreadInfo(bool propagateThreadPrincipal, bool propagateCallContext, bool propagateCASMarkers)
            {
                if (propagateThreadPrincipal)
                {
                    principal = Thread.CurrentPrincipal;
                }

                if (propagateCallContext && (miGetLogicalCallContext != null))
                {
                    callContext = (LogicalCallContext)miGetLogicalCallContext.Invoke(Thread.CurrentThread, null);
                    callContext = (LogicalCallContext)callContext.Clone();

                    // TODO: consider serialize/deserialize call context to get a MBV snapshot
                    //       instead of leaving it up to the Clone method.
                }

                if (propagateCASMarkers)
                {
                    // TODO: Uncomment the following when Thread.GetCompressedStack is no longer guarded
                    //       by a StrongNameIdentityPermission.
                    //
                    // compressedStack = Thread.CurrentThread.GetCompressedStack();
                }
            }
            #endregion

            #region public methods
            /// <summary> Documentation in progress... </summary>
            /// <param name="ti"> Documentation in progress... </param>
            public static void Restore(ThreadInfo ti)
            {
                if (ti == null) throw new ArgumentNullException(nameof(ti));

                // Restore call context.
                //
                miSetLogicalCallContext?.Invoke(Thread.CurrentThread, new object[]{ti.callContext});

                // Restore HttpContext with the moral equivalent of
                // HttpContext.Current = ti.httpContext;
                //
                //CallContext.SetData(HttpContextSlotName, ti.httpContext);
                
                // Restore thread identity.  It's important that this be done after
                // restoring call context above, since restoring call context also
                // overwrites the current thread principal setting.  If propagateCallContext
                // and propagateThreadPrincipal are both true, then the following is redundant.
                // However, since propagating call context requires the use of reflection
                // to capture/restore call context, I want that behavior to be independently
                // switchable so that it can be disabled; while still allowing thread principal
                // to be propagated.  This also covers us in the event that call context
                // propagation changes so that it no longer propagates thread principal.
                //
                Thread.CurrentPrincipal = ti.principal;
                
                if (ti.compressedStack != null)
                {
                    // TODO: Uncomment the following when Thread.SetCompressedStack is no longer guarded
                    //       by a StrongNameIdentityPermission.
                    //
                    // Thread.CurrentThread.SetCompressedStack(ti.compressedStack);
                }
            }
            #endregion
        }

        #endregion

        #region ThreadPool.WorkRequest
        /// <summary> Documentation in progress... </summary>
        class WorkRequest : IWorkRequest
        {
            #region internal consts
            /// <summary> Documentation in progress... </summary>
            internal const int PENDING = 0;
            /// <summary> Documentation in progress... </summary>
            internal const int PROCESSED = 1;
            /// <summary> Documentation in progress... </summary>
            internal const int CANCELLED = 2;
            #endregion

            #region internal variables
            /// <summary> Documentation in progress... </summary>
            internal readonly Delegate targetProc;         // Function to call.
            /// <summary> Documentation in progress... </summary>
            internal readonly object procArg;            // State to pass to function.
            /// <summary> Documentation in progress... </summary>
            internal readonly object[] procArgs;           // Used with Delegate.DynamicInvoke.
            /// <summary> Documentation in progress... </summary>
            internal DateTime timeStampStarted;   // Time work request was originally en-queued (held constant).
            /// <summary> Documentation in progress... </summary>
            internal DateTime workingTime;        // Current timestamp used for triggering new threads (moving target).
            /// <summary> Documentation in progress... </summary>
            internal ThreadInfo threadInfo;         // Everything we know about a thread.
            /// <summary> Documentation in progress... </summary>
            internal int state = PENDING;    // The state of this particular request.
            #endregion

            #region constructor

            /// <summary> Initializes a new instance of the <see cref="WorkRequest"/> class. </summary>
            /// <param name="cb"> Documentation in progress... </param>
            /// <param name="arg"> Documentation in progress... </param>
            /// <param name="propagateThreadPrincipal"> Documentation in progress... </param>
            /// <param name="propagateCallContext"> Documentation in progress... </param>
            /// <param name="propagateCASMarkers"> Documentation in progress... </param>
            public WorkRequest(WorkRequestDelegate cb, object arg,
                                bool propagateThreadPrincipal, bool propagateCallContext, bool propagateCASMarkers)
            {
                targetProc = cb;
                procArg = arg;
                procArgs = null;

                Initialize(propagateThreadPrincipal, propagateCallContext, propagateCASMarkers);
            }

            /// <summary> Initializes a new instance of the <see cref="WorkRequest"/> class. </summary>
            /// <param name="cb"> Documentation in progress... </param>
            /// <param name="args"> Documentation in progress... </param>
            /// <param name="propagateThreadPrincipal"> Documentation in progress... </param>
            /// <param name="propagateCallContext"> Documentation in progress... </param>
            /// <param name="propagateCASMarkers"> Documentation in progress... </param>
            public WorkRequest(Delegate cb, object[] args, bool propagateThreadPrincipal, bool propagateCallContext, bool propagateCASMarkers)
            {
                targetProc = cb;
                procArg = null;
                procArgs = args;

                Initialize(propagateThreadPrincipal, propagateCallContext, propagateCASMarkers);
            }

            /// <summary> Documentation in progress... </summary>
            /// <param name="propagateThreadPrincipal"> Documentation in progress... </param>
            /// <param name="propagateCallContext"> Documentation in progress... </param>
            /// <param name="propagateCASMarkers"> Documentation in progress... </param>
            void Initialize(bool propagateThreadPrincipal, bool propagateCallContext, bool propagateCASMarkers)
            {
                workingTime = timeStampStarted = DateTime.Now;
                threadInfo = ThreadInfo.Capture(propagateThreadPrincipal, propagateCallContext, propagateCASMarkers);
            }
            #endregion

            #region public methods
            /// <summary> Documentation in progress... </summary>
            /// <returns> Documentation in progress... </returns>
            public bool Cancel()
            {
                // If the work request was pending, mark it cancelled.  Otherwise,
                // this method was called too late.  Note that this call can
                // cancel an operation without any race conditions.  But if the
                // result of this test-and-set indicates the request is in the
                // "processed" state, it might actually be about to be processed.
                //
                return(Interlocked.CompareExchange(ref state, CANCELLED, PENDING) == PENDING);
            }
            #endregion
        }
        #endregion

        #region ThreadPool.ThreadWrapper
        /// <summary> Documentation in progress... </summary>
        class ThreadWrapper
        {
            #region private variables
            /// <summary> Documentation in progress... </summary>
            readonly ThreadPool pool;
            /// <summary> Documentation in progress... </summary>
            readonly bool isPermanent;
            /// <summary> Documentation in progress... </summary>
            readonly ThreadPriority priority;
            /// <summary> Documentation in progress... </summary>
            readonly string name;
            #endregion

            #region constructor
            /// <summary> Initializes a new instance of the <see cref="ThreadWrapper"/> class. </summary>
            /// <param name="pool"> Documentation in progress... </param>
            /// <param name="isPermanent"> Documentation in progress... </param>
            /// <param name="priority"> Documentation in progress... </param>
            /// <param name="name"> Documentation in progress... </param>
            public ThreadWrapper(ThreadPool pool, bool isPermanent,
                                  ThreadPriority priority, string name)
            {
                this.pool = pool;
                this.isPermanent = isPermanent;
                this.priority = priority;
                this.name = name;

                lock(pool)
                {
                    // Update the total # of threads in the pool.
                    //
                    pool.currentThreadCount++;
                }
            }
            #endregion

            #region public methods
            /// <summary> Documentation in progress... </summary>
            public void Start()
            {
                var t = new Thread(ThreadProc);
                t.SetApartmentState(ApartmentState.MTA);
                t.Name = name;
                t.Priority = priority;
                t.IsBackground = pool.useBackgroundThreads;
                t.Start();
            }
            #endregion

            #region private methods
            /// <summary> Documentation in progress... </summary>
            void ThreadProc()
            {
                logger.Writeline(TraceEventType.Information,
                    $"[{Thread.CurrentThread.ManagedThreadId}, {Thread.CurrentThread.Name}] Worker thread started");
                bool done = false;

                while (!done)
                {
                    WorkRequest wr = null;
                    ThreadWrapper newThread = null;

                    lock(pool)
                    {
                        // As long as the request queue is empty and a shutdown hasn't
                        // been initiated, wait for a new work request to arrive.
                        //
                        bool timedOut = false;

                        while (!pool.stopInProgress && !timedOut && (pool.requestQueue.Count == 0))
                        {
                            if (!Monitor.Wait(pool, (isPermanent ? Timeout.Infinite : pool.decayTime)))
                            {
                                // Timed out waiting for something to do.  Only dynamically created
                                // threads will get here, so bail out.
                                //
                                timedOut = true;
                            }
                        }

                        // We exited the loop above because one of the following conditions
                        // was met:
                        //   - ThreadPool.Stop was called to initiate a shutdown.
                        //   - A dynamic thread timed out waiting for a work request to arrive.
                        //   - There are items in the work queue to process.

                        // If we exited the loop because there's work to be done,
                        // a shutdown hasn't been initiated, and we aren't a dynamic thread
                        // that timed out, pull the request off the queue and prepare to
                        // process it.
                        //
                        if (!pool.stopInProgress && !timedOut && (pool.requestQueue.Count > 0))
                        {
                            wr = (WorkRequest)pool.requestQueue.Dequeue();
                            Debug.Assert(wr != null);

                            // Check to see if this work request languished in the queue
                            // very long.  If it was in the queue >= the new thread trigger
                            // time, and if we haven't reached the max thread count cap,
                            // add a new thread to the pool.
                            //
                            // If the decision is made, create the new thread object (updating
                            // the current # of threads in the pool), but defer starting the new
                            // thread until the lock is released.
                            //
                            TimeSpan requestTimeInQ = DateTime.Now.Subtract(wr.workingTime);

                            if ((requestTimeInQ >= pool.newThreadTrigger) && (pool.currentThreadCount < pool.maxThreadCount))
                            {
                                // Note - the constructor for ThreadWrapper will update
                                // pool.currentThreadCount.
                                //
                                newThread = new ThreadWrapper(pool, false, priority, $"{pool.threadPoolName} (dynamic)");

                                // Since the current request we just dequeued is stale,
                                // everything else behind it in the queue is also stale.
                                // So reset the timestamps of the remaining pending work
                                // requests so that we don't start creating threads
                                // for every subsequent request.
                                //
                                pool.ResetWorkRequestTimes();
                            }
                        }
                        else
                        {
                            // Should only get here if this is a dynamic thread that
                            // timed out waiting for a work request, or if the pool
                            // is shutting down.
                            //
                            Debug.Assert((timedOut && !isPermanent) || pool.stopInProgress);
                            pool.currentThreadCount--;

                            if (pool.currentThreadCount == 0)
                            {
                                // Last one out turns off the lights.
                                //
                                Debug.Assert(pool.stopInProgress);

                                pool.Stopped?.Invoke();

                                pool.stopCompleteEvent.Set();
                            }

                            done = true;
                        }
                    } // lock

                    // No longer holding pool lock here...

                    if (done || (wr == null)) continue;

                    // Check to see if this request has been cancelled while
                    // stuck in the work queue.
                    //
                    // If the work request was pending, mark it processed and proceed
                    // to handle.  Otherwise, the request must have been cancelled
                    // before we plucked it off the request queue.
                    //
                    if (Interlocked.CompareExchange(ref wr.state, WorkRequest.PROCESSED, WorkRequest.PENDING) != WorkRequest.PENDING)
                    {
                        // Request was cancelled before we could get here.
                        // Bail out.
                        continue;
                    }

                    if (newThread != null)
                    {
                        logger.Writeline(TraceEventType.Information,
                            $"[{Thread.CurrentThread.ManagedThreadId}, {Thread.CurrentThread.Name}] Adding dynamic thread to pool");
                        newThread.Start();
                    }

                    // Dispatch the work request.
                    //
                    ThreadInfo originalThreadInfo = null;

                    try
                    {
                        // Impersonate (as much as possible) what we know about
                        // the thread that issued the work request.
                        //
                        originalThreadInfo = ThreadInfo.Impersonate(wr.threadInfo);

                        if (wr.targetProc is WorkRequestDelegate targetProc)
                        {
                            targetProc(wr.procArg, wr.timeStampStarted);
                        }
                        else
                        {
                            wr.targetProc.DynamicInvoke(wr.procArgs);
                        }
                    }
                    catch(Exception e)
                    {
                        logger.Writeline(TraceEventType.Information, $"Exception thrown performing callback:\n{e.Message}\n{e.StackTrace}");
                    }
                    finally
                    {
                        // Restore our worker thread's identity.
                        //
                        ThreadInfo.Restore(originalThreadInfo);
                    }
                }

                Debug.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}, {Thread.CurrentThread.Name}] Worker thread exiting pool");
            }
            #endregion
        }
        #endregion
    }
    #endregion

}
