using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;

namespace TrainerTextsWPF
{
    public enum NameLevel
    {
        Newcomer,
        Apprentice,
        Amateur,
        Advanced,
        Expert,
        Master,
        Virtuoso,
        Guru,
        Professional,
    }
    public abstract class GraphOperations
    {
        protected abstract string GetPath();
        protected abstract void Serialize();
        protected abstract object Deserialize();
        protected abstract void CreateFile();
    }
    public sealed class GraphLevel : GraphOperations
    {
        private InfoLevel _infoLevel = new InfoLevel();
        private string PathFile { get; set; }
        private readonly string _fileName = "level.json";
        private readonly Dictionary<int, NameLevel> infoLevels = new()
        {
            {1000, NameLevel.Newcomer},
            {5000, NameLevel.Apprentice},
            {20000, NameLevel.Amateur},
            {50000, NameLevel.Advanced},
            {100000, NameLevel.Expert},
            {200000, NameLevel.Master},
            {350000, NameLevel.Virtuoso},
            {500000, NameLevel.Guru},
            {1000000, NameLevel.Professional}
        };

        public GraphLevel()
        {
            PathFile = GetPath();
            CreateFile();
            _infoLevel = (InfoLevel)Deserialize();
        }

        protected override string GetPath() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TrainerTexts", _fileName);

        protected override void CreateFile()
        {
            if (File.Exists(PathFile))
                return;

            using (File.Create(PathFile)) { };
            _infoLevel.CountLetters = 0;
            _infoLevel.Level = NameLevel.Newcomer;
            _infoLevel.MaximumLenghtProgressBar = 1000;
            Serialize();
        }

        protected override void Serialize()
        {
            string result = JsonSerializer.Serialize(_infoLevel);
            using StreamWriter streamwriter = new StreamWriter(PathFile);
            streamwriter.Write(result);
            streamwriter.Close();
        }

        protected override object Deserialize()
        {
            InfoLevel? info = new InfoLevel();
            string json;
            using (StreamReader streamReader = new StreamReader(PathFile))
            {
                json = streamReader.ReadToEnd();
                streamReader.Close();
            }
            if (!string.IsNullOrEmpty(json))
            {
                info = JsonSerializer.Deserialize<InfoLevel>(json);

                if (info is null)
                    throw new Exception($"Error deserialize in class {typeof(InfoLevel)}");
            }
            return info;
        }

        public ProgressBar UpdateGraph(ProgressBar progressBar, ref TextBlock textBox, int countletters = 0)
        {
            if (_infoLevel.Level == NameLevel.Professional)
                return progressBar;

            _infoLevel.CountLetters += countletters;
            if (_infoLevel.MaximumLenghtProgressBar < _infoLevel.CountLetters)
            {
                var enumerator = infoLevels.GetEnumerator();
                enumerator.MoveNext();
                foreach (var kvp in infoLevels)
                {
                    enumerator.MoveNext();
                    if (kvp.Value == _infoLevel.Level && _infoLevel.MaximumLenghtProgressBar < _infoLevel.CountLetters)
                    {
                        _infoLevel.MaximumLenghtProgressBar = enumerator.Current.Key;
                        _infoLevel.Level = enumerator.Current.Value;
                        _infoLevel.CountLetters = 0;
                        break;
                    }
                }
            }
            UpdateProgressBar(progressBar, ref textBox);
            Serialize();
            return progressBar;
        }

        private ProgressBar UpdateProgressBar(ProgressBar progressBar, ref TextBlock textBox)
        {
            progressBar.Maximum = _infoLevel.MaximumLenghtProgressBar;
            progressBar.Value = _infoLevel.CountLetters;
            textBox.Text = $"Your level: {_infoLevel.Level.ToString()}";
            return progressBar;
        }
    }
    [Serializable]
    public class InfoLevel
    {
        public NameLevel Level { get; set; }
        public int CountLetters { get; set; }
        public int MaximumLenghtProgressBar
        {
            get; set;
        }
    }
}