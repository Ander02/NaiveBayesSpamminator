using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NaiveBayesSpamminator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var fileNames = Directory.GetFiles($"{Directory.GetCurrentDirectory()}/TrainingData", "*.csv");
            var random = new Random(42);

            var mails = new List<MailObject>();
            foreach (var fileName in fileNames)
            {
                var csvLines = File.ReadAllLines(fileName, Encoding.UTF8)
                                   .Skip(1)
                                   .OrderBy(d => random.Next())
                                   .ToList();

                foreach (var line in csvLines.Take(100).ToList())
                {
                    var item = line.Split("\",");

                    mails.Add(new MailObject
                    {
                        Text = item[0],
                        IsSpam = item[1] == "1"
                    });
                }
            }
            var spamCount = mails.Where(d => d.IsSpam).Count();
            var notSpamCount = mails.Where(d => !d.IsSpam).Count();

            Console.WriteLine("Spams: " + spamCount);
            Console.WriteLine("Não Spams: " + notSpamCount);

            var trainingDataCount = (int)(mails.Count() * 0.8);
            var trainingData = mails.Take(trainingDataCount).ToList();
            var validationDataCount = mails.Count() - trainingDataCount;
            var validationData = mails.TakeLast(validationDataCount).ToList();

            var withoutStopWordsBayes = new NaiveBayesClassifier();
            Console.WriteLine("Sem Stop Words");
            Analyze(withoutStopWordsBayes, trainingData, validationData, new List<string> { });
            Console.WriteLine();
            Console.WriteLine("Com Stop Words");
            var withStopWordsBayes = new NaiveBayesClassifier();

            var stopWords = trainingData.Where(d => d.IsSpam)
                                        .SelectMany(d => d.Text.Split(" "))
                                        .GroupBy(d => d)
                                        .Select(d => new
                                        {
                                            Word = d.Key,
                                            Count = d.Count()
                                        })
                                        .Where(d => d.Count >= 150)
                                        .OrderByDescending(d => d.Count)
                                        .ToList();

            var stringBuilder = new StringBuilder();
            foreach (var word in stopWords)
                stringBuilder.AppendLine($"{word.Word}=={word.Count}");

            File.WriteAllBytes($"{Directory.GetCurrentDirectory()}/Data/stopWords.csv", Encoding.UTF8.GetBytes(stringBuilder.ToString()));

            Analyze(withStopWordsBayes, trainingData, validationData, stopWords.Select(d => d.Word).ToList());
            Console.ReadLine();
        }

        private static void Analyze(NaiveBayesClassifier bayes, List<MailObject> trainingData, List<MailObject> validationData, IList<string> stopWords)
        {
            bayes.Learn(trainingData, stopWords);

            var success = 0;
            var fail = 0;
            foreach (var mail in validationData)
            {
                var result = bayes.Classify(mail.Text);
                if (result[mail.IsSpam] >= 0.5)
                    success++;
                else
                    fail++;
            }

            Console.WriteLine("Acertos: " + success);
            Console.WriteLine("Erros: " + fail);
            Console.WriteLine("Total: " + (success + fail));
        }
    }
}
