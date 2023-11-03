[assembly: System.CLSCompliant(true)]
[assembly: System.Resources.NeutralResourcesLanguage("en-US")]
[assembly: System.Runtime.InteropServices.ComVisible(false)]
[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.7.2", FrameworkDisplayName=".NET Framework 4.7.2")]
namespace System.Reactive.Concurrency
{
    public class ControlScheduler : System.Reactive.Concurrency.LocalScheduler, System.Reactive.Concurrency.ISchedulerPeriodic
    {
        public ControlScheduler(System.Windows.Forms.Control control) { }
        public System.Windows.Forms.Control Control { get; }
        public override System.IDisposable Schedule<TState>(TState state, System.Func<System.Reactive.Concurrency.IScheduler, TState, System.IDisposable> action) { }
        public override System.IDisposable Schedule<TState>(TState state, System.TimeSpan dueTime, System.Func<System.Reactive.Concurrency.IScheduler, TState, System.IDisposable> action) { }
        public System.IDisposable SchedulePeriodic<TState>(TState state, System.TimeSpan period, System.Func<TState, TState> action) { }
    }
}
namespace System.Reactive.Linq
{
    public static class ControlObservable
    {
        public static System.IObservable<TSource> ObserveOn<TSource>(this System.IObservable<TSource> source, System.Windows.Forms.Control control) { }
        public static System.IObservable<TSource> SubscribeOn<TSource>(this System.IObservable<TSource> source, System.Windows.Forms.Control control) { }
    }
}