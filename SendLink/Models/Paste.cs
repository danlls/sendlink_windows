using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendLink
{
    class Paste
    {
        public long _id { get; set; }
        public string pasteString { get; set; }
        public DateTime receivedTime { get; set; }
        public string deviceName { get; set; }

        public Paste(string pasteString, DateTime receivedTime, string deviceName)
        {
            this.pasteString = pasteString;
            this.receivedTime = receivedTime;
            this.deviceName = deviceName;
        }


        public string PasteString
        {
            get { return pasteString; }
            set { pasteString = value; }
        }

        public DateTime ReceivedTime
        {
            get { return receivedTime; }
            set { receivedTime = value; }
        }

        public string DeviceName
        {
            get { return deviceName; }
            set { deviceName = value; }
        }

        public long ID
        {
            get { return _id; }
            set { _id = value; }
        }
    }
}
