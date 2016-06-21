using System;
using System.Collections.Generic;

namespace TimeSystem.Core
{
    /// <summary>
    /// GameClock must be update regularly using the Tick method, which will update
    /// minute attribute, as well as others like a regular clock.  Also, this class will
    /// call scheduled events that where scheduled for specific times once the clock hits
    /// that exact time.
    /// </summary>
    public class GameClock
    {
        #region Properties

        /// <summary>
        /// The current day of the clock
        /// </summary>
        public int Day { get { return _currentTime/MinutesInDay; } }

        /// <summary>
        /// The current hour of the clock
        /// </summary>
        public int Hour { get { return _currentTime%MinutesInDay/MinutesInHour; } }

        /// <summary>
        /// The current Minute of the Clock
        /// </summary>
        public int Minute { get { return _currentTime%MinutesInDay%MinutesInHour; } }

        /// <summary>
        /// The amount of days until the clock is stopped.
        /// </summary>
        public int CompletionDay { get { return _completionTime/MinutesInDay; } }

        /// <summary>
        /// The amount of events scheduled in the clock for individual minutes.
        /// </summary>
        public int ScheduledEventsCount { get { return _scheduledEvents.Count; } }

        #endregion

        #region Private Attributes

        private int _currentTime; //The current minute of the clock, this is the heart of keeping track of the time.

        private int _completionTime; //The time when the clock stops running, in minutes.

        private Dictionary<int, List<ScheduledEvent>> _scheduledEvents; //The dictionary containing all events.

        private bool _isClockCompleted; // Set once the clock has completed its last day.

        private Action _completionCallback; // Called when the CompletionDay is hit, the clock will stop and this will be called

        #endregion

        #region Default ReadOnly Attributes

        public readonly int MinuteDefault = 0; // The time minute will start at on Reset.
        public readonly int HourDefault = 0; // The time hour will start at on Reset.
        public readonly int DayDefault = 0; // The time day will start at on Reset.

        private static readonly int MinutesInDay = 1440; // The amount of minutes in a day.
        private static readonly int MinutesInHour = 60; // The amount of minutes in an hour.

        #endregion

        #region Constructors

        /// <summary>
        /// Ceates a new clock object that will call the passed completionCallback
        /// once the clock is completed
        /// </summary>
        /// <param name="completionCallback">The method to call on completion</param>
        /// <param name="dayThreshold">The amount of days until the clock is complete</param>
        public GameClock(Action completionCallback, int dayThreshold = 3) : this(dayThreshold)
        {
            _completionCallback = completionCallback;
        }

