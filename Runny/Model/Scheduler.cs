using System.Collections.Generic;
using System.Xml.Serialization;

namespace Runny.Model
{
    [XmlRoot("scheduler")]
    public class Scheduler
    {
        public Scheduler()
        {
            Jobs = new List<Job>();
        }

        [XmlElement("job")]
        public List<Job> Jobs { get; set; }
    }

    public class Job
    {
        public int? NonTwentyFourHourExecuteOnSecondsTimer { get; set; }

        [XmlElement("executeOnTimer")]
        public int ExecuteOnTimer { get; set; }
        [XmlElement("startDate")]
        public string StartDate { get; set; }

        [XmlElement("endDate")]
        public string EndDate { get; set; }

        [XmlElement("time")]
        public string Time { get; set; }

        [XmlElement("day")]
        public string Day { get; set; }

        [XmlElement("loop")]
        public string Loop { get; set; }

        [XmlElement("command")]
        public string Command { get; set; }

        [XmlElement("commandParams")]
        public string CommandParams { get; set; }

        [XmlElement("comment")]
        public string Comment { get; set; }
    }
}