using System;
using System.Collections.Generic;
using System.Linq;

namespace HugeFileReader;

public class Status
{
    private Dictionary<string, StatusInfo> statusInfo = new Dictionary<string, StatusInfo>();
    private SortedList<DateTime, string> statusOrder = new SortedList<DateTime, string>();

    private class StatusInfo
    {
        public string Key { get; set; }
        public string Status { get; set; }
        public DateTime Expires { get; } = DateTime.Now.AddSeconds(7);
    }

    public void Clear()
    {
        statusInfo.Clear();
        statusOrder.Clear();
    }

    public string GetNext()
    {
        if (!statusOrder.Any()) return null;

        KeyValuePair<DateTime, string> oldStatus = statusOrder.First();

        if (DateTime.Now >= oldStatus.Key)
        {
            // remove old status
            statusOrder.Remove(oldStatus.Key);
            statusInfo.Remove(oldStatus.Value);
        }

        return GetString();
    }

    public string Add(string text, string key)
    {
        if (statusInfo.ContainsKey(key))
        {
            // update existing
            StatusInfo si = statusInfo[key];

            // remove old expiry
            statusOrder.Remove(si.Expires);
            si.Status = text;

            // update expiry
            si = new StatusInfo { Status = text, Key = key };
            statusInfo[key] = si;
            statusOrder[si.Expires] = key;
        }
        else
        {
            StatusInfo si = new StatusInfo { Status = text, Key = key };
            statusOrder[si.Expires] = key;
            statusInfo[key] = si;
        }

        return GetString();
    }

    public bool Any()
    {
        return statusInfo.Any();
    }

    public int Count()
    {
        return statusInfo.Count;
    }

    // =-=- private -=-=

    private string GetString()
    {
        List<string> textList = new List<string>();
        foreach (var si in statusInfo.Values)
        {
            textList.Add(si.Status);
        }

        return string.Join(" | ", textList);
    }
}
