using System;
using System.Globalization;
using System.Speech.Recognition;
using NAudio.Wave;
using System.Threading;

namespace MentionMarketBingo
{
    public class SystemAudioRecognizer : IDisposable
    {
        private WasapiLoopbackCapture? _capture;
        private SpeechRecognitionEngine? _recognizer;
        private bool _isListening;
        private MemoryStream? _audioStream;
        private bool _isInitialized = false;

        public event EventHandler<System.Speech.Recognition.SpeechRecognizedEventArgs> SpeechRecognized;
        public event EventHandler<string> ErrorOccurred;

        public void StartListening()
        {
            try
            {
                if (_isListening) return;

                // Initialize audio stream
                _audioStream = new MemoryStream();

                // Initialize system audio capture
                _capture = new WasapiLoopbackCapture();
                _capture.DataAvailable += OnAudioDataAvailable;
                _capture.RecordingStopped += OnRecordingStopped;

                // Initialize speech recognizer
                _recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
                _recognizer.SetInputToWaveStream(_audioStream);
                _recognizer.LoadGrammar(new DictationGrammar());
                _recognizer.SpeechRecognized += OnSpeechRecognized;
                _recognizer.SpeechRecognitionRejected += OnRecognitionRejected;

                _capture.StartRecording();
                _recognizer.RecognizeAsync(RecognizeMode.Multiple);
                _isListening = true;
                _isInitialized = true;

                ErrorOccurred?.Invoke(this, "Audio capture and speech recognition started");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Start failed: {ex.Message}");
            }
        }

        private void OnAudioDataAvailable(object sender, WaveInEventArgs e)
        {
            if (_audioStream != null && _isInitialized)
            {
                try
                {
                    // Write audio data directly to the stream
                    _audioStream.Write(e.Buffer, 0, e.BytesRecorded);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, $"Error writing audio data: {ex.Message}");
                }
            }
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                ErrorOccurred?.Invoke(this, $"Audio capture stopped due to error: {e.Exception.Message}");
            }
            _isListening = false;
        }

        public void StopListening()
        {
            if (!_isListening) return;

            try
            {
                _recognizer?.RecognizeAsyncCancel();
                if (_capture != null)
                {
                    _capture.StopRecording();
                    _capture.Dispose();
                    _capture = null;
                }
                if (_audioStream != null)
                {
                    _audioStream.Dispose();
                    _audioStream = null;
                }
                _isListening = false;
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Stop failed: {ex.Message}");
            }
        }

        private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence > 0.3f)
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
            ErrorOccurred?.Invoke(this, $"Recognition rejected (no speech detected or unclear)");
        }

        public void Dispose()
        {
            StopListening();
            _recognizer?.Dispose();
        }
    }

}