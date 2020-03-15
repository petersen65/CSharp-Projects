using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Xml.Linq;
using System.Reflection;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Data.EntityClient;
using System.Configuration;
using System.Data.Objects;

namespace CSharp4
{
    internal static class Program
    {
        private const string XML_PERSONS = @".\Persons.xml";
        private const string XML_SETTINGS = @".\Settings.xml";

        private static void Main(string[] args)
        {
            foreach (var person in GetPersonsFromStatic())
                Console.WriteLine("{0}, {1}", person.LastName, person.FirstName);

            Console.WriteLine();

            foreach (var person in GetPersonsFromExpando())
                Console.WriteLine("{0}, {1}", person.LastName, person.FirstName);

            Console.WriteLine();

            foreach (var person in GetPersonsFromExpando())
                Console.WriteLine(person.GetFullName());

            Console.WriteLine();
            dynamic settings = GetSettingsFromExpando();

            foreach (var person in GetPersonsFromExpando())
                Console.WriteLine(settings.Format, GetPersonValuesFromSettings(person, settings.Members));

            ShowCovariance();
            ShowContravariance();

            Console.WriteLine();
            ShowDynamicInvocationWithCLR();
            ShowDynamicInvocationWithDLR();
            
            Console.WriteLine();
            ShowOptionalParametersAndNamedArgs();

            Console.WriteLine();
            ShowLazyUsage();

            Console.WriteLine();
            ShowParallelFeatures();

            Console.WriteLine();
            ShowEntityFramework();

            Console.WriteLine();
            ShowLinqToEntities();

            Console.WriteLine();
            ShowIterators(); // C# 3.0 iterators

            Console.WriteLine();
            ShowExpressionTrees(); // C# 3.0 expression trees

            Console.WriteLine();
            ShowObjectInitializers();

            Console.WriteLine();
            ShowExtensionMethods();

            Console.WriteLine();
            Console.ReadKey();
        }

        private static IList<Person> GetPersonsFromStatic()
        {
            var persons = new List<Person>();
            var xdoc = XDocument.Load(XML_PERSONS);

            foreach (var n in xdoc.Root.Descendants("Person"))
            {
                var person = new Person();

                foreach (var descendant in n.Descendants())
                {
                    switch (descendant.Name.LocalName)
                    {
                        case "FirstName":
                            person.FirstName = descendant.Value.Trim();
                            break;
                        
                        case "LastName":
                            person.LastName = descendant.Value.Trim();
                            break;
                    }
                }

                persons.Add(person);
            }

            return persons;
        }

        private static IList<dynamic> GetPersonsFromExpando()
        {
            var persons = new List<dynamic>();
            var xdoc = XDocument.Load(XML_PERSONS);

            foreach (var n in xdoc.Root.Descendants("Person"))
            {
                dynamic person = new ExpandoObject();

                foreach (var descendant in n.Descendants())
                    (person as IDictionary<string, object>)[descendant.Name.LocalName] = descendant.Value.Trim();

                person.GetFullName = (Func<string>)(() => string.Format("{0} {1}", person.FirstName, person.LastName));
                persons.Add(person);
            }

            return persons;
        }

        private static dynamic GetSettingsFromExpando()
        {
            dynamic settings = new ExpandoObject();
            var xdoc = XDocument.Load(XML_SETTINGS);
            var node = xdoc.Root.Descendants("Output").FirstOrDefault();

            settings.Format = node.Attribute("format").Value;
            settings.Members = node.Attribute("parameters").Value.Split(',');

            return settings;
        }

        private static object[] GetPersonValuesFromSettings(IDictionary<string, object> person, string[] members)
        {
            var values = new List<object>();

            foreach (var m in members)
                values.Add(person[m]);

            return values.ToArray();
        }

        private static void ShowCovariance()
        {
            // variance allows conversions between generic interfaces and delegates
            // covariance: convert to less derived type
            // out T is covariant, because of implicit conversions
            // works with interfaces + delegates with reference types only

            // cast: string -> object
            // cast: IEnumerable<out string> -> IEnumerable<out object>
            var sl = new List<string>() as IEnumerable<string>;
            IEnumerable<object> ol = sl;

            // Manager -> Employee
            // IEnumerable<out Manager> -> IEnumerable<out Employee>
            var ml = new List<Manager>() as IEnumerable<Manager>;
            IEnumerable<Employee> el = ml;
        }

