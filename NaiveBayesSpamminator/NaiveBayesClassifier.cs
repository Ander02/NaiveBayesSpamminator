using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NaiveBayesSpamminator
{
    public class NaiveBayesClassifier
    {
        private Dictionary<string, Dictionary<bool, double>> vocabularyProbability = new Dictionary<string, Dictionary<bool, double>>();

        private Dictionary<bool, double> mailProbability = new Dictionary<bool, double>();

        private readonly IList<bool> hypotesis = new List<bool>() { true, false };

        public void Learn(string jsonMailProbability, string jsonVocabularyProbability)
        {
            vocabularyProbability = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<bool, double>>>(jsonVocabularyProbability);
            mailProbability = JsonConvert.DeserializeObject<Dictionary<bool, double>>(jsonMailProbability);
        }

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

            //Normalize
            foreach (var vocabularyWord in vocabularyProbability)
                Normalize(vocabularyWord.Value);
            Normalize(mailProbability);

            var jsonVocabularyProbability = JsonConvert.SerializeObject(vocabularyProbability);
            var jsonEmailProbability = JsonConvert.SerializeObject(mailProbability);

            //            File.Create($"{Directory.GetCurrentDirectory()}/Data/mail.json");
            File.WriteAllBytes($"{Directory.GetCurrentDirectory()}/Data/mail.json", Encoding.UTF8.GetBytes(jsonEmailProbability));
            //            File.Create($"{Directory.GetCurrentDirectory()}/Data/vocabulary.json");
            File.WriteAllBytes($"{Directory.GetCurrentDirectory()}/Data/vocabulary.json", Encoding.UTF8.GetBytes(jsonVocabularyProbability));
        }

        private void Normalize(Dictionary<bool, double> dict)
        {
            var total = (dict[true] + dict[false]);

            dict[true] = dict[true] / total;
            dict[false] = dict[false] / total;
        }

        public Dictionary<bool, double> Classify(string email)
        {
            var wordsInVocabulary = email.Split(" ").Where(text => vocabularyProbability.ContainsKey(text));

            var probability = new Dictionary<bool, double>();
            foreach (var hypotesi in hypotesis)
                probability.Add(hypotesi, mailProbability.GetValueOrDefault(hypotesi, 1) * ProbabilityProdutory(wordsInVocabulary, hypotesi));

            Normalize(probability);
            return probability;
        }

        private double ProbabilityProdutory(IEnumerable<string> words, bool isSpam)
        {
            double prod = 1.0;
            foreach (var word in words)
            {
                var multiplier = vocabularyProbability.GetValueOrDefault(word, new Dictionary<bool, double>
                {
                    { isSpam, 1 }
                }).GetValueOrDefault(isSpam, 1);
                if (multiplier == 0)
                    multiplier = 1;
                prod *= multiplier;
            }
            return prod;
        }
    }
}
