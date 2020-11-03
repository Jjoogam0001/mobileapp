﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Shared;
using Toggl.Shared.Extensions;

namespace Toggl.Storage.Settings
{
    public sealed class SettingsStorage : IAccessRestrictionStorage, IOnboardingStorage, IUserPreferences, ILastTimeUsageStorage
    {
        private const string outdatedApiKey = "OutdatedApi";
        private const string outdatedClientKey = "OutdatedClient";
        private const string unauthorizedAccessKey = "UnauthorizedAccessForApiToken";
        private const string noWorkspaceKey = "noWorkspace";
        private const string noDefaultWorkspaceKey = "noDefaultWorkspace";

        private const string userSignedUpUsingTheAppKey = "UserSignedUpUsingTheApp";
        private const string isNewUserKey = "IsNewUser";
        private const string lastAccessDateKey = "LastAccessDate";
        private const string firstAccessDateKey = "FirstAccessDate";
        private const string completedOnboardingKey = "CompletedOnboarding";

        private const string preferManualModeKey = "PreferManualMode";
        private const string runningTimerNotificationsKey = "RunningTimerNotifications";
        private const string stoppedTimerNotificationsKey = "StoppedTimerNotifications";

        private const string startButtonWasTappedBeforeKey = "StartButtonWasTappedBefore";
        private const string hasTappedTimeEntryKey = "HasTappedTimeEntry";
        private const string hasEditedTimeEntryKey = "HasEditedTimeEntry";
        private const string projectOrTagWasAddedBeforeKey = "ProjectOrTagWasAddedBefore";
        private const string stopButtonWasTappedBeforeKey = "StopButtonWasTappedBefore";
        private const string hasSelectedProjectKey = "HasSelectedProject";
        private const string navigatedAwayFromMainViewAfterTappingStopButtonKey = "NavigatedAwayFromMainView";
        private const string hasTimeEntryBeenContinuedKey = "HasTimeEntryBeenContinued";

        private const string onboardingPrefix = "Onboarding_";
        private const string onboardingTimeEntryWasCreatedKey = "OnboardingTimeEntryWasCreated";

        private const string ratingViewOutcomeKey = "RatingViewOutcome";
        private const string ratingViewOutcomeTimeKey = "RatingViewOutcomeTime";
        private const string ratingViewNumberOfTimesShownKey = "RatingViewNumberOfTimesShown";

        private const string lastSyncAttemptKey = "LastSyncAttempt";
        private const string lastSuccessfulSyncKey = "LastSuccessfulSync";
        private const string lastLoginKey = "LastLogin";
        private const string lastTimePlaceholdersWereCreated = "LastPullTimeEntries";
        private const string lastTimeExternalCalendarsSyncedKey = "LastTimeExternalCalendarsSynced";

        private const string calendarIntegrationEnabledKey = "CalendarIntegrationEnabled";
        private const string enabledCalendarsKey = "EnabledCalendars";
        private const char calendarIdSeparator = ';';
        private const string isFirstTimeConnectingCalendarsKey = "IsFirstTimeConnectingCalendars";
        private const string calendarViewWasOpenedBeforeKey = "CalendarViewWasOpenedBefore";
        private const string connectCalendarsPopupWasShownKey = "ConnectCalendarsPopupWasShown";

        private const string calendarTabWasOpenedKey = "CalendarTabWasOpened";
        private const string reportsTabWasOpenedKey = "ReportsTabWasOpened";

        private const string calendarNotificationsEnabledKey = "CalendarNotificationsEnabled";
        private const string timeSpanBeforeCalendarNotificationsKey = "TimeSpanBeforeCalendarNotifications";

        private const string didShowSiriClipboardInstructionKey = "didShowSiriClipboardInstructionKey";

        private const string swipeActionsDisabledKey = "swipeActionsDisabled";

        private readonly string[] oldOnboardingDismissedKeys =
        {
            "Onboarding_Toggl.Core.UI.Onboarding.MainView.StartTimeEntryOnboardingStep",
            "Onboarding_Toggl.Core.UI.Onboarding.MainView.StopTimeEntryOnboardingStep",
            "Onboarding_Toggl.Core.UI.Onboarding.MainView.EditTimeEntryOnboardingStep"
        };