        /// <summary>
        /// Creates a new object that will finish after the completed CompletionDay amount
        /// </summary>
        /// <param name="completionDay">The amount of days until the clock is completed</param>
        public GameClock(int completionDay = 3)
        {
            _completionTime = GetMinutesInDays(completionDay);
            Reset();
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Gets the amount of minutes in the passed hours.
        /// </summary>
        /// <param name="hours">The amount of hours to convert to minutes</param>
        /// <returns>The amount of minutes in the passed hours.</returns>
        public static int GetMinutesInHours(int hours)
        {
            return hours*MinutesInHour;
        }

        /// <summary>
        /// Gets the amount of minutes in the passed days.
        /// </summary>
        /// <param name="days">The amount of days to convert to minutes.</param>
        /// <returns>The amount of minutes in the passed days.</returns>
        public static int GetMinutesInDays(int days)
        {
            return days*MinutesInDay;
        }

        /// <summary>
        /// Gets the amount of minutes summed from the passed days, hours, and minutes.
        /// </summary>
        /// <param name="days">The amount of days to convert to minutes.</param>
        /// <param name="hours">The amount of hours to convert to minutes.</param>
        /// <param name="minutes">the amount of minutes to add to the sum.</param>
        /// <returns>The total time in the passed days, hours, and minutes.</returns>
        public static int GetTotalMinutes(int days, int hours, int minutes)
        {
            return GetMinutesInDays(days) + GetMinutesInHours(hours) + minutes;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Ticks the time up by one minute, and then adjust all nessesary clock internals
        /// for the added minute.
        /// </summary>
        public void Tick()
        {
            //If the clock has finished, it returns out of the method.
            if (_isClockCompleted) return;

            UpdateTime();
            TriggerEvents();
            CheckCompletion();
        }

        /// <summary>
        /// Updates the current time by adding a minute on.
        /// </summary>
        private void UpdateTime()
        {
            _currentTime++;
        }

        /// <summary>
        /// Resets all clock Values back to default values and cleans up the events dictionary;
        /// </summary>
        public void Reset()
        {
            _currentTime = 0;
            _isClockCompleted = false;
            _scheduledEvents = new Dictionary<int, List<ScheduledEvent>>();
        }

        /// <summary>
        /// Schedules an event into the ScheduledEvent Dictionary for calling when the clock
        /// hits the time passed.
        /// </summary>
        /// <param name="day">The day for the event to trigger</param>
        /// <param name="hour">The hour for the event to trigger</param>
        /// <param name="minute">The minute for the event to trigger</param>
        /// <param name="callback">The callback to be called when the event triggers.</param>
        public void ScheduleEvent(int day, int hour, int minute, Action callback)
        {
            //Get the scheduled time in minutes.
            var scheduleTime = GetTotalMinutes(day, hour, minute);
            // Create an empty list for refrence.
            List<ScheduledEvent> eventListToAddTo;

            //If the TryGet Value returns nothing...
            if (!_scheduledEvents.TryGetValue(scheduleTime, out eventListToAddTo))
            {
                // ...Create a new List of Scehduled events...
                eventListToAddTo = new List<ScheduledEvent>();

                // ...And add it to the dictionary.
                _scheduledEvents.Add(scheduleTime, eventListToAddTo);
            }

            // Add the new scheduled event to the event list for the given minute, which is no for sure in the dictionary
            eventListToAddTo.Add(new ScheduledEvent(scheduleTime, callback));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Check to see if the clock has reached its completion time.
        /// If so, it will call the completion callback, and then set the _isClockCompleted
        /// attribute to true
        /// </summary>
        private void CheckCompletion()
        {
            // If Day is below its threshold (default 3) we return out
            if (Day < CompletionDay) return;

            // Called if enough days have gone by that it needs to exit
            _completionCallback.Run();
            _isClockCompleted = true;
        }

        /// <summary>
        /// Seaches the Scheduled Events Dictionary for the current minute as an index, and then if found
        /// will call all events stored at that times, and then remove them from the dicitonary for
        /// faster searching on subsequent searches.
        /// </summary>
        private void TriggerEvents()
        {
            List<ScheduledEvent> currentTimeEvents;
            // If the Scheduled events does not have this key, exit out of this method.
            if (!_scheduledEvents.TryGetValue(_currentTime, out currentTimeEvents)) return;

            // If the Scheduled Events did have this minute as an event...
            // For Each scheduled event in this minutes event list...
            foreach (var currentTimeEvent in currentTimeEvents)
                // ... Call the completed method on each event.
                currentTimeEvent.Completed();

            // Remove the entire list of events for this minute from the list as this minute has now past.
            _scheduledEvents.Remove(_currentTime);
        }

        #endregion
    }

    /// <summary>
    /// Used to schedule an event in the clocks scheduled event list.
    /// </summary>
    public class ScheduledEvent
    {
        #region Properties

        /// <summary>
        /// The scheduled minute of this evnet.
        /// </summary>
        public int ScheduledTime { get; private set; }

        /// <summary>
        /// The callback to call when the object is completed.
        /// </summary>
        public Action CompletedCallback { get; private set; }

        #endregion

        #region Private Attributes

        private bool _isCompleted; //Set when the scheduled event is completed.

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor for the Scheduled event.
        /// </summary>
        /// <param name="scheduledMinute"></param>
        /// <param name="completedCallback">Callback to be called when the event schedules.</param>
        public ScheduledEvent(int scheduledMinute, Action completedCallback)
        {
            ScheduledTime = scheduledMinute;
            CompletedCallback = completedCallback;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks to see if the days, hour, and minute passed match the scheduled events attributes.
        /// </summary>
        /// <param name="timeToCheck">The time to check if it matches the scheduled time of this event</param>
        /// <returns>True if the attributes match, otherwise false.</returns>
        public bool IsMatch(int timeToCheck)
        {
            return ScheduledTime == timeToCheck;
        }

        /// <summary>
        /// Call to complete the scheduled event
        /// </summary>
        public void Completed()
        {
            // If this event is already completed, return out of the method.
            if (_isCompleted) return;

            // Calls the extension method run for action, which is just a safe
            // way to run a delegate. (ie it isnt null)
            CompletedCallback.Run();
            _isCompleted = true;
        }

        #endregion
    }
}