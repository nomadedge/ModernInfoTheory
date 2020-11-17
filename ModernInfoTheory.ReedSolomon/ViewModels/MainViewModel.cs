using ModernInfoTheory.Hamming.Common;
using ModernInfoTheory.ReedSolomon.GF;
using ModernInfoTheory.ReedSolomon.RS;
using System;
using System.Collections.Generic;

namespace ModernInfoTheory.VarshamovTenengolts.ViewModels
{
    public class MainViewModel : Observable
    {
        private int _maxErrorsCount;
        private string _message;
        private string _encodedMessage;
        private string _decodedMessage;
        private GenericGF _field;
        private ReedSolomonEncoder _rse;
        private ReedSolomonDecoder _rsd;

        public int MaxErrorsCount { get => _maxErrorsCount; set => Set(ref _maxErrorsCount, value); }
        public string Message { get => _message; set => Set(ref _message, value); }
        public string EncodedMessage { get => _encodedMessage; set => Set(ref _encodedMessage, value); }
        public string DecodedMessage { get => _decodedMessage; set => Set(ref _decodedMessage, value); }

        public MainViewModel()
        {
            Message = string.Empty;
            EncodedMessage = string.Empty;
            DecodedMessage = string.Empty;
            _field = new GenericGF(285, 256, 0);// x^8 + x^4 + x^3 + x^2 + 1
            _rse = new ReedSolomonEncoder(_field);
            _rsd = new ReedSolomonDecoder(_field);
        }

        public void EncodeMessage()
        {
            var encodedMessage = string.Empty;

            var intMessage = new List<int>();
            foreach (var character in Message)
            {
                intMessage.Add(Convert.ToInt32(character));
            }
            for (int i = 0; i < MaxErrorsCount * 2; i++)
            {
                intMessage.Add(0);
            }
            var extendedMessage = intMessage.ToArray();
            _rse.Encode(extendedMessage, MaxErrorsCount * 2);
            foreach (var element in extendedMessage)
            {
                encodedMessage += ((char)element).ToString();
            }
            encodedMessage += Environment.NewLine;
            foreach (var element in extendedMessage)
            {
                encodedMessage += $"{element.ToString("x2")} ";
            }
            EncodedMessage = encodedMessage;
        }

        public void DecodeMessage()
        {
            var decodedMessage = string.Empty;

            var intMessage = new List<int>();
            foreach (var character in Message)
            {
                intMessage.Add(Convert.ToInt32(character));
            }
            var extendedMessage = intMessage.ToArray();

            if (_rsd.Decode(extendedMessage, MaxErrorsCount * 2))
            {
                foreach (var element in extendedMessage)
                {
                    decodedMessage += ((char)element).ToString();
                }
                decodedMessage += Environment.NewLine;
                foreach (var element in extendedMessage)
                {
                    decodedMessage += $"{element.ToString("x2")} ";
                }
                DecodedMessage = decodedMessage;
            }
            else
            {
                DecodedMessage = "Слишком много ошибок";
            }
        }
    }
}
