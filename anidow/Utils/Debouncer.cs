using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Timers;
using System.Windows.Threading;
using Timer = System.Timers.Timer;

/// <summary>
/// Source: https://gist.github.com/yui-konnu/41fc925a1df4ff98fec6152cbe2184cd
/// </summary>

namespace Anidow.Utils
{
    /// <summary>
    ///     Event debouncer helps to prevent calling the same event handler too often (like mark Dirty or Invalidate)
    /// </summary>
    public static class Debouncer
    {
        private static readonly ConcurrentDictionary<object, DebounceRegistration> _activeDebouncers =
            new ConcurrentDictionary<object, DebounceRegistration>();

        /// <summary>
        ///     Debounce abstract key-ed object
        /// </summary>
        /// <typeparam name="TConfig"></typeparam>
        /// <param name="key"></param>
        /// <param name="createConfigIfNotExist"></param>
        /// <param name="configDebounce"></param>
        public static void Debounce<TConfig>(object key, Func<TConfig> createConfigIfNotExist,
            Action<TConfig> configDebounce)
            where TConfig : IConfig
        {
            var created = false;
            var debouncer = _activeDebouncers.GetOrAdd(key, k =>
            {
                created = true;

                var config = createConfigIfNotExist();
                var res = new DebounceRegistration
                {
                    Key = key,
                    Config = config
                };
                configDebounce(config);
                return res;
            });
            using (debouncer.Lock())
            {
                if (!created)
                {
                    configDebounce((TConfig) debouncer.Config);
                }

                debouncer.Debounce();
            }
        }

        public static void DebounceAction(object key, Action<ActionConfig> actionOnTimeout, Dispatcher sync,
            TimeSpan? timeout = null)
        {
            DebounceActionCustom(key, d =>
            {
                d.OnTimeout = actionOnTimeout;
                d.SyncronizationObject = null;
                d.SyncronizationDispatcher = sync;
                if (timeout.HasValue)
                {
                    d.Timeout = timeout.Value;
                }
            });
        }

        public static void DebounceAction(object key, Action<ActionConfig> actionOnTimeout, TimeSpan? timeout = null,
            ISynchronizeInvoke sync = null)
        {
            DebounceActionCustom(key, d =>
            {
                d.OnTimeout = actionOnTimeout;
                d.SyncronizationObject = sync;
                d.SyncronizationDispatcher = null;
                if (timeout.HasValue)
                {
                    d.Timeout = timeout.Value;
                }
            });
        }

        public static void DebounceActionCustom(object key, Action<ActionConfig> configDebounce)
        {
            Debounce(key, () => new ActionConfig(), configDebounce);
        }

        /// <summary>
        ///     Enqueue item to debounce
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="itemToEnqueue"></param>
        /// <param name="actionOnQueueOnTimeout"></param>
        /// <param name="timeout"></param>
        /// <param name="dispatcher"></param>
        /// <param name="syncObj"></param>
        public static void DebounceQueue<T>(object key, T itemToEnqueue,
            Action<DebounceQueueConfig<T>> actionOnQueueOnTimeout, TimeSpan? timeout = null,
            Dispatcher dispatcher = null, ISynchronizeInvoke syncObj = null)
        {
            Debounce(key, () => new DebounceQueueConfig<T>(), config =>
            {
                config.Queue.Add(itemToEnqueue);
                config.OnTimeout = actionOnQueueOnTimeout;
                if (timeout.HasValue)
                {
                    config.Timeout = timeout.Value;
                }

                config.SyncronizationDispatcher = dispatcher;
                config.SyncronizationObject = syncObj ?? config.SyncronizationObject;
            });
        }

        /// <summary>
        ///     Configuration contract of debouncer
        /// </summary>
        public interface IConfig
        {
            /// <summary>
            ///     Timeout that timer will countdown
            /// </summary>
            TimeSpan Timeout { get; }

            /// <summary>
            ///     Sync invoke object for timer
            /// </summary>
            ISynchronizeInvoke SyncronizationObjectForTimer { get; }

            /// <summary>
            ///     Action that will be invoked when timeout passed
            /// </summary>
            void OnTimeout();
        }

