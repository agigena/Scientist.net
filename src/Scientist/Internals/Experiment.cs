﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitHub.Internals
{
    internal class Experiment<T> : IExperiment<T>, IExperimentAsync<T>
    {
        internal const string CandidateExperimentName = "candidate";

        private static readonly Func<Task<bool>> _alwaysRun = () => Task.FromResult(true);
        private static readonly Action<Operation, Exception> _alwaysThrow
            = (operation, exception) => { throw exception; };

        private string _name;
        private Func<Task<T>> _control;

        private readonly Dictionary<string, Func<Task<T>>> _candidates;
        private Func<T, T, bool> _comparison = DefaultComparison;
        private Func<Task> _beforeRun;
        private Action<Operation, Exception> _thrown = _alwaysThrow;
        private Func<Task<bool>> _runIf = _alwaysRun;
        private readonly List<Func<T, T, Task<bool>>> _ignores = new List<Func<T, T, Task<bool>>>();
        private readonly Dictionary<string, dynamic> _contexts = new Dictionary<string, dynamic>();

        public Experiment(string name)
        {
            _name = name;
            _candidates = new Dictionary<string, Func<Task<T>>>();
        }

        public bool ThrowOnMismatches { get; set; }

        public void RunIf(Func<Task<bool>> block) =>
            _runIf = block;

        public void RunIf(Func<bool> block) =>
            _runIf = () => Task.FromResult(block());

        public void Thrown(Action<Operation, Exception> block) =>
            _thrown = block;

        public void Use(Func<Task<T>> control) =>
            _control = control;

        public void Use(Func<T> control) =>
            _control = () => Task.FromResult(control());

        public void Try(Func<Task<T>> candidate)
        {
            if (_candidates.ContainsKey(CandidateExperimentName))
            {
                throw new InvalidOperationException(
                    "You have already added a default try. Give this candidate a new name with the Try(string, Func<Task<T>>) overload");
            }
            _candidates.Add(CandidateExperimentName, candidate);
        }

        public void Try(Func<T> candidate)
        {
            if (_candidates.ContainsKey(CandidateExperimentName))
            {
                throw new InvalidOperationException(
                    "You have already added a default try. Give this candidate a new name with the Try(string, Func<Task<T>>) overload");
            }
            _candidates.Add(CandidateExperimentName, () => Task.FromResult(candidate()));
        }

        public void Try(string name, Func<Task<T>> candidate)
        {
            if (_candidates.ContainsKey(name))
            {
                throw new InvalidOperationException(
                    $"You already have a candidate named {name}. Provide a different name for this test.");
            }
            _candidates.Add(name, candidate);
        }

        public void Try(string name, Func<T> candidate)
        {
            if (_candidates.ContainsKey(name))
            {
                throw new InvalidOperationException(
                    $"You already have a candidate named {name}. Provide a different name for this test.");
            }
            _candidates.Add(name, () => Task.FromResult(candidate()));
        }

        public void Ignore(Func<T, T, bool> block) =>
            _ignores.Add((con, can) => Task.FromResult(block(con, can)));

        public void Ignore(Func<T, T, Task<bool>> block) =>
            _ignores.Add(block);

        public void AddContext(string key, dynamic data)
        {
            _contexts.Add(key, data);
        }

        internal ExperimentInstance<T> Build() =>
            new ExperimentInstance<T>(new ExperimentSettings<T>
            {
                BeforeRun = _beforeRun,
                Candidates = _candidates,
                Comparator = _comparison,
                Contexts = _contexts,
                Control = _control,
                Ignores = _ignores,
                Name = _name,
                RunIf = _runIf,
                Thrown = _thrown,
                ThrowOnMismatches = ThrowOnMismatches
            });

        public void Compare(Func<T, T, bool> comparison)
        {
            _comparison = comparison;
        }

        static readonly Func<T, T, bool> DefaultComparison = (instance, comparand) =>
        {
            return (instance == null && comparand == null)
                || (instance != null && instance.Equals(comparand))
                || (CompareInstances(instance as IEquatable<T>, comparand));
        };

        static bool CompareInstances(IEquatable<T> instance, T comparand) => instance != null && instance.Equals(comparand);

        public void BeforeRun(Action action)
        {
            _beforeRun = async () => { action(); await Task.FromResult(0); };
        }

        public void BeforeRun(Func<Task> action)
        {
            _beforeRun = action;
        }
    }
}
