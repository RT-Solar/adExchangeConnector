using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

namespace Org.IdentityConnectors.ActiveDirectory
{
    class SecurityLogReader
    {
        public static long? FetchLatestEventId(String computerName, String domain, String username, String password)
        {
            SecureString securePassword = ToSecureString(password);
            EventLogQuery query = new EventLogQuery("Security", PathType.LogName, "*[System]");
            query.Session = new EventLogSession(computerName, domain, username, securePassword, SessionAuthentication.Default);
            EventLogReader logReader = new EventLogReader(query);
            logReader.Seek(SeekOrigin.End, 0);
            EventRecord latestEvent = logReader.ReadEvent();

            return latestEvent != null ? latestEvent.RecordId : 0;
        }

        public static HashSet<String> FetchGroupsAffectedSubjects(String computerName, String domain, String username, String password, long fromRecordId)
        {
            HashSet<String> result = new HashSet<string>();
            SecureString securePassword = ToSecureString(password);
            // 4728	A member was added to a security-enabled global group
            // 4729	A member was removed from a security-enabled global group
            // 4732	A member was added to a security-enabled local group
            // 4733	A member was removed from a security-enabled local group
            // 4746	A member was added to a security-disabled local group
            // 4747	A member was removed from a security-disabled local group
            // 4751	A member was added to a security-disabled global group
            // 4752	A member was removed from a security-disabled global group
            // 4756	A member was added to a security-enabled universal group
            // 4757	A member was removed from a security-enabled universal group
            // 4761	A member was added to a security-disabled universal group
            // 4762	A member was removed from a security-disabled universal group
            // 4785	A member was added to a basic application group
            // 4786	A member was removed from a basic application group
            EventLogQuery query = new EventLogQuery("Security", PathType.LogName, "*[System[("
                + "(EventID=4728)"
                + " or (EventID=4729)"
                + " or (EventID=4732)"
                + " or (EventID=4733)"
                + " or (EventID=4746)"
                + " or (EventID=4747)"
                + " or (EventID=4751)"
                + " or (EventID=4752)"
                + " or (EventID=4756)"
                + " or (EventID=4757)"
                + " or (EventID=4761)"
                + " or (EventID=4762)"
                + " or (EventID=4785)"
                + " or (EventID=4786)"
                + ")"
                + "and (EventRecordID > '" + fromRecordId + "')]]");

            query.Session = new EventLogSession(computerName, domain, username, securePassword, SessionAuthentication.Default);

            EventLogReader logReader = new EventLogReader(query);
            EventRecord logEvent;
            while ((logEvent = logReader.ReadEvent()) != null)
            {
                if (logEvent.Properties.Count == 0 || logEvent.Properties[0].Value == null)
                {
                    continue;
                }
                result.Add(logEvent.Properties[0].Value.ToString());
            }
            return result;
        }

        private static SecureString ToSecureString(String str)
        {
            SecureString securePassword = new SecureString();
            foreach (char c in str)
            {
                securePassword.AppendChar(c);
            }
            securePassword.MakeReadOnly();
            return securePassword;
        }
    }
}
