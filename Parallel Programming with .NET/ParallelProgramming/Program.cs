using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Collections.Concurrent;

namespace ParallelProgramming
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            //ParallelFor();
            //Console.WriteLine();

            //ParallelForEach();
            //Console.WriteLine();

            //ParallelInvoke();
            //Console.WriteLine();

            //ParallelTasks();
            //Console.WriteLine();

            //TaskResults();
            //Console.WriteLine();

            //ContinuationTasks();
            //Console.WriteLine();

            //NestedTasks();
            //Console.WriteLine();

            //ChildTasks();
            //Console.WriteLine();

            TaskCancellation();
            Console.WriteLine();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        private static void ParallelFor()
        {
            Console.WriteLine("Parallel For");

            for (var i = 0; i < 10; i++)
                Console.WriteLine("{0} - {1}", i, GetTotal());

            Console.WriteLine();
            Parallel.For(0, 10, i => { Console.WriteLine("{0} - {1}", i, GetTotal()); });

            Console.WriteLine();
            Parallel.For(1, 21, (i, pls) => { Console.WriteLine("{0} - {1}", i, pls.LowestBreakIteration); if (i >= 6) pls.Break(); });

            Console.WriteLine();
            Parallel.For(1, 21, (i, pls) => { Thread.Sleep(400); Console.WriteLine("{0} - {1}", i, pls.IsStopped); if (i >= 13) pls.Stop(); });

            Console.WriteLine();
            var total = 0L;
            var sync = new object();

            for (var i = 1; i < 100000001; i++) total += i;
            Console.WriteLine("Total: {0}", total);

            total = 0L;
            Parallel.For(1, 100000001, i => { total += i; });
            Console.WriteLine("Total: {0}", total);

            total = 0L;
            Parallel.For(1, 100000001, i => { lock (sync) total += i; });
            Console.WriteLine("Total: {0}", total);

            total = 0L;
            Parallel.For(1, 100000001, () => 0L, (i, pls, localTotal) => localTotal += i, localTotal => { lock (sync) total += localTotal; });
            Console.WriteLine("Total: {0}", total);

            try
            {
                Parallel.For(-10, 10, i => { Console.WriteLine("100 / {0} = {1}", i, 100 / i); });
            }
            catch (AggregateException ae)
            {
                Console.WriteLine("Exception: {0}", ae.Message);

                foreach (var e in ae.InnerExceptions)
                    Console.WriteLine("InnerException: {0}", e.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}: ", ex.Message);
            }

            try
            {
                var exceptions = new ConcurrentQueue<Exception>();

                Parallel.For(-10, 10,
                    i =>
                    {
                        try
                        {
                            Console.WriteLine("100 / {0} = {1}", i, 100 / i);
                        }
                        catch (Exception ex)
                        {
                            exceptions.Enqueue(ex);
                        }
                    });

                if (exceptions.Count > 0)
                    throw new AggregateException(exceptions);
            }
            catch (AggregateException ae)
            {
                Console.WriteLine("Exception: {0}", ae.Message);

                foreach (var e in ae.InnerExceptions)
                    Console.WriteLine("InnerException: {0}", e.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}: ", ex.Message);
            }
        }

        private static void ParallelForEach()
        {
            Console.WriteLine("Parallel ForEach");

            foreach (var i in Enumerable.Range(0, 10))
                Console.WriteLine("{0} - {1}", i, GetTotal());

            Console.WriteLine();
            Parallel.ForEach(Enumerable.Range(0, 10), i => { Console.WriteLine("{0} - {1}", i, GetTotal()); });
        }

        private static void ParallelInvoke()
        {
            Console.WriteLine("Parallel Invoke");

            Parallel.Invoke(
                () =>
                {
                    Console.WriteLine("Task 1 started");
                    Thread.Sleep(5000);
                    Console.WriteLine("Task 1 complete");
                },
                () =>
                {
                    Console.WriteLine("Task 2 started");
                    Thread.Sleep(3000);
                    Console.WriteLine("Task 2 complete");
                },
                () =>
                {
                    Console.WriteLine("Task 3 started");
                    Thread.Sleep(1000);
                    Console.WriteLine("Task 3 complete");
                });

            try
            {
                Parallel.Invoke(
                    () =>
                    {
                        Console.WriteLine("Task 1 started");
                        Thread.Sleep(5000);
                        Console.WriteLine("Task 1 complete");
                    },
                    () =>
                    {
                        var i = 0;
                        Console.WriteLine("Task 2 started");
                        Console.WriteLine(1 / i);
                    },
                    () =>
                    {
                        Console.WriteLine("Task 3 started");
                        Thread.Sleep(1000);
                        throw new InvalidOperationException("Test Exception");
                    });
            }
            catch (AggregateException ae)
            {
                Console.WriteLine("Exception: {0}", ae.Message);

                foreach (var e in ae.InnerExceptions)
                    Console.WriteLine("InnerException: {0}", e.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}: ", ex.Message);
            }
        }

        private static void ParallelTasks()
        {
            Console.WriteLine("Parallel Tasks");
            Console.WriteLine("CRM Application Starting");

            var userDataBackgroundTask = new Task(
                () =>
                {
                    Console.WriteLine("Loading User Data");
                    Thread.Sleep(2000);
                    throw new InvalidOperationException("User Data Exception");
                    //Console.WriteLine("User Data loaded");
                });

            var customerDataBackgroundTask = new Task(
                () =>
                {
                    Console.WriteLine("Loading Customer Data");
                    Thread.Sleep(2000);
                    throw new InvalidOperationException("Customer Data Exception");
                    //Console.WriteLine("Customer Data loaded");
                });

            var server1BackgroundTask = new Task(
                () =>
                {
                    Console.WriteLine("Checking Server 1");
                    Thread.Sleep(4000);
                    Console.WriteLine("Server 1 OK");
                });

            var server2BackgroundTask = new Task(
                () =>
                {
                    Console.WriteLine("Checking Server 2");
                    Thread.Sleep(5000);
                    Console.WriteLine("Server 2 OK");
                });

            userDataBackgroundTask.Start();
            customerDataBackgroundTask.Start();

            server1BackgroundTask.Start();
            server2BackgroundTask.Start();

            Console.WriteLine("CRM Application Loaded");
            Console.ReadKey();

            try
            {
                Console.WriteLine("Customer Wait: {0}", customerDataBackgroundTask.Wait(500));
                customerDataBackgroundTask.Wait();
            }
            catch (AggregateException ae)
            {
                Console.WriteLine("Exception: {0}", ae.Message);

                foreach (var e in ae.InnerExceptions)
                    Console.WriteLine("InnerException: {0}", e.Message);
            }

            customerDataBackgroundTask.Dispose();

            try
            {
                userDataBackgroundTask.Wait();
            }
            catch (AggregateException ae)
            {
                Console.WriteLine("Exception: {0}", ae.Message);

                foreach (var e in ae.InnerExceptions)
                    Console.WriteLine("InnerException: {0}", e.Message);
            }

            userDataBackgroundTask.Dispose();

            Task.WaitAny(server1BackgroundTask, server2BackgroundTask);
            Task.WaitAll(server1BackgroundTask, server2BackgroundTask);

            server2BackgroundTask.Dispose();
            server1BackgroundTask.Dispose();
        }

        private static void TaskResults()
        {
            Console.WriteLine("Task Results");

            var withResult = new Task<long>(GetTotal);

            withResult.Start();
            Console.WriteLine("Result = {0}", withResult.Result); // Task.Result waits for completion
            withResult.Dispose();
        }

        private static void ContinuationTasks()
        {
            Console.WriteLine("Continuation Tasks");
            Console.WriteLine("CRM Application Starting");

            var userDataBackgroundTask = new Task<string>(
                () =>
                {
                    Console.WriteLine("Loading User Data");
                    Thread.Sleep(2000);
                    Console.WriteLine("User Data loaded");
                    return "1234";
                });

            var userPermissionsBackgroundTask = userDataBackgroundTask.ContinueWith(
                antecedent =>
                {
                    Console.WriteLine("Loading User Permissions");
                    Thread.Sleep(2000);
                    Console.WriteLine("User Permissions for '{0}' Loaded", antecedent.Result);
                    return "Admin";
                });

            var server1BackgroundTask = userDataBackgroundTask.ContinueWith(
                antecedent =>
                {
                    Console.WriteLine("Checking Server 1");
                    Thread.Sleep(4000);
                    Console.WriteLine("Server 1 OK");
                });

            var server2BackgroundTask = userDataBackgroundTask.ContinueWith(
                antecedent =>
                {
                    Console.WriteLine("Checking Server 2");
                    Thread.Sleep(5000);
                    Console.WriteLine("Server 2 OK");
                });

            var finalServerBackgroundTask = Task.Factory.ContinueWhenAll(
                new Task[] { server1BackgroundTask, server2BackgroundTask },
                antecedents =>
                {
                    Console.WriteLine("CRM Application Server Configuration of {0} Loaded", antecedents.Count());
                });

            var exceptionBackgroundTask = new Task(
                () =>
                {
                    Thread.Sleep(2000);
                    throw new InvalidOperationException("Exception Background Task");
                });

            var exceptionContinuationBackgroundTask = exceptionBackgroundTask.ContinueWith(
                antecedent =>
                {
                    Thread.Sleep(1000);
                    throw new InvalidOperationException("Exception Continuation Background Task");
                });

            userDataBackgroundTask.Start();

            Console.WriteLine("CRM Application Loaded");
            Console.ReadKey();

            Console.WriteLine("User Permissions: {0}", userPermissionsBackgroundTask.Result);
            finalServerBackgroundTask.Wait();

            finalServerBackgroundTask.Dispose();
            server2BackgroundTask.Dispose();
            server1BackgroundTask.Dispose();
            userPermissionsBackgroundTask.Dispose();
            userDataBackgroundTask.Dispose();

            exceptionBackgroundTask.Start();

            try
            {
                Task.WaitAll(exceptionBackgroundTask, exceptionContinuationBackgroundTask);
            }
            catch (AggregateException ae)
            {
                Console.WriteLine("Exception: {0}", ae.Message);

                foreach (var e in ae.InnerExceptions)
                    Console.WriteLine("InnerException: {0}", e.Message);
            }

            exceptionContinuationBackgroundTask.Dispose();
            exceptionBackgroundTask.Dispose();
        }

        private static void NestedTasks()
        {
            Console.WriteLine("Nested Tasks");
            Console.WriteLine("CRM Application Starting");

            var userDataBackgroundTask = new Task(
                () =>
                {
                    Console.WriteLine("Loading User Data");
                    Thread.Sleep(2000);

                    var server1BackgroundTask = new Task(
                        () =>
                        {
                            Console.WriteLine("Checking Server 1");
                            Thread.Sleep(4000);
                            //Console.WriteLine("Server 1 OK");
                            throw new InvalidOperationException("Error while checking Server 1");
                        });

                    var server2BackgroundTask = new Task(
                        () =>
                        {
                            Console.WriteLine("Checking Server 2");
                            Thread.Sleep(5000);
                            //Console.WriteLine("Server 2 OK");
                            throw new InvalidOperationException("Error while checking Server 2");
                        });

                    server1BackgroundTask.Start();
                    server2BackgroundTask.Start();

                    try
                    {
                        Task.WaitAll(server1BackgroundTask, server2BackgroundTask);
                        // Need to use AggregateException.Flatten().InnerExceptions in outer catch handler
                    }
                    finally
                    {
                        server2BackgroundTask.Dispose();
                        server1BackgroundTask.Dispose();
                    }

                    Console.WriteLine("User Data loaded");
                });

            userDataBackgroundTask.Start();

            Console.WriteLine("CRM Application Loaded");
            Console.ReadKey();

            try
            {
                userDataBackgroundTask.Wait();
            }
            catch (AggregateException ae)
            {
                Console.WriteLine("Exception: {0}", ae.Message);

                foreach (var e in ae.Flatten().InnerExceptions)
                    Console.WriteLine("InnerException: {0}", e.Message);
            }

            userDataBackgroundTask.Dispose();
        }

        private static void ChildTasks()
        {
            Console.WriteLine("Child Tasks");
            Console.WriteLine("CRM Application Starting");

            var userDataBackgroundTask = new Task(
                () =>
                {
                    Console.WriteLine("Loading User Data");
                    Thread.Sleep(2000);

                    var server1BackgroundTask = new Task(
                        () =>
                        {
                            Console.WriteLine("Checking Server 1");
                            Thread.Sleep(4000);
                            //Console.WriteLine("Server 1 OK");
                            throw new InvalidOperationException("Error while checking Server 1");
                        }, TaskCreationOptions.AttachedToParent);

                    var server2BackgroundTask = new Task(
                        () =>
                        {
                            Console.WriteLine("Checking Server 2");
                            Thread.Sleep(5000);
                            //Console.WriteLine("Server 2 OK");
                            throw new InvalidOperationException("Error while checking Server 2");
                        }, TaskCreationOptions.AttachedToParent);

                    server1BackgroundTask.Start();
                    server2BackgroundTask.Start();

                    Console.WriteLine("User Data loaded");
                    throw new InvalidOperationException("Error while loading UserData");
                });

            userDataBackgroundTask.Start();

            try
            {
                userDataBackgroundTask.Wait();
                Console.WriteLine("CRM Application Loaded");
            }
            catch (AggregateException ae)
            {
                Console.WriteLine("Exception: {0}", ae.Message);

                foreach (var e in ae.Flatten().InnerExceptions)
                    Console.WriteLine("InnerException: {0}", e.Message);
            }

            Console.ReadKey();
            userDataBackgroundTask.Dispose();
        }

        private static void TaskCancellation()
        {
            Console.WriteLine("Task Cancellation");

            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            var longRunning = new Task(
                () =>
                {
                    token.ThrowIfCancellationRequested();

                    for (var i = 0; i < 100; i++)
                    {
                        Console.WriteLine("{0}%", i);
                        Thread.Sleep(1000);

                        token.ThrowIfCancellationRequested();
                    }
                }, token);

            longRunning.Start();

            Console.WriteLine("Press any key to cancel long running background task");
            Console.ReadKey();
            tokenSource.Cancel();

            try
            {
                longRunning.Wait();
            }
            catch (AggregateException ae)
            {
                //if (ae.InnerExceptions.OfType<OperationCanceledException>().Count() == 0)
                //    throw;

                //if (ae.InnerExceptions.Count(e => e is OperationCanceledException) == 0)
                //    throw;

                ae.Handle(e => e is OperationCanceledException);
            }

            Console.WriteLine("Status:    {0}", longRunning.Status);
            Console.WriteLine("Canceled:  {0}", longRunning.IsCanceled);
            Console.WriteLine("Completed: {0}", longRunning.IsCompleted);

            longRunning.Dispose();
        }

        private static long GetTotal()
        {
            var total = 0L;

            for (var i = 1; i <= 300000000; i++)
                total += i;

            return total;
        }
    }
}