        private readonly Version version;
        private readonly IKeyValueStorage keyValueStorage;
        private readonly int tooltipCount;
        private readonly HashSet<OnboardingConditionKey> shownTooltips;

        private readonly ISubject<bool> userSignedUpUsingTheAppSubject;
        private readonly ISubject<bool> isNewUserSubject;
        private readonly ISubject<bool> projectOrTagWasAddedSubject;
        private readonly ISubject<bool> startButtonWasTappedSubject;
        private readonly ISubject<bool> hasTappedTimeEntrySubject;
        private readonly ISubject<bool> hasEditedTimeEntrySubject;
        private readonly ISubject<bool> stopButtonWasTappedSubject;
        private readonly ISubject<bool> hasSelectedProjectSubject;
        private readonly ISubject<bool> isManualModeEnabledSubject;
        private readonly ISubject<bool> areRunningTimerNotificationsEnabledSubject;
        private readonly ISubject<bool> areStoppedTimerNotificationsEnabledSubject;
        private readonly ISubject<bool> hasTimeEntryBeenContinuedSubject;
        private readonly ISubject<bool> navigatedAwayFromMainViewAfterTappingStopButtonSubject;
        private readonly ISubject<bool> calendarIntegrationEnabledSubject;
        private readonly ISubject<List<string>> enabledCalendarsSubject;
        private readonly ISubject<bool> calendarNotificationsEnabledSubject;
        private readonly ISubject<TimeSpan> timeSpanBeforeCalendarNotificationsSubject;
        private readonly ISubject<bool> swipeActionsEnabledSubject;
        private readonly ISubject<OnboardingConditionKey> onboardingConditionMetSubject;

        private readonly TimeSpan defaultTimeSpanBeforeCalendarNotificationsSubject = TimeSpan.FromMinutes(10);

        public SettingsStorage(Version version, IKeyValueStorage keyValueStorage)
        {
            Ensure.Argument.IsNotNull(keyValueStorage, nameof(keyValueStorage));

            this.version = version;
            this.keyValueStorage = keyValueStorage;

            var allTooltips = Enum.GetValues(typeof(OnboardingConditionKey)).Cast<OnboardingConditionKey>().ToArray();
            tooltipCount = allTooltips.Length;
            shownTooltips = new HashSet<OnboardingConditionKey>();
            initializeShownTooltipHashSet(allTooltips);

            (isNewUserSubject, IsNewUser) = prepareSubjectAndObservable(isNewUserKey, keyValueStorage.GetBool);
            (enabledCalendarsSubject, EnabledCalendars) = prepareSubjectAndObservable(EnabledCalendarIds());
            (isManualModeEnabledSubject, IsManualModeEnabledObservable) = prepareSubjectAndObservable(preferManualModeKey, keyValueStorage.GetBool);
            (areRunningTimerNotificationsEnabledSubject, AreRunningTimerNotificationsEnabledObservable) = prepareSubjectAndObservable(runningTimerNotificationsKey, keyValueStorage.GetBool);
            (areStoppedTimerNotificationsEnabledSubject, AreStoppedTimerNotificationsEnabledObservable) = prepareSubjectAndObservable(stoppedTimerNotificationsKey, keyValueStorage.GetBool);
            (hasTappedTimeEntrySubject, HasTappedTimeEntry) = prepareSubjectAndObservable(hasTappedTimeEntryKey, keyValueStorage.GetBool);
            (hasEditedTimeEntrySubject, HasEditedTimeEntry) = prepareSubjectAndObservable(hasEditedTimeEntryKey, keyValueStorage.GetBool);
            (hasSelectedProjectSubject, HasSelectedProject) = prepareSubjectAndObservable(hasSelectedProjectKey, keyValueStorage.GetBool);
            (stopButtonWasTappedSubject, StopButtonWasTappedBefore) = prepareSubjectAndObservable(stopButtonWasTappedBeforeKey, keyValueStorage.GetBool);
            (userSignedUpUsingTheAppSubject, UserSignedUpUsingTheApp) = prepareSubjectAndObservable(userSignedUpUsingTheAppKey, keyValueStorage.GetBool);
            (startButtonWasTappedSubject, StartButtonWasTappedBefore) = prepareSubjectAndObservable(startButtonWasTappedBeforeKey, keyValueStorage.GetBool);
            (projectOrTagWasAddedSubject, ProjectOrTagWasAddedBefore) = prepareSubjectAndObservable(projectOrTagWasAddedBeforeKey, keyValueStorage.GetBool);
            (calendarIntegrationEnabledSubject, CalendarIntegrationEnabledObservable) = prepareSubjectAndObservable(calendarIntegrationEnabledKey, keyValueStorage.GetBool);
            (calendarNotificationsEnabledSubject, CalendarNotificationsEnabled) = prepareSubjectAndObservable(calendarNotificationsEnabledKey, keyValueStorage.GetBool);
            (navigatedAwayFromMainViewAfterTappingStopButtonSubject, NavigatedAwayFromMainViewAfterTappingStopButton) = prepareSubjectAndObservable(navigatedAwayFromMainViewAfterTappingStopButtonKey, keyValueStorage.GetBool);
            (hasTimeEntryBeenContinuedSubject, HasTimeEntryBeenContinued) = prepareSubjectAndObservable(hasTimeEntryBeenContinuedKey, keyValueStorage.GetBool);
            (timeSpanBeforeCalendarNotificationsSubject, TimeSpanBeforeCalendarNotifications) = prepareSubjectAndObservable(keyValueStorage.GetTimeSpan(timeSpanBeforeCalendarNotificationsKey) ?? defaultTimeSpanBeforeCalendarNotificationsSubject);
            (swipeActionsEnabledSubject, SwipeActionsEnabled) = prepareSubjectAndObservable(swipeActionsDisabledKey, key => !keyValueStorage.GetBool(key));

            onboardingConditionMetSubject = new Subject<OnboardingConditionKey>();
            OnboardingConditionMet = onboardingConditionMetSubject.AsObservable().DistinctUntilChanged();
        }

