using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LinqFeatures
{
    internal class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }

        public override string ToString()
        {
            return string.Format("{0} - {1}", Name, Age);
        }
    }

    internal static class LinqToCSV
    {
        public static IEnumerable<string> GetCsvSource(string path)
        {
            using (var sr = new StreamReader(path))
            {
                while (!sr.EndOfStream)
                    yield return sr.ReadLine();
            }
        }
    }

    internal class Program
    {
        private const string PATH_AGE = @"..\..\LINQ Sources\AgeTranslation.txt";
        private const string PATH_FAMILY = @"..\..\LINQ Sources\FamilyMembers.txt";

        private static void Main(string[] args)
        {
            var familyMembers =
                from line in LinqToCSV.GetCsvSource(PATH_FAMILY)
                let columns = line.Split(',')
                select new Person { Name = columns[0], Age = int.Parse(columns[1]) };

            var familyMemberGroups =
                from line in LinqToCSV.GetCsvSource(PATH_FAMILY)
                let columns = line.Split(',')
                group line by int.Parse(columns[1]);

            var ageTranslation =
                from line in LinqToCSV.GetCsvSource(PATH_AGE)
                select line;

            foreach (var item in familyMembers)
                Console.WriteLine(item);
            
            foreach (var item in ageTranslation)
                Console.WriteLine(item);
        }
    }
}
