﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.
// See the LICENSE file in the project root for more information. 

#if HAS_WINFORMS

using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Reactive.Testing;

using Assert = Xunit.Assert;

namespace ReactiveTests.Tests
{
    [TestClass]
    public class ControlSchedulerTest
    {
        [TestMethod]
        public void Ctor_ArgumentChecking()
        {
            ReactiveAssert.Throws<ArgumentNullException>(() => new ControlScheduler(null));
        }

        [TestMethod]
        public void Control()
        {
            var lbl = new Label();
            Assert.Same(lbl, new ControlScheduler(lbl).Control);
        }

        [TestMethod]
        public void Now()
        {
            var res = new ControlScheduler(new Label()).Now - DateTime.Now;
            Assert.True(res.Seconds < 1);
        }

        [TestMethod]
        public void Schedule_ArgumentChecking()
        {
            var s = new ControlScheduler(new Label());
            ReactiveAssert.Throws<ArgumentNullException>(() => s.Schedule(42, default(Func<IScheduler, int, IDisposable>)));
            ReactiveAssert.Throws<ArgumentNullException>(() => s.Schedule(42, TimeSpan.FromSeconds(1), default(Func<IScheduler, int, IDisposable>)));
            ReactiveAssert.Throws<ArgumentNullException>(() => s.Schedule(42, DateTimeOffset.Now, default(Func<IScheduler, int, IDisposable>)));
        }

        [TestMethod]
        public void Schedule()
        {
            using (WinFormsTestUtils.RunTest(out var lbl))
            {
                var evt = new ManualResetEvent(false);

                var id = Thread.CurrentThread.ManagedThreadId;

                var sch = new ControlScheduler(lbl);
                
                sch.Schedule(() => { lbl.Text = "Okay"; Assert.NotEqual(id, Thread.CurrentThread.ManagedThreadId); });
                sch.Schedule(() => { Assert.Equal("Okay", lbl.Text); Assert.NotEqual(id, Thread.CurrentThread.ManagedThreadId); evt.Set(); });

                evt.WaitOne();
            }
        }

        [TestMethod]
        public void ScheduleError()
        {
            using (WinFormsTestUtils.RunTest(out var lbl))
            {
                var evt = new ManualResetEvent(false);

                var ex = new Exception();

                lbl.Invoke(new Action(() =>
                {
                    Application.ThreadException += (o, e) =>
                    {
                        Assert.Same(ex, e.Exception);
                        evt.Set();
                    };
                }));

                var sch = new ControlScheduler(lbl);
                sch.Schedule(() => { throw ex; });

                evt.WaitOne();
            }
        }

        [TestMethod]
        public void ScheduleRelative()
        {
            ScheduleRelative_(TimeSpan.FromSeconds(0.1));
        }

        [TestMethod]
        public void ScheduleRelative_Zero()
        {
            ScheduleRelative_(TimeSpan.Zero);
        }

        private void ScheduleRelative_(TimeSpan delay)
        {
            using (WinFormsTestUtils.RunTest(out var lbl))
            {
                var evt = new ManualResetEvent(false);

                var id = Thread.CurrentThread.ManagedThreadId;
                
                var sch = new ControlScheduler(lbl);

                sch.Schedule(delay, () =>
                {
                    lbl.Text = "Okay";
                    Assert.NotEqual(id, Thread.CurrentThread.ManagedThreadId);
                    
                    sch.Schedule(() =>
                    {
                        Assert.Equal("Okay", lbl.Text);
                        Assert.NotEqual(id, Thread.CurrentThread.ManagedThreadId);
                        evt.Set();
                    });
                });

                evt.WaitOne();
            }
        }

