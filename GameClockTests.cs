using NUnit.Framework;
using TimeSystem.Core;

namespace TimeSystem.Editor.Core
{
    /// <summary>
    /// Abstract test class used for inherting the base setup method,
    /// as well as the CycleThroughMinutes method to cycle through the clock
    /// </summary>
    public abstract class GameClockTests
    {
        protected GameClock GameClock;
        protected bool IsClockCompleted;

        [SetUp]
        protected virtual void Init()
        {
            // We are using our own value of 3 days in case the default is
            // one day changed.
            GameClock = new GameClock(ClockCompletedCallback, 3);
            IsClockCompleted = false;
        }

        /// <summary>
        /// Cycles through the passed minutes and calls the tick method on for each minute
        /// </summary>
        /// <param name="minutes">The amount of ticks to call</param>
        protected void CycleThroughMinutes(int minutes)
        {
            for (var i = 0; i < minutes; i++)
                GameClock.Tick();
        }

        private void ClockCompletedCallback()
        {
            IsClockCompleted = true;
        }
    }

    /// <summary>
    /// Tests the Clock Cycle methods.
    /// </summary>
    [TestFixture]
    public class ClockCycleTests : GameClockTests
    {
        [Test]
        public void IsMinuteDefault()
        {
            Assert.AreEqual(GameClock.MinuteDefault, GameClock.Minute);
        }

        [Test]
        public void IsHourDefault()
        {
            Assert.AreEqual(GameClock.HourDefault, GameClock.Hour);
        }

        [Test]
        public void IsDayDefault()
        {
            Assert.AreEqual(GameClock.DayDefault, GameClock.Day);
        }

        [Test]
        public void DoesTickIncreaseMinuteByOne()
        {
            var currentMinute = GameClock.Minute;
            GameClock.Tick();
            Assert.AreEqual(currentMinute + 1, GameClock.Minute);
        }

        [Test]
        public void DoesTickNotEqualThePreviousMinute()
        {
            var currentMinute = GameClock.Minute;
            GameClock.Tick();
            Assert.AreNotEqual(currentMinute, GameClock.Minute);
        }


        [Test, TestCase(1), TestCase(2)]
        public void DoesIncreasingMinutePastMinuteThresholdResetToDefault(int hoursToPass)
        {
            var defaultMinute = GameClock.Minute;
            //Get 1 hour of minutes
            CycleThroughMinutes(GameClock.GetMinutesInHours(hoursToPass));
            Assert.AreEqual(defaultMinute, GameClock.Minute);
        }

        [Test, TestCase(1), TestCase(2)]
        public void DoesIncreasingMinutePastMinuteThresholdIncreaseHourByOne(int hoursToPass)
        {
            var previousHour = GameClock.Hour;
            //Get 1 hour of minutes
            CycleThroughMinutes(GameClock.GetMinutesInHours(hoursToPass));
            Assert.AreEqual(previousHour + hoursToPass, GameClock.Hour);
        }

        [Test, TestCase(1), TestCase(2)]
        public void DoesTickMinuteUntilMinuteThresholdNotIncreaseHourByOne(int hoursToPass)
        {
            var previousHour = GameClock.Hour + hoursToPass -1;
            //Get 1 hour of minutes and remove 1 minute
            CycleThroughMinutes(GameClock.GetMinutesInHours(hoursToPass) - 1);
            Assert.AreEqual(previousHour, GameClock.Hour);
        }

        [Test]
        public void DoesIncreasingHourPastHourThresholdResetHourToDefault()
        {
            //Get 1 day of minutes
            CycleThroughMinutes(GameClock.GetMinutesInDays(1));
            Assert.AreEqual(0, GameClock.Hour);
        }

        [Test]
        public void DoesIncreasingHourPastHourThresholdIncreaseDayByOne()
        {
            var previousDay = GameClock.Day;
            //Get the minutes for 1 day
            CycleThroughMinutes(GameClock.GetMinutesInDays(1));
            Assert.AreEqual(previousDay + 1, GameClock.Day);
        }

        [Test]
        public void DoesIncreasingHourUntilHourThresholdNotIncreaseByOne()
        {
            var previousDay = GameClock.Day;
            //Get the minutes for 1 day, and take off 1 minute
            CycleThroughMinutes(GameClock.GetMinutesInDays(1) - 1);
            Assert.AreEqual(previousDay, GameClock.Day);
        }

        [Test]
        public void DoesIncreasingDayPastTheDayThresholdCallTimeCompletedCallback()
        {
            CycleThroughMinutes(GameClock.GetMinutesInDays(GameClock.CompletionDay));
            Assert.IsTrue(IsClockCompleted);
        }

        [Test]
        public void DoesIncreasingDayUntilBeforeThresholdNotCallTimeCompletedCallback()
        {
            CycleThroughMinutes(GameClock.GetMinutesInDays(GameClock.CompletionDay) - 1);
            Assert.IsFalse(IsClockCompleted);
        }