        /// <summary>
        ///     Internal debouncer entry
        /// </summary>
        private class DebounceRegistration : IDisposable
        {
            public IConfig Config;

            public object Key;
            //public Timer Timer;

            public IDisposable TimerDisposer;

            public void Dispose()
            {
                TimerDisposer?.Dispose();
                _activeDebouncers.TryRemove(Key, out _);
            }

            public IDisposable Lock()
            {
                return new DisposableAction(d =>
                {
                    Monitor.Enter(this);

                    d.Disposed += (sender, e) => { Monitor.Exit(this); };
                });
            }

            public void Debounce()
            {
                //lock (this) //moved to static method debounce
                {
                    TimerDisposer?.Dispose();

                    TimerDisposer = new DisposableAction(d =>
                    {
                        //TODO: consider using System.Threading.Timer instead

                        var timer = new Timer();
                        timer.Elapsed += Timer_Elapsed;
                        timer.AutoReset = false;
                        timer.Enabled = false;

                        timer.Interval = Config.Timeout.TotalMilliseconds;
                        timer.SynchronizingObject = Config.SyncronizationObjectForTimer;
                        timer.Start();

                        d.Disposed += (sender, e) =>
                        {
                            using (Lock())
                            {
                                timer.Stop();
                                timer.Elapsed -= Timer_Elapsed;
                                timer.Dispose();
                                TimerDisposer = null;
                            }
                        };
                    });
                }
            }

            private void Timer_Elapsed(object sender, ElapsedEventArgs e)
            {
                //fist we dispose debouncer, and then invoke it's action
                using (Lock())
                {
                    Dispose();
                }

                //Happening in syncronized mode already thanks to Timer.SyncronizingObject
                //but if dispatcher is used, then need to sync manually
                Config.OnTimeout();
            }
        }

        public abstract class ConfigBase : IConfig
        {
            public ISynchronizeInvoke SyncronizationObject { get; set; }

            public Dispatcher SyncronizationDispatcher { get; set; }

            ISynchronizeInvoke IConfig.SyncronizationObjectForTimer => SyncronizationObject;

            public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(500);

            void IConfig.OnTimeout()
            {
                OnTimeout();
            }

            protected void OnTimeout()
            {
                Action doInvoke = InvokeOnTimeout;
                if (SyncronizationDispatcher != null)
                {
                    SyncronizationDispatcher.BeginInvoke(doInvoke, null);
                }
                else
                {
                    doInvoke();
                }
            }

            protected abstract void InvokeOnTimeout();
        }

        /// <summary>
        ///     Standard action debounce config
        /// </summary>
        public class ActionConfig : ConfigBase
        {
            public object Data;
            public new Action<ActionConfig> OnTimeout;

            protected override void InvokeOnTimeout()
            {
                OnTimeout?.Invoke(this);
            }
        }

        public class DebounceQueueConfig<T> : ConfigBase
        {
            public new Action<DebounceQueueConfig<T>> OnTimeout;

            /// <summary>
            ///     Does not have to be thread-safe, because it's already thread-safe due to DebounceRegistration
            /// </summary>
            public List<T> Queue { get; } = new List<T>();

            protected override void InvokeOnTimeout()
            {
                OnTimeout(this);
            }
        }

        private class DisposableAction : IDisposable
        {
            public DisposableAction(Action<DisposableAction> action)
            {
                Action = action;

                action(this);
            }

            public Action<DisposableAction> Action { get; }

            public event EventHandler Disposed;

            #region IDisposable Support

            private bool disposedValue; // To detect redundant calls

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: dispose managed state (managed objects).
                        if (Disposed != null)
                        {
                            Disposed.Invoke(this, EventArgs.Empty);
                        }
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                    // TODO: set large fields to null.


                    disposedValue = true;
                }
            }

            // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
            // ~DisposableAction()
            // {
            //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            //   Dispose(false);
            // }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
                // TODO: uncomment the following line if the finalizer is overridden above.
                // GC.SuppressFinalize(this);
            }

            #endregion
        }
    }
}