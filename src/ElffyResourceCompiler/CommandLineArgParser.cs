using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElffyResourceCompiler
{
    internal static class CommandLineArgParser
    {
        public static CommandLineArgument Parse(string[] args)
        {
            if(args == null) { throw new ArgumentNullException(nameof(args)); }
            var normalArgs = new List<string>();
            var optionalArgs = new Dictionary<string, string>();
            for(int i = 0; i < args.Length; i++) {
                var current = args[i];
                if(current.StartsWith("-")) {
                    if(optionalArgs.ContainsKey(current)) {
                        throw new ArgumentException($"Optional argument '{current}' is duplicated.");
                    }
                    if(args.Length > i + 1) {
                        if(args[i + 1].StartsWith("-")) {
                            optionalArgs.Add(current, "");
                        }
                        else {
                            optionalArgs.Add(current, args[i + 1]);
                            i++;
                        }
                    }
                    else {
                        optionalArgs.Add(current, "");
                    }
                }
                else {
                    normalArgs.Add(current);
                }
            }
            return new CommandLineArgument(normalArgs.ToArray(), optionalArgs);
        }
    }

    internal class CommandLineArgument
    {
        public ArgumentCollection Args { get; }
        public OptionalArgumentCollection OptionalArgs { get; }

        public CommandLineArgument(string[] normalArgs, Dictionary<string ,string> optionalArgs)
        {
            if(normalArgs == null) { throw new ArgumentNullException(nameof(normalArgs)); }
            if(optionalArgs == null) { throw new ArgumentNullException(nameof(optionalArgs)); }
            Args = new ArgumentCollection(normalArgs);
            OptionalArgs = new OptionalArgumentCollection(optionalArgs);
        }
    }

    internal class ArgumentCollection : IReadOnlyList<string>
    {
        private string[] _args;
        public string this[int index] => _args[index];

        public int Count => _args.Length;

        internal ArgumentCollection(string[] args)
        {
            _args = args ?? throw new ArgumentNullException(nameof(args));
        }

        public IEnumerator<string> GetEnumerator() => _args.Cast<string>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _args.GetEnumerator();
    }

    internal class OptionalArgumentCollection : IReadOnlyDictionary<string, string>
    {
        private IDictionary<string, string> _optioanlArgs;

        public int Count => _optioanlArgs.Count;

        public IEnumerable<string> Keys => _optioanlArgs.Keys;

        public IEnumerable<string> Values => _optioanlArgs.Values;

        public string this[string key] => _optioanlArgs[key];

        internal OptionalArgumentCollection(IDictionary<string, string> optionalArgs)
        {
            _optioanlArgs = optionalArgs ?? throw new ArgumentNullException(nameof(_optioanlArgs));
        }

        public bool ContainsKey(string key) => _optioanlArgs.ContainsKey(key);

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _optioanlArgs.GetEnumerator();

        public bool TryGetValue(string key, out string value) => _optioanlArgs.TryGetValue(key, out value!);

        IEnumerator IEnumerable.GetEnumerator() => _optioanlArgs.GetEnumerator();
    }
}