        [Test]
        public void DoesResetSetTheValuesOfTheClockBackToDefault()
        {
            //Random value so that reset needs to be performed
            CycleThroughMinutes(GameClock.GetTotalMinutes(2, 11, 43));
            GameClock.Reset();
            var isDefault = GameClock.Day == GameClock.DayDefault && GameClock.Hour == GameClock.HourDefault && GameClock.Minute == GameClock.MinuteDefault;
            Assert.IsTrue(isDefault);
        }
    }

    /// <summary>
    /// Tests the Scheduled Events class integration with the GameClock class
    /// </summary>
    public class GameClockScheduledEventTests : GameClockTests
    {
        private readonly int _eventDay = 1;
        private readonly int _eventHour = 11;
        private readonly int _eventMinute = 45;
        private int _totalTime;

        private int _preAddScehduledEventCount;
        private bool _isCompleted;

        protected override void Init()
        {
            base.Init();
            _preAddScehduledEventCount = GameClock.ScheduledEventsCount;
            _isCompleted = false;
            GameClock.ScheduleEvent(_eventDay, _eventHour, _eventMinute, () => { _isCompleted = true; });
            _totalTime = GameClock.GetTotalMinutes(_eventDay, _eventHour, _eventMinute);
        }

        [Test]
        public void DoesAddingAScheduledEventIncreaseTheScheduledEventCount()
        {
            Assert.AreEqual(_preAddScehduledEventCount + 1, GameClock.ScheduledEventsCount);
        }

        [Test]
        public void DoesAScheduledEventGetCalledAtTheCorrectTime()
        {
            CycleThroughMinutes(_totalTime);
            Assert.IsTrue(_isCompleted);
        }

        [Test]
        public void DoesAScheduledEventNotGetCalledAtUntilTheCorrectTime()
        {
            CycleThroughMinutes(_totalTime - 1);
            Assert.IsFalse(_isCompleted);
        }

        [Test]
        public void DoesCallingAScheduledEventRemoveItFromTheScheduledEventsList()
        {
            var eventsCount = GameClock.ScheduledEventsCount;
            //Cycling to the event should call it and remove it from the list
            CycleThroughMinutes(_totalTime);
            Assert.AreEqual(eventsCount - 1, GameClock.ScheduledEventsCount);
        }

        [Test]
        public void DoesAddingTwoEventsAtTheSameMinuteCauseTheEventSizeToNotGrow()
        {
            var eventsCount = GameClock.ScheduledEventsCount;
            GameClock.ScheduleEvent(_eventDay, _eventHour, _eventMinute, () => { });
            Assert.AreEqual(eventsCount, GameClock.ScheduledEventsCount);
        }

        [Test]
        public void DoesTriggeringAnEventsListCauseAllJoindEventsToTriggerCompletedCallback()
        {
            var isCompletedTwo = false;
            GameClock.ScheduleEvent(_eventDay, _eventHour, _eventMinute, () => { isCompletedTwo = true; });
            CycleThroughMinutes(_totalTime);
            Assert.IsTrue(_isCompleted && isCompletedTwo);
        }
    }

    /// <summary>
    /// Tests the Scheduled Event class.
    /// </summary>
    [TestFixture]
    public class ScheduledEventTest
    {
        private readonly int _eventDay = 1;
        private readonly int _eventHour = 11;
        private readonly int _eventMinute = 45;
        private int _totalTime;
        private ScheduledEvent _scheduledEvent;
        private bool _isCompleted;

        [SetUp]
        public void Init()
        {
            _isCompleted = false;
            _totalTime = GameClock.GetTotalMinutes(_eventDay, _eventHour, _eventMinute);
            _scheduledEvent = new ScheduledEvent(_totalTime, EventCallback);
        }

        [Test]
        public void DoesSettingADayWithTheConstructorSetTheScheduledTimeProperty()
        {
            Assert.AreEqual(_totalTime, _scheduledEvent.ScheduledTime);
        }

        [Test]
        public void DoesIsMatchReturnTrueWhenThereIsAMatch()
        {
            Assert.IsTrue(_scheduledEvent.IsMatch(_totalTime));
        }

        [Test]
        public void DoesIsMatchReturnFalseWhenThereIsNotAMatch()
        {
            Assert.IsFalse(_scheduledEvent.IsMatch(_totalTime + 1));
        }

        [Test]
        public void DoesCallActionCallTheCallbackSetInTheConstructor()
        {
            _scheduledEvent.Completed();
            Assert.IsTrue(_isCompleted);
        }

        /// <summary>
        /// Used to set the _isCompleted boolean to true.
        /// </summary>
        private void EventCallback()
        {
            _isCompleted = true;
        }
    }
}