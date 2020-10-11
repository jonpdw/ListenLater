using System;
using System.Collections.Generic;
using System.Diagnostics;

// using (var w = new Watch("Main")) {
 //
 // using (w.WatchInner("SomeInnerScope1")) {
 //     // do something
 // }
 // }

namespace APITest {
    public class Watch : IDisposable {
        private readonly Timing _timing;

        private readonly Stopwatch _sw;

        public Watch(string n) {
            _timing = new Timing(n, 0);
            _sw = new Stopwatch();
            _sw.Start();
        }

        public Watch(string n, int indentLevel) {
            _timing = new Timing(n, indentLevel);
            _sw = new Stopwatch();
            _sw.Start();
        }

        public Timing Timing {
            get {
                _timing.Elapsed = _sw.Elapsed;
                return _timing;
            }
        }

        public void Dispose() {
            _sw.Stop();
            _timing.Elapsed = _sw.Elapsed;
            if (_timing._indentLevel == 0) {
                _timing.Print();
            }
        }

        public Watch WatchInner(string scopeName) {
            var timingBuilder = new Watch(scopeName, _timing._indentLevel + 1);
            _timing.InnerScopes.Add(timingBuilder._timing);
            return timingBuilder;
        }
    }
    
    public class Timing
    {
        public Timing(string scopeName, int indentLevel)
        {
            // Console.WriteLine($"Started: {scopeName}");
            InnerScopes = new List<Timing>();
            ScopeName = scopeName;
            _indentLevel = indentLevel;
        }

        public TimeSpan Elapsed { get; internal set; }

        public List<Timing> InnerScopes { get; }

        public string ScopeName { get; }

        public int _indentLevel { get; set; }
        
        public void Print() {
            Console.WriteLine($"{new string('\t',_indentLevel)}{ScopeName,-25}: {Elapsed.TotalSeconds:F5}");
            // foreach (var innerStop in InnerScopes) {
            //     innerStop.Print();
            // }
        }

    }
}
