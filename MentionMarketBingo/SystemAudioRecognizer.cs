using System;
using System.Globalization;
using System.Speech.Recognition;

namespace MentionMarketBingo
{
    public class SystemAudioRecognizer : IDisposable
    {
        private SpeechRecognitionEngine? _recognizer;
        private bool _isListening;

        public event EventHandler<System.Speech.Recognition.SpeechRecognizedEventArgs> SpeechRecognized;
        public event EventHandler<string> ErrorOccurred;

        public void StartListening()
        {
            try
            {
                if (_isListening) return;

                // Initialize speech recognizer with microphone input
                _recognizer = new SpeechRecognitionEngine(new CultureInfo("en-US"));
                _recognizer.SetInputToDefaultAudioDevice();
                _recognizer.LoadGrammar(new DictationGrammar());
                _recognizer.SpeechRecognized += OnSpeechRecognized;
                _recognizer.SpeechRecognitionRejected += OnRecognitionRejected;

                _recognizer.RecognizeAsync(RecognizeMode.Multiple);
                _isListening = true;

                ErrorOccurred?.Invoke(this, "Speech recognition started using microphone");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Start failed: {ex.Message}");
            }
        }

        public void StopListening()
        {
            if (!_isListening) return;

            try
            {
                _recognizer?.RecognizeAsyncCancel();
                _isListening = false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Stop failed: {ex.Message}");
            }
        }

        private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence > 0.1f)
            {
                SpeechRecognized?.Invoke(this, e);
            }
            else
            {
                ErrorOccurred?.Invoke(this, $"Speech recognized but confidence too low: {e.Result.Confidence:P1}");
            }
        }

        private void OnRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            // Optional: Log rejection if needed for debugging
        }

        public void Dispose()
        {
            StopListening();
            _recognizer?.Dispose();
        }
    }
}