[assembly: System.CLSCompliant(true)]
[assembly: System.Resources.NeutralResourcesLanguage("en-US")]
[assembly: System.Runtime.InteropServices.ComVisible(false)]
[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.7.2", FrameworkDisplayName=".NET Framework 4.7.2")]
namespace System.Reactive.Concurrency
{
    public class DispatcherScheduler : System.Reactive.Concurrency.LocalScheduler, System.Reactive.Concurrency.ISchedulerPeriodic
    {
        public DispatcherScheduler(System.Windows.Threading.Dispatcher dispatcher) { }
        public DispatcherScheduler(System.Windows.Threading.Dispatcher dispatcher, System.Windows.Threading.DispatcherPriority priority) { }
        public System.Windows.Threading.Dispatcher Dispatcher { get; }
        public System.Windows.Threading.DispatcherPriority Priority { get; }
        public static System.Reactive.Concurrency.DispatcherScheduler Current { get; }
        [System.Obsolete("Use the Current property to retrieve the DispatcherScheduler instance for the cur" +
            "rent thread\'s Dispatcher object.")]
        public static System.Reactive.Concurrency.DispatcherScheduler Instance { get; }
        public override System.IDisposable Schedule<TState>(TState state, System.Func<System.Reactive.Concurrency.IScheduler, TState, System.IDisposable> action) { }
        public override System.IDisposable Schedule<TState>(TState state, System.TimeSpan dueTime, System.Func<System.Reactive.Concurrency.IScheduler, TState, System.IDisposable> action) { }
        public System.IDisposable SchedulePeriodic<TState>(TState state, System.TimeSpan period, System.Func<TState, TState> action) { }
    }
}
namespace System.Reactive.Linq
{
    public static class DispatcherObservable
    {
        public static System.IObservable<TSource> ObserveOn<TSource>(this System.IObservable<TSource> source, System.Reactive.Concurrency.DispatcherScheduler scheduler) { }
        public static System.IObservable<TSource> ObserveOn<TSource>(this System.IObservable<TSource> source, System.Windows.Threading.Dispatcher dispatcher) { }
        public static System.IObservable<TSource> ObserveOn<TSource>(this System.IObservable<TSource> source, System.Windows.Threading.DispatcherObject dispatcherObject) { }
        public static System.IObservable<TSource> ObserveOn<TSource>(this System.IObservable<TSource> source, System.Windows.Threading.Dispatcher dispatcher, System.Windows.Threading.DispatcherPriority priority) { }
        public static System.IObservable<TSource> ObserveOn<TSource>(this System.IObservable<TSource> source, System.Windows.Threading.DispatcherObject dispatcherObject, System.Windows.Threading.DispatcherPriority priority) { }
        public static System.IObservable<TSource> ObserveOnDispatcher<TSource>(this System.IObservable<TSource> source) { }
        public static System.IObservable<TSource> ObserveOnDispatcher<TSource>(this System.IObservable<TSource> source, System.Windows.Threading.DispatcherPriority priority) { }
        public static System.IObservable<TSource> SubscribeOn<TSource>(this System.IObservable<TSource> source, System.Reactive.Concurrency.DispatcherScheduler scheduler) { }
        public static System.IObservable<TSource> SubscribeOn<TSource>(this System.IObservable<TSource> source, System.Windows.Threading.Dispatcher dispatcher) { }
        public static System.IObservable<TSource> SubscribeOn<TSource>(this System.IObservable<TSource> source, System.Windows.Threading.DispatcherObject dispatcherObject) { }
        public static System.IObservable<TSource> SubscribeOn<TSource>(this System.IObservable<TSource> source, System.Windows.Threading.Dispatcher dispatcher, System.Windows.Threading.DispatcherPriority priority) { }
        public static System.IObservable<TSource> SubscribeOn<TSource>(this System.IObservable<TSource> source, System.Windows.Threading.DispatcherObject dispatcherObject, System.Windows.Threading.DispatcherPriority priority) { }
        public static System.IObservable<TSource> SubscribeOnDispatcher<TSource>(this System.IObservable<TSource> source) { }
        public static System.IObservable<TSource> SubscribeOnDispatcher<TSource>(this System.IObservable<TSource> source, System.Windows.Threading.DispatcherPriority priority) { }
    }
}