        private static void ShowContravariance()
        {
            // variance allows conversions between generic interfaces and delegates
            // contravariance: convert to more derived type
            // in T is contravariant, because of implicit conversions
            // works with interfaces + delegates with reference types only

            // cast: string -> object
            // cast: IComparable<in string> <- IComparable<in object>
            var oc = new ObjectComparer() as IComparable<object>;
            IComparable<string> sc = oc;

            // cast: Manager -> Employee
            // cast: IComparable<int Manager> <- IComparable<in Employee>
            var ec = new EmployeeComparer() as IComparable<Employee>;
            IComparable<Manager> mc = ec;
        }

        private static void ShowDynamicInvocationWithCLR()
        {
            object result;
            var sobj = new MyStaticObject() as object;
            var t = sobj.GetType();

            result = t.InvokeMember("MyMethod", BindingFlags.InvokeMethod, null, sobj, new object[] { });
            
            Console.WriteLine(Convert.ToInt32(result));
            Console.WriteLine();
        }

        private static void ShowDynamicInvocationWithDLR()
        {
            dynamic sobj = new MyStaticObject();
            dynamic dobj = new MyDynamicObject();

            Console.WriteLine(sobj.MyMethod());
            Console.WriteLine();
            
            dobj.MyMethod();
            dobj.Add(1, 3);
            dobj.Foo("Michael", 42);
            dobj.Bar(3.4, "Petra", 'X');
        }

        private static void ShowOptionalParametersAndNamedArgs()
        {
            var car = new Car();

            car.Accelerate(100);
            car.Accelerate(110, inReverse: true);
            car.Accelerate(speed: 120, inReverse: true, gear:42);

            Console.WriteLine(arg2: 2, arg0: 0, arg1: 1, format: "Named Arguments: {0} {1} {2}");
        }

        private static void ShowLazyUsage()
        {
            var l1 = new Lazy<Manager>(false);
            var l2 = new Lazy<Employee>(() => new Employee(), LazyThreadSafetyMode.ExecutionAndPublication);
            var t1 = new ThreadLocal<Manager>();
            var t2 = new ThreadLocal<Employee>(() => new Employee());
            var t3 = new ThreadLocal<int>(() => Thread.CurrentThread.ManagedThreadId);
            Action a1 = () => { Console.WriteLine("ThreadLocal t3 {0} {1}", t3.IsValueCreated, t3.Value); };

            Console.WriteLine("Lazy l1: {0} {1}", l1.IsValueCreated, l1.Value);
            Console.WriteLine("Lazy l2: {0}", l2.Value);

            Console.WriteLine("ThreadLocal t1 {0} {1}", t1.IsValueCreated, t1.Value);
            Console.WriteLine("ThreadLocal t2 {0} {1}", t2.IsValueCreated, t2.Value);

            Parallel.Invoke(Enumerable.Repeat(a1, 8).ToArray());
        }

