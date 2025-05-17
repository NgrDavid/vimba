using System.Collections.Generic;
using System.ComponentModel;

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
            VimbaApi.Handle.Startup();
            try
            {
                var cameraList = VimbaApi.Handle.Cameras;
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
            finally { VimbaApi.Handle.Shutdown(); }
        }
    }
}
