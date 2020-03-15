using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.EntityClient;
using System.Transactions;
using LanguageFeatures.Northwind.SQL;
using LanguageFeatures.LINQ_To_Entities;
using NorthwindCustomer = LanguageFeatures.LINQ_To_Entities.Customer;

namespace LanguageFeatures
{
    /// <summary>
    /// 
    /// </summary>
    internal static class LinqLite
    {
        public static IEnumerable<T> Where2<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            foreach (var item in source)
            {
                if (predicate(item))
                    yield return item;
            }
        }

        public static IEnumerable<TResult> Select2<T, TResult>(this IEnumerable<T> source,
                                                               Func<T, TResult> projection)
        {
            foreach (var item in source)
                yield return projection(item);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        #region Generics
        private static int Min(int a, int b)
        {
            return a < b ? a : b;
        }

        private static IComparable Min2(IComparable a, IComparable b)
        {
            return a.CompareTo(b) < 0 ? a : b;
        }

        private static T Min3<T>(T a, T b) where T : IComparable<T>
        {
            return a.CompareTo(b) < 0 ? a : b;
        }

        private static void Generics()
        {
            Console.WriteLine("Generics ------------------------------------------------------------------");

            Console.WriteLine("Min: {0}", Min(2, 3));
            Console.WriteLine("Min2: {0}", (int)Min2(6, 1));
            Console.WriteLine("Min3: {0}", Min3(3.5, 5.7));

            Console.WriteLine();
        }
        #endregion

        #region Delegates and Anonymous Methods
        private delegate void SimpleDelegate();
        private delegate int ReturnValueDelegate();
        private delegate void TwoParametersDelegate(string name, int age);

        private class DemoDelegate
        {
            public void MethodA()
            {
                Console.WriteLine("MethodA");
            }

            public int MethodB()
            {
                Console.WriteLine("MethodB");
                return 0;
            }

            public void MethodC(string n, int a)
            {
                Console.WriteLine("MethodC({0}, {1})", n, a);
            }

            public void Repeat3Times(SimpleDelegate sd)
            {
                for (int i = 0; i < 3; i++)
                    sd();
            }

            public void Repeat3Times2(TwoParametersDelegate tpd)
            {
                for (int i = 0; i < 3; i++)
                    tpd("Petra", 44);
            }
        }

        private static void DelegatesAndAnonymousMethods()
        {
            var demoDelegate = new DemoDelegate();
            var simpleDelegate = new SimpleDelegate(demoDelegate.MethodA);
            ReturnValueDelegate returnValueDelegate = demoDelegate.MethodB;
            TwoParametersDelegate twoParametersDelegate = demoDelegate.MethodC;

            Console.WriteLine("Delegates -----------------------------------------------------------------");

            simpleDelegate();
            returnValueDelegate();
            twoParametersDelegate("Michael", 43);
            
            demoDelegate.Repeat3Times(simpleDelegate);
            demoDelegate.Repeat3Times2(delegate(string n, int a) { Console.WriteLine("delegate({0}, {1})", n, a); });

            Console.WriteLine();
        }
        #endregion

        #region Enumerators and Yield
        private class CountdownEnumerator : IEnumerator<int>
        {
            private int _counter;
            private Countdown _countdown;

            public CountdownEnumerator(Countdown countdown)
	        {
                _countdown = countdown;
                Reset();
	        }

            public void Dispose()
            {
                _countdown = null;
            }

            public int Current
            {
                get 
                { 
                    return _counter; 
                }
            }

            object IEnumerator.Current
            {
                get 
                { 
                    return this.Current; 
                }
            }

            public bool MoveNext()
            {
                var result = _counter > 0;

                if (result)
                    _counter--;

                return result;
            }

            public void Reset()
            {
                _counter = _countdown.StartCountdown + 1;
            }
        }

        private class Countdown : IEnumerable<int>
        {
            public int StartCountdown;

            public IEnumerator<int> GetEnumerator()
            {
                return new CountdownEnumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        private class CountdownYield : IEnumerable<int>
        {
            public int StartCountdown;

            public IEnumerator<int> GetEnumerator()
            {
                for (int i = StartCountdown; i >= 0; i--)
                    yield return i;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        private static void EnumeratorsAndYield()
        {
            var countdown = new Countdown { StartCountdown = 3 };
            var iEnum = countdown.GetEnumerator();
            var countdownYield = new CountdownYield { StartCountdown = 3 };

            Console.WriteLine("Enumerators ---------------------------------------------------------------");

            while (iEnum.MoveNext())
                Console.WriteLine("Enum1: {0}", iEnum.Current);

            iEnum.Reset();
            
            while (iEnum.MoveNext())
                Console.WriteLine("Enum2: {0}", iEnum.Current);

            foreach (var i in countdown)
                Console.WriteLine("Enum3: {0}", i);

            foreach (var i in countdownYield)
                Console.WriteLine("Enum4: {0}", i);

            Console.WriteLine();
        }
        #endregion

        #region Query Expressions
        private class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }

            public override string ToString()
            {
                return string.Format("{0} - {1}", Name, Age);
            }
        }

        private static class Family
        {
            public static IEnumerable<Person> GetFamily()
            {
                yield return new Person { Name = "Petra", Age = 44 };
                yield return new Person { Name = "Michael", Age = 43 };
                yield return new Person { Name = "Jenny", Age = 15 };
                yield return new Person { Name = "Jessy", Age = 16 };
            }
        }

        private static class LinqToCSV
        {
            public const string PATH_AGE = @"..\..\LINQ File Sources\AgeTranslation.txt";
            public const string PATH_FAMILY = @"..\..\LINQ File Sources\FamilyMembers.txt";
            
            public static IEnumerable<string> GetCsvSource(string path)
            {
                using (var sr = new StreamReader(path))
                {
                    while (!sr.EndOfStream)
                        yield return sr.ReadLine();
                }
            }
        }

        private static void QueryExpressions()
        {
            Console.WriteLine("Query Expressions ---------------------------------------------------------");

            var myFamily = Family.GetFamily();

            var myOldFamily = Family.GetFamily()
                                .Where2(p => p.Age > 16);

            var myOldFamilyNames = Family.GetFamily()
                                    .Where2(p => p.Age > 16)
                                    .Select2(p => p.Name);

            var myOldFamilyNames2 = Family.GetFamily()
                                    .Where2(p => p.Age > 16)
                                    .Select2(p => new { p.Age, p.Name, 
                                                        All = string.Format("{0} - {1}", p.Name, p.Age) });

            foreach (var item in myFamily)
                Console.WriteLine(item);

            foreach (var item in myOldFamily)
                Console.WriteLine(item);

            foreach (var item in myOldFamilyNames)
                Console.WriteLine(item);

            foreach (var item in myOldFamilyNames2)
                Console.WriteLine(item);

            var familyMembers =
                from line in LinqToCSV.GetCsvSource(LinqToCSV.PATH_FAMILY)
                let columns = line.Split(',')
                select new Person { Name = columns[0], Age = int.Parse(columns[1]) };

            IEnumerable<IGrouping<int, string>> familyMemberGroups =
                from line in LinqToCSV.GetCsvSource(LinqToCSV.PATH_FAMILY)
                let age = int.Parse(line.Split(',')[1])
                group line by age;

            foreach (var item in familyMembers)
                Console.WriteLine(item);

            foreach (IGrouping<int, string> group in familyMemberGroups)
            {
                Console.WriteLine("Key: {0}", group.Key);

                foreach (string item in group)
                    Console.WriteLine(item.Split(',')[0]);
            }

            Console.WriteLine();
        }
        #endregion

        #region Query Operators
        private enum Countries
        {
            USA,
            Italy
        }

        private class Order
        {
            public int Quantity;
            public bool Shipped;
            public string Month;
            public int IdProduct;

            public override string ToString()
            {
                return string.Format("{0} - {1} - {2} - {3}", Quantity, Shipped, Month, IdProduct);
            }
        }

        private class Product
        {
            public int IdProduct;
            public decimal Price;

            public override string ToString()
            {
                return string.Format("{0} - {1}", IdProduct, Price);
            }
        }

        private class Customer
        {
            public string Name;
            public string City;
            public Countries Country;
            public Order[] Orders;
            
            public override string ToString()
            {
                return string.Format("{0} - {1} - {2} - {3}", Name, City, Country, Orders.Count());
            }
        }

        private static Customer[] customers = new Customer[]
            {
                new Customer { Name = "Paolo", City = "Brescia", Country = Countries.Italy,
                               Orders = new Order[]
                               {
                                   new Order { Quantity = 3, IdProduct = 1, Shipped = false, Month = "January"},
                                   new Order { Quantity = 5, IdProduct = 2, Shipped = true, Month = "May"}
                               }},
                new Customer { Name = "Marco", City = "Torino", Country = Countries.Italy,
                               Orders = new Order[]
                               {
                                   new Order { Quantity = 10, IdProduct = 1, Shipped = false, Month = "July"},
                                   new Order { Quantity = 20, IdProduct = 3, Shipped = true, Month = "December"}
                               }},
                new Customer { Name = "James", City = "Dallas", Country = Countries.USA,
                               Orders = new Order[]
                               {
                                   new Order { Quantity = 25, IdProduct = 3, Shipped = true, Month = "November"},
                                   new Order { Quantity = 10, IdProduct = 1, Shipped = true, Month = "March"},
                                   new Order { Quantity = 5, IdProduct = 9, Shipped = false, Month = "August"}
                               }},
                new Customer { Name = "Frank", City = "Seattle", Country = Countries.USA,
                               Orders = new Order[]
                               {
                                   new Order { Quantity = 20, IdProduct = 5, Shipped = false, Month = "July"},
                                   new Order { Quantity = 15, IdProduct = 7, Shipped = false, Month = "September"}
                               }},
            };

        private static Product[] products = new Product[]
            {
                new Product { IdProduct = 1, Price = 10 },
                new Product { IdProduct = 2, Price = 20 },
                new Product { IdProduct = 3, Price = 30 },
                new Product { IdProduct = 4, Price = 40 },
                new Product { IdProduct = 5, Price = 50 },
                new Product { IdProduct = 6, Price = 50 }
            };

        private static void QueryOperators()
        {
            Console.WriteLine("Query Operators -----------------------------------------------------------");

            #region Where Operator
            var where1 =
                from c in customers
                where c.Country == Countries.Italy
                select new { c.Name, c.City };

            var where2 =
                customers
                .Where((c, index) => c.Country == Countries.Italy && index >= 1)
                .Select(c => c.Name);

            foreach (var item in where1)
                Console.WriteLine(item);

            foreach (var item in where2)
                Console.WriteLine(item);

            Console.WriteLine();
            #endregion

            #region SelectMany Operator
            var select1 =
                customers
                .Where(c => c.Country == Countries.Italy)
                .Select(c => c.Orders);

            var select2 =
                customers
                .Where(c => c.Country == Countries.Italy)
                .SelectMany(c => c.Orders);

            var select3 =
                from c in customers
                where c.Country == Countries.Italy
                    from o in c.Orders
                    select o;

            foreach (var item in select1)
                Console.WriteLine(item);

            foreach (var item in select2)
                Console.WriteLine(item);

            foreach (var item in select3)
                Console.WriteLine(item);

            Console.WriteLine();
            #endregion

            #region Ordering Operators
            var ordering1 =
                from c in customers
                where c.Country == Countries.Italy
                orderby c.Name descending
                select new { c.Name, c.City };

            var ordering2 =
                customers
                .Where(c => c.Country == Countries.Italy)
                .OrderByDescending(c => c.Name)
                .ThenBy(c => c.City)
                .Select(c => new { c.Name, c.City });

            var ordering3 =
                (from c in customers
                 where c.Country == Countries.Italy
                 orderby c.Name descending, c.City
                 select new { c.Name, c.City }).Reverse();

            foreach (var item in ordering1)
                Console.WriteLine(item);
            
            foreach (var item in ordering2)
                Console.WriteLine(item);
            
            foreach (var item in ordering3)
                Console.WriteLine(item);
            
            Console.WriteLine();
            #endregion

            #region Grouping Operators
            var group1 =
                from c in customers
                group c by c.Country;

            var group2 =
                customers
                .GroupBy(c => c.Country, c => c.Name);

            foreach (var group in group1)
            {
                Console.WriteLine(group.Key);

                foreach (var item in group)
                    Console.WriteLine(item);
            }

            foreach (var group in group2)
            {
                Console.WriteLine(group.Key);

                foreach (var item in group)
                    Console.WriteLine(item);
            }
            
            Console.WriteLine();
            #endregion

            #region Join Operators
            var join1 =
                customers
                .SelectMany(c => c.Orders)
                .Join(products,
                      o => o.IdProduct, p => p.IdProduct,
                      (o, p) => new { o.IdProduct, o.Month, o.Shipped, p.Price });

            var innerJoin =
                from c in customers
                    from o in c.Orders
                    join p in products on o.IdProduct equals p.IdProduct
                    select new { o.IdProduct, o.Month, o.Shipped, p.Price };

            var leftOuterJoin =
                from c in customers
                    from o in c.Orders
                    join p in products on o.IdProduct equals p.IdProduct into ordersProducts
                    let firstProduct = ordersProducts.FirstOrDefault()
                    let price = firstProduct != null ? firstProduct.Price : decimal.MinusOne
                    select new { o.IdProduct, o.Month, o.Shipped, o.Quantity, Price = price };

            var leftOuterCountSum =
                from p in products
                join o in (from c in customers
                               from o in c.Orders
                               select o) on p.IdProduct equals o.IdProduct into productsOrders
                select new { p.IdProduct, p.Price, 
                             OrderCount = productsOrders.Count(),
                             OrderQuantitySum = productsOrders.Sum(po => po.Quantity) };

            var join3 =
                from p in products
                join o in (from c in customers
                               from o in c.Orders
                               select o) on p.IdProduct equals o.IdProduct
                select new { p.IdProduct, p.Price, o.Month, o.Shipped, o.Quantity };

            var join4 =
                products
                .GroupJoin(customers.SelectMany(c => c.Orders),
                           p => p.IdProduct, o => o.IdProduct,
                           (p, orders) => new { p.IdProduct, Orders = orders });

            var join5 =
                from p in products
                join o in (from c in customers
                               from o in c.Orders
                               select o) on p.IdProduct equals o.IdProduct into orders
                select new { p.IdProduct, Orders = orders };

            foreach (var item in join1)
                Console.WriteLine(item);

            foreach (var item in innerJoin)
                Console.WriteLine(item);

            foreach (var item in innerJoin)
                Console.WriteLine(item);
            
            foreach (var item in leftOuterJoin)
                Console.WriteLine(item);

            foreach (var item in leftOuterCountSum)
                Console.WriteLine(item);

            foreach (var product in join4)
            {
                Console.WriteLine("Product {0}:", product.IdProduct);

                foreach (var order in product.Orders)
                    Console.WriteLine("    {0}", order);
            }

            foreach (var product in join5)
            {
                Console.WriteLine("Product {0}:", product.IdProduct);

                foreach (var order in product.Orders)
                    Console.WriteLine("    {0}", order);
            }
            
            Console.WriteLine();
            #endregion

            #region Set Operators
            var set1 =
                (from c in customers
                     from o in c.Orders
                     join p in products on o.IdProduct equals p.IdProduct
                     select p).Distinct();

            var set2 =
                (from c in customers
                     from o in c.Orders
                     select o)
                .Intersect(from c in customers
                           where c.Country == Countries.USA
                               from o in c.Orders
                               select o);

            var set3 =
                (from c in customers
                     from o in c.Orders
                     select o)
                .Union(from c in customers
                       where c.Country == Countries.USA
                           from o in c.Orders
                           select o);

            var set4 =
                (from c in customers
                     from o in c.Orders
                     select o)
                .Except(from c in customers
                        where c.Country == Countries.USA
                           from o in c.Orders
                           select o);

            foreach (var item in set1)
                Console.WriteLine(item);

            foreach (var item in set2)
                Console.WriteLine(item);

            foreach (var item in set3)
                Console.WriteLine(item);

            foreach (var item in set4)
                Console.WriteLine(item);

            Console.WriteLine();
            #endregion

            #region Aggregate Operators
            #endregion

            #region Generation Operators
            #endregion

            #region Quantifiers Operators
            #endregion

            #region Partitioning Operators
            #endregion

            #region Element Operators
            #endregion

            #region Other Operators
            #endregion

            #region Conversion Operators
            #endregion
        }
        #endregion

        #region LINQ To SQL
        private static void LinqToSql()
        {
            Console.WriteLine("LINQ To SQL ---------------------------------------------------------------");

            using (var dataContext = new NorthwindDataContext())
            {
                var sql1 =
                    from c in dataContext.Categories
                    select new { ID = c.CategoryID, Name = c.CategoryName, Descr = c.Description };

                var sql2 =
                    from p in dataContext.Products
                    select new { ID = p.ProductID, Name = p.ProductName, CatID = p.CategoryID, Suppl = p.SupplierID };

                var sql3 =
                    from p in dataContext.Products.OfType<Poultry>()
                    select p;

                var sql4 =
                    (from p in dataContext.Products
                     where p is Cereal
                     select p)
                    .Union(from p in dataContext.Products
                           where p is Beverage && p.Discontinued
                           select p);
                
                foreach (var item in sql1)
                    Console.WriteLine(item);
                
                foreach (var item in sql2)
                    Console.WriteLine(item);

                foreach (var item in sql3)
                    Console.WriteLine("{0} - {1} - {2}", item.ProductID, item.ProductName, item.Category.CategoryName);
                
                foreach (var item in sql4)
                    Console.WriteLine("{0} - {1} - {2}", item.ProductID, item.ProductName, item.Category.CategoryName);
            }

            Console.WriteLine();
        }
        #endregion

        #region LINQ To Entities
        private static void LinqToEntities()
        {
            var custQuery = "SELECT c FROM NorthwindContainer.CustomerSet AS c WHERE c.Country = 'Italy'";
            var custValQuery = "SELECT VALUE c FROM NorthwindContainer.CustomerSet AS c WHERE c.Country = 'Italy'";
            var northwindConnection = ConfigurationManager.ConnectionStrings["NorthwindContainer"].ConnectionString;

            Console.WriteLine("LINQ To Entities ----------------------------------------------------------");

            using (var conn = new EntityConnection(northwindConnection))
            using (var cmd = new EntityCommand(custQuery, conn))
            {
                conn.Open();

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    while (reader.Read())
                    {
                        var c = (DbDataRecord)reader.GetValue(0);
                        Console.WriteLine(string.Format("{0} - {1} - {2}", c[2], c[6], c[8]));
                    }
                }

                conn.Close();
            }

            using (var container = new NorthwindContainer())
            {
                var query = container.CreateQuery<NorthwindCustomer>(custValQuery);

                foreach (var item in query)
                    Console.WriteLine("{0} - {1} - {2}", item.CustomerID, item.CompanyName, item.Phone);

                var query2 =
                    from c in container.CustomerSet
                    where c.Country == "Italy"
                    select new { c.CustomerID, c.CompanyName, c.Address };

                foreach (var item in query2)
                    Console.WriteLine(item);

                var query3 =
                    (from o in container.OrderSet
                     where o.ShipCountry == "USA"
                     select o.Customer).Distinct();

                foreach (var item in query3)
                    Console.WriteLine("{0} - {1} - {2}", item.CustomerID, item.CompanyName, item.Region);
            }

            using (var container = new NorthwindContainer())
            {
                var customer =
                    (from c in container.CustomerSet
                     where c.CustomerID == "ALFKI"
                     select c).First();

                var customer2 =
                    (from c in container.CustomerSet
                     where c.CustomerID == "ALFKI"
                     select c).First();
                
                var originalContact = customer.ContactName;

                Console.WriteLine("{0} - {1}", customer.CustomerID, customer.GetHashCode());
                Console.WriteLine("{0} - {1}", customer2.CustomerID, customer2.GetHashCode());
                Console.WriteLine("{0} - {1}", customer2.CustomerID, customer.Equals(customer2));

                Console.WriteLine("{0} - {1} - {2}", customer.CustomerID, customer.ContactName, customer.EntityState);
                customer.ContactName = "Michael Petersen - Changed";
                Console.WriteLine("{0} - {1} - {2}", customer.CustomerID, customer.ContactName, customer.EntityState);
                container.SaveChanges();
                Console.WriteLine("{0} - {1} - {2}", customer.CustomerID, customer.ContactName, customer.EntityState);

                customer.ContactName = originalContact;
                Console.WriteLine("{0} - {1} - {2}", customer.CustomerID, customer.ContactName, customer.EntityState);
                container.SaveChanges(false);
                Console.WriteLine("{0} - {1} - {2}", customer.CustomerID, customer.ContactName, customer.EntityState);
                container.AcceptAllChanges();
                Console.WriteLine("{0} - {1} - {2}", customer.CustomerID, customer.ContactName, customer.EntityState);
            }

            using (var scope = new TransactionScope())
            using (var container = new NorthwindContainer())
            {
                Transaction.Current.TransactionCompleted += 
                    (s, e) => { Console.WriteLine(e.Transaction.TransactionInformation.Status); };

                var customer =
                    (from c in container.CustomerSet
                     where c.CustomerID == "ALFKI"
                     select c).First();
                
                var originalContact = customer.ContactName;
                customer.ContactName = "Michael Petersen - Changed";
                customer.ContactName = originalContact;
                Console.WriteLine("{0} - {1} - {2}", customer.CustomerID, customer.ContactName, customer.EntityState);

                container.SaveChanges();
                scope.Complete();
            }

            Console.WriteLine();
        }
        #endregion

        private static void Main(string[] args)
        {
            Generics();
            DelegatesAndAnonymousMethods();
            EnumeratorsAndYield();
            QueryExpressions();
            QueryOperators();
            LinqToSql();
            LinqToEntities();

            Console.ReadKey();
        }
    }
}
