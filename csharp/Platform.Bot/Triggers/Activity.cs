using System;
using System.Collections.Generic;

namespace Platform.Bot
{
    internal class Activity
    {
        public string Url { get; set; }

        public List<string> Repositories { get; set; }

        public List<DateTime> Dates = new List<DateTime>();
    }
}