        [TestMethod]
        public void ScheduleRelative_Nested()
        {
            using (WinFormsTestUtils.RunTest(out var lbl))
            {
                var evt = new ManualResetEvent(false);

                var id = Thread.CurrentThread.ManagedThreadId;

                var sch = new ControlScheduler(lbl);

                sch.Schedule(TimeSpan.FromSeconds(0.1), () =>
                {
                    sch.Schedule(TimeSpan.FromSeconds(0.1), () =>
                    {
                        lbl.Text = "Okay";
                        Assert.NotEqual(id, Thread.CurrentThread.ManagedThreadId);

                        sch.Schedule(() =>
                        {
                            Assert.Equal("Okay", lbl.Text);
                            Assert.NotEqual(id, Thread.CurrentThread.ManagedThreadId);
                            evt.Set();
                        });
                    });
                });

                evt.WaitOne();
            }
        }

        [TestMethod]
        public void ScheduleRelative_Cancel()
        {
            using (WinFormsTestUtils.RunTest(out var lbl))
            {
                var evt = new ManualResetEvent(false);

                var id = Thread.CurrentThread.ManagedThreadId;

                var sch = new ControlScheduler(lbl);

                sch.Schedule(TimeSpan.FromSeconds(0.1), () =>
                {
                    lbl.Text = "Okay";
                    Assert.NotEqual(id, Thread.CurrentThread.ManagedThreadId);

                    var d = sch.Schedule(TimeSpan.FromSeconds(0.1), () =>
                    {
                        lbl.Text = "Oops!";
                    });

                    sch.Schedule(() =>
                    {
                        d.Dispose();
                    });

                    sch.Schedule(TimeSpan.FromSeconds(0.2), () =>
                    {
                        Assert.Equal("Okay", lbl.Text);
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
            var s = new ControlScheduler(new Label());
            ReactiveAssert.Throws<ArgumentNullException>(() => s.SchedulePeriodic(42, TimeSpan.FromSeconds(1), default(Func<int, int>)));
            ReactiveAssert.Throws<ArgumentOutOfRangeException>(() => s.SchedulePeriodic(42, TimeSpan.Zero, x => x));
            ReactiveAssert.Throws<ArgumentOutOfRangeException>(() => s.SchedulePeriodic(42, TimeSpan.FromMilliseconds(1).Subtract(TimeSpan.FromTicks(1)), x => x));
        }

        [TestMethod]
        public void SchedulePeriodic()
        {
            using (WinFormsTestUtils.RunTest(out var lbl))
            {
                var evt = new ManualResetEvent(false);

                var id = Thread.CurrentThread.ManagedThreadId;

                var sch = new ControlScheduler(lbl);

                var d = new SingleAssignmentDisposable();

                d.Disposable = sch.SchedulePeriodic(1, TimeSpan.FromSeconds(0.1), n =>
                {
                    lbl.Text = "Okay " + n;
                    Assert.NotEqual(id, Thread.CurrentThread.ManagedThreadId);

                    if (n == 3)
                    {
                        d.Dispose();

                        sch.Schedule(TimeSpan.FromSeconds(0.2), () =>
                        {
                            Assert.Equal("Okay 3", lbl.Text);
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

        [TestMethod]
        public void SchedulePeriodic_Nested()
        {
            using (WinFormsTestUtils.RunTest(out var lbl))
            {
                var evt = new ManualResetEvent(false);

                var id = Thread.CurrentThread.ManagedThreadId;

                var sch = new ControlScheduler(lbl);

                sch.Schedule(() =>
                {
                    lbl.Text = "Okay";

                    var d = new SingleAssignmentDisposable();

                    d.Disposable = sch.SchedulePeriodic(1, TimeSpan.FromSeconds(0.1), n =>
                    {
                        lbl.Text = "Okay " + n;
                        Assert.NotEqual(id, Thread.CurrentThread.ManagedThreadId);

                        if (n == 3)
                        {
                            d.Dispose();

                            sch.Schedule(TimeSpan.FromSeconds(0.2), () =>
                            {
                                Assert.Equal("Okay 3", lbl.Text);
                                Assert.NotEqual(id, Thread.CurrentThread.ManagedThreadId);
                                evt.Set();
                            });
                        }

                        return n + 1;
                    });
                });

                evt.WaitOne();
            }
        }
    }
}
#endif
