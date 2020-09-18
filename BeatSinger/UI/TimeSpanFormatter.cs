using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSinger.UI
{
    public class TimeSpanFormatter : ICustomFormatter
    {
        public string Format(string format, object arg, IFormatProvider formatProvider)
        {

            if (arg is TimeSpan timeSpan)
            {
                if (timeSpan == TimeSpan.MinValue)
                    return "--";
                if (!string.IsNullOrEmpty(format))
                    return timeSpan.ToString(format);
                else
                    return timeSpan.ToString();
            }
            else
                return "<ERROR>";
        }
    }
}
