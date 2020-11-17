using ModernInfoTheory.Hamming.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ModernInfoTheory.VarshamovTenengolts.ViewModels
{
    public class MainViewModel : Observable
    {
        private int _wordLength;
        private string _message;
        private string _fixedMessage;
        private string _possibleWords;

        public int WordLength { get => _wordLength; set => Set(ref _wordLength, value); }
        public string Message { get => _message; set => Set(ref _message, value); }
        public string FixedMessage { get => _fixedMessage; set => Set(ref _fixedMessage, value); }
        public string PossibleWords { get => _possibleWords; set => Set(ref _possibleWords, value); }

        public MainViewModel()
        {
            Message = string.Empty;
            PossibleWords = string.Empty;
        }

        private List<int> FixWord(List<int> errorBits)
        {
            var weight = 0;
            var s = 0;
            int errorBit;
            int n;
            var fixedBits = errorBits.Select(b => b).ToList();

            for (int i = 0; i < errorBits.Count; i++)
            {
                s += errorBits[i] * (i + 1);
                weight += errorBits[i];
            }

            if (errorBits.Count < WordLength)
            {
                var t = ((WordLength + 1) - s % (WordLength + 1)) % (WordLength + 1);

                if (weight >= t)
                {
                    errorBit = 0;
                    n = t;
                }
                else
                {
                    errorBit = 1;
                    n = WordLength - t;
                }

                var count = 0;
                for (int i = errorBits.Count - 1; i >= 0; i--)
                {
                    if (errorBits[i] != errorBit)
                    {
                        count++;
                    }
                    if (count == n)
                    {
                        fixedBits.Insert(i, errorBit);
                        break;
                    }
                    if (i == 0)
                    {
                        throw new Exception();
                    }
                }
            }
            else
            {
                var t = s % (WordLength + 1);

                if (t == 0)
                {
                    fixedBits.RemoveAt(fixedBits.Count - 1);
                }
                else if (t == weight)
                {
                    fixedBits.RemoveAt(0);
                }
                else
                {
                    if (t < weight)
                    {
                        errorBit = 0;
                        n = t;
                    }
                    else
                    {
                        errorBit = 1;
                        n = WordLength + 1 - t;
                    }

                    var count = 0;
                    for (int i = errorBits.Count - 1; i >= 0; i--)
                    {
                        if (errorBits[i] != errorBit)
                        {
                            count++;
                        }
                        if (count == n)
                        {
                            if (i == 0 || errorBits[i - 1] != errorBit)
                            {
                                throw new Exception();
                            }
                            fixedBits.RemoveAt(i - 1);
                            break;
                        }
                        if (i == 0)
                        {
                            throw new Exception();
                        }
                    }
                }
            }

            var sTest = 0;
            for (int i = 0; i < fixedBits.Count; i++)
            {
                sTest += fixedBits[i] * (i + 1);
            }
            if (sTest % (WordLength + 1) != 0)
            {
                throw new Exception();
            }

            return fixedBits;
        }

        public List<string> FixErrors()
        {
            var words = Message.Split(" ");
            var fixedMessage = string.Empty;
            var wrongWords = new List<string>();

            foreach (var word in words)
            {
                if (word.Count() == WordLength)
                {
                    fixedMessage += $"{word} ";
                    continue;
                }

                var bits = new List<int>();
                foreach (var letter in word)
                {
                    bits.Add(Convert.ToInt32(letter.ToString()));
                }

                try
                {
                    var fixedBits = FixWord(bits);
                    var fixedWord = string.Empty;
                    foreach (var bit in fixedBits)
                    {
                        fixedWord += bit.ToString();
                    }
                    fixedMessage += $"{fixedWord} ";
                }
                catch (Exception)
                {
                    wrongWords.Add(word);
                }
            }

            FixedMessage = fixedMessage;
            return wrongWords;
        }

        public void GenerateMessage()
        {
            var message = string.Empty;
            for (int i = 0; i < Math.Pow(2, WordLength - 1); i++)
            {
                var word = Convert.ToString(i, 2).PadLeft(WordLength - 1, '0');
                message += $"{word} ";
            }
            for (int i = 0; i < Math.Pow(2, WordLength + 1); i++)
            {
                var word = Convert.ToString(i, 2).PadLeft(WordLength + 1, '0');
                if (i == Math.Pow(2, WordLength + 1) - 1)
                {
                    message += word;
                    break;
                }
                message += $"{word} ";
            }

            Message = message;
        }

        public void GeneratePossibleWords()
        {
            var possibleWords = string.Empty;
            for (int i = 0; i < Math.Pow(2, WordLength); i++)
            {
                var word = Convert.ToString(i, 2).PadLeft(WordLength, '0');
                var bits = new List<int>();
                foreach (var letter in word)
                {
                    bits.Add(Convert.ToInt32(letter.ToString()));
                }
                var sTest = 0;
                for (int j = 0; j < bits.Count; j++)
                {
                    sTest += bits[j] * (j + 1);
                }
                if (sTest % (WordLength + 1) == 0)
                {
                    possibleWords += $"{word} ";
                }
            }

            PossibleWords = possibleWords;
        }
    }
}