        private void initializeShownTooltipHashSet(OnboardingConditionKey[] allOnboardingConditionKeys)
        {
            foreach (var tooltip in allOnboardingConditionKeys)
            {
                if (OnboardingConditionWasMetBefore(tooltip))
                    shownTooltips.Add(tooltip);
            }
            if (shownTooltips.Count == tooltipCount)
                setCompletedOnboarding();
        }

        #region IAccessRestrictionStorage

        public void SetClientOutdated()
        {
            keyValueStorage.SetString(outdatedClientKey, version.ToString());
        }

        public void SetApiOutdated()
        {
            keyValueStorage.SetString(outdatedApiKey, version.ToString());
        }

        public void SetUnauthorizedAccess(string apiToken)
        {
            keyValueStorage.SetString(unauthorizedAccessKey, apiToken);
        }

        public void SetNoWorkspaceStateReached(bool hasNoWorkspace)
        {
            keyValueStorage.SetBool(noWorkspaceKey, hasNoWorkspace);
        }

        public void SetNoDefaultWorkspaceStateReached(bool hasNoDefaultWorkspace)
        {
            keyValueStorage.SetBool(noDefaultWorkspaceKey, hasNoDefaultWorkspace);
        }

        public bool HasNoWorkspace()
        {
            return keyValueStorage.GetBool(noWorkspaceKey);
        }

        public bool HasNoDefaultWorkspace()
            => keyValueStorage.GetBool(noDefaultWorkspaceKey);

        public bool IsClientOutdated()
            => isOutdated(outdatedClientKey);

        public bool IsApiOutdated()
            => isOutdated(outdatedApiKey);

        public bool IsUnauthorized(string apiToken)
            => apiToken == keyValueStorage.GetString(unauthorizedAccessKey);

        private bool isOutdated(string key)
        {
            var storedVersion = getStoredVersion(key);
            return storedVersion != null && version <= storedVersion;
        }

        private Version getStoredVersion(string key)
        {
            var stored = keyValueStorage.GetString(key);
            return stored == null ? null : Version.Parse(stored);
        }

