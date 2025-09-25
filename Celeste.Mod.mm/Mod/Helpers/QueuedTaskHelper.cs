using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

// the class has been part of the public API for years, so we can't just change the namespace
// ReSharper disable CheckNamespace
namespace Celeste.Mod {
    [Obsolete("Use QueuedTaskHelperV2 instead.")]
    public static class QueuedTaskHelper {

        // Make sure to lock Timers and update both of those at the same time before unlocking!
        private static readonly Dictionary<object, object> Map = new Dictionary<object, object>();
        private static readonly Dictionary<object, Stopwatch> Timers = new Dictionary<object, Stopwatch>();

        public static readonly double DefaultDelay = 0.5D;

        [Obsolete($"Queued tasks now support {nameof(CancellationToken)}s. Pass the token when creating the task and cancel it when necessary.")]
        public static void Cancel(object key) {
            lock (Timers) {
                if (Timers.Remove(key, out Stopwatch timer)) {
                    timer.Stop();
                    if (!Map.Remove(key))
                        throw new Exception("Queued task cancellation failed!");
                }
            }
        }

        [Obsolete("Use QueuedTaskHelperV2.Do instead.")]
        public static Task Do(object key, Action a)
            => Do(key, DefaultDelay, a);

        [Obsolete("Use QueuedTaskHelperV2.Do instead.")]
        public static Task Do(object key, double delay, Action a) {
            lock (Timers) {
                if (!Timers.TryGetValue(key, out Stopwatch timer)) {
                    timer = Stopwatch.StartNew();
                    Timers.Add(key, timer);
                }

                if (!Map.TryGetValue(key, out object queued)) {
                    queued = new Func<Task>(async () => {
                        await Task.Yield();

                        do {
                            await Task.Delay(TimeSpan.FromSeconds(delay - timer.Elapsed.TotalSeconds));
                        } while (timer.Elapsed.TotalSeconds < delay);

                        if (!timer.IsRunning)
                            return;

                        Cancel(key);

                        a?.Invoke();
                    })();

                    Map.Add(key, queued);
                }

                timer.Restart();
                return (Task) queued;
            }
        }

        [Obsolete("Use QueuedTaskHelperV2.Get instead.")]
        public static Task<T> Get<T>(object key, Func<T> f)
            => Get(key, DefaultDelay, f);

        [Obsolete("Use QueuedTaskHelperV2.Get instead.")]
        public static Task<T> Get<T>(object key, double delay, Func<T> f) {
            lock (Timers) {
                if (!Timers.TryGetValue(key, out Stopwatch timer)) {
                    timer = Stopwatch.StartNew();
                    Timers.Add(key, timer);
                }

                if (!Map.TryGetValue(key, out object queued)) {
                    queued = new Func<Task<T>>(async () => {
                        await Task.Yield();

                        do {
                            await Task.Delay(TimeSpan.FromSeconds(delay - timer.Elapsed.TotalSeconds));
                        } while (timer.Elapsed.TotalSeconds < delay);

                        Cancel(key);

                        return f != null ? f.Invoke() : default;
                    })();

                    Map.Add(key, queued);
                }

                timer.Restart();
                return (Task<T>) queued;
            }
        }
    }

    /// <summary>
    ///   A class containing methods that allow for asynchronously executing a delegate after a specified delay.
    /// </summary>
    public static class QueuedTaskHelperV2 {
        internal static readonly Dictionary<object, QueuedTaskBase> RunningTasks = new();
        public static readonly TimeSpan DefaultDelay = TimeSpan.FromSeconds(0.5);

