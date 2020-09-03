﻿using Toggl.Core.Models.Interfaces;
using Toggl.Storage.Models.Calendar;

namespace Toggl.Core.Models.Calendar
{
    public interface IThreadSafeExternalCalendar : IThreadSafeModel, IDatabaseExternalCalendar
    {
    }
}