        #endregion

        #region IOnboardingStorage

        public IObservable<bool> UserSignedUpUsingTheApp { get; }

        public IObservable<bool> IsNewUser { get; }

        public IObservable<bool> StartButtonWasTappedBefore { get; }

        public IObservable<bool> HasTappedTimeEntry { get; }

        public IObservable<bool> HasEditedTimeEntry { get; }

        public IObservable<bool> ProjectOrTagWasAddedBefore { get; }

        public IObservable<bool> StopButtonWasTappedBefore { get; }

        public IObservable<bool> HasSelectedProject { get; }

        public IObservable<bool> NavigatedAwayFromMainViewAfterTappingStopButton { get; }

        public IObservable<bool> HasTimeEntryBeenContinued { get; }

        public IObservable<OnboardingConditionKey> OnboardingConditionMet { get; }

        public bool CalendarPermissionWasAskedBefore()
            => keyValueStorage.GetBool(calendarViewWasOpenedBeforeKey);

        public void SetCalendarPermissionWasAskedBefore()
            => keyValueStorage.SetBool(calendarViewWasOpenedBeforeKey, true);

        public bool ConnectCalendarsPopupWasShown()
            => keyValueStorage.GetBool(connectCalendarsPopupWasShownKey);

        public void SetConnectCalendarsPopupWasShown()
            => keyValueStorage.SetBool(connectCalendarsPopupWasShownKey, true);

        public void SetLastOpened(DateTimeOffset date)
        {
            keyValueStorage.SetDateTimeOffset(lastAccessDateKey, date);
        }

        public void SetFirstOpened(DateTimeOffset dateTime)
        {
            if (GetFirstOpened() == null)
                keyValueStorage.SetString(firstAccessDateKey, dateTime.ToString());
        }

        public void SetUserSignedUp()
        {
            userSignedUpUsingTheAppSubject.OnNext(true);
            keyValueStorage.SetBool(userSignedUpUsingTheAppKey, true);
        }

        public void SetNavigatedAwayFromMainViewAfterStopButton()
        {
            navigatedAwayFromMainViewAfterTappingStopButtonSubject.OnNext(true);
            keyValueStorage.SetBool(navigatedAwayFromMainViewAfterTappingStopButtonKey, true);
        }

        public void SetTimeEntryContinued()
        {
            hasTimeEntryBeenContinuedSubject.OnNext(true);
            keyValueStorage.SetBool(hasTimeEntryBeenContinuedKey, true);
        }

        public void SetIsNewUser(bool isNewUser)
        {
            isNewUserSubject.OnNext(isNewUser);
            keyValueStorage.SetBool(isNewUserKey, isNewUser);
        }

        private void setCompletedOnboarding()
        {
            keyValueStorage.SetBool(completedOnboardingKey, true);
        }

        public bool CompletedOnboarding() => keyValueStorage.GetBool(completedOnboardingKey);

        public DateTimeOffset? GetLastOpened() => keyValueStorage.GetDateTimeOffset(lastAccessDateKey);

        public DateTimeOffset? GetFirstOpened() => keyValueStorage.GetDateTimeOffset(firstAccessDateKey);

        public void StartButtonWasTapped()
        {
            startButtonWasTappedSubject.OnNext(true);
            keyValueStorage.SetBool(startButtonWasTappedBeforeKey, true);
        }

        public void TimeEntryWasTapped()
        {
            hasTappedTimeEntrySubject.OnNext(true);
            keyValueStorage.SetBool(hasTappedTimeEntryKey, true);
        }

        public void ProjectOrTagWasAdded()
        {
            projectOrTagWasAddedSubject.OnNext(true);
            keyValueStorage.SetBool(projectOrTagWasAddedBeforeKey, true);
        }

        public void StopButtonWasTapped()
        {
            stopButtonWasTappedSubject.OnNext(true);
            keyValueStorage.SetBool(stopButtonWasTappedBeforeKey, true);
        }

        public void SelectsProject()
        {
            hasSelectedProjectSubject.OnNext(true);
            keyValueStorage.SetBool(hasSelectedProjectKey, true);
        }

