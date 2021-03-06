﻿using System;
using Toggl.Core.Calendar;

namespace Toggl.Core.Extensions
{
    public static class CalendarItemExtensions
    {
        public static bool IsEditable(this CalendarItem calendarItem)
            => calendarItem.Source == CalendarItemSource.TimeEntry;

        public static bool IsRunningTimeEntry(this CalendarItem calendarItem)
            => calendarItem.Source == CalendarItemSource.TimeEntry && calendarItem.Duration == null;

        public static DateTimeOffset EndTime(this CalendarItem calendarItem, DateTimeOffset now, TimeSpan? offsetFromNow = null)
            => calendarItem.EndTime.HasValue
                ? calendarItem.EndTime.Value.LocalDateTime
                : now + (offsetFromNow ?? TimeSpan.Zero);

        public static TimeSpan Duration(this CalendarItem calendarItem, DateTimeOffset now)
            => calendarItem.Duration ?? now - calendarItem.StartTime.LocalDateTime;
    }
}
