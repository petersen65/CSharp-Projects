using System;
using System.Collections.Generic;

// using static is a new kind of using clause that lets you import static members of types directly into scope
using static System.Console;
using static System.Math;

namespace CSharp6
{
    // auto-property initializers
    // initializer directly initializes the backing field
    // backing field of a getter-only auto-property is implicitly declared as readonly 
    internal class Person
    {
        public string Name { get; set; } = "Default";
        public int Age { get; set; } = 0;
        public string NameAndAge => $"{Name} {Age}";

        // expression-bodied function members 
        public static implicit operator string (Person p) => p.Name;
        public void Print() => WriteLine(NameAndAge);
    }

    // immutable type are now getting easier
    internal class Point
    {
        // only getters now possible
        public int X { get; }
        public int Y { get; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        // expression bodies for methods, getters, and operators
        // string formatters as C# language feature
        public double Distance => Sqrt(X * X + Y * Y);
        public override string ToString() => $"({X},{Y})";
        public static Point operator +(Point p1, Point p2) => new Point(p1.X + p2.X, p1.Y + p2.Y);
        public static Point operator -(Point p1, Point p2) => new Point(p1.X - p2.X, p1.Y - p2.Y);
    }

    internal static class Program
    {
        private static void Main(string[] args)
        {
            var x = 32L;
            var p = new Person { Name = "Michael", Age = 50 };

            // getting name of program elements
            WriteLine(string.Format("Name of variable is {0}", nameof(x)));
            WriteLine(string.Format("Name of property is {0}", nameof(p.Name)));

            // string interpolation
            var s = string.Format("{0} is {1} years old", p.Name, p.Age);
            WriteLine(s);

            s = $"{p.Name} is {p.Age} years old";
            WriteLine(s);

            s = $"{p.Name:20} is {p.Age:D3} years old";
            WriteLine(s);

            s = $"{p.Name} is {p.Age} year{(p.Age == 1 ? "" : "s")} old";
            WriteLine(s);

            // null-conditional operators 
            var age = p?.Age;
            p = null;
            var name = p?.Name;
            var age2 = p?.Age ?? 0;
            var agestring = p?.Age.ToString();

            // index initializers just like object initializers
            var dict1 = new Dictionary<int, string> { [0] = "text1", [5] = "text2", [5656] = "text3" };

            // exception filters
            // await in catch and finally blocks
            try
            {
                if (dict1 != null)
                    throw new DivideByZeroException();
            }
            catch (Exception e) when (Filter(e))
            {
                // await in catch and finally blocks
            }

            try
            {
                if (dict1 != null)
                    throw new DivideByZeroException();
            }
            catch (Exception e) when (Log(e))
            {
                // await in catch and finally blocks
            }

            // expression-bodied function members 
            new Person().Print();

            // end of program
            WriteLine("\nPress enter to exit.");
            ReadLine();
        }

        private static bool Filter(Exception e)
        {
            return true;
        }

        private static bool Log(Exception e)
        {
            return true;
        }

        // expression-bodied function members 
        private static Point Move(int x, int y) => new Point(x, y);
    } 
}