        public void EditedTimeEntry()
        {
            hasEditedTimeEntrySubject.OnNext(true);
            keyValueStorage.SetBool(hasEditedTimeEntryKey, true);
        }

        public void SetDidShowRatingView()
        {
            keyValueStorage.SetInt(ratingViewNumberOfTimesShownKey, NumberOfTimesRatingViewWasShown() + 1);
        }

        public int NumberOfTimesRatingViewWasShown()
        {
            return keyValueStorage.GetInt(ratingViewOutcomeKey, 0);
        }

        public void SetRatingViewOutcome(RatingViewOutcome outcome, DateTimeOffset dateTime)
        {
            keyValueStorage.SetInt(ratingViewOutcomeKey, (int)outcome);
            keyValueStorage.SetDateTimeOffset(ratingViewOutcomeTimeKey, dateTime);
        }

        public RatingViewOutcome? RatingViewOutcome()
        {
            var defaultIntValue = -1;
            var intValue = keyValueStorage.GetInt(ratingViewOutcomeKey, defaultIntValue);
            if (intValue == defaultIntValue)
                return null;
            return (RatingViewOutcome)intValue;
        }

        public DateTimeOffset? RatingViewOutcomeTime()
            => keyValueStorage.GetDateTimeOffset(ratingViewOutcomeTimeKey);

        public bool DidShowSiriClipboardInstruction() => keyValueStorage.GetBool(didShowSiriClipboardInstructionKey);

        public void SetDidShowSiriClipboardInstruction(bool value) => keyValueStorage.SetBool(didShowSiriClipboardInstructionKey, value);

        public void SetOnboardingConditionWasMet(OnboardingConditionKey onboardingConditionKey)
        {
            keyValueStorage.SetBool(onboardingPrefix + onboardingConditionKey, true);
            shownTooltips.Add(onboardingConditionKey);
            if (shownTooltips.Count == tooltipCount)
                setCompletedOnboarding();
            onboardingConditionMetSubject.OnNext(onboardingConditionKey);
        }

        public bool OnboardingConditionWasMetBefore(OnboardingConditionKey onboardingConditionKey)
            => keyValueStorage.GetBool(onboardingPrefix + onboardingConditionKey);

        public bool OnboardingTimeEntryWasCreated()
            => keyValueStorage.GetBool(onboardingTimeEntryWasCreatedKey);

        public void SetOnboardingTimeEntryWasCreated()
            => keyValueStorage.SetBool(onboardingTimeEntryWasCreatedKey, true);

        public bool CalendarTabWasOpened()
            => keyValueStorage.GetBool(calendarTabWasOpenedKey);

        public void SetCalendarTabWasOpened()
            => keyValueStorage.SetBool(calendarTabWasOpenedKey, true);

        public bool ReportsTabWasOpened()
            => keyValueStorage.GetBool(reportsTabWasOpenedKey);

        public void SetReportsTabWasOpened()
            => keyValueStorage.SetBool(reportsTabWasOpenedKey, true);

        public bool IsRunningTheAppFirstTime()
        {
            return !keyValueStorage.GetBool(startButtonWasTappedBeforeKey) &&
                   !keyValueStorage.GetBool(stopButtonWasTappedBeforeKey) &&
                   !keyValueStorage.GetBool(hasEditedTimeEntryKey) &&
                   oldOnboardingDismissedKeys.None(key => keyValueStorage.GetBool(key));
        }

