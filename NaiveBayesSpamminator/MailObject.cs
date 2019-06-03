using System;

namespace NaiveBayesSpamminator
{
    public class MailObject
    {
        public string Text { get; set; }
        public bool IsSpam { get; set; }

        public MailObject() { }

        public MailObject(string text) : this(text, "\n") { }

        public MailObject(string text, string separator)
        {
            var separatedText = text.Split(separator);

            Text = separatedText[0];
            IsSpam = Convert.ToBoolean(separatedText[1]);
        }
    }
}
