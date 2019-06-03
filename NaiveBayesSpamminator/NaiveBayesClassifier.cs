using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveBayesSpamminator
{
    public class NaiveBayesClassifier
    {
        private static Dictionary<string, Dictionary<bool, double>> vocabularyProbability = new Dictionary<string, Dictionary<bool, double>>();

        private static readonly Dictionary<bool, double> mailProbability = new Dictionary<bool, double>();

        private static IList<bool> hypotesis = new List<bool>() { true, false };

        public void Learn(IList<MailObject> mails) => Learn(mails, new List<string> { });

        public void Learn(IList<MailObject> mails, IList<string> stopWords)
        {
            vocabularyProbability = mails.SelectMany(mail => mail.Text.Split(" "))
                                         .Distinct()
                                         .Where(word => word != string.Empty)
                                         .Where(word => !stopWords.Contains(word))
                                         .ToDictionary(d => d, d => new Dictionary<bool, double>());

            foreach (var hypotesi in hypotesis)
            {
                var docs = mails.Where(d => d.IsSpam == hypotesi);

                mailProbability.Add(hypotesi, (docs.Count() + 0.0) / mails.Count());

                var builder = new StringBuilder();
                foreach (var doc in docs) builder.Append(doc.Text + " ");

                var textWords = builder.ToString().Split(" ").Where(d => d != string.Empty);

                var totalWordsInText = textWords.Count();

                foreach (var wordInVocabulary in vocabularyProbability)
                {
                    var totalOfCasesInTheText = textWords.Count(d => d == wordInVocabulary.Key);

                    var element = vocabularyProbability.FirstOrDefault(d => d.Key == wordInVocabulary.Key);
                    element.Value.Add(hypotesi, (totalOfCasesInTheText + 0.0) / (totalWordsInText + vocabularyProbability.Count()));
                }
            }
        }

        public Dictionary<bool, double> Classify(string email)
        {
            var wordsInVocabulary = email.Split(" ").Where(text => vocabularyProbability.ContainsKey(text));

            var probability = new Dictionary<bool, double>();
            foreach (var hypotesi in hypotesis)
            {
                probability.Add(hypotesi, mailProbability.GetValueOrDefault(hypotesi) * ProbabilityProdutory(wordsInVocabulary, hypotesi));
            }

            return probability;
        }

        private double ProbabilityProdutory(IEnumerable<string> words, bool isSpam)
        {
            double prod = 1.0;
            foreach (var word in words)
                prod *= vocabularyProbability.GetValueOrDefault(word).GetValueOrDefault(isSpam);

            return prod;
        }

    }
}