        void IOnboardingStorage.Reset()
        {
            keyValueStorage.SetBool(startButtonWasTappedBeforeKey, false);
            startButtonWasTappedSubject.OnNext(false);

            keyValueStorage.SetBool(hasTappedTimeEntryKey, false);
            hasTappedTimeEntrySubject.OnNext(false);

            keyValueStorage.SetBool(userSignedUpUsingTheAppKey, false);
            userSignedUpUsingTheAppSubject.OnNext(false);

            keyValueStorage.SetBool(hasEditedTimeEntryKey, false);
            hasEditedTimeEntrySubject.OnNext(false);

            keyValueStorage.SetBool(stopButtonWasTappedBeforeKey, false);
            stopButtonWasTappedSubject.OnNext(false);

            keyValueStorage.SetBool(projectOrTagWasAddedBeforeKey, false);
            projectOrTagWasAddedSubject.OnNext(false);

            keyValueStorage.SetBool(navigatedAwayFromMainViewAfterTappingStopButtonKey, false);
            navigatedAwayFromMainViewAfterTappingStopButtonSubject.OnNext(false);

            keyValueStorage.SetBool(hasTimeEntryBeenContinuedKey, false);
            hasTimeEntryBeenContinuedSubject.OnNext(false);

            keyValueStorage.SetBool(calendarIntegrationEnabledKey, false);
            calendarIntegrationEnabledSubject.OnNext(false);

            keyValueStorage.Remove(enabledCalendarsKey);
            enabledCalendarsSubject.OnNext(new List<string>());

            keyValueStorage.Remove(onboardingTimeEntryWasCreatedKey);

            keyValueStorage.RemoveAllWithPrefix(onboardingPrefix);
        }

        #endregion

        #region IUserPreferences

        public IObservable<bool> IsManualModeEnabledObservable { get; }
        public IObservable<bool> AreRunningTimerNotificationsEnabledObservable { get; }
        public IObservable<bool> AreStoppedTimerNotificationsEnabledObservable { get; }
        public IObservable<bool> SwipeActionsEnabled { get; }
        public IObservable<bool> CalendarIntegrationEnabledObservable { get; }
        public IObservable<List<string>> EnabledCalendars { get; }
        public IObservable<bool> CalendarNotificationsEnabled { get; }
        public IObservable<TimeSpan> TimeSpanBeforeCalendarNotifications { get; }

        public bool IsManualModeEnabled
            => keyValueStorage.GetBool(preferManualModeKey);

        public bool AreRunningTimerNotificationsEnabled
            => keyValueStorage.GetBool(runningTimerNotificationsKey);

        public bool AreStoppedTimerNotificationsEnabled
            => keyValueStorage.GetBool(stoppedTimerNotificationsKey);

        public bool AreSwipeActionsEnabled
            => !keyValueStorage.GetBool(swipeActionsDisabledKey);

        public void EnableManualMode()
        {
            keyValueStorage.SetBool(preferManualModeKey, true);
            isManualModeEnabledSubject.OnNext(true);
        }

        public void EnableTimerMode()
        {
            keyValueStorage.SetBool(preferManualModeKey, false);
            isManualModeEnabledSubject.OnNext(false);
        }

        public void SetRunningTimerNotifications(bool state)
        {
            keyValueStorage.SetBool(runningTimerNotificationsKey, state);
            areRunningTimerNotificationsEnabledSubject.OnNext(state);
        }

        public void SetStoppedTimerNotifications(bool state)
        {
            keyValueStorage.SetBool(stoppedTimerNotificationsKey, state);
            areStoppedTimerNotificationsEnabledSubject.OnNext(state);
        }

        void IUserPreferences.Reset()
        {
            EnableTimerMode();
            SetStoppedTimerNotifications(false);
            SetRunningTimerNotifications(false);
            isManualModeEnabledSubject.OnNext(false);
            SetEnabledCalendars();
        }

        bool IUserPreferences.CalendarIntegrationEnabled()
            => keyValueStorage.GetBool(calendarIntegrationEnabledKey);

        public void SetCalendarIntegrationEnabled(bool enabled)
        {
            keyValueStorage.SetBool(calendarIntegrationEnabledKey, enabled);
            calendarIntegrationEnabledSubject.OnNext(enabled);
        }

        public List<string> EnabledCalendarIds()
        {
            var aggregatedIds = keyValueStorage.GetString(enabledCalendarsKey);
            if (string.IsNullOrEmpty(aggregatedIds))
                return new List<string>();

            return aggregatedIds
                .Split(calendarIdSeparator)
                .ToList();
        }