        /// <summary>
        ///   Schedules the given <paramref name="delegate"/> to execute asynchronously after the <see cref="DefaultDelay"/>.
        /// </summary>
        /// <remarks>
        ///   When a task is scheduled while another task is still running, multiple outcomes can occur depending on the state of the delay.
        ///   See <see cref="QueuedTaskBase.UpdateDelay"/> for details.
        /// </remarks>
        /// <param name="key">
        ///   The identifier of the task.
        /// </param>
        /// <param name="delegate">
        ///   The delegate to execute after waiting for <see cref="DefaultDelay"/>.
        /// </param>
        /// <returns>
        ///   A <see cref="QueuedTask"/> representing the scheduled execution of <paramref name="delegate"/>.
        /// </returns>
        /// <throws cref="InvalidOperationException">
        ///   A task with the same key that is not a <see cref="QueuedTask"/> is already running.
        /// </throws>
        public static QueuedTask Do(object key, Action @delegate)
            => Do(key, DefaultDelay, @delegate, CancellationToken.None);

        /// <summary>
        ///   Schedules the given <paramref name="delegate"/> to execute asynchronously after a given <paramref name="delay"/>.
        /// </summary>
        /// <remarks>
        ///   When a task is scheduled while another task is still running, multiple outcomes can occur depending on the state of the delay.
        ///   See <see cref="QueuedTaskBase.UpdateDelay"/> for details.
        /// </remarks>
        /// <param name="key">
        ///   The identifier of the task.
        /// </param>
        /// <param name="delay">
        ///   The delay time before executing the delegate.
        /// </param>
        /// <param name="delegate">
        ///   The delegate to execute after waiting for <paramref name="delay"/>.
        /// </param>
        /// <returns>
        ///   A <see cref="QueuedTask"/> representing the delayed execution of the specified action.
        /// </returns>
        /// <throws cref="InvalidOperationException">
        ///   A task with the same key that is not a <see cref="QueuedTask"/> is already running.
        /// </throws>
        public static QueuedTask Do(object key, TimeSpan delay, Action @delegate)
            => Do(key, delay, @delegate, CancellationToken.None);

        /// <summary>
        ///   Schedules the given <paramref name="delegate"/> to execute asynchronously after a given <paramref name="delay"/>.
        /// </summary>
        /// <remarks>
        ///   When a task is scheduled while another task is still running, multiple outcomes can occur depending on the state of the delay.
        ///   See <see cref="QueuedTaskBase.UpdateDelay"/> for details.
        /// </remarks>
        /// <param name="key">
        ///   The identifier of the task.
        /// </param>
        /// <param name="delay">
        ///   The delay time before executing the delegate.
        /// </param>
        /// <param name="delegate">
        ///   The delegate to execute after waiting for <paramref name="delay"/>.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token to observe while waiting for the delay.
        /// </param>
        /// <returns>
        ///   A <see cref="QueuedTask"/> representing the delayed execution of the specified action.
        /// </returns>
        /// <throws cref="InvalidOperationException">
        ///   A task with the same key that is not a <see cref="QueuedTask"/> is already running.
        /// </throws>
        public static QueuedTask Do(object key, TimeSpan delay, Action @delegate, CancellationToken cancellationToken) {
            lock (RunningTasks) {
                if (RunningTasks.TryGetValue(key, out QueuedTaskBase baseTask) && !baseTask.Queued.IsCompleted) {
                    if (baseTask is not QueuedTask existingTask)
                        throw new InvalidOperationException("A queued task with an incompatible type is already running.");

                    existingTask.UpdateDelay(delay);
                    return existingTask;
                }

                QueuedTask task = new(key, @delegate, delay, cancellationToken);
                RunningTasks[key] = task;
                return task;
            }
        }

        /// <summary>
        ///   Schedules the given <paramref name="delegate"/> to execute asynchronously after the <see cref="DefaultDelay"/>.
        /// </summary>
        /// <remarks>
        ///   When a task is scheduled while another task is still running, multiple outcomes can occur depending on the state of the delay.
        ///   See <see cref="QueuedTaskBase.UpdateDelay"/> for details.
        /// </remarks>
        /// <typeparam name="T">
        ///   The return type of <paramref name="delegate"/>.
        /// </typeparam>
        /// <param name="key">
        ///   The identifier of the task.
        /// </param>
        /// <param name="delegate">
        ///   The delegate to execute after waiting for <see cref="DefaultDelay"/>.
        /// </param>
        /// <returns>
        ///   A <see cref="QueuedTask{T}"/> representing the scheduled execution of <paramref name="delegate"/>.
        /// </returns>
        /// <throws cref="InvalidOperationException">
        ///   A task with the same key that is not a <see cref="QueuedTask{T}"/> is already running.
        /// </throws>
        public static QueuedTask<T> Get<T>(object key, Func<T> @delegate)
            => Get(key, @delegate, DefaultDelay, CancellationToken.None);

