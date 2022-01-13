using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bonsai.Vimba
{
    class SerialNumberConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            using var vimba = new AVT.VmbAPINET.Vimba();
            vimba.Startup();
            try
            {
                var cameraList = vimba.Cameras;
                var values = new List<string>(cameraList.Count);
                for (int i = 0; i < cameraList.Count; i++)
                {
                    var camera = cameraList[i];
                    var serialNumber = camera.SerialNumber;
                    if (!string.IsNullOrEmpty(serialNumber))
                    {
                        values.Add(serialNumber);
                    }
                }

                return new StandardValuesCollection(values);
            }
            finally { vimba.Shutdown(); }
        }
    }
}
