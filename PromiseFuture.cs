using System;
using System.Threading.Tasks;
using System.Threading;

namespace PromiseFuture
{
    public class Future
    {
        public static Promise<TResult> Bring<TResult>(Func<TResult> byusing)
        {
            return new Promise<TResult>(byusing);
        }

        public static SupervisedPromise<TResult> Bring<TResult>(Func<FutureSupervisor, TResult> byusing)
        {
            return new SupervisedPromise<TResult>(byusing);
        }

        public static Promise<object> Bring(dynamic byusing, bool supervised = false)
        {
            if (supervised)
                return Future.Bring<object>((Func<FutureSupervisor, object>)byusing);
            else
                return Future.Bring<object>((Func<object>)byusing);
        }

        private static TaskScheduler current;

        internal static TaskScheduler CurrentScheduler
        {
            get { return current; }
            private set { current = value; }
        }

        public static void UnderGUI()
        {
            if (CurrentScheduler != null)
                return;
            if (SynchronizationContext.Current == null)
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            CurrentScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }
    }


    public class Promise<TResult>
    {
        Func<TResult> _byusing = null;

        internal Promise(Func<TResult> byusing)
        {
            _byusing = byusing;
        }

        public Promise<TNextResult> Later<TNextResult>(Func<TResult, TNextResult> byusing)
        {
            Func<TNextResult> f = () =>
                byusing(this._byusing());
            return new Promise<TNextResult>(f);
        }

        public Promise<TResult> Unless<TExcept>(Func<TExcept, TResult> onerror)
            where TExcept : Exception
        {
            var f_old = this._byusing;
            Func<TResult> f_new = () => {
                try {
                    return f_old();
                } catch (TExcept e) {
                    return onerror(e);
                }
                return default(TResult);
            };
            this._byusing = f_new;
            return this;
        }

        public Future<TResult> Start()
        {
            return Start(FutureOptions.Async);
        }

        virtual public Future<TResult> Start(FutureOptions options)
        {
            return new Future<TResult>(this._byusing, options);
        }

        public Promise<object> Unless(dynamic onerror)
        {
            object prom = this;
            if (prom is SupervisedPromise<object>)
                return ((SupervisedPromise<object>)prom).Unless<Exception>((Func<Exception, object>)onerror);
            if (prom is Promise<object>)
                return ((Promise<object>)prom).Unless<Exception>((Func<Exception, object>)onerror);
            return null;
        }

        public Promise<object> Later(dynamic byusing)
        {
            object prom = this;
            if (prom is SupervisedPromise<object>)
                return ((SupervisedPromise<object>)prom)
                    .Later<object>((Func<FutureSupervisor, object, object>)byusing);
            if (prom is Promise<object>)
                return ((Promise<object>)prom)
                    .Later<object>((Func<object, object>)byusing);
            return null;
        }
    }

    public class SupervisedPromise<TResult> : Promise<TResult>
    {
        Func<FutureSupervisor, TResult> _byusingsupervised = null;

        internal SupervisedPromise(Func<FutureSupervisor, TResult> byusing)
            : base(null)
        {
            _byusingsupervised = byusing;
        }

        public SupervisedPromise<TNextResult> Later<TNextResult>(Func<FutureSupervisor, TResult, TNextResult> byusing)
        {
            Func<FutureSupervisor, TNextResult> f = (canceltoken) =>
                byusing(canceltoken, this._byusingsupervised(canceltoken));
            return new SupervisedPromise<TNextResult>(f);
        }

        new public SupervisedPromise<TResult> Unless<TExcept>(Func<TExcept, TResult> onerror)
            where TExcept : Exception
        {
            var f_old = this._byusingsupervised;
            Func<FutureSupervisor, TResult> f_new = (cancel) => {
                try {
                    return f_old(cancel);
                } catch (TExcept e) {
                    return onerror(e);
                }
                return default(TResult);
            };
            this._byusingsupervised = f_new;
            return this;
        }

        override public Future<TResult> Start(FutureOptions options)
        {
            return new Future<TResult>(this._byusingsupervised, options);
        }

    }

    public enum FutureOptions
    {
        Async,
        Synchronous,
        LongRunning,
        ResultInGUI
    }

    public class FutureSupervisor
    {
        CancellationToken _token = CancellationToken.None;

        internal FutureSupervisor(CancellationToken token)
        {
            _token = token;
        }

        public bool CancelRequested {
            get { return _token.IsCancellationRequested; }
        }
    }

    public class Future<T>
    {
        Func<FutureSupervisor, T> _fulfillmentsupervised = null;
        Func<T> _fulfillment = null;
        Task<T> _task = null;
        CancellationTokenSource _cts = null;
        T _result = default(T);
        bool _fulfilled = false;
        FutureOptions _options = FutureOptions.Async;

        internal Future(Func<FutureSupervisor, T> fulfillment, FutureOptions options)
        {
            _fulfillmentsupervised = fulfillment;
            _options = options;
            launchTask();
        }

        internal Future(Func<T> fulfillment, FutureOptions options)
        {
            _fulfillment = fulfillment;
            _options = options;
            launchTask();
        }

        private void launchTask()
        {
            var topt = TaskCreationOptions.None;
            if (_options == FutureOptions.LongRunning)
                topt = TaskCreationOptions.LongRunning;

            if (_fulfillment != null) {
                _task = new Task<T>(() => {
                    return _fulfillment();
                }, topt);
            }
            if (_fulfillmentsupervised != null) {
                _cts = new System.Threading.CancellationTokenSource();
                var supervisor = new FutureSupervisor(_cts.Token);
                _task = new Task<T>(() => {
                    return _fulfillmentsupervised(supervisor);
                }, _cts.Token, topt);
            }
            if (_options != FutureOptions.Synchronous) {
                var sched = TaskScheduler.Current;
                if (_options == FutureOptions.ResultInGUI)
                    sched = Future.CurrentScheduler;
                _task.Start(sched);
            }
        }

        public T Value
        {
            get { return this.getResult(); }
        }

        private T getResult()
        {
            if (_fulfilled)
                return _result;

            if (_task != null) {
                try {
                    if (_options == FutureOptions.Synchronous) {
                        _task.RunSynchronously();
                    }
                    _task.Wait();
                    _result = _task.Result;
                    _fulfilled = true;
                    return _result;
                } catch (AggregateException ae) {
                    Cancelled = true;
                    Faults = new AggregateException(ae.Flatten().InnerExceptions);
                    if (_options == FutureOptions.Synchronous &&
                                       Faults.InnerExceptions.Count > 0) {
                        // rethrow if running synchronous.
                        throw Faults.InnerExceptions[0];
                    }
                } finally {
                    _task.Dispose();
                }
            }
            return default(T);
        }

        public void Cancel()
        {
            if (_task != null && _cts != null)
                _cts.Cancel();
            Cancelled = true;
        }

        public bool Cancelled { get; private set; }

        public AggregateException Faults { get; private set; }

        public static implicit operator T(Future<T> Future)
        {
            return Future.Value;
        }
    }
}