        /// <summary>
        ///   Schedules the given <paramref name="delegate"/> to execute asynchronously after a given <paramref name="delay"/>.
        /// </summary>
        /// <remarks>
        ///   When a task is scheduled while another task is still running, multiple outcomes can occur depending on the state of the delay.
        ///   See <see cref="QueuedTaskBase.UpdateDelay"/> for details.
        /// </remarks>
        /// <typeparam name="T">
        ///   The return type of <paramref name="delegate"/>.
        /// </typeparam>
        /// <param name="key">
        ///   The identifier of the task.
        /// </param>
        /// <param name="delay">
        ///   The delay time before executing the delegate.
        /// </param>
        /// <param name="delegate">
        ///   The delegate to execute after waiting for <paramref name="delay"/>.
        /// </param>
        /// <returns>
        ///   A <see cref="QueuedTask{T}"/> representing the delayed execution of the specified action.
        /// </returns>
        /// <throws cref="InvalidOperationException">
        ///   A task with the same key that is not a <see cref="QueuedTask{T}"/> is already running.
        /// </throws>
        public static QueuedTask<T> Get<T>(object key, Func<T> @delegate, TimeSpan delay)
            => Get(key, @delegate, delay, CancellationToken.None);

        /// <summary>
        ///   Schedules the given <paramref name="delegate"/> to execute asynchronously after a given <paramref name="delay"/>.
        /// </summary>
        /// <remarks>
        ///   When a task is scheduled while another task is still running, multiple outcomes can occur depending on the state of the delay.
        ///   See <see cref="QueuedTaskBase.UpdateDelay"/> for details.
        /// </remarks>
        /// <typeparam name="T">
        ///   The return type of <paramref name="delegate"/>.
        /// </typeparam>
        /// <param name="key">
        ///   The identifier of the task.
        /// </param>
        /// <param name="delay">
        ///   The delay time before executing the delegate.
        /// </param>
        /// <param name="delegate">
        ///   The delegate to execute after waiting for <paramref name="delay"/>.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token to observe while waiting for the delay.
        /// </param>
        /// <returns>
        ///   A <see cref="QueuedTask{T}"/> representing the delayed execution of the specified action.
        /// </returns>
        /// <throws cref="InvalidOperationException">
        ///   A task with the same key that is not a <see cref="QueuedTask{T}"/> is already running.
        /// </throws>
        public static QueuedTask<T> Get<T>(object key, Func<T> @delegate, TimeSpan delay, CancellationToken cancellationToken) {
            lock (RunningTasks) {
                if (RunningTasks.TryGetValue(key, out QueuedTaskBase baseTask) && !baseTask.Queued.IsCompleted) {
                    if (baseTask is not QueuedTask<T> existingTask)
                        throw new InvalidOperationException("A queued task with an incompatible type is already running.");

                    existingTask.UpdateDelay(delay);
                    return existingTask;
                }

                QueuedTask<T> task = new(key, @delegate, delay, cancellationToken);
                RunningTasks[key] = task;
                return task;
            }
        }
    }

    /// <summary>
    ///   The base class of all queued tasks.
    /// </summary>
    public abstract class QueuedTaskBase {
        /// <summary>
        ///   The key associated with the task.
        /// </summary>
        public object Key { get; set; }

        /// <summary>
        ///   The <see cref="System.Threading.CancellationToken"/> that can cancel the task.
        /// </summary>
        public CancellationToken CancellationToken { get; init; }

