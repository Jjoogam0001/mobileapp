﻿namespace Toggl.PrimeRadiant
{
    public interface IAccessRestrictionStorage
    {
        void SetClientOutdated();
        void SetApiOutdated();
        void SetUnauthorizedAccess(string apiToken);
        bool IsClientOutdated();
        bool IsApiOutdated();
        bool IsUnauthorized(string apiToken);
    }
}
