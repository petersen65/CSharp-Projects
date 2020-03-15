using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace CSharp3
{
    internal class AutomaticProperties
    {
        public string Name { get; set; }
        public int Age { get; private set; }
        public string Major { get; set; }
    }

    internal class Point
    {
        public int X, Y;
    }

    internal class Circle
    {
        public Point Center;
        public int Radius;
    }

    internal class TypeInferencing
    {
        private Dictionary<int, string> GetDictionary()
        {
            var d = new Dictionary<int, string>();
            return d;
        }
    }

    internal static class IntExtensions
    {
        public static int Add(this int x, int y)
        {
            return x + y;
        }

        public static void Times(this int x, Action p)
        {
            for (var i = 0; i < x; i++)
                p();
        }
    }

    internal static class PointExtensions
    {
        public static int Sum(this Point p)
        {
            return p.X + p.Y;
        }
    }

    internal class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }

        public override string ToString()
        {
            return string.Format("{0} {1}", Name, Age);
        }
    }

    internal static class Family
    {
        public static IEnumerable<Person> GetFamily()
        {
            yield return new Person { Name = "Petra", Age = 44 };
            yield return new Person { Name = "Michael", Age = 43 };
            yield return new Person { Name = "Jenny", Age = 15 };
            yield return new Person { Name = "Jessy", Age = 16 };
        }
    }

    internal static class MyLinq
    {
        public static IEnumerable<T> Where<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            foreach (var item in source)
            {
                if (predicate(item))
                    yield return item;
            }
        }

        public static IEnumerable<TResult> Select<T, TResult>(this IEnumerable<T> source, Func<T, TResult> projection)
        {
            foreach (var item in source)
                yield return projection(item);
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            // Object Initializers
            var p1 = new Point { X = 1, Y = 4 };
            var c1 = new Circle { Center = new Point { X = 4, Y = 5 }, Radius = 55 };

            // Collection Initializers
            var l1 = new List<int> { 1, 2, 3, 4, 5 };
            var l2 = new List<Point> { new Point { X = 1, Y = 1 }, new Point { X = 2, Y = 2 } };
            var ia1 = new int[] { 3, 4, 5 };

            // Anonymous Types
            var at1 = new { Z = 5, p1, ia1, Name = "Michael" };

            // Extension Methods
            Console.WriteLine("Sum of {0} and {1} is {2}", 4, 7, 4.Add(7));
            Console.WriteLine("Sum of {0} and {1} is {2}", 20, 22, new Point { X = 20, Y = 22 }.Sum());

            // Lamda Expressions
            5.Times(delegate { Console.WriteLine("Hello, Ruby!"); });
            3.Times(() => Console.WriteLine("Hello, Ruby - again!"));
            l2.Sort((Point a, Point b) => { return a.Sum() - b.Sum(); });
            l2.Sort((Point a, Point b) => a.Sum() - b.Sum());
            l2.Sort((a, b) => a.Sum() - b.Sum());

            // Understanding LINQ - streaming approach
            Console.WriteLine("MY LINQ is coming ...");

            var myFamily = Family.GetFamily();

            var myOldFamily = Family.GetFamily()
                                .Where(p => p.Age > 16);

            var myOldFamilyNames = Family.GetFamily()
                                    .Where(p => p.Age > 16)
                                    .Select(p => p.Name);

            var myOldFamilyNames2 = Family.GetFamily()
                                    .Where(p => p.Age > 16)
                                    .Select(p => new { p.Age, p.Name, All = string.Format("{0} - {1}", p.Name, p.Age) });

            // Query Expressions - based on custom developed Where/Select Query Operators
            var myOldFamily2 =
                from p in Family.GetFamily()
                where p.Age > 16
                select p;

            var myOldFamilyNames3 =
                from p in Family.GetFamily()
                where p.Age > 16
                select p.Name;

            var myOldFamilyNames4 =
                from p in Family.GetFamily()
                where p.Age > 16
                select new { p.Age, p.Name, All = string.Format("{0} - {1}", p.Name, p.Age) };

            foreach (var item in myFamily)
                Console.WriteLine(item);

            foreach (var item in myOldFamily)
                Console.WriteLine(item);

            foreach (var item in myOldFamilyNames)
                Console.WriteLine(item);
            
            foreach (var item in myOldFamilyNames2)
                Console.WriteLine(item);

            foreach (var item in myOldFamily2)
                Console.WriteLine(item);

            foreach (var item in myOldFamilyNames3)
                Console.WriteLine(item);
            
            foreach (var item in myOldFamilyNames4)
                Console.WriteLine(item);
        }
    }
}
