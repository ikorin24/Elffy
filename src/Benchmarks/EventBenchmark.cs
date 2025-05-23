﻿#nullable enable
using System;
using BenchmarkDotNet.Attributes;
using Elffy;

namespace Benchmarks.Events
{
    [MarkdownExporterAttribute.GitHub]
    [RyuJitX64Job]
    [MemoryDiagnoser]
    [CategoriesColumn]
    public class EventBenchmark
    {
        private readonly Action<EventBenchmark> _eventDelegate1;
        private readonly Action<EventBenchmark> _eventDelegate2;
        private readonly Action<EventBenchmark> _eventDelegate3;
        private readonly Action<EventBenchmark> _eventDelegate4;
        private readonly Action<EventBenchmark> _eventDelegate5;

        private EventSource<EventBenchmark> _event2;

        public event Action<EventBenchmark>? Event1;
        public Event<EventBenchmark> Event2 => _event2.Event;

        public EventBenchmark()
        {
            _eventDelegate1 = EventDelegate;
            _eventDelegate2 = EventDelegate;
            _eventDelegate3 = EventDelegate;
            _eventDelegate4 = EventDelegate;
            _eventDelegate5 = EventDelegate;
        }

        [Benchmark(Description = "Dotnet Event")]
        [BenchmarkCategory("zero")]
        public Action<EventBenchmark>? DotnetEvent0()
        {
            Event1?.Invoke(this);
            return Event1;
        }

        [Benchmark(Description = "Elffy Event")]
        [BenchmarkCategory("zero")]
        public EventSource<EventBenchmark> ElffyEvent0()
        {
            _event2.Invoke(this);
            return _event2;
        }

        [Benchmark(Description = "Dotnet Event")]
        [BenchmarkCategory("one")]
        public Action<EventBenchmark>? DotnetEvent1()
        {
            Event1 += _eventDelegate1;
            Event1?.Invoke(this);
            Event1 -= _eventDelegate1;
            return Event1;
        }

        [Benchmark(Description = "Elffy Event")]
        [BenchmarkCategory("one")]
        public EventSource<EventBenchmark> ElffyEvent1()
        {
            using var u1 = Event2.Subscribe(_eventDelegate1);
            _event2.Invoke(this);
            return _event2;
        }

        [Benchmark(Description = "Dotnet Event")]
        [BenchmarkCategory("two")]
        public Action<EventBenchmark>? DotnetEvent2()
        {
            Event1 += _eventDelegate1;
            Event1 += _eventDelegate2;
            Event1?.Invoke(this);
            Event1 -= _eventDelegate1;
            Event1 -= _eventDelegate2;
            return Event1;
        }

        [Benchmark(Description = "Elffy Event")]
        [BenchmarkCategory("two")]
        public EventSource<EventBenchmark> ElffyEvent2()
        {
            using var u1 = Event2.Subscribe(_eventDelegate1);
            using var u2 = Event2.Subscribe(_eventDelegate2);
            _event2.Invoke(this);
            return _event2;
        }

        [Benchmark(Description = "Dotnet Event")]
        [BenchmarkCategory("three")]
        public Action<EventBenchmark>? DotnetEvent3()
        {
            Event1 += _eventDelegate1;
            Event1 += _eventDelegate2;
            Event1 += _eventDelegate3;
            Event1?.Invoke(this);
            Event1 -= _eventDelegate1;
            Event1 -= _eventDelegate2;
            Event1 -= _eventDelegate3;
            return Event1;
        }

        [Benchmark(Description = "Elffy Event")]
        [BenchmarkCategory("three")]
        public EventSource<EventBenchmark> ElffyEvent3()
        {
            using var u1 = Event2.Subscribe(_eventDelegate1);
            using var u2 = Event2.Subscribe(_eventDelegate2);
            using var u3 = Event2.Subscribe(_eventDelegate3);
            _event2.Invoke(this);
            return _event2;
        }

        [Benchmark(Description = "Dotnet Event")]
        [BenchmarkCategory("four")]
        public Action<EventBenchmark>? DotnetEvent4()
        {
            Event1 += _eventDelegate1;
            Event1 += _eventDelegate2;
            Event1 += _eventDelegate3;
            Event1 += _eventDelegate4;
            Event1?.Invoke(this);
            Event1 -= _eventDelegate1;
            Event1 -= _eventDelegate2;
            Event1 -= _eventDelegate3;
            Event1 -= _eventDelegate4;
            return Event1;
        }

        [Benchmark(Description = "Elffy Event")]
        [BenchmarkCategory("four")]
        public EventSource<EventBenchmark> ElffyEvent4()
        {
            using var u1 = Event2.Subscribe(_eventDelegate1);
            using var u2 = Event2.Subscribe(_eventDelegate2);
            using var u3 = Event2.Subscribe(_eventDelegate3);
            using var u4 = Event2.Subscribe(_eventDelegate4);
            _event2.Invoke(this);
            return _event2;
        }

        [Benchmark(Description = "Dotnet Event")]
        [BenchmarkCategory("five")]
        public Action<EventBenchmark>? DotnetEvent5()
        {
            Event1 += _eventDelegate1;
            Event1 += _eventDelegate2;
            Event1 += _eventDelegate3;
            Event1 += _eventDelegate4;
            Event1 += _eventDelegate5;
            Event1?.Invoke(this);
            Event1 -= _eventDelegate1;
            Event1 -= _eventDelegate2;
            Event1 -= _eventDelegate3;
            Event1 -= _eventDelegate4;
            Event1 -= _eventDelegate5;
            return Event1;
        }

        [Benchmark(Description = "Elffy Event")]
        [BenchmarkCategory("five")]
        public EventSource<EventBenchmark> ElffyEvent5()
        {
            using var u1 = Event2.Subscribe(_eventDelegate1);
            using var u2 = Event2.Subscribe(_eventDelegate2);
            using var u3 = Event2.Subscribe(_eventDelegate3);
            using var u4 = Event2.Subscribe(_eventDelegate4);
            using var u5 = Event2.Subscribe(_eventDelegate5);
            _event2.Invoke(this);
            return _event2;
        }

        private void EventDelegate(EventBenchmark arg)
        {
        }
    }
}