        /// <summary>
        ///   A task that will complete after the given delay.
        /// </summary>
        /// <seealso cref="UpdateDelay"/>
        protected Task DelayTask { get; private set; }

        /// <summary>
        ///   The queued <see cref="Task"/>.
        /// </summary>
        public Task Queued { get; init; }

        protected QueuedTaskBase(object key, TimeSpan delay, CancellationToken cancellationToken) {
            Key = key;
            CancellationToken = cancellationToken;
            UpdateDelay(delay);
        }

        /// <summary>
        ///   Updates the delay depending on its status.
        ///   <list type="table">
        ///     <listheader>
        ///       <term>Delay State</term>
        ///       <description>Outcome</description>
        ///     </listheader>
        ///     <item>
        ///       <term>Not yet awaited</term>
        ///       <description>The delay is replaced</description>
        ///     </item>
        ///     <item>
        ///       <term>Awaiting</term>
        ///       <description>The delay is overlaid (see remarks)</description>
        ///     </item>
        ///     <item>
        ///       <term>Awaited</term>
        ///       <description>The delay is ignored, as the callback is already executing</description>
        ///     </item>
        ///   </list>
        /// </summary>
        /// <remarks>
        ///   When overlaying a <c>5</c>-second delay on top of a <c>10</c>-second delay,
        ///   which starts <c>7.5</c> seconds after the first one, the total delay becomes <c>12.5</c> seconds.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///   Attempted to overlay a delay on a completed task.
        /// </exception>
        internal void UpdateDelay(TimeSpan delay) {
            if (Queued?.IsCompleted ?? false)
                throw new InvalidOperationException("Cannot overlay a delay on a completed task.");
            DelayTask = Task.Delay(delay, CancellationToken);
        }

        protected async Task WaitForDelay() {
            // force the task to run asynchronously by yielding - we don't want to block the main thread
            await Task.Yield();

            // we need to wait on loop because DelayTask could have been changed by UpdateDelay
            do {
                await DelayTask;
            } while (!DelayTask.IsCompleted);
        }

        protected void LogError(Exception exception) {
            Logger.Error(nameof(QueuedTask), $"A queued task with key hashcode {Key.GetHashCode()} failed.");
            Logger.LogDetailed(exception);
        }

        protected void CompleteTask() {
            lock (QueuedTaskHelperV2.RunningTasks) {
                QueuedTaskHelperV2.RunningTasks.Remove(Key);
            }
        }
    }

    /// <summary>
    ///   A queued task that executes an <see cref="Action"/> after a delay.
    /// </summary>
    /// <seealso cref="QueuedTaskHelperV2.Do(object, Action)"/>
    public sealed class QueuedTask : QueuedTaskBase {
        internal QueuedTask(object key, Action @delegate, TimeSpan delay, CancellationToken cancellationToken)
            : base(key, delay, cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(@delegate);
            Queued = Run(@delegate);
        }

        private async Task Run(Action @delegate) {
            try {
                await WaitForDelay();
                @delegate();
            } catch (Exception e) when (e is not OperationCanceledException) {
                LogError(e);
                throw;
            } finally {
                CompleteTask();
            }
        }
    }

    /// <summary>
    ///   A queued task that executes a <see cref="Func{T}"/> after a delay.
    /// </summary>
    /// <seealso cref="QueuedTaskHelperV2.Get{T}(object, Func{T})"/>
    public sealed class QueuedTask<T> : QueuedTaskBase {
        /// <inheritdoc cref="QueuedTaskBase.Queued"/>
        public new Task<T> Queued => (Task<T>) base.Queued;

        internal QueuedTask(object key, Func<T> @delegate, TimeSpan delay, CancellationToken cancellationToken)
            : base(key, delay, cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(@delegate);
            base.Queued = Run(@delegate);
        }

        private async Task<T> Run(Func<T> @delegate) {
            try {
                await WaitForDelay();
                return @delegate();
            } catch (Exception e) when (e is not OperationCanceledException) {
                LogError(e);
                throw;
            } finally {
                CompleteTask();
            }
        }
    }
}
