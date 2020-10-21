using MathNet.Numerics.LinearAlgebra;
using ModernInfoTheory.Hamming.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ModernInfoTheory.Hamming.ViewModels
{
    public class MainViewModel : Observable
    {
        private Matrix<double> _hMatrix;
        private Matrix<double> _gMatrix;

        private int _wordLength;
        private string _message;
        private string _checkMatrix;
        private string _generateMatrix;
        private string _encodedMessage;

        public int WordLength { get => _wordLength; set => Set(ref _wordLength, value); }
        public string Message { get => _message; set => Set(ref _message, value); }
        public string CheckMatrix { get => _checkMatrix; private set => Set(ref _checkMatrix, value); }
        public string GenerateMatrix { get => _generateMatrix; private set => Set(ref _generateMatrix, value); }
        public string EncodedMessage { get => _encodedMessage; set => Set(ref _encodedMessage, value); }

        public MainViewModel()
        {
            Message = string.Empty;
        }

        public void CreateMatrices()
        {
            var r = (int)Math.Ceiling(Math.Log(WordLength + 1, 2));

            var aMatrix = new List<List<double>>();
            var iMatrix = new List<List<double>>();

            for (int i = 0; i < r; i++)
            {
                aMatrix.Add(new List<double>());
                iMatrix.Add(new List<double>());
            }
            for (int i = 1; i < Math.Pow(2, r); i++)
            {
                if (Math.Log(i, 2) != (int)Math.Log(i, 2))
                {
                    var iBinary = Convert.ToString(i, 2).PadLeft(r, '0');
                    for (int j = 0; j < r; j++)
                    {
                        aMatrix[j].Add(Convert.ToDouble(iBinary[r - j - 1].ToString()));
                    }
                }
                if (aMatrix[0].Count == WordLength - r)
                {
                    break;
                }
            }
            for (int i = 1; i < Math.Pow(2, r); i++)
            {
                if (Math.Log(i, 2) == (int)Math.Log(i, 2))
                {
                    var iBinary = Convert.ToString(i, 2).PadLeft(r, '0');
                    for (int j = 0; j < r; j++)
                    {
                        iMatrix[j].Add(Convert.ToDouble(iBinary[r - j - 1].ToString()));
                    }
                }
            }

            _hMatrix = Matrix<double>.Build.DenseOfRows(aMatrix).Append(Matrix<double>.Build.DenseOfRows(iMatrix));
            CheckMatrix = _hMatrix.ToMatrixString(100, 100);

            _gMatrix = Matrix<double>.Build.DenseIdentity(aMatrix[0].Count).Append(Matrix<double>.Build.DenseOfRows(aMatrix).Transpose());
            GenerateMatrix = _gMatrix.ToMatrixString(100, 100);
        }

        public void EncodeMessage()
        {
            CreateMatrices();
            var r = (int)Math.Ceiling(Math.Log(WordLength + 1, 2));
            EncodedMessage = string.Empty;

            var words = Message.Split(" ");
            foreach (var word in words)
            {
                var bits = new List<double>();
                foreach (var letter in word)
                {
                    bits.Add(Convert.ToDouble(letter.ToString()));
                }
                var matrix = Matrix<double>.Build.DenseOfRowMajor(1, WordLength - r, bits);
                var encodedMatrix = matrix * _gMatrix;

                encodedMatrix = encodedMatrix.Map(n => (double)((int)n % 2));

                EncodedMessage += $"{encodedMatrix.ToMatrixString(100, 100).Replace(" ", "").Replace(Environment.NewLine, string.Empty)} ";
            }
        }

        public string CheckIfCorrect()
        {
            CreateMatrices();
            var r = (int)Math.Ceiling(Math.Log(WordLength + 1, 2));

            var words = Message.Split(" ");
            var wrongWords = new List<string>();
            foreach (var word in words)
            {
                var bits = new List<double>();
                foreach (var letter in word)
                {
                    bits.Add(Convert.ToDouble(letter.ToString()));
                }
                var matrix = Matrix<double>.Build.DenseOfRowMajor(1, WordLength, bits);
                var checkMatrix = matrix * _hMatrix.Transpose();

                checkMatrix = checkMatrix.Map(n => (double)((int)n % 2));

                if (checkMatrix.RowSums().Sum() != 0)
                {
                    wrongWords.Add(word);
                }
            }

            if (wrongWords.Any())
            {
                var result = "Список некорректных слов:";
                foreach (var wrongWord in wrongWords)
                {
                    result += $" {wrongWord}";
                }
                return result;
            }
            else
            {
                return "Все слова корректны.";
            }
        }
    }
}
