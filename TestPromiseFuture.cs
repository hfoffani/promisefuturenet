
/*
 * Copyright 2015 Hernán M. Foffani
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */

using System;
#if NUNIT
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using ClassCleanup = NUnit.Framework.TestFixtureTearDownAttribute;
using ClassInitialize = NUnit.Framework.TestFixtureSetUpAttribute;
using NUnit.Framework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace PromiseFuture
{
    [TestClass]
    public class TestPromiseFuture
    {
        [TestMethod]
        public void UseCase_01()
        {
            var i = 3;
            var future = Future
                .Bring<int>(() => i + 1)
                .Start();

            Assert.AreEqual(4, future.Value);
        }

        [TestMethod]
        public void UseCase_02()
        {
            var i = 3;
            var future = Future
                .Bring<int>(() => i + 1000)
                .Later<string>((a) => a.ToString())
                .Later<int>((s) => s.ToCharArray().Length)
                .Start();

            Assert.AreEqual(4, future.Value);
        }

        [TestMethod]
        public void UseCase_03()
        {
            var i = 30;
            var futureVal = Future
                .Bring<int>(() => i + 1000)
                .Start();

            var future = Future
                .Bring<string>(() => ((int)futureVal).ToString())
                .Start();

            Assert.AreEqual("1030", future.Value);
        }

        [TestMethod]
        public void UseCase_04()
        {
            var i = 30;
            var f1 = Future
                .Bring<int>(() => i + 1000)
                .Start();

            var f2 = Future
                .Bring<int>(() => i * 1000)
                .Start();

            var future = Future
                .Bring<string>(() => (f1 + f2).ToString())
                .Start();

            Assert.AreEqual("31030", future.Value);
        }

        [TestMethod]
        public void UseCase_05()
        {
            var i = 30;
            var f1 = Future
                .Bring<int>(() => i + 1000)
                .Start();

            var f2 = Future
                .Bring<int>(() => i * 1000)
                .Start();

            var f3 = Future
                .Bring<int>(() => i * 1000)
                .Start();

            var future = Future
                .Bring<string>(() => (f1 + f2 + f3).ToString())
                .Start();

            Assert.AreEqual("61030", future.Value);
        }

        [TestMethod]
        public void UseCase_06()
        {
            var error = false;
            var i = 0;
            var future = Future
                .Bring<int>(() => 10 / i)
                .Unless<DivideByZeroException>((e) => { error = true; return -5; })
                .Start();

            var res = future.Value;
            Assert.IsTrue(error);
            Assert.AreEqual(-5, res);
        }

        [TestMethod,
        ExpectedException(typeof(DivideByZeroException))]
        public void UseCase_07()
        {
            var i = 0;
            var future = Future
                .Bring<int>(() => 10 / i)
                .Unless<ApplicationException>((e) => { return -5; })
                .Start(FutureOptions.Synchronous);

            var x = future.Value;
        }

        [TestMethod]
        public void UseCase_11()
        {
            var i = 3;
            var future = Future
                .Bring<int>(() => {
                    i = i + 1;
                    System.Threading.Thread.Sleep(1000);
                    return i + 1;
                })
                .Start(FutureOptions.Synchronous);

            System.Threading.Thread.Sleep(500);
            Assert.AreEqual(3, i);
            Assert.AreEqual(5, future.Value);
        }

        [TestMethod]
        public void UseCase_12()
        {
            var i = 3;
            var future = Future
                .Bring<int>(() => {
                    i = i + 1;
                    System.Threading.Thread.Sleep(1000);
                    return i + 1;
                })
                .Start(FutureOptions.LongRunning);

            System.Threading.Thread.Sleep(500);
            Assert.AreEqual(4, i);
            Assert.AreEqual(5, future.Value);
        }

        [TestMethod]
        public void UseCase_13()
        {
            var error = false;
            var i = 0;
            var future = Future
                .Bring<int>(() => 10 / i)
                .Unless<DivideByZeroException>((e) => { error = true; return -5; })
                .Start(FutureOptions.Async);

            var res = future.Value;
            Assert.AreEqual(-5, res);
            Assert.IsTrue(error);
            Assert.IsFalse(future.Cancelled);
        }

        [TestMethod]
        public void UseCase_14()
        {
            var error = false;
            var i = 0;
            var future = Future
                .Bring<int>(() => 10 / i)
                .Unless<ApplicationException>((e) => { error = true; return -5; })
                .Start(FutureOptions.Async);
            var res = future.Value;

            Assert.AreEqual(0, res);
            Assert.IsFalse(error);
            Assert.IsTrue(future.Cancelled);
            Assert.IsNotNull(future.Faults);
            Assert.AreEqual(1, future.Faults.InnerExceptions.Count);
            Assert.IsTrue(typeof(DivideByZeroException).IsInstanceOfType(future.Faults.InnerExceptions[0]));
        }

        [TestMethod]
        public void UseCase_15()
        {
            var i = 0;
            var future = Future
                .Bring<int>((supervisor) => {
                    for (int j = 0; j < 100; j++) {
                        if (supervisor.CancelRequested)
                            return 5;
                        System.Threading.Thread.Sleep(30);
                    }
                    return i + 1;
                })
                .Start();

            System.Threading.Thread.Sleep(500);
            future.Cancel();
            var res = future.Value;

            Assert.AreEqual(5, res);
            Assert.IsTrue(future.Cancelled);
            Assert.IsNull(future.Faults);
        }

        [TestMethod]
        public void UseCase_20()
        {
            Func<int> g = () => 1 + 2;
            dynamic gd = g;
            var future = Future
                .Bring(gd)
                .Start();

            var res = future.Value;

            Assert.AreEqual(3, res);
        }

        [TestMethod]
        public void UseCase_21()
        {
            Func<FutureSupervisor, object> g = (supervisor) => 1 + 2;
            dynamic gd = g;
            var future = Future
                .Bring(gd, supervised: true)
                .Start();

            var res = future.Value;

            Assert.AreEqual(3, res);
        }

        [TestMethod]
        public void UseCase_22()
        {
            Func<FutureSupervisor, object> g = (supervisor) => 1 + 2;
            dynamic gd = g;
            Func<FutureSupervisor, object, object> t = (supervisor, p) => p;
            dynamic td = t;
            Func<Exception, object> e = (err) => { return 33; };
            dynamic ed = e;

            var future = Future
                .Bring(gd, supervised: true)
                .Later(td)
                .Unless(ed)
                .Start();

            var res = future.Value;

            Assert.AreEqual(3, res);
        }

        [TestMethod]
        public void UseCase_30()
        {
            var i = 3;
            var future = Future
                .Bring<int>(() => i + 1)
                .Start();

            Assert.AreEqual(4, future.Value);
            Assert.AreEqual(4, future.Value);
            Assert.AreEqual(4, future.Value);
        }

        [TestMethod]
        public void UseCase_31()
        {
            var i = 3;
            var future = Future
                .Bring<int>(() => i + 1)
                .Start(FutureOptions.Synchronous);

            Assert.AreEqual(4, future.Value);
            Assert.AreEqual(4, future.Value);
            Assert.AreEqual(4, future.Value);
        }

        [TestMethod]
        public void UseCase_40()
        {
            Future.UnderGUI();
            var i = 30;
            var f1 = Future
                .Bring<int>(() => i + 1000)
                .Start();

            var f2 = Future
                .Bring<int>(() => i * 1000)
                .Start();

            var future = Future
                .Bring<string>(() => (f1 + f2).ToString())
                .Start(FutureOptions.ResultInGUI);

            Assert.AreEqual("31030", future.Value);
        }


        [TestInitialize()]
        public void BeforeTest()
        {
            System.Threading.SynchronizationContext.SetSynchronizationContext(
                new System.Threading.SynchronizationContext());
        }

#if NUNIT
#else
        [TestCleanup()]
        public void AfterTest()
        {
        }

        [ClassInitialize()]
        public static void FSU(TestContext testContext)
        {
        }

        [ClassCleanup()]
        public static void FTU()
        {
        }
#endif

    }
}
