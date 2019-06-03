using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NaiveBayesSpamminator
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var fileNames = Directory.GetFiles($"{Directory.GetCurrentDirectory()}/TrainingData", "*.txt");

            var mails = new List<MailObject>();
            foreach (var fileName in fileNames)
            {
                var fileText = await File.ReadAllTextAsync(fileName, Encoding.UTF8);

                mails.Add(new MailObject(fileText, ";"));
            }

            var bayes = new NaiveBayesClassifier();

            bayes.Learn(mails);

            var result = bayes.Classify("Email");

            foreach (var item in result)
            {
                Console.WriteLine($"{item.Key} -- {item.Value}");
            };
            Console.ReadLine();
        }
    }
}
