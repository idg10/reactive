﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.
// See the LICENSE file in the project root for more information. 

#if HAS_WPF
using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;
using System.Windows.Threading;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Assert = Xunit.Assert;

namespace ReactiveTests.Tests
{
    [TestClass]
    public class DispatcherSchedulerTest : TestBase
    {
        [TestMethod]
        public void Ctor_ArgumentChecking()
        {
            ReactiveAssert.Throws<ArgumentNullException>(() => new DispatcherScheduler(null));
        }

        [TestMethod]
        public void Current()
        {
            using (DispatcherHelpers.RunTest(out var d))
            {
                var e = new ManualResetEvent(false);

                d.BeginInvoke(() =>
                {
                    var c = DispatcherScheduler.Current;
                    c.Schedule(() => { e.Set(); });
                });

                e.WaitOne();
            }
        }

        [TestMethod]
        public void Current_None()
        {
            var e = default(Exception);

            var t = new Thread(() =>
            {
                try
                {
                    var ignored = DispatcherScheduler.Current;
                }
                catch (Exception ex)
                {
                    e = ex;
                }
            });

            t.Start();
            t.Join();

            Assert.True(e != null && e is InvalidOperationException);
        }

        [TestMethod]
        public void Dispatcher()
        {
            using (DispatcherHelpers.RunTest(out var disp))
            {
                Assert.Same(disp.Dispatcher, new DispatcherScheduler(disp).Dispatcher);
            }
        }

        [TestMethod]
        public void Now()
        {
            using (DispatcherHelpers.RunTest(out var disp))
            {
                var res = new DispatcherScheduler(disp).Now - DateTime.Now;
                Assert.True(res.Seconds < 1);
            }
        }

        [TestMethod]
        public void Schedule_ArgumentChecking()
        {
            using (DispatcherHelpers.RunTest(out var disp))
            {
                var s = new DispatcherScheduler(disp);
                ReactiveAssert.Throws<ArgumentNullException>(() => s.Schedule(42, default(Func<IScheduler, int, IDisposable>)));
                ReactiveAssert.Throws<ArgumentNullException>(() => s.Schedule(42, TimeSpan.FromSeconds(1), default(Func<IScheduler, int, IDisposable>)));
                ReactiveAssert.Throws<ArgumentNullException>(() => s.Schedule(42, DateTimeOffset.Now, default(Func<IScheduler, int, IDisposable>)));
            }
        }

        [TestMethod]
        [Asynchronous]
        public void Schedule()
        {
            using (DispatcherHelpers.RunTest(out var disp))
            {
                RunAsync(evt =>
                {
                    var id = Thread.CurrentThread.ManagedThreadId;
                    var sch = new DispatcherScheduler(disp);
                    sch.Schedule(() =>
                    {
                        Assert.NotEqual(id, Thread.CurrentThread.ManagedThreadId);
                        evt.Set();
                    });
                });
            }
        }

        [TestMethod]
        public void ScheduleError()
        {
            using (DispatcherHelpers.RunTest(out var disp))
            {
                var ex = new Exception();

                var id = Thread.CurrentThread.ManagedThreadId;
                var evt = new ManualResetEvent(false);

                Exception thrownEx = null;
                disp.UnhandledException += (o, e) =>
                {
                    thrownEx = e.Exception;
                    evt.Set();
                    e.Handled = true;
                };
                var sch = new DispatcherScheduler(disp);
                sch.Schedule(() => { throw ex; });
                evt.WaitOne();

                Assert.Same(ex, thrownEx);
            }
        }

        [TestMethod]
        public void ScheduleRelative()
        {
            ScheduleRelative_(TimeSpan.FromSeconds(0.2));
        }

        [TestMethod]
        public void ScheduleRelative_Zero()
        {
            ScheduleRelative_(TimeSpan.Zero);
        }

        private void ScheduleRelative_(TimeSpan delay)
        {
            using (DispatcherHelpers.RunTest(out var disp))
            {
                var evt = new ManualResetEvent(false);

                var id = Thread.CurrentThread.ManagedThreadId;

                var sch = new DispatcherScheduler(disp);

                sch.Schedule(delay, () =>
                {
                    Assert.NotEqual(id, Thread.CurrentThread.ManagedThreadId);

                    sch.Schedule(delay, () =>
                    {
                        Assert.NotEqual(id, Thread.CurrentThread.ManagedThreadId);
                        evt.Set();
                    });
                });

                evt.WaitOne();
            }
        }

        [TestMethod]
        public void ScheduleRelative_Cancel()
        {
            using (DispatcherHelpers.RunTest(out var disp))
            {
                var evt = new ManualResetEvent(false);
                
                var id = Thread.CurrentThread.ManagedThreadId;

                var sch = new DispatcherScheduler(disp);
                
                sch.Schedule(TimeSpan.FromSeconds(0.1), () =>
                {
                    Assert.NotEqual(id, Thread.CurrentThread.ManagedThreadId);

                    var d = sch.Schedule(TimeSpan.FromSeconds(0.1), () =>
                    {
                        Assert.True(false);
                        evt.Set();
                    });

                    sch.Schedule(() =>
                    {
                        d.Dispose();
                    });

                    sch.Schedule(TimeSpan.FromSeconds(0.2), () =>
                    {
                        Assert.NotEqual(id, Thread.CurrentThread.ManagedThreadId);
                        evt.Set();
                    });
                });

                evt.WaitOne();
            }
        }

        [TestMethod]
        public void SchedulePeriodic_ArgumentChecking()
        {
            using (DispatcherHelpers.RunTest(out var disp))
            {
                var s = new DispatcherScheduler(disp);

                ReactiveAssert.Throws<ArgumentNullException>(() => s.SchedulePeriodic(42, TimeSpan.FromSeconds(1), default(Func<int, int>)));
                ReactiveAssert.Throws<ArgumentOutOfRangeException>(() => s.SchedulePeriodic(42, TimeSpan.FromSeconds(-1), x => x));
            }
        }

        [TestMethod]
        public void SchedulePeriodic()
        {
            using (DispatcherHelpers.RunTest(out var disp))
            {
                var evt = new ManualResetEvent(false);

                var id = Thread.CurrentThread.ManagedThreadId;

                var sch = new DispatcherScheduler(disp);

                var d = new SingleAssignmentDisposable();

                d.Disposable = sch.SchedulePeriodic(1, TimeSpan.FromSeconds(0.1), n =>
                {
                    Assert.NotEqual(id, Thread.CurrentThread.ManagedThreadId);

                    if (n == 3)
                    {
                        d.Dispose();

                        sch.Schedule(TimeSpan.FromSeconds(0.2), () =>
                        {
                            Assert.NotEqual(id, Thread.CurrentThread.ManagedThreadId);
                            evt.Set();
                        });
                    }

                    if (n > 3)
                    {
                        Assert.True(false);
                    }

                    return n + 1;
                });

                evt.WaitOne();
            }
        }
    }
}
#endif