        private static void ShowParallelFeatures()
        {
            MyStaticObject mso1 = new MyStaticObject(), 
                           mso2 = new MyStaticObject(), 
                           mso3 = new MyStaticObject();
            var range = Enumerable.Range(1, 5);
            var watch = new Stopwatch();

            watch.Reset();
            watch.Start();
            mso1.MyAction();
            mso2.MyAction();
            mso3.MyAction();
            watch.Stop();
            Console.WriteLine("Sequential: {0}", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            Parallel.Invoke(mso1.MyAction, mso2.MyAction, mso3.MyAction);
            watch.Stop();
            Console.WriteLine("Parallel: {0}", watch.ElapsedMilliseconds);
            
            watch.Reset();
            watch.Start();
            for (var i = 0; i < 4; i++)
                mso1.MyAction(i);
            for (var i = 0; i < 4; i++)
                mso2.MyAction(i);
            for (var i = 1; i < 3; i++)
                mso2.MyAction(i);
            watch.Stop();
            Console.WriteLine("Sequential: {0}", watch.ElapsedMilliseconds);
            
            watch.Reset();
            watch.Start();
            Parallel.For(0, 4, mso1.MyAction);
            Parallel.For(0, 4, i => { mso2.MyAction(i); });
            Parallel.For(1, 3, delegate(int i) { mso2.MyAction(i); }); // C# 3.0 anonymous method
            watch.Stop();
            Console.WriteLine("Parallel: {0}", watch.ElapsedMilliseconds);
            
            watch.Reset();
            watch.Start();
            foreach (var r in range)
                mso1.MyAction(r);
            foreach (var r in range)
                mso2.MyAction(r);
            watch.Stop();
            Console.WriteLine("Sequential: {0}", watch.ElapsedMilliseconds);
            
            watch.Reset();
            watch.Start();
            Parallel.ForEach(range, mso1.MyAction);
            Parallel.ForEach(range, r => { mso2.MyAction(r); });
            watch.Stop();
            Console.WriteLine("Parallel: {0}", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            var q = from r in range
                    select mso1.MyFunction(r);
            var l = q.ToList();
            watch.Stop();
            Console.WriteLine("Sequential: {0}", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            var qp = from r in range.AsParallel()
                     select mso1.MyFunction(r);
            var lp = qp.ToList();
            watch.Stop();
            Console.WriteLine("Parallel: {0}", watch.ElapsedMilliseconds);
        }

        private static void ShowEntityFramework()
        {
            var allCustomerNames = "SELECT c FROM EF4TestEntities.CustomerNames AS c";
            var allValueCustomerNames = "SELECT VALUE c FROM EF4TestEntities.CustomerNames AS c";
            var michaelValueCustomerNames = "SELECT VALUE c FROM EF4TestEntities.CustomerNames AS c WHERE c.FirstName = @firstName";
            var ef4TestEntities = ConfigurationManager.ConnectionStrings["EF4TestEntities"].ConnectionString;

            // All customers as raw DbDataRecords through EntityClient provider
            using (var cn = new EntityConnection(ef4TestEntities))
            using (var cmd = new EntityCommand(allCustomerNames, cn))
            {
                cn.Open();

                using (var dr = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    while (dr.Read())
                        Console.WriteLine(((DbDataRecord)dr.GetValue(0)).GetValue(1));
                }
            }

            // All customers as CustomerName value through EntityClient provider
            using (var cn = new EntityConnection(ef4TestEntities))
            using (var cmd = new EntityCommand(allValueCustomerNames, cn))
            {
                cn.Open();

                using (var dr = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    while (dr.Read())
                        Console.WriteLine("{0} {1}", dr.GetValue(0), dr.GetValue(1));
                }
            }

            // All customers as raw DbDataRecords through ObjectContext
            using (var context = new EF4TestEntities())
            {
                var customerNames = context.CreateQuery<DbDataRecord>(allCustomerNames);

                foreach (var c in customerNames)
                    Console.WriteLine(((CustomerName)c.GetValue(0)).FirstName);
            }

            // All customers as CustomerName through ObjectContext
            using (var context = new EF4TestEntities())
            {
                var customerNames = context.CreateQuery<CustomerName>(allValueCustomerNames);

                foreach (var c in customerNames)
                    Console.WriteLine("{0} {1}", c.FirstName, c.LastName);
            }

            // Michael as CustomerName through ObjectContext
            using (var context = new EF4TestEntities())
            {
                var customerNames = 
                    context.CreateQuery<CustomerName>(michaelValueCustomerNames, 
                                                      new[] { new ObjectParameter("firstName", "Michael") });

                foreach (var c in customerNames)
                    Console.WriteLine("{0} {1}", c.CustomerId, c.FirstName);
            }

            // All customers as CustomerName through LINQ
            var all = from cn in new EF4TestEntities().CustomerNames
                      orderby cn.EmailAddress
                      select new { Id = cn.CustomerId, First = cn.FirstName, Last = cn.LastName };

            foreach (var cn in all)
                Console.WriteLine("{0} {1}", cn.Id, cn.First, cn.Last);

            // Michael as CustomerName through LINQ
            var michael = from cn in new EF4TestEntities().CustomerNames
                          where cn.FirstName == "Michael"
                          select cn;

            Console.WriteLine((michael as ObjectQuery).ToTraceString());

            foreach (var cn in michael)
                Console.WriteLine("{0} {1}", cn.FirstName, cn.LastName);
        }

        private static void ShowLinqToEntities()
        {
            var ef4TestEntities = new EF4TestEntities();

            var customerNames = from cn in ef4TestEntities.CustomerNames
                                select cn;

            var shortNames = from cn in ef4TestEntities.CustomerNames
                             select new { First = cn.FirstName, Last = cn.LastName };

            var spatialPoints = from sp in ef4TestEntities.SpatialPoints
                                select new { Id = sp.Id, X = sp.X, Y = sp.Y };

            // Display all initially existing data records
            foreach (var cn in customerNames)
                Console.WriteLine("Customer: {0} {1} {2} {3}", cn.CustomerId, cn.FirstName, cn.LastName, cn.EmailAddress);

            foreach (var sn in shortNames)
                Console.WriteLine("Customer (short): {0} {1}", sn.First, sn.Last);

            foreach (var sp in spatialPoints)
                Console.WriteLine("Point: {0} {1} {2}", sp.Id, sp.X, sp.Y);

            // Create new baseline for data within the database
            foreach (var cn in customerNames)
                ef4TestEntities.CustomerNames.DeleteObject(cn);

            ef4TestEntities.CustomerNames.AddObject(new CustomerName { FirstName = "Petra", LastName = "Jones", EmailAddress = "jones@xx.com" });
            ef4TestEntities.CustomerNames.AddObject(new CustomerName { FirstName = "Michael", LastName = "Smith", EmailAddress = "smith@xx.com" });
            ef4TestEntities.CustomerNames.AddObject(new CustomerName { FirstName = "fn1", LastName = "ln1", EmailAddress = "em1" });
            ef4TestEntities.SaveChanges();

            // Display newly created baseline of data
            foreach (var cn in customerNames)
                Console.WriteLine("Customer: {0} {1} {2} {3}", cn.CustomerId, cn.FirstName, cn.LastName, cn.EmailAddress);

            // Update existing data record
            var toUpdate = (from c in ef4TestEntities.CustomerNames
                            where c.FirstName == "fn1" && c.LastName == "ln1" && c.EmailAddress == "em1"
                            select c).First();

            toUpdate.EmailAddress = "em2";
            ef4TestEntities.SaveChanges();

            // Display updated data record
            foreach (var cn in customerNames)
                Console.WriteLine("Customer: {0} {1} {2} {3}", cn.CustomerId, cn.FirstName, cn.LastName, cn.EmailAddress);

            // Delete specific data record
            var toDelete = (from c in ef4TestEntities.CustomerNames
                            where c.FirstName == "fn1" && c.LastName == "ln1" && c.EmailAddress == "em2"
                            select c).First();

            ef4TestEntities.CustomerNames.DeleteObject(toDelete);
            ef4TestEntities.SaveChanges();

            // Proof that data record has beed deleted
            foreach (var cn in customerNames)
                Console.WriteLine("Customer: {0} {1} {2} {3}", cn.CustomerId, cn.FirstName, cn.LastName, cn.EmailAddress);
        }

        private static void ShowIterators()
        {
            foreach (var c in new Countdown(3))
                Console.WriteLine(c);

            foreach (var c in new Countdown2(4))
                Console.WriteLine(c);
        }

        private static void ShowExpressionTrees()
        {
            Func<int, int, int> addDelegate = (a, b) => a + b;
            Expression<Func<int, int, int>> addExpression = (a, b) => a + b;

            Console.WriteLine("Delegate {0}", addDelegate.ToString());
            Console.WriteLine("Expression {0}", addExpression.ToString());

            Console.WriteLine("addDelegate: {0}", addDelegate(12, 30));
            Console.WriteLine("addExpression: {0}", addExpression.Compile()(12, 30));
        }

        private static void ShowObjectInitializers()
        {
            int[] ints = { 1, 2, 3 }; // C# 3.0 array initialization style
            var ints2 = new[] { 1, 2, 3 }; // C# 4.0 array initialization style with type inference
            var person = new Person { FirstName = "Michael", LastName = "Smith" };

            // C# 4.0 anonymous types
            var anonymous = new { person.FirstName, person.LastName };
            var anonymous2 = new { FirstName = "Michael", LastName = "Smith" }; // same type as above
            var ans = new[] { new { Name = "X", Age = 1 }, new { Name = "Y", Age = 2 } };
            
            // C# 4.0 object initialization with newly created points
            var rect = new Rectangle { TL = new Point { X = 1, Y = 2 }, BR = new Point { X = 3, Y = 4 } };

            // C# 4.0 object initialization with reuse of 'Rectangle2 ctor' created points
            var rect2 = new Rectangle2 { TL = { X = 1, Y = 2 }, BR = { X = 3, Y = 4 } };

            // C# 4.0 collection initializer
            var ints3 = new List<int> { 1, 2, 3 };
            var points = new List<Point> { new Point { X = 1, Y = 2 }, new Point { X = 3, Y = 4 } };
            var points2 = new ArrayList { new Point { X = 1, Y = 2 }, new Point { X = 3, Y = 4 } };

            Console.WriteLine(anonymous.GetType());
            Console.WriteLine(anonymous2.GetType());
            Console.WriteLine(ans.GetType());
        }

        private static void ShowExtensionMethods()
        {
            int i = 0, j = 0;

            var c1 = from c in new Countdown(10)
                     where c > 3
                     let s = ++i
                     select new { Step = s, Counter = c };

            // calls custom select/where query operators of CountdownEnumeration, see below
            Console.WriteLine("Query Expression");

            foreach (var c in c1)
                Console.WriteLine("{0} / {1}", c.Step, c.Counter);

            var c2 = new Countdown(10)
                         .Where(c => c > 3)
                         .Select(c => new { Step = ++j, Counter = c });

            // calls custom select/where query operators of CountdownEnumeration, see below
            Console.WriteLine("Extension Methods");

            foreach (var c in c2)
                Console.WriteLine("{0} / {1}", c.Step, c.Counter);
        }
    }
    
    internal class Employee
    {
    }

    internal class Manager : Employee
    {
    }

    internal class EmployeeComparer : IComparable<Employee>
    {
        public int CompareTo(Employee other)
        {
            return -1;
        }
    }

    internal class ObjectComparer : IComparable<object>
    {
        public int CompareTo(object other)
        {
            return -1;
        }
    }

    internal class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    internal class MyStaticObject
    {
        public int MyMethod()
        {
            return 42;
        }

        public void MyAction()
        {
            Thread.Sleep(200);
            Console.WriteLine("MyAction: {0}", DateTime.Now.ToShortTimeString());
        }

        public void MyAction(int i)
        {
            Thread.Sleep(200);
            Console.WriteLine("MyAction: {0}", i);
        }

        public int MyFunction(int i)
        {
            Thread.Sleep(200);
            Console.WriteLine("MyFunction: {0}", i);

            return i;
        }
    }

    internal class MyDynamicObject : DynamicObject
    {
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            Console.WriteLine("Name: {0}", binder.Name);

            foreach (var arg in args)
                Console.WriteLine("Argument: {0}", arg);

            result = args.Length > 0 ? args[0] : null;
            return true;
        }
    }

    internal class Car
    {
        public void Accelerate(double speed, int? gear = null, bool inReverse = false)
        {
            Console.WriteLine("Accelerate: {0}, {1}, {2}", speed, gear ?? -1, inReverse);
        }
    }

    internal class Countdown : IEnumerable<int> // C# 3.0 iterators (typical use case)
    {
        private int _start;
        
        public Countdown(int start)
        {
            _start = start;
        }

        public IEnumerator<int> GetEnumerator()
        {
            for (int i = _start; i > 0; i--)
                yield return i;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public static class CountdownEnumeration
    {
        public static IEnumerable<T> Select<T>(this IEnumerable<int> source, Func<int, T> selector)
        {
            foreach (var c in source)
            {
                Console.WriteLine("Select {0}", c);
                yield return selector(c);
            }
        }

        public static IEnumerable<int> Where(this IEnumerable<int> source, Func<int, bool> predicate)
        {
            foreach (var c in source)
            {
                if (predicate(c))
                {
                    Console.WriteLine("Where {0}", c);
                    yield return c;
                }
            }
        }
    }

    internal class Countdown2 // C# 3.0 iterators (shorter use case with public 'GetEnumerator' method)
    {
        private int _start;
        
        public Countdown2(int start)
        {
            _start = start;
        }

        public IEnumerator<int> GetEnumerator()
        {
            for (int i = _start; i > 0; i--)
                yield return i;
        }
    }

    internal class Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    internal class Rectangle
    {
        public Point TL { get; set; }
        public Point BR { get; set; }
    }

    internal class Rectangle2
    {
        public Point TL { get; private set; }
        public Point BR { get; private set; }

        public Rectangle2()
        {
            TL = new Point();
            BR = new Point();
        }
    }
}
