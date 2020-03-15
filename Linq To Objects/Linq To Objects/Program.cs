using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Linq_To_Objects
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            ExtensionMethods();
            LambdaExpressions();
            LanguageIntegratedQuery();

            Console.ReadLine();
        }


        #region Extension Methods
        private static void ExtensionMethods()
        {
            var testString = "Hello, World.";
            var testInt = 99;
            var product = new Product { StockCode = "RAM", QuantityInStock = 100, Price = 49.99 };

            testString.OutputToConsole("[", "]");
            testInt.OutputToConsole();
            product.CalcStockValue().OutputToConsole();

            Console.WriteLine();
        }
        #endregion

        #region Lambda Expressions
        delegate int IncLambda(int i);
        delegate int MultLambda(int x, int y, int z);
        delegate float NoParamLambda();

        private static void ThreadCode()
        {
            Console.WriteLine("New Thread!");
        }

        private static void LambdaExpressions()
        {
            var dino = new { Make = "Ferrari", Model = "Dino", Color = "Red" };
            var sameDino = new { Make = "Ferrari", Model = "Dino", Color = "Red" };
            var anotherDino = new { Make = "Ferrari", Color = "Red", Model = "Dino" };
            var newThread = new Thread(ThreadCode);
            var anotherThread = new Thread(delegate() { Console.WriteLine("Another Thread!"); });
            var paramThread = new Thread(delegate(object parameter) { Console.WriteLine(parameter); });

            int[] oneToTen = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var evenNumbers = oneToTen.Where(n => n % 2 == 0);
            IncLambda incLambdaFullStatement = (int x) => { return x + 1; };
            IncLambda incLambdaExpression = x => x + 1;
            MultLambda multLambda = (x, y, z) => x * y * z;
            NoParamLambda noParamLamda = () => 99F;

            newThread.Start();
            anotherThread.Start();
            paramThread.Start("Param Thread!");

            newThread.Join();
            anotherThread.Join();
            paramThread.Join();
            
            Console.WriteLine(dino);
            Console.WriteLine(anotherDino);
            Console.WriteLine(dino == sameDino);
            Console.WriteLine(dino.Equals(sameDino));
            Console.WriteLine(dino.GetHashCode());
            Console.WriteLine(sameDino.GetHashCode());
            Console.WriteLine(dino.GetType());
            Console.WriteLine(sameDino.GetType());
            Console.WriteLine(anotherDino.GetType());

            foreach (var i in evenNumbers)
                Console.Write("{0} ", i);

            Console.WriteLine();
            Console.WriteLine();
        }
        #endregion

        #region Language Integrated Query
        private class Employee
        {
            public string Name { get; set; }
            public string Title { get; set; }
            public int Salary { get; set; }
            public string[] Skills { get; set; }

            public Employee(string name, string title, int salary = 50000, string[] skills = null)
            {
                Name = name;
                Title = title;
                Salary = salary;
                Skills = skills;
            }

            public override string ToString()
            {
                return string.Format("{0}, {1} (EUR {2})", Name, Title, Salary);
            }
        }

        private class StockItem
        {
            public string Name { get; set; }
            public string Category { get; set; }
            public double Price { get; set; }

            public StockItem(string name, string category, double price)
            {
                Name = name;
                Category = category;
                Price = price;
            }

            public override string ToString()
            {
                return string.Format("{0}/{1}/{2}", Name, Category, Price);
            }
        }

        private class StockCategory
        {
            public string Name { get; set; }
            public string MajorCategory { get; set; }

            public StockCategory(string name, string majorCategory)
            {
                Name = name;
                MajorCategory = majorCategory;
            }
            
            public override string ToString()
            {
                return string.Format("{0}/{1}", Name, MajorCategory);
            }
        }

        private static void LanguageIntegratedQuery()
        {
            var dataSource = new List<string> { "A", "B", "C" };

            var queryOperators = Directory.GetDirectories(@"C:\")
                .Where(d => d.Length > 10)
                .OrderBy(d => d.Length);

            var queryExpression = from d in Directory.GetDirectories(@"C:\")
                                  where d.Length > 10
                                  orderby d.Length
                                  select d;

            var strings = from s in dataSource
                          select s;

            var employees = new List<Employee> 
                { 
                    new Employee("Bob", "Senior Developer", 40000, new string[] { "ASP.NET", "C#", "JavaScript", "SQL", "XML" }),
                    new Employee("Mel", "Principal Developer", skills: new string[] { "BizTalk", "C#", "XML" }),
                    new Employee("Sam", "Developer", 32000, new string[] { "ASP.NET", "C#", "Oracle", "XML" }),
                    new Employee(title: "Developer", name: "Mel", salary: 29000, skills: new string[] { "C#", "C++", "SQL" }),
                    new Employee("Jim", "Junior Programmer", 20000, new string[] { "HTML", "Visual Basic" })
                };

            var stock = new List<StockItem>
                {
                    new StockItem("Apple", "Fruit", 0.30),
                    new StockItem("Banana", "Fruit", 0.35),
                    new StockItem("Orange", "Fruit", 0.29),
                    new StockItem("Cabbage", "Vegetable", 0.49),
                    new StockItem("Carrot", "Vegetable", 0.29),
                    new StockItem("Lettuce", "Vegetable", 0.30),
                    new StockItem("Milk", "Dairy", 1.12)
                };

            var categories = new List<StockCategory>
                {
                    new StockCategory("Dairy", "Chilled"),
                    new StockCategory("Fruit", "Fresh"),
                    new StockCategory("Vegetable", "Healthy")
                };

            var fruit = new string[] { "Apple", "Banana", "Cherry", 
                                       "Damson", "Elderberry", "Grape", 
                                       "Kiwi", "Lemon", "Melon", "Orange" };

            var developers = employees
                .Where(e => e.Title.Contains("Developer") && e.Salary > 30000);

            var developers2 = from e in employees
                              where e.Title.Contains("Developer") && e.Salary > 30000
                              select e;

            var developers3 = from e in employees
                              let isDeveloper = e.Title.Contains("Developer")
                              where (isDeveloper && e.Salary > 30000) || (!isDeveloper && e.Salary < 21000)
                              select new { Employee = e.Name, Job = e.Title };

            var developers4 = employees.SelectMany(e => e.Skills);
            var developers5 = employees.SelectMany(e => e.Skills, (e, s) => new { e.Name, s });

            var developers6 = employees
                .Where(e => e.Title == "Developer")
                .SelectMany(e => e.Skills, (e, s) => new { e.Name, Skill = s })
                .Where(es => es.Skill.StartsWith("C"));

            var developers7 = from e in employees
                              where e.Title == "Developer"
                              from s in e.Skills
                              where s.StartsWith("C")
                              select new { e.Name, Skill = s };

            var developers8 = employees.OrderBy(e => e.Name).ThenBy(e => e.Title);

            var developers9 = from e in employees
                              orderby e.Name, e.Title
                              select e;

            var groups = stock.GroupBy(si => si.Category);
            var groups2 = stock.GroupBy(si => si.Category, p => p.Name);

            var groups3 = stock.GroupBy(si => si.Category,
                                        p => p.Name,
                                        (key, items) => new { Category = key, Count = items.Count(), Items = items });

            var groups4 = from si in stock
                          group si by si.Category;

            var groups5 = from si in stock
                          group si.Name by si.Category;

            var groups6 = from si in stock
                          group si by si.Category into category
                          select new { Category = category.Key, Count = category.Count(), Items = category };

            var groups7 = from si in stock
                          group si by si.Category into category
                          select new { Category = category.Key, 
                                       Sum = category.Sum(si => si.Price), 
                                       Min = category.Min(si => si.Price),
                                       Max = category.Max(si => si.Price),
                                       Average = category.Average(si => si.Price),
                                       CustomDouble = category.Aggregate(0D, (r, n) => r + n.Price * 0.9),
                                       CustomString = category.Aggregate(string.Empty, (r, n) => r == string.Empty ? n.Name : r + "," + n.Name) };

            // join: two lists as input streams
            // one named outer, the other named inner
            // parent/child relationship based on shared foreign key
            var joined = stock.Join(categories,
                                    o => o.Category,
                                    i => i.Name,
                                    (o, i) => new { Name = o.Name, 
                                                    Price = o.Price, 
                                                    MinorCategory = i.Name, 
                                                    MajorCategory = i.MajorCategory });

            var joined2 = from o in stock
                          join i in categories on o.Category equals i.Name
                          select new { Name = o.Name, 
                                       Price = o.Price, 
                                       MinorCategory = i.Name, 
                                       MajorCategory = i.MajorCategory };

            var joined3 = categories.GroupJoin(stock, o => o.Name,
                                               i => i.Category,
                                               (o, i) => new { MinorCategory = o.Name, 
                                                               MajorCategory = o.MajorCategory,
                                                               StockItems = i });

            var joined4 = categories.GroupJoin(stock, o => o.Name,
                                               i => i.Category,
                                               (o, i) => new { MinorCategory = o.Name, 
                                                               MajorCategory = o.MajorCategory,
                                                               Count = i.Count(),
                                                               Total = i.Sum(si => si.Price) });

            var joined5 = from o in categories
                          join i in stock on o.Name equals i.Category into sis
                          select new { MinorCategory = o.Name, 
                                       MajorCategory = o.MajorCategory,
                                       StockItems = sis };

            var joined6 = from o in categories
                          join i in stock on o.Name equals i.Category into sis
                          select new { MinorCategory = o.Name, 
                                       MajorCategory = o.MajorCategory,
                                       Count = sis.Count(),
                                       Total = sis.Sum(si => si.Price) };

            var partitioned = fruit.Take(5);
            var partitioned2 = fruit.Skip(5);

            int pageSize = 3, page = 2;
            var partitioned3 = fruit.Skip((page - 1) * pageSize).Take(pageSize);
            var partitioned4 = fruit.TakeWhile(s => s.Length < 8);
            var partitioned5 = fruit.SkipWhile(s => s.Length < 10);

            var partitioned6 = (from s in fruit
                                select s).Skip((page - 1) * pageSize).Take(pageSize);

            foreach (var folder in queryOperators)
                Console.WriteLine(folder);

            foreach (var folder in queryExpression)
                Console.WriteLine(folder);

            dataSource.Add("D");

            foreach (var s in strings)
                Console.WriteLine(s);

            foreach (var developer in developers9)
                Console.WriteLine(developer);

            foreach (var g in groups2)
            {
                Console.WriteLine(g.Key);

                foreach (var p in g)
                    Console.WriteLine("   {0}", p);
            }

            foreach (var g in groups6)
            {
                Console.WriteLine(g.Count == 1 ? "{0}, {1} item" : "{0}, {1} items", g.Category, g.Count);

                foreach (var p in g.Items)
                    Console.WriteLine("   {0}", p);
            }

            foreach (var g in groups7)
                Console.WriteLine(g);

            foreach (var g in joined6)
                Console.WriteLine(g);

            foreach (var g in partitioned5)
                Console.WriteLine(g);

            Console.WriteLine();
        }
        #endregion
    }

    #region Extension Methods
    internal interface IProduct
    {
        string StockCode { get; set; }
        int QuantityInStock { get; set; }
        double Price { get; set; }
    }

    internal class Product : IProduct
    {
        public string StockCode { get; set; }
        public int QuantityInStock { get; set; }
        public double Price { get; set; }
    }

    internal static class SampleExtensionMethods
    {
        public static void OutputToConsole(this string s, string prefix, string suffix)
        {
            Console.WriteLine("{0}{1}{2}", prefix, s, suffix);
        }

        public static void OutputToConsole(this int i)
        {
            Console.WriteLine(i);
        }

        public static void OutputToConsole(this double d)
        {
            Console.WriteLine(d);
        }

        public static double CalcStockValue(this IProduct product)
        {
            return product.Price * product.QuantityInStock;
        }
    }
    #endregion
}
