using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Penezenka_App.OtherClasses;

namespace Penezenka_App.Model
{
    [DataContract]
    public class RecordFilter
    {
        [DataMember]
        public DateTimeOffset StartDateTime { get; set; }
        [DataMember]
        public DateTimeOffset EndDateTime { get; set; }
        [DataMember]
        public bool AllTags { get; set; }
        [DataMember]
        public List<Tag> Tags { get; set; }
        [DataMember]
        public bool AllAccounts { get; set; }
        [DataMember]
        public List<Account> Accounts { get; set; }
        [DataMember]
        public bool IsDefault { get; set; }

        public static RecordFilter Default = new RecordFilter()
        {
            StartDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
            EndDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(-1),
            AllTags = true,
            AllAccounts = true
        };

        public RecordFilter()
        {
            AllTags = true;
            AllAccounts = true;
        }
        public RecordFilter(DateTimeOffset month)
        {
            AllTags = true;
            AllAccounts = true;
            SetMonth(month);
        }
        public string GetRecordsWhereClause()
        {
            System.Text.StringBuilder whereClauseBuilder = new System.Text.StringBuilder(" WHERE Date>=");
            whereClauseBuilder.Append(Misc.DateTimeToInt(StartDateTime));
            whereClauseBuilder.Append(" AND Date<=");
            whereClauseBuilder.Append(Misc.DateTimeToInt(EndDateTime));
            if (AreAccountsFiltered())
            {
                whereClauseBuilder.Append(" AND ");
                whereClauseBuilder.Append(GetAccountsWhereClause());
            }
            return whereClauseBuilder.ToString();
        }
        public string GetTagsWhereClause()
        {
            if (!AllTags && Tags != null)
            {
                if (Tags.Count > 0)
                {
                    string join = string.Join(",", Tags.Select(x => x.ID));
                    return " AND Tag_ID IN (" + join + ")";
                }
                if (Tags.Count == 0)
                {
                    return " AND Tag_ID IS NULL";
                }
            }
            return "";
        }
        public string GetAccountsWhereClause()
        {
            if (AreAccountsFiltered())
            {
                return " Account IN (" + string.Join(",", Accounts.Select(a => a.ID)) + ")";
            }
            else
            {
                return "";
            }
        }

        public void SetMonth(DateTimeOffset month)
        {
            StartDateTime = new DateTime(month.Year, month.Month, 1);
            if(month == DateTimeOffset.MaxValue)
            {
                EndDateTime = new DateTime(month.Year, month.Month, month.Day);
            } else
            {
                EndDateTime = new DateTime(month.Year, month.Month, 1).AddMonths(1).AddDays(-1);
            }
        }

        public bool IsMonth(DateTimeOffset month)
        {
            var filter = new RecordFilter(month);
            return StartDateTime == filter.StartDateTime && EndDateTime == filter.EndDateTime;
        }

        public bool AreAccountsFiltered()
        {
            return !AllAccounts && Accounts != null && Accounts.Count > 0;
        }
    }
}