        public void SetEnabledCalendars(params string[] ids)
        {
            if (ids == null)
            {
                keyValueStorage.Remove(enabledCalendarsKey);
            }
            else if (ids.None())
            {
                keyValueStorage.Remove(enabledCalendarsKey);
            }
            else if (ids.Any(id => id.Contains(calendarIdSeparator)))
            {
                throw new ArgumentException($"One of the ids contains a character that's used as a separator ({calendarIdSeparator})");
            }
            else
            {
                var aggregatedIds = ids.Aggregate((accumulator, id) => $"{accumulator}{calendarIdSeparator}{id}");
                keyValueStorage.SetString(enabledCalendarsKey, aggregatedIds);
            }

            enabledCalendarsSubject.OnNext(ids?.ToList() ?? new List<string>());
        }

        public void SetCalendarNotificationsEnabled(bool enabled)
        {
            keyValueStorage.SetBool(calendarNotificationsEnabledKey, enabled);
            calendarNotificationsEnabledSubject.OnNext(enabled);
        }

        public void SetTimeSpanBeforeCalendarNotifications(TimeSpan timeSpan)
        {
            keyValueStorage.SetTimeSpan(timeSpanBeforeCalendarNotificationsKey, timeSpan);
            timeSpanBeforeCalendarNotificationsSubject.OnNext(timeSpan);
        }

        public void SetSwipeActionsEnabled(bool enabled)
        {
            keyValueStorage.SetBool(swipeActionsDisabledKey, !enabled);
            swipeActionsEnabledSubject.OnNext(enabled);
        }

        public bool IsFirstTimeConnectingCalendars()
            => !keyValueStorage.GetBool(isFirstTimeConnectingCalendarsKey);

        public void SetIsFirstTimeConnectingCalendars()
            => keyValueStorage.SetBool(isFirstTimeConnectingCalendarsKey, true);

        #endregion

        #region ILastTimeUsageStorage

        public DateTimeOffset? LastSyncAttempt => keyValueStorage.GetDateTimeOffset(lastSyncAttemptKey);

        public DateTimeOffset? LastSuccessfulSync => keyValueStorage.GetDateTimeOffset(lastSuccessfulSyncKey);

        public DateTimeOffset? LastLogin => keyValueStorage.GetDateTimeOffset(lastLoginKey);

        public DateTimeOffset? LastTimePlaceholdersWereCreated => keyValueStorage.GetDateTimeOffset(lastTimePlaceholdersWereCreated);

        public DateTimeOffset? LastTimeExternalCalendarsSynced => keyValueStorage.GetDateTimeOffset(lastTimeExternalCalendarsSyncedKey);

        public void SetFullSyncAttempt(DateTimeOffset now)
        {
            keyValueStorage.SetDateTimeOffset(lastSyncAttemptKey, now);
        }

        public void SetSuccessfulFullSync(DateTimeOffset now)
        {
            keyValueStorage.SetDateTimeOffset(lastSuccessfulSyncKey, now);
        }

        public void SetLogin(DateTimeOffset now)
        {
            keyValueStorage.SetDateTimeOffset(lastLoginKey, now);
        }

        public void SetPlaceholdersWereCreated(DateTimeOffset now)
        {
            keyValueStorage.SetDateTimeOffset(lastTimePlaceholdersWereCreated, now);
        }

        public void SetLastTimeExternalCalendarsSynced(DateTimeOffset now)
        {
            keyValueStorage.SetDateTimeOffset(lastTimeExternalCalendarsSyncedKey, now);
        }

        #endregion

        private (ISubject<T>, IObservable<T>) prepareSubjectAndObservable<T>(string key, Func<string, T> initialValueSelector)
            => prepareSubjectAndObservable(initialValueSelector(key));

        private (ISubject<T>, IObservable<T>) prepareSubjectAndObservable<T>(T initialValue)
        {
            var subject = new BehaviorSubject<T>(initialValue);
            var observable = subject.AsObservable().DistinctUntilChanged();

            return (subject, observable);
        }

        public bool CheckIfAnnouncementWasShown(string announcementId)
            => keyValueStorage.GetBool(announcementId);

        public void MarkAnnouncementAsShown(string id)
        {
            keyValueStorage.SetBool(id, true);
        }
    }
}
