using System;
using System.Diagnostics;

namespace VRIQ.Timing
{
    /// <summary>
    /// 실험 세션이 시작된 시점부터 경과한 시간을 제공
    /// Unity 프레임 속도나 Time.timeScale의 영향을 받지 않음
    /// <summary>
    public sealed class SessionClock
    {
        private readonly long _startTimestamp;

        public DateTimeOffset UtcStart { get; }

        public SessionClock()
        {
            _startTimestamp = Stopwatch.GetTimestamp();
            UtcStart = DateTimeOffset.UtcNow;
        }

        public long NowNanoseconds
        {
            get
            {
                long elapsed =
                    Stopwatch.GetTimestamp() - _startTimestamp;

                return (long)(
                    elapsed *
                    (1_000_000_000.0 / Stopwatch.Frequency)
                );
            }
        }

        public double NowSeconds =>
            NowNanoseconds / 1_000_000_000.0;
    }
}