using ResNetMatcher;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

// DataView + Data
namespace Task2 {
    public class DataView : INotifyPropertyChanged {

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool isMatching;
        public bool IsMatching {
            get {
                return isMatching;
            }
            set {
                isMatching = value;
                IsChooseEnabled = !isMatching;
                IsMatchEnabled = !isMatching;
                OnPropertyChanged("IsMatching");
            }
        }

        private bool isMatchEnabled;
        public bool IsMatchEnabled {
            get {
                return isMatchEnabled;
            }
            set {
                isMatchEnabled = value;
                OnPropertyChanged("IsMatchEnabled");
            }
        }

        public bool IsDirSelected {
            get {
                return dirName != null;
            }
            set { }
        }

        private string dirName = null;
        public string DirName {
            get {
                return dirName;
            }
            set {
                dirName = value;
                IsMatchEnabled = true;
                OnPropertyChanged("DirName");
                OnPropertyChanged("IsDirSelected");
                matched.Clear();
                OnPropertyChanged("ClassList");
                OnPropertyChanged("ClassMatchResult");
            }
        }

        private int selectedClassId;
        public int SelectedClassId {
            get {
                return selectedClassId;
            }
            set {
                selectedClassId = value;
                OnPropertyChanged("SelectedClassId");
                OnPropertyChanged("ClassMatchResult");
            }
        }

        private bool isChooseEnabled = true;
        public bool IsChooseEnabled {
            get {
                return isChooseEnabled;
            }
            set {
                isChooseEnabled = value;
                OnPropertyChanged("IsChooseEnabled");
            }
        }

        public class MatchResult {
            public string imagePath;
            public int classId;
            public string className;
        }

        private List<MatchResult> matched = new List<MatchResult>();

        public IEnumerable ClassList {
            // get => from match in matched group match by match.classId into g select new { Count = g.Count(), ClassName = g.First().className };
            get {
                var count = from match in matched group match by match.classId into g select new { Count = g.Count(), ClassName = g.First().className };
                return from label in Labels.classLabels select new { ClassName = label, Count = (from c in count where c.ClassName == label select c.Count).FirstOrDefault() };
            }
            // get => from match, label in matched, Labels.classLabels group match by match.classId into g select new { Count = g.Count(), ClassName = g.First().className };
        }

        public int classAtList(int i) {
            return SelectedClassId == -1 ? -1 : (from match in matched group match by match.classId into g select new { classId = g.First().classId }).ElementAt(i).classId;
        }

        public IEnumerable ClassMatchResult {
            get {
                return from match in matched where match.classId == selectedClassId select new { ClassName = match.className, Image = match.imagePath };
            }
            set { }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void matchHandler(string fileName, int classId, float match) {
            matched.Add(new MatchResult() { imagePath = fileName, classId = classId, className = Labels.classLabels[classId] });

            OnPropertyChanged("ClassList");
            OnPropertyChanged("ClassMatchResult");
        }

        Matcher matcher = new Matcher();

        internal void StartMatching() {
            // matchHandler("C:\\Users\\bitrate16\\Pictures\\Screenshots\\Screenshot (1).png", new Random().Next(0, Labels.classLabels.Length), 0.15131f);
            matcher.Match(matchHandler, dirName);
            IsMatching = true;
        }

        internal void StopMatching() {
            matcher.CancelMatch();
            IsMatching = false;
        }

        public DataView() {

        }
    }
}